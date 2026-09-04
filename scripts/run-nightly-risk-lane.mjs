import { spawn } from 'node:child_process';
import {
  chmodSync,
  existsSync,
  lstatSync,
  mkdirSync,
  mkdtempSync,
  readFileSync,
  readdirSync,
  renameSync,
  rmSync,
  statSync,
  writeFileSync,
} from 'node:fs';
import { tmpdir } from 'node:os';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

import { validateControlledPlaywrightRun } from '../Frontend/quran-dashboard-ui/scripts/validate-controlled-playwright-report.mjs';

import {
  loadNightlyRiskManifest,
  materializeNightlyCommand,
} from './nightly-risk-contract.mjs';

const REPOSITORY_ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const DEFAULT_MANIFEST_PATH = resolve(REPOSITORY_ROOT, 'nightly-risk-lane.json');
const CONTRACT_TEST_MANIFESTS = new Map([
  ['diagnostic-retry', resolve(REPOSITORY_ROOT, 'scripts/test-fixtures/nightly-diagnostic-retry.json')],
  ['skipped-mobile-evidence', resolve(REPOSITORY_ROOT, 'scripts/test-fixtures/nightly-skipped-mobile.json')],
  ['provisioning-blocks-browser', resolve(REPOSITORY_ROOT, 'scripts/test-fixtures/nightly-provisioning-blocks-browser.json')],
  ['browser-cleanup-lifecycle', resolve(REPOSITORY_ROOT, 'scripts/test-fixtures/nightly-browser-cleanup-lifecycle.json')],
  ['evidence-extra-file', resolve(REPOSITORY_ROOT, 'scripts/test-fixtures/nightly-evidence-extra-file.json')],
  ['evidence-run-id-mismatch', resolve(REPOSITORY_ROOT, 'scripts/test-fixtures/nightly-evidence-run-id-mismatch.json')],
  ['evidence-unsafe-screenshot', resolve(REPOSITORY_ROOT, 'scripts/test-fixtures/nightly-evidence-unsafe-screenshot.json')],
  ['evidence-invalid-diagnostics', resolve(REPOSITORY_ROOT, 'scripts/test-fixtures/nightly-evidence-invalid-diagnostics.json')],
  ['browser-cleanup-failure', resolve(REPOSITORY_ROOT, 'scripts/test-fixtures/nightly-browser-cleanup-failure.json')],
  ['browser-timeout', resolve(REPOSITORY_ROOT, 'scripts/test-fixtures/nightly-browser-timeout.json')],
  ['evidence-symlink', resolve(REPOSITORY_ROOT, 'scripts/test-fixtures/nightly-evidence-symlink.json')],
  ['orphan-descendant-timeout', resolve(REPOSITORY_ROOT, 'scripts/test-fixtures/nightly-orphan-descendant-timeout.json')],
  ['diagnostic-cleanup-failure', resolve(REPOSITORY_ROOT, 'scripts/test-fixtures/nightly-diagnostic-cleanup-failure.json')],
]);
let activeChild = null;
let cancellationSignal = null;

