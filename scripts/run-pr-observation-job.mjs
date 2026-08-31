import { spawn } from 'node:child_process';
import { mkdirSync, renameSync, writeFileSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

import {
  loadObservationMatrix,
  materializeCommand,
} from './pr-observation-contract.mjs';

const REPOSITORY_ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const DEFAULT_MATRIX_PATH = resolve(REPOSITORY_ROOT, 'pr-observation-matrix.json');

let options;
try {
  options = parseArguments(process.argv.slice(2));
} catch (error) {
  console.error(error.message);
  printUsage();
  process.exit(2);
}

const matrix = loadObservationMatrix(options.matrixPath, REPOSITORY_ROOT);

if (options.list) {
  for (const job of matrix.jobs) {
    console.log(`${job.id}\t${job.title}`);
  }
  process.exit(0);
}

const job = matrix.jobs.find(({ id }) => id === options.jobId);
if (!job) {
  console.error(`Unknown PR observation job: ${options.jobId}`);
  printUsage();
  process.exit(2);
}

const jobResultsDirectory = options.resultsDirectory ?? defaultResultsDirectory(job.id);
const commands = job.commands.map((command) =>
  materializeCommand(command, REPOSITORY_ROOT, jobResultsDirectory),
);

if (options.dryRun) {
  console.log(
    JSON.stringify(
      {
        matrixId: matrix.id,
        scheduling: matrix.scheduling,
        job: {
          id: job.id,
          title: job.title,
          policy: job.policy,
          inputContract: job.inputContract ?? null,
          commands,
        },
      },
      null,
      2,
    ),
  );
  process.exit(0);
}

mkdirSync(jobResultsDirectory, { recursive: true });
const startedAt = new Date();
const startedAtMs = startedAt.getTime();
const deadlineMs = startedAtMs + job.policy.timeoutSeconds * 1_000;
const commandResults = [];
let finalStatus = 'passed';

for (let index = 0; index < commands.length; index += 1) {
  const command = commands[index];
  const remainingMs = deadlineMs - Date.now();
  if (remainingMs <= 0) {
    finalStatus = 'timed-out';
    appendNotRunCommands(commands, index, commandResults);
    break;
  }

  console.log(`[pr-observation] ${job.id}/${command.id} started phase=${command.phase}`);
  const result = await runCommand(command, remainingMs);
  commandResults.push(result);
  console.log(
    `[pr-observation] ${job.id}/${command.id} ${result.status} durationMs=${result.durationMs}`,
  );

  if (result.status !== 'passed') {
    finalStatus = result.status;
    appendNotRunCommands(commands, index + 1, commandResults);
    break;
  }
}

const completedAt = new Date();
const result = {
  schemaVersion: 1,
  matrixId: matrix.id,
  jobId: job.id,
  title: job.title,
  mode: job.policy.blocking ? 'blocking' : 'observation',
  status: finalStatus,
  firstAttemptStatus: finalStatus,
  startedAt: startedAt.toISOString(),
  completedAt: completedAt.toISOString(),
  durationMs: completedAt.getTime() - startedAtMs,
  durationScope: matrix.durationScope,
  durationComponents: matrix.durationComponents,
  queueTimeIncluded: false,
  timeoutSeconds: job.policy.timeoutSeconds,
  maxAttempts: 1,
  attemptsExecuted: 1,
  inputContract: job.inputContract ?? null,
  commands: commandResults,
};
writeJsonAtomically(resolve(jobResultsDirectory, 'job-result.json'), result);

const resultPath = resolve(jobResultsDirectory, 'job-result.json');
console.log([
  `[pr-observation] job=${job.id}`,
  `status=${finalStatus}`,
  `blocking=${job.policy.blocking}`,
  `durationMs=${result.durationMs}`,
  `result=${resultPath}`,
].join(' '));

if (finalStatus !== 'passed' && !job.policy.blocking) {
  console.log('[pr-observation] first-attempt failure retained as non-blocking observation evidence.');
}

process.exitCode = finalStatus === 'passed' || !job.policy.blocking ? 0 : 1;

function parseArguments(arguments_) {
  const parsed = {
    dryRun: false,
    jobId: '',
    list: false,
    matrixPath: DEFAULT_MATRIX_PATH,
    resultsDirectory: null,
  };

  for (let index = 0; index < arguments_.length; index += 1) {
    const argument = arguments_[index];
    if (argument === '--job') {
      parsed.jobId = requireValue(arguments_, ++index, '--job');
    } else if (argument === '--results-dir') {
      parsed.resultsDirectory = resolve(
        process.cwd(),
        requireValue(arguments_, ++index, '--results-dir'),
      );
    } else if (argument === '--matrix') {
      parsed.matrixPath = resolve(process.cwd(), requireValue(arguments_, ++index, '--matrix'));
    } else if (argument === '--dry-run') {
      parsed.dryRun = true;
    } else if (argument === '--list') {
      parsed.list = true;
    } else if (argument === '--help' || argument === '-h') {
      printUsage();
      process.exit(0);
    } else {
      throw new Error(`Unknown argument: ${argument}`);
    }
  }

  if (!parsed.list && !parsed.jobId) {
    throw new Error('--job is required unless --list is used.');
  }
  return parsed;
}

function requireValue(arguments_, index, option) {
  const value = arguments_[index];
  if (!value || value.startsWith('--')) {
    throw new Error(`${option} requires a value.`);
  }
  return value;
}

function printUsage() {
  console.log(`Usage:
  node scripts/run-pr-observation-job.mjs --list
  node scripts/run-pr-observation-job.mjs --job JOB_ID [--results-dir PATH] [--dry-run]
  node scripts/run-pr-observation-job.mjs --matrix PATH --job JOB_ID [--results-dir PATH]`);
}

function defaultResultsDirectory(jobId) {
  const runId = `${new Date().toISOString().replaceAll(/[-:.TZ]/g, '')}-${process.pid}`;
  return resolve(REPOSITORY_ROOT, '.pr-observation', runId, jobId);
}

function appendNotRunCommands(commands_, startIndex, results) {
  for (let index = startIndex; index < commands_.length; index += 1) {
    const command = commands_[index];
    results.push({
      id: command.id,
      phase: command.phase,
      status: 'not-run',
      durationMs: 0,
      exitCode: null,
      signal: null,
    });
  }
}

function runCommand(command, timeoutMs) {
  return new Promise((resolvePromise) => {
    const commandStartedAt = Date.now();
    const child = spawn(command.executable, command.arguments, {
      cwd: command.cwd,
      detached: process.platform !== 'win32',
      env: process.env,
      stdio: 'inherit',
    });
    let timedOut = false;
    let spawnError = null;
    let forceKillTimer;

    const timeout = setTimeout(() => {
      timedOut = true;
      terminateProcessTree(child, 'SIGTERM');
      forceKillTimer = setTimeout(() => terminateProcessTree(child, 'SIGKILL'), 5_000);
      forceKillTimer.unref();
    }, timeoutMs);
    timeout.unref();

    child.once('error', (error) => {
      spawnError = error;
    });
    child.once('close', (exitCode, signal) => {
      clearTimeout(timeout);
      clearTimeout(forceKillTimer);
      resolvePromise({
        id: command.id,
        phase: command.phase,
        status: timedOut ? 'timed-out' : exitCode === 0 ? 'passed' : 'failed',
        durationMs: Date.now() - commandStartedAt,
        exitCode,
        signal,
        ...(spawnError ? { error: spawnError.message } : {}),
      });
    });
  });
}

function terminateProcessTree(child, signal) {
  if (!child.pid) {
    return;
  }
  try {
    if (process.platform === 'win32') {
      child.kill(signal);
    } else {
      process.kill(-child.pid, signal);
    }
  } catch (error) {
    if (error.code !== 'ESRCH') {
      console.error(`[pr-observation] could not send ${signal}: ${error.message}`);
    }
  }
}

function writeJsonAtomically(path, value) {
  const temporaryPath = `${path}.tmp-${process.pid}`;
  writeFileSync(temporaryPath, `${JSON.stringify(value, null, 2)}\n`, {
    encoding: 'utf8',
    mode: 0o600,
  });
  renameSync(temporaryPath, path);
}
