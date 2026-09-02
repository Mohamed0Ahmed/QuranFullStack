import { spawn } from 'node:child_process';
import { isAbsolute, relative, resolve, sep } from 'node:path';

export function createCancellationController() {
  let signal = null;
  let active = null;
  return {
    cancel(nextSignal) {
      signal ??= nextSignal;
      active?.terminate();
    },
    get signal() { return signal; },
    setActive(child) { active = child; if (signal) child.terminate(); },
    clearActive(child) { if (active === child) active = null; },
  };
}

export function classifyReleaseCandidate({ primary, cancellation }) {
  if (cancellation?.signal || primary.some((result) => result.status === 'cancelled')) return 'cancelled';
  if (primary.some((result) => result.status === 'timed-out' || result.firstAttemptStatus === 'timed-out')) return 'timed-out';
  if (!primary.every((result) => result.status === 'passed')) return 'failed';
  return 'passed';
}

export function validateResultsLocation({ repositoryRoot, resultsDirectory }) {
  const pathFromRepository = relative(resolve(repositoryRoot), resolve(resultsDirectory));
  const insideRepository = pathFromRepository === ''
    || (pathFromRepository !== '..' && !pathFromRepository.startsWith(`..${sep}`) && !isAbsolute(pathFromRepository));
  return insideRepository
    ? { status: 'failed', reason: 'results-directory-inside-candidate' }
    : { status: 'passed' };
}

export function isolatedTemporaryEnvironment(executionHome) {
  const temporaryDirectory = resolve(executionHome);
  return {
    TMPDIR: temporaryDirectory,
    TMP: temporaryDirectory,
    TEMP: temporaryDirectory,
  };
}

export function validateCheckoutState({ candidate, head, porcelain }) {
  if (!/^[a-f0-9]{40}$/.test(candidate) || head !== candidate) return { status: 'failed', reason: 'candidate-head-changed' };
  return porcelain === '' ? { status: 'passed' } : { status: 'failed', reason: 'candidate-worktree-dirty' };
}

export function createReleaseCandidateFinalizer(receipt, cancellation, write, setExitCode) {
  const cancel = () => {
    receipt.status = 'cancelled';
    receipt.firstAttemptStatus = 'cancelled';
    write(receipt);
    setExitCode(1);
  };
  return {
    cancel,
    async finalize() {
      await eventLoopFence();
      if (cancellation.signal) cancel();
      else {
        write(receipt);
        setExitCode(receipt.status === 'passed' ? 0 : 1);
      }
      await eventLoopFence();
      if (cancellation.signal) cancel();
      return receipt.status === 'passed' ? 0 : 1;
    },
  };
}

export async function finalizeReleaseCandidateReceipt(receipt, cancellation, write) {
  let exitCode;
  const finalizer = createReleaseCandidateFinalizer(receipt, cancellation, write, (code) => { exitCode = code; });
  await finalizer.finalize();
  return exitCode;
}

function eventLoopFence() {
  return new Promise((resolvePromise) => setImmediate(resolvePromise));
}