for (const signal of ['SIGINT', 'SIGTERM']) {
  process.once(signal, () => {
    cancellationSignal = signal;
    activeChild?.terminate();
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

const manifest = loadNightlyRiskManifest(options.manifestPath, REPOSITORY_ROOT);
const commands = manifest.commands.map((command) =>
  materializeNightlyCommand(command, REPOSITORY_ROOT, options.resultsDirectory));

if (options.dryRun) {
  console.log(JSON.stringify({
    id: manifest.id,
    title: manifest.title,
    timeoutSeconds: manifest.timeoutSeconds,
    commands: commands.map(({ arguments: _arguments, cwd: _cwd, ...command }) => command),
  }, null, 2));
  process.exit(0);
}

if (existsSync(options.resultsDirectory)
  && (!statSync(options.resultsDirectory).isDirectory() || readdirSync(options.resultsDirectory).length > 0)) {
  console.error('[nightly-risk] results directory must be new or empty.');
  process.exit(2);
}
mkdirSync(options.resultsDirectory, { recursive: true, mode: 0o700 });
const executionHome = mkdtempSync(resolve(tmpdir(), 'qdb-nightly-risk-home-'));
chmodSync(executionHome, 0o700);

const startedAt = new Date();
const deadlineMs = startedAt.getTime() + manifest.timeoutSeconds * 1_000;
const primary = [];
let diagnostic = { requested: false, status: 'not-requested' };
let browserCleanup = { status: 'not-required', checkIds: [] };
try {
  for (let index = 0; index < commands.length; index += 1) {
    const command = commands[index];
    const dependencies = command.dependsOn ?? [];
    if (cancellationSignal || (command.databaseOwning === true && browserCleanup.status !== 'passed' && browserCleanup.status !== 'not-required') || dependencies.some((dependency) =>
      primary.find((result) => result.id === dependency)?.status !== 'passed')) {
      primary.push(notRunCommand(command, command.databaseOwning === true && browserCleanup.status !== 'passed' && browserCleanup.status !== 'not-required'
        ? 'blocked-by-browser-cleanup' : cancellationSignal ? 'cancelled' : 'failed-prerequisite'));
      continue;
    }
    const remainingMs = deadlineMs - Date.now();
    if (remainingMs <= 0) {
      primary.push(notRunCommand(command));
      continue;
    }

    console.log(`[nightly-risk] phase=${command.phase} command=${command.id} started`);
    const result = await runCommand(command, Math.min(remainingMs, command.timeoutSeconds * 1_000), {
      artifactRoot: options.artifactRoot,
      attempt: 'primary',
      executionHome,
      resultsDirectory: options.resultsDirectory,
    });
    if (command.approvedReporterArtifacts) {
      if (result.status === 'timed-out' || result.status === 'cancelled') {
        removeUnverifiedBrowserEvidence(options.resultsDirectory, 'primary');
      } else {
        result.reporterEvidence = validateBrowserEvidence(
          options.resultsDirectory,
          'primary',
          manifest.requiredBrowserJourneys,
          result.status,
        );
        if (result.reporterEvidence.status !== 'passed') removeUnverifiedBrowserEvidence(options.resultsDirectory, 'primary');
      }
      result.runtimeCleanup = await cleanupOwnedBrowserRuntime(command, options.resultsDirectory, 'primary');
      browserCleanup = result.runtimeCleanup;
      if (result.status === 'passed' && result.runtimeCleanup.status !== 'passed') result.status = 'failed';
    }
    if (command.testEvidence && result.status === 'passed') {
      result.testEvidence = validateOperationalEvidence(command);
      if (result.testEvidence.status !== 'passed') result.status = 'failed';
    }
    if (command.approvedReporterArtifacts && result.status === 'passed' && result.reporterEvidence?.status !== 'passed') {
      result.status = 'failed';
    }
    primary.push(result);
    console.log(`[nightly-risk] phase=${command.phase} command=${command.id} status=${result.status} durationMs=${result.durationMs}`);
  }
  const diagnosticCommand = commands.find(({ diagnosticRetry }) => diagnosticRetry === true);
  const browser = diagnosticCommand && primary.find(({ id }) => id === diagnosticCommand.id);
  if (diagnosticEligible(browser, commands, primary)) {
    diagnostic = await runDiagnosticRetry({ command: diagnosticCommand, manifest, options, deadlineMs, executionHome, cleanup: browser?.runtimeCleanup });
  } else if (options.diagnosticRetry) {
    diagnostic = { commandId: diagnosticCommand?.id, requested: true, status: 'not-eligible' };
  }
} finally {
  rmSync(executionHome, { force: true, recursive: true });
}
const completedAt = new Date();
const primaryStatus = primary.every((result) => result.status === 'passed')
  ? 'passed'
  : primary.some((result) => result.status === 'timed-out') ? 'timed-out' : 'failed';
const result = {
  schemaVersion: 1,
  laneId: manifest.id,
  status: primaryStatus,
  startedAt: startedAt.toISOString(),
  completedAt: completedAt.toISOString(),
  durationMs: completedAt.getTime() - startedAt.getTime(),
  timeoutSeconds: manifest.timeoutSeconds,
  timing: {
    browser: browserTiming(primary),
    blocking: false,
  },
  retention: manifest.evidenceRetention,
  primary,
  browserCleanup,
  diagnostic,
};
writeJsonAtomically(resolve(options.resultsDirectory, 'nightly-risk-result.json'), result);
console.log(`[nightly-risk] status=${result.status} diagnostic=${diagnostic.status}`);
process.exitCode = result.status === 'passed' ? 0 : 1;

function parseArguments(arguments_) {
  const parsed = {
    artifactRoot: '',
    diagnosticRetry: false,
    dryRun: false,
    manifestPath: DEFAULT_MANIFEST_PATH,
    resultsDirectory: '',
  };
  for (let index = 0; index < arguments_.length; index += 1) {
    const argument = arguments_[index];
    if (argument === '--artifact-root') parsed.artifactRoot = requireValue(arguments_, ++index, argument);
    else if (argument === '--results-dir') parsed.resultsDirectory = resolve(process.cwd(), requireValue(arguments_, ++index, argument));
    else if (argument === '--contract-test') {
      const fixture = requireValue(arguments_, ++index, argument);
      if (!CONTRACT_TEST_MANIFESTS.has(fixture)) throw new Error(`Unknown contract test fixture: ${fixture}`);
      parsed.manifestPath = CONTRACT_TEST_MANIFESTS.get(fixture);
    }
    else if (argument === '--diagnostic-retry') parsed.diagnosticRetry = true;
    else if (argument === '--dry-run') parsed.dryRun = true;
    else if (argument === '--help' || argument === '-h') {
      printUsage();
      process.exit(0);
    } else throw new Error(`Unknown argument: ${argument}`);
  }
  if (!parsed.resultsDirectory) throw new Error('--results-dir is required.');
  if (!parsed.dryRun && !parsed.artifactRoot) throw new Error('--artifact-root is required for execution.');
  return parsed;
}

function requireValue(arguments_, index, option) {
  const value = arguments_[index];
  if (!value || value.startsWith('--')) throw new Error(`${option} requires a value.`);
  return value;
}

function printUsage() {
  console.log('Usage: node scripts/run-nightly-risk-lane.mjs --artifact-root PATH --results-dir PATH [--diagnostic-retry] [--dry-run]');
}

function notRunCommand(command, reason) {
  return {
    id: command.id,
    phase: command.phase,
    status: 'not-run',
    durationMs: 0,
    exitCode: null,
    signal: null,
    ...(reason ? { reason } : {}),
  };
}

function diagnosticEligible(browser, commands, primary) {
  if (!browser || browser.executed !== true || !['failed', 'timed-out'].includes(browser.status)
    || (browser.runtimeCleanup !== undefined && browser.runtimeCleanup.status !== 'passed')) return false;
  const command = commands.find(({ id }) => id === browser.id);
  return command?.diagnosticRetry === true
    && (command.dependsOn ?? []).every((dependency) =>
      primary.find((result) => result.id === dependency)?.status === 'passed');
}

async function runDiagnosticRetry({ command, manifest: _manifest, options: options_, deadlineMs: deadline, executionHome: home, cleanup }) {
  if (!options_.diagnosticRetry) return { requested: false, status: 'not-requested' };
  if (command.diagnosticRetry !== true) return { requested: true, status: 'not-available' };
  if (cleanup?.status !== 'passed' && cleanup !== undefined) return { commandId: command.id, requested: true, status: 'blocked-by-cleanup' };
  const remainingMs = deadline - Date.now();
  if (remainingMs <= 0) return { commandId: command.id, requested: true, status: 'not-run' };

  console.log(`[nightly-risk] diagnostic command=${command.id} started`);
  const retry = await runCommand(command, Math.min(remainingMs, command.timeoutSeconds * 1_000), {
    artifactRoot: options_.artifactRoot,
    attempt: 'diagnostic',
    executionHome: home,
    resultsDirectory: options_.resultsDirectory,
  });
  if (command.approvedReporterArtifacts) {
    if (retry.status === 'timed-out' || retry.status === 'cancelled') {
      removeUnverifiedBrowserEvidence(options_.resultsDirectory, 'diagnostic');
    } else {
      retry.reporterEvidence = validateBrowserEvidence(
        options_.resultsDirectory,
        'diagnostic',
        _manifest.requiredBrowserJourneys,
        retry.status,
      );
      if (retry.reporterEvidence.status !== 'passed') removeUnverifiedBrowserEvidence(options_.resultsDirectory, 'diagnostic');
    }
    retry.runtimeCleanup = await cleanupOwnedBrowserRuntime(command, options_.resultsDirectory, 'diagnostic');
    if (retry.runtimeCleanup.status !== 'passed') {
      retry.status = 'failed';
      retry.cleanupClassification = 'cleanup-unverified';
    }
  }
  if (command.approvedReporterArtifacts && retry.status === 'passed' && retry.reporterEvidence?.status !== 'passed') {
    retry.status = 'failed';
  }
  console.log(`[nightly-risk] diagnostic command=${command.id} status=${retry.status} durationMs=${retry.durationMs}`);
  return {
    commandId: command.id,
    classificationOnly: true,
    requested: true,
    status: retry.status,
    result: retry,
  };
}

function runCommand(command, timeoutMs, context) {
  return new Promise((resolvePromise) => {
    const startedAt = Date.now();
    const child = spawn(command.executable, command.arguments, {
      cwd: command.cwd,
      detached: process.platform !== 'win32',
      env: createCommandEnvironment(context, command),
      stdio: 'ignore',
    });
    let timedOut = false;
    let spawned = false;
    let forceKillTimer;
    let timeout;
    let terminationRequested = false;
    let closed;
    let resolved = false;
    const complete = () => {
      if (resolved || !closed || (terminationRequested && forceKillTimer)) return;
      resolved = true;
      if (!terminationRequested) clearTimeout(timeout);
      resolvePromise({
        id: command.id,
        phase: command.phase,
        status: !context.ignoreCancellation && cancellationSignal ? 'cancelled' : timedOut ? 'timed-out' : spawned && closed.exitCode === 0 ? 'passed' : 'failed',
        durationMs: Date.now() - startedAt,
        exitCode: closed.exitCode,
        signal: closed.signal,
        executed: spawned,
      });
      if (activeChild?.terminate === terminate) activeChild = null;
    };
    const terminate = () => {
      if (terminationRequested) return;
      terminationRequested = true;
      clearTimeout(timeout);
      terminateProcessTree(child, 'SIGTERM');
      forceKillTimer = setTimeout(() => {
        // The direct child can exit before descendants. Always finish the group sweep before cleanup.
        terminateProcessTree(child, 'SIGKILL');
        forceKillTimer = null;
        complete();
      }, 5_000);
    };
    timeout = setTimeout(() => {
      timedOut = true;
      terminate();
    }, timeoutMs);
    timeout.unref();
    child.once('spawn', () => { spawned = true; });
    child.once('error', () => { spawned = false; });
    activeChild = { terminate };
    child.once('close', (exitCode, signal) => {
      closed = { exitCode, signal };
      complete();
    });
  });
}

function removeUnverifiedBrowserEvidence(resultsDirectory, attempt) {
  rmSync(resolve(resultsDirectory, 'attempts', attempt), { force: true, recursive: true });
}

function createCommandEnvironment({ artifactRoot, attempt, executionHome, resultsDirectory }, command) {
  const environment = {
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
  if (command.id === 'verify-full-canonical-artifact' || command.id === 'phrase-index-build-activation') {
    environment.QURAN_DASHBOARD_ARTIFACT_EXECUTION = 'scheduled';
    environment.QURAN_TEST_ARTIFACT_ROOT = artifactRoot;
  }
  if (command.id === 'verify-full-canonical-artifact') environment.QDB_TEST_ARTIFACTS_SEALED = '1';
  environment.QDB_NIGHTLY_ATTEMPT = attempt;
  environment.QDB_NIGHTLY_RESULTS_DIR = resultsDirectory;
  if (command.approvedReporterArtifacts) {
    environment.QDB_PR_OBSERVATION_RESULT_DIR = resolve(resultsDirectory, 'attempts', attempt);
  }
  return environment;
}

function validateBrowserEvidence(resultsDirectory, attempt, requiredJourneys, expectedStatus) {
  const root = resolve(resultsDirectory, 'attempts', attempt, 'playwright-evidence');
  let runDirectory;
  try {
    const entries = readdirSync(root, { withFileTypes: true });
    if (entries.length !== 1 || !entries[0].isDirectory() || !lstatSync(resolve(root, entries[0].name)).isDirectory()) {
      return { status: 'failed', checkIds: ['playwright-evidence-root-invalid'] };
    }
    runDirectory = entries[0].name;
  } catch {
    return { status: 'failed', checkIds: ['playwright-evidence-missing'] };
  }
  const evidence = validateControlledPlaywrightRun(resolve(root, runDirectory), expectedStatus);
  const checkIds = [...evidence.checkIds];
  for (const journey of requiredJourneys) {
    const matches = evidence.tests.filter((test) => test?.journey === journey);
    if (matches.length !== 1) checkIds.push('designated-mobile-journey-count-invalid');
    else if (expectedStatus === 'passed' && (matches[0].status !== 'passed' || matches[0].retry !== 0)) {
      checkIds.push('designated-mobile-journey-not-first-attempt-passed');
    }
  }
  return checkIds.length === 0
    ? { status: 'passed', checkIds: ['designated-mobile-journeys-first-attempt-passed', 'controlled-evidence-validated'] }
    : { status: 'failed', checkIds: [...new Set(checkIds)] };
}

function validateOperationalEvidence(command) {
  const resultsDirectory = command.arguments[command.arguments.indexOf('--results-dir') + 1];
  if (!resultsDirectory) return { status: 'failed', checkIds: ['trx-results-directory-missing'] };
  const files = listFiles(resultsDirectory).filter((file) => file.endsWith('.trx'));
  if (files.length !== 1) return { status: 'failed', checkIds: ['trx-file-count-invalid'] };
  try {
    const metadata = JSON.parse(readFileSync(resolve(resultsDirectory, 'nightly-test-evidence.json'), 'utf8'));
    if (metadata?.schemaVersion !== 1 || metadata.status !== 'passed' || metadata.lane !== command.testEvidence.lane) {
      return { status: 'failed', checkIds: ['trx-lane-mismatch'] };
    }
    const results = [...readFileSync(files[0], 'utf8').matchAll(/<UnitTestResult\b([^>]*)\/>/g)].map((match) => match[1]);
    if (results.length === 0) return { status: 'failed', checkIds: ['trx-no-executed-tests'] };
    const outcomes = results.map((attributes) => attribute(attributes, 'outcome'));
    const names = results.map((attributes) => attribute(attributes, 'testName'));
    if (names.some((name) => typeof name !== 'string' || !name.startsWith(`${command.testEvidence.class}.`))) {
      return { status: 'failed', checkIds: ['trx-class-mismatch'] };
    }
    if (outcomes.some((outcome) => outcome !== 'Passed')) {
      return { status: 'failed', checkIds: ['trx-non-passing-test'] };
    }
    return { status: 'passed', checkIds: ['trx-executed-tests-passed'], executed: results.length };
  } catch {
    return { status: 'failed', checkIds: ['trx-invalid'] };
  }
}

function attribute(attributes, name) {
  return new RegExp(`${name}="([^"]*)"`).exec(attributes)?.[1];
}

function listFiles(path) {
  try {
    return readdirSync(path, { withFileTypes: true }).flatMap((entry) => entry.isDirectory()
      ? listFiles(resolve(path, entry.name))
      : entry.isFile() ? [resolve(path, entry.name)] : []);
  } catch {
    return [];
  }
}

async function cleanupOwnedBrowserRuntime(command, resultsDirectory, attempt) {
  if (command.runtimeCleanup !== true) return { status: 'failed', checkIds: ['runtime-cleanup-contract-missing'] };
  const script = resolve(
    REPOSITORY_ROOT,
    command.runtimeCleanupScript ?? 'Frontend/quran-dashboard-ui/scripts/cleanup-controlled-playwright-runtime.mjs',
  );
  const executionHome = mkdtempSync(resolve(tmpdir(), 'qdb-nightly-cleanup-'));
  try {
    const cleanup = await runCommand({ id: 'controlled-browser-cleanup', phase: 'browser-cleanup', executable: process.execPath, arguments: [script, resultsDirectory, attempt], cwd: command.cwd }, 60_000, {
      artifactRoot: '', attempt: `${attempt}-cleanup`, executionHome, ignoreCancellation: true, resultsDirectory,
    });
    return cleanup.status === 'passed'
      ? { status: 'passed', checkIds: ['owned-runtime-cleanup-verified'] }
      : { status: 'failed', checkIds: ['owned-runtime-cleanup-unverified'] };
  } finally {
    rmSync(executionHome, { force: true, recursive: true });
  }
}

function browserTiming(primary_) {
  const result_ = primary_.find(({ id }) => id === 'full-chromium-suite');
  return result_
    ? { commandId: result_.id, status: result_.status, durationMs: result_.durationMs }
    : { commandId: 'full-chromium-suite', status: 'not-run', durationMs: 0 };
}

function terminateProcessTree(child, signal) {
  if (!child.pid) return;
  try {
    if (process.platform === 'win32') child.kill(signal);
    else process.kill(-child.pid, signal);
  } catch (error) {
    if (error.code !== 'ESRCH') console.error(`[nightly-risk] process termination failed: ${error.code ?? 'unknown'}`);
  }
}

function writeJsonAtomically(path, value) {
  const temporaryPath = `${path}.tmp-${process.pid}`;
  writeFileSync(temporaryPath, `${JSON.stringify(value, null, 2)}\n`, { encoding: 'utf8', mode: 0o600 });
  renameSync(temporaryPath, path);
}
