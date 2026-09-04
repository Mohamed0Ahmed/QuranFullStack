import { execFileSync } from 'node:child_process';
import { randomBytes } from 'node:crypto';
import { chmodSync, existsSync, mkdirSync, mkdtempSync, readdirSync, renameSync, rmSync, statSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

import {
  loadReleaseCandidateManifest,
  materializeReleaseCandidateCommand,
  validatePrimaryEvidence,
} from './release-candidate-contract.mjs';
import { classifyReleaseCandidate, createCancellationController, createReleaseCandidateFinalizer, isolatedTemporaryEnvironment, runDetachedCommand, runReleaseCandidateCommands, validateCheckoutState, validateResultsLocation } from './release-candidate-orchestration.mjs';

const REPOSITORY_ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const MANIFEST_PATH = resolve(REPOSITORY_ROOT, 'release-candidate-lane.json');
const cancellation = createCancellationController();
let activeFinalizer = null;
for (const signal of ['SIGINT', 'SIGTERM']) {
  process.once(signal, () => {
    cancellation.cancel(signal);
    activeFinalizer?.cancel();
  });
}

let options;
try {
  options = parseArguments(process.argv.slice(2));
} catch (error) {
  console.error(error.message);
  printUsage();
  process.exit(2);
}

const manifest = loadReleaseCandidateManifest(MANIFEST_PATH, REPOSITORY_ROOT);
const candidate = resolveCandidate(options.candidate);
const commands = manifest.commands.map((command) => ({
  ...materializeReleaseCandidateCommand(command, REPOSITORY_ROOT, options.resultsDirectory),
  ...(command.databaseOwning === true ? { runId: randomBytes(16).toString('hex') } : {}),
}));

if (options.dryRun) {
  console.log(JSON.stringify({
    id: manifest.id,
    timeoutSeconds: manifest.timeoutSeconds,
    executionScope: manifest.executionScope,
    commands: commands.map(({ cwd: _cwd, arguments: _arguments, ...command }) => command),
  }, null, 2));
  process.exit(0);
}

if (existsSync(options.resultsDirectory)
  && (!statSync(options.resultsDirectory).isDirectory() || readdirSync(options.resultsDirectory).length > 0)) {
  console.error('[release-candidate] results directory must be new or empty.');
  process.exit(2);
}
mkdirSync(options.resultsDirectory, { recursive: true, mode: 0o700 });
const executionHome = mkdtempSync(resolve(tmpdir(), 'qdb-release-candidate-home-'));
chmodSync(executionHome, 0o700);
const deadlineMs = Date.now() + manifest.timeoutSeconds * 1_000;
let primary;
let databaseCleanup;
let candidateCheck = checkoutState(candidate);
if (candidateCheck.status === 'passed') {
  try {
    ({ primary, databaseCleanup } = await runReleaseCandidateCommands({
      commands,
      deadlineMs,
      executionHome,
      cancellation,
      runCommand: (command, timeoutMs, home, controller) => runDetachedCommand(command, timeoutMs, commandEnvironment(command, home, command.runId), controller),
      validateEvidence: (command) => validatePrimaryEvidence(command.id, commandResultDirectory(command), REPOSITORY_ROOT),
      cleanupDatabaseRun: (command, home) => cleanupDatabaseRun(command.runId, home),
    }));
  } finally {
    rmSync(executionHome, { force: true, recursive: true });
  }
} else {
  rmSync(executionHome, { force: true, recursive: true });
}

if (cancellation.signal) {
  primary ??= [];
  primary.push({ id: 'cancellation-finalization', status: 'cancelled', firstAttemptStatus: 'cancelled', attemptsExecuted: 0, maxAttempts: 1, durationMs: 0, exitCode: null, signal: cancellation.signal });
  databaseCleanup ??= { status: 'not-required' };
} else if (candidateCheck.status !== 'passed') {
  primary = [{ id: 'candidate-check-before', status: 'failed', firstAttemptStatus: 'failed', attemptsExecuted: 1, maxAttempts: 1, durationMs: 0, exitCode: null, signal: null, reason: candidateCheck.reason }];
  databaseCleanup = { status: 'not-required' };
} else {
  candidateCheck = checkoutState(candidate);
  if (candidateCheck.status !== 'passed') primary.push({ id: 'candidate-check-after', status: 'failed', firstAttemptStatus: 'failed', attemptsExecuted: 1, maxAttempts: 1, durationMs: 0, exitCode: null, signal: null, reason: candidateCheck.reason });
}

const status = classifyReleaseCandidate({ primary, cancellation });
const receipt = {
  schemaVersion: 2,
  laneId: manifest.id,
  executionScope: manifest.executionScope,
  status,
  candidate,
  maxAttempts: 1,
  attemptsExecuted: 1,
  firstAttemptStatus: status,
  primary,
  databaseCleanup,
  candidateCheck,
};
activeFinalizer = createReleaseCandidateFinalizer(
  receipt,
  cancellation,
  (value) => writeJsonAtomically(resolve(options.resultsDirectory, 'release-candidate-result.json'), value),
  (code) => { process.exitCode = code; },
);
await activeFinalizer.finalize();
console.log(`[release-candidate] status=${receipt.status}`);

function parseArguments(arguments_) {
  const parsed = { candidate: '', dryRun: false, resultsDirectory: '' };
  for (let index = 0; index < arguments_.length; index += 1) {
    const argument = arguments_[index];
    if (argument === '--candidate') parsed.candidate = requireValue(arguments_, ++index, argument);
    else if (argument === '--results-dir') parsed.resultsDirectory = resolve(process.cwd(), requireValue(arguments_, ++index, argument));
    else if (argument === '--dry-run') parsed.dryRun = true;
    else if (argument === '--help' || argument === '-h') {
      printUsage();
      process.exit(0);
    } else throw new Error(`Unknown argument: ${argument}`);
  }
  if (!parsed.resultsDirectory) throw new Error('--results-dir is required.');
  if (validateResultsLocation({ repositoryRoot: REPOSITORY_ROOT, resultsDirectory: parsed.resultsDirectory }).status !== 'passed') {
    throw new Error('--results-dir must be outside the repository so candidate verification remains immutable.');
  }
  return parsed;
}

function requireValue(arguments_, index, option) {
  const value = arguments_[index];
  if (!value || value.startsWith('--')) throw new Error(`${option} requires a value.`);
  return value;
}

function printUsage() {
  console.log('Usage: node scripts/run-release-candidate-lane.mjs --results-dir PATH [--candidate SHA] [--dry-run]');
}

function commandEnvironment(command, executionHome, runId) {
  const environment = {
    ...isolatedTemporaryEnvironment(executionHome),
    HOME: executionHome,
    PATH: process.env.PATH ?? '',
    DOTNET_CLI_HOME: resolve(executionHome, 'dotnet'),
    NUGET_HTTP_CACHE_PATH: resolve(executionHome, 'nuget-http-cache'),
    NUGET_PACKAGES: resolve(executionHome, 'nuget-packages'),
    XDG_CACHE_HOME: resolve(executionHome, 'cache'),
    XDG_CONFIG_HOME: resolve(executionHome, 'config'),
    XDG_DATA_HOME: resolve(executionHome, 'data'),
    npm_config_cache: resolve(executionHome, 'npm-cache'),
    npm_config_userconfig: '/dev/null',
  };
  if (typeof process.env.LANG === 'string') environment.LANG = process.env.LANG;
  if (typeof process.env.LC_ALL === 'string') environment.LC_ALL = process.env.LC_ALL;
  if (typeof process.env.ConnectionStrings__QuranDashboardTest === 'string') {
    environment.ConnectionStrings__QuranDashboardTest = process.env.ConnectionStrings__QuranDashboardTest;
  }
  if (runId) environment.QURAN_DASHBOARD_TEST_RUN_ID = runId;
  return environment;
}

function commandResultDirectory(command) {
  const index = command.arguments.indexOf('--results-dir');
  return index === -1 ? '' : command.arguments[index + 1];
}

async function cleanupDatabaseRun(runId, executionHome) {
  if (!runId) return { status: 'failed', checkId: 'database-cleanup-run-id-missing' };
  const cleanup = await runDetachedCommand({
    id: 'database-cleanup', executable: 'Backend/scripts/cleanup-test-runtime', arguments: ['--run-id', runId, '--require-proof'], cwd: REPOSITORY_ROOT,
  }, 60_000, commandEnvironment({ id: 'database-cleanup' }, executionHome), createCancellationController());
  return cleanup.status === 'passed'
    ? { status: 'passed', checkId: 'project-owned-postgresql-cleanup-proved' }
    : { status: 'failed', checkId: 'project-owned-postgresql-cleanup-unverified' };
}

function resolveCandidate(value) {
  let current;
  try { current = execFileSync('git', ['rev-parse', 'HEAD'], { cwd: REPOSITORY_ROOT, encoding: 'utf8', stdio: ['ignore', 'pipe', 'ignore'] }).trim(); } catch { throw new Error('Current Git candidate is unavailable.'); }
  if (!/^[a-f0-9]{40}$/.test(current) || (value && value !== current)) throw new Error('Candidate must be the immutable current Git commit.');
  return current;
}

function checkoutState(expected) {
  try {
    const head = execFileSync('git', ['rev-parse', 'HEAD'], { cwd: REPOSITORY_ROOT, encoding: 'utf8', stdio: ['ignore', 'pipe', 'ignore'] }).trim();
    const status = execFileSync('git', ['status', '--porcelain=v1', '--untracked-files=all'], { cwd: REPOSITORY_ROOT, encoding: 'utf8', stdio: ['ignore', 'pipe', 'ignore'] }).trim();
    return validateCheckoutState({ candidate: expected, head, porcelain: status });
  } catch { return { status: 'failed', reason: 'candidate-check-unavailable' }; }
}

function writeJsonAtomically(path, value) {
  const temporaryPath = `${path}.tmp-${process.pid}`;
  writeFileSync(temporaryPath, `${JSON.stringify(value, null, 2)}\n`, { encoding: 'utf8', mode: 0o600 });
  renameSync(temporaryPath, path);
}
