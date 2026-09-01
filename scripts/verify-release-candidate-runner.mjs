import assert from 'node:assert/strict';
import { existsSync, mkdtempSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { resolve } from 'node:path';

import { classifyReleaseCandidate, createCancellationController, runDetachedCommand, runReleaseCandidateCommands } from './release-candidate-orchestration.mjs';

const root = mkdtempSync(resolve(tmpdir(), 'qdb-release-runner-'));
try {
  await verifyCancellation('SIGINT');
  await verifyCancellation('SIGTERM');
  await verifyTimeout();
  await verifyCleanupBlocksDatabaseWork();
  await verifyCleanupFailureMatrix();
  await verifyThrowCleanup();
  await verifyCleanupThrows();
  await verifyInvalidCleanupResult();
} finally {
  rmSync(root, { force: true, recursive: true });
}

async function verifyThrowCleanup() {
  for (const [id, runCommand, validateEvidence] of [
    ['command-throws', async () => { throw new Error('synthetic'); }, () => ({ status: 'passed' })],
    ['evidence-throws', async () => ({ id: 'evidence-throws', status: 'passed', firstAttemptStatus: 'passed', attemptsExecuted: 1, maxAttempts: 1 }), () => { throw new Error('synthetic'); }],
  ]) {
    let cleanups = 0;
    const result = await runReleaseCandidateCommands({
      commands: [{ id, databaseOwning: true, timeoutSeconds: 1 }, { id: 'later-db', databaseOwning: true, timeoutSeconds: 1 }], deadlineMs: Date.now() + 1_000, executionHome: root, cancellation: createCancellationController(),
      runCommand, validateEvidence, cleanupDatabaseRun: async () => { cleanups += 1; return { status: 'failed' }; },
    });
    assert.equal(cleanups, 1);
    assert.equal(result.primary[0].status, 'failed');
    assert.equal(result.primary[1].reason, 'blocked-by-database-cleanup');
  }
}

async function verifyCleanupThrows() {
  const result = await runReleaseCandidateCommands({
    commands: [{ id: 'cleanup-throws', databaseOwning: true, timeoutSeconds: 1 }, { id: 'later-db', databaseOwning: true, timeoutSeconds: 1 }],
    deadlineMs: Date.now() + 1_000,
    executionHome: root,
    cancellation: createCancellationController(),
    runCommand: async () => ({ id: 'cleanup-throws', status: 'passed', firstAttemptStatus: 'passed', attemptsExecuted: 1, maxAttempts: 1 }),
    validateEvidence: () => ({ status: 'passed' }),
    cleanupDatabaseRun: async () => { throw new Error('synthetic'); },
  });
  assert.deepEqual(result.primary[0].databaseCleanup, { status: 'failed', checkId: 'database-cleanup-threw' });
  assert.equal(result.primary[0].status, 'failed');
  assert.equal(result.primary[1].reason, 'blocked-by-database-cleanup');
}

async function verifyInvalidCleanupResult() {
  const result = await runReleaseCandidateCommands({
    commands: [{ id: 'cleanup-invalid', databaseOwning: true, timeoutSeconds: 1 }],
    deadlineMs: Date.now() + 1_000,
    executionHome: root,
    cancellation: createCancellationController(),
    runCommand: async () => ({ id: 'cleanup-invalid', status: 'passed', firstAttemptStatus: 'passed', attemptsExecuted: 1, maxAttempts: 1 }),
    validateEvidence: () => ({ status: 'passed' }),
    cleanupDatabaseRun: async () => undefined,
  });
  assert.deepEqual(result.primary[0].databaseCleanup, { status: 'failed', checkId: 'database-cleanup-invalid' });
  assert.equal(result.primary[0].status, 'failed');
}

async function verifyCleanupFailureMatrix() {
  for (const [id, childStatus, evidenceStatus, firstAttemptStatus] of [
    ['pass-cleanup-fail', 'passed', 'passed', 'failed'],
    ['ordinary-fail-cleanup-fail', 'failed', 'passed', 'failed'],
    ['evidence-fail-cleanup-fail', 'passed', 'failed', 'failed'],
  ]) {
    const result = await runReleaseCandidateCommands({
      commands: [{ id, databaseOwning: true, timeoutSeconds: 1 }], deadlineMs: Date.now() + 1_000, executionHome: root, cancellation: createCancellationController(),
      runCommand: async () => ({ id, status: childStatus, firstAttemptStatus: childStatus, attemptsExecuted: 1, maxAttempts: 1 }),
      validateEvidence: () => ({ status: evidenceStatus }), cleanupDatabaseRun: async () => ({ status: 'failed' }),
    });
    assert.equal(result.primary[0].status, 'failed');
    assert.equal(result.primary[0].firstAttemptStatus, firstAttemptStatus);
  }
  const cleanupPassed = await runReleaseCandidateCommands({
    commands: [{ id: 'cleanup-pass', databaseOwning: true, timeoutSeconds: 1 }], deadlineMs: Date.now() + 1_000, executionHome: root, cancellation: createCancellationController(),
    runCommand: async () => ({ id: 'cleanup-pass', status: 'passed', firstAttemptStatus: 'passed', attemptsExecuted: 1, maxAttempts: 1 }),
    validateEvidence: () => ({ status: 'passed' }), cleanupDatabaseRun: async () => ({ status: 'passed' }),
  });
  assert.equal(cleanupPassed.primary[0].status, 'passed');
}
console.log('Release candidate synthetic runner verifier passed.');

async function verifyCancellation(signal) {
  const home = mkdtempSync(resolve(root, `home-${signal}-`));
  const cancellation = createCancellationController();
  const commands = [ignoring(`active-${signal}`, true), passed(`later-${signal}`)];
  const run = runReleaseCandidateCommands({
    commands,
    deadlineMs: Date.now() + 1_000,
    executionHome: home,
    cancellation,
    runCommand: (command, timeout, _home, controller) => runDetachedCommand(command, timeout, process.env, controller, { graceMs: 10 }),
    validateEvidence: () => ({ status: 'passed' }),
    cleanupDatabaseRun: async () => ({ status: 'passed' }),
  });
  setTimeout(() => cancellation.cancel(signal), 20);
  const result = await run;
  assert.equal(cancellation.signal, signal);
  assert.equal(result.primary[0].status, 'cancelled');
  assert.equal(result.primary[1].status, 'cancelled');
  assert.equal(result.primary[1].attemptsExecuted, 0);
  rmSync(home, { force: true, recursive: true });
  assert.equal(existsSync(home), false);
}

async function verifyTimeout() {
  const home = mkdtempSync(resolve(root, 'home-timeout-'));
  const cancellation = createCancellationController();
  const result = await runDetachedCommand(ignoring('timeout', false), 20, process.env, cancellation, { graceMs: 10 });
  assert.equal(result.status, 'timed-out');
  assert.equal(result.firstAttemptStatus, 'timed-out');
  rmSync(home, { force: true, recursive: true });
  assert.equal(existsSync(home), false);
}

async function verifyCleanupBlocksDatabaseWork() {
  const home = mkdtempSync(resolve(root, 'home-cleanup-'));
  const cancellation = createCancellationController();
  const commands = [ignoring('database-timeout', true), passed('blocked-database', true), passed('safe-bookkeeping')];
  const result = await runReleaseCandidateCommands({
    commands,
    deadlineMs: Date.now() + 1_000,
    executionHome: home,
    cancellation,
    runCommand: (command, timeout, _home, controller) => runDetachedCommand(command, command.id === 'database-timeout' ? 20 : timeout, process.env, controller, { graceMs: 10 }),
    validateEvidence: () => ({ status: 'passed' }),
    cleanupDatabaseRun: async () => ({ status: 'failed' }),
  });
  assert.equal(result.primary[0].status, 'failed');
  assert.equal(result.primary[0].firstAttemptStatus, 'timed-out');
  assert.equal(classifyReleaseCandidate({ primary: result.primary, externalEvidence: { status: 'passed' }, cancellation }), 'timed-out');
  assert.equal(result.primary[1].reason, 'blocked-by-database-cleanup');
  assert.equal(result.primary[1].attemptsExecuted, 0);
  assert.equal(result.primary[2].status, 'passed');
  rmSync(home, { force: true, recursive: true });
}

function ignoring(id, databaseOwning) {
  return { id, cwd: process.cwd(), executable: process.execPath, arguments: ['-e', "process.on('SIGTERM',()=>{});setInterval(()=>{},1000)"], timeoutSeconds: 1, databaseOwning };
}

function passed(id, databaseOwning = false) {
  return { id, cwd: process.cwd(), executable: process.execPath, arguments: ['-e', 'process.exit(0)'], timeoutSeconds: 1, databaseOwning };
}