export async function runReleaseCandidateCommands({
  commands,
  cleanupDatabaseRun,
  deadlineMs,
  executionHome,
  runCommand,
  validateEvidence,
  cancellation,
}) {
  const primary = [];
  let databaseCleanup = { status: 'not-required' };
  for (const command of commands) {
    if (cancellation.signal) {
      primary.push(notStarted(command, 'cancelled'));
      continue;
    }
    if (command.databaseOwning === true && databaseCleanup.status === 'failed') {
      primary.push(notStarted(command, 'blocked-by-database-cleanup'));
      continue;
    }
    if ((command.dependsOn ?? []).some((id) => primary.find((result) => result.id === id)?.status !== 'passed')) {
      primary.push(notStarted(command, 'failed-prerequisite'));
      continue;
    }
    const remainingMs = deadlineMs - Date.now();
    if (remainingMs <= 0) {
      primary.push(notStarted(command, 'lane-timeout'));
      continue;
    }
    let result;
    try {
      result = await runCommand(command, Math.min(remainingMs, command.timeoutSeconds * 1_000), executionHome, cancellation);
      if (result.status === 'passed') {
        try {
          result.evidence = validateEvidence(command);
          if (result.evidence.status !== 'passed') result.status = result.firstAttemptStatus = 'failed';
        } catch {
          result.status = result.firstAttemptStatus = 'failed';
          result.evidence = { status: 'failed', checkId: 'evidence-validation-threw' };
        }
      }
    } catch {
      result = { id: command.id, status: 'failed', firstAttemptStatus: 'failed', attemptsExecuted: 1, maxAttempts: 1, executed: true, durationMs: 0, exitCode: null, signal: null, evidence: { status: 'failed', checkId: 'command-execution-threw' } };
    } finally {
      if (command.databaseOwning === true) {
        let cleanup;
        try {
          cleanup = await cleanupDatabaseRun(command, executionHome);
          if (!cleanup || !['passed', 'failed'].includes(cleanup.status)) {
            cleanup = { status: 'failed', checkId: 'database-cleanup-invalid' };
          }
        } catch {
          cleanup = { status: 'failed', checkId: 'database-cleanup-threw' };
        }
        databaseCleanup = cleanup;
        if (result) {
          result.databaseCleanup = cleanup;
          if (cleanup.status !== 'passed') {
            result.status = 'failed';
            if (!['cancelled', 'timed-out'].includes(result.firstAttemptStatus)) result.firstAttemptStatus = 'failed';
          }
        }
      }
    }
    primary.push(result);
  }
  return { databaseCleanup, primary };
}

export function runDetachedCommand(command, timeoutMs, environment, cancellation, { graceMs = 5_000 } = {}) {
  return new Promise((resolvePromise) => {
    const startedAt = Date.now();
    const child = spawn(command.executable, command.arguments, { cwd: command.cwd, detached: process.platform !== 'win32', env: environment, stdio: 'ignore' });
    let timedOut = false;
    let settled = false;
    let closed;
    let killTimer;
    let terminate;
    const handle = { terminate: () => terminate() };
    const finish = (exitCode, signal) => {
      if (settled) return;
      settled = true;
      clearTimeout(timeout);
      if (killTimer) clearTimeout(killTimer);
      cancellation.clearActive(handle);
      const status = cancellation.signal ? 'cancelled' : timedOut ? 'timed-out' : exitCode === 0 ? 'passed' : 'failed';
      resolvePromise({ id: command.id, status, firstAttemptStatus: status, attemptsExecuted: 1, maxAttempts: 1, executed: true, durationMs: Date.now() - startedAt, exitCode, signal });
    };
    terminate = () => {
      if (killTimer) return;
      terminateGroup(child, 'SIGTERM');
      killTimer = setTimeout(() => { terminateGroup(child, 'SIGKILL'); finish(closed?.exitCode ?? null, closed?.signal ?? 'SIGKILL'); }, graceMs);
    };
    const timeout = setTimeout(() => { timedOut = true; terminate(); }, timeoutMs);
    timeout.unref();
    cancellation.setActive(handle);
    child.once('close', (exitCode, signal) => { closed = { exitCode, signal }; if (!killTimer) finish(exitCode, signal); });
    child.once('error', () => finish(null, null));
  });
}

function notStarted(command, reason) {
  const status = reason === 'cancelled' ? 'cancelled' : reason === 'lane-timeout' ? 'timed-out' : 'not-run';
  return { id: command.id, status, firstAttemptStatus: status, attemptsExecuted: 0, maxAttempts: 1, durationMs: 0, exitCode: null, signal: null, reason };
}

function terminateGroup(child, signal) {
  if (!child.pid) return;
  try { if (process.platform === 'win32') child.kill(signal); else process.kill(-child.pid, signal); } catch (error) { if (error.code !== 'ESRCH') throw error; }
}
