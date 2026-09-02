import { spawn } from 'node:child_process';
import { existsSync, mkdirSync, readFileSync, readdirSync, renameSync, writeFileSync } from 'node:fs';
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

if (existsSync(jobResultsDirectory) && readdirSync(jobResultsDirectory).length > 0) {
  console.error(`[pr-observation] results directory must be new or empty: ${jobResultsDirectory}`);
  process.exit(2);
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
  const result = await runCommand(command, remainingMs, jobResultsDirectory);
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
const journeyDecision = evaluateJourneyGroups(job, jobResultsDirectory, finalStatus);
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
  ...(journeyDecision ?? {}),
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

if (finalStatus !== 'passed' && journeyDecision) {
  console.log('[pr-observation] first-attempt full-catalogue failure retained with independent journey-group enforcement.');
} else if (finalStatus !== 'passed' && !job.policy.blocking) {
  console.log('[pr-observation] first-attempt failure retained as non-blocking observation evidence.');
}

if (journeyDecision) {
  console.log(
    `[pr-observation] journey-enforcement=${journeyDecision.enforcementStatus}`,
  );
}
process.exitCode = journeyDecision
  ? journeyDecision.enforcementStatus === 'passed' ? 0 : 1
  : finalStatus === 'passed' || !job.policy.blocking ? 0 : 1;

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

function runCommand(command, timeoutMs, jobResultsDirectory) {
  return new Promise((resolvePromise) => {
    const commandStartedAt = Date.now();
    const child = spawn(command.executable, command.arguments, {
      cwd: command.cwd,
      detached: process.platform !== 'win32',
      env: {
        ...process.env,
        QDB_PR_OBSERVATION_RESULT_DIR: jobResultsDirectory,
      },
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

function evaluateJourneyGroups(job, jobResultsDirectory, finalStatus) {
  const configuredGroups = job.policy.journeyGroups;
  if (!configuredGroups) {
    return null;
  }

  const declaredJourneys = new Map(
    configuredGroups.flatMap((group) => group.journeys.map((journey) => [journey, group.id])),
  );
  const evidence = readPlaywrightEvidence(jobResultsDirectory);
  const testsByJourney = new Map();
  const contractErrors = [...evidence.errors];
  if (evidence.declaredTestCount !== declaredJourneys.size) {
    contractErrors.push(`declared-test-count:${evidence.declaredTestCount ?? 'missing'}`);
  }
  if (evidence.tests.length !== declaredJourneys.size) {
    contractErrors.push(`playwright-test-count:${evidence.tests.length}`);
  }
  for (const test of evidence.tests) {
    if (!isValidJourneyTestEvidence(test)) {
      contractErrors.push(`invalid-test-evidence:${test?.id ?? 'missing'}`);
      continue;
    }
    if (typeof test.journey !== 'string' || !declaredJourneys.has(test.journey)) {
      contractErrors.push(`undeclared-journey:${test.journey ?? 'missing'}`);
      continue;
    }
    if (test.retry !== 0) {
      contractErrors.push(`unexpected-retry:${test.journey}`);
    }
    const entries = testsByJourney.get(test.journey) ?? [];
    entries.push(test);
    testsByJourney.set(test.journey, entries);
  }

  const journeyGroups = configuredGroups.map((group) => {
    const tests = group.journeys.flatMap((journey) => testsByJourney.get(journey) ?? []);
    const missingJourneys = group.journeys.filter((journey) => !testsByJourney.has(journey));
    const duplicateJourneys = group.journeys.filter(
      (journey) => (testsByJourney.get(journey)?.length ?? 0) > 1,
    );
    const passed = missingJourneys.length === 0
      && duplicateJourneys.length === 0
      && tests.every((test) => test.status === 'passed' && test.retry === 0);
    return {
      id: group.id,
      mode: group.blocking ? 'blocking' : 'observation',
      status: tests.length === 0 ? 'not-run' : passed ? 'passed' : 'failed',
      journeys: group.journeys,
      durationMs: tests.reduce((sum, test) => sum + test.durationMs, 0),
      tests: tests.map(({ id, journey, status, durationMs, retry }) => ({
        id,
        journey,
        status,
        durationMs,
        retry,
      })),
      missingJourneys,
      duplicateJourneys,
    };
  });
  for (const group of journeyGroups) {
    contractErrors.push(...group.missingJourneys.map((journey) => `missing-journey:${journey}`));
    contractErrors.push(...group.duplicateJourneys.map((journey) => `duplicate-journey:${journey}`));
  }
  if (finalStatus === 'failed' && evidence.sealedStatus === 'passed') {
    contractErrors.push('command-failed-after-passing-sealed-run');
  }

  const blockingGroupsPassed = journeyGroups
    .filter(({ mode }) => mode === 'blocking')
    .every(({ status }) => status === 'passed');
  const enforcementStatus = finalStatus !== 'timed-out'
    && contractErrors.length === 0
    && blockingGroupsPassed
    ? 'passed'
    : 'failed';
  return {
    mode: 'mixed',
    enforcementStatus,
    journeyEvidence: evidence.path,
    journeyContractErrors: contractErrors,
    journeyGroups,
  };
}

function isValidJourneyTestEvidence(test) {
  return test
    && typeof test.id === 'string'
    && typeof test.journey === 'string'
    && ['passed', 'failed', 'timedOut', 'skipped', 'interrupted'].includes(test.status)
    && Number.isFinite(test.durationMs)
    && test.durationMs >= 0
    && Number.isInteger(test.retry)
    && test.retry >= 0;
}

function readPlaywrightEvidence(jobResultsDirectory) {
  const root = resolve(jobResultsDirectory, 'playwright-evidence');
  let runDirectories = [];
  try {
    runDirectories = readdirSync(root, { withFileTypes: true })
      .filter((entry) => entry.isDirectory())
      .map((entry) => entry.name);
  } catch {
    return { errors: ['playwright-evidence-missing'], path: null, tests: [] };
  }
  if (runDirectories.length !== 1) {
    return {
      errors: [`playwright-evidence-run-count:${runDirectories.length}`],
      path: null,
      tests: [],
    };
  }

  const runId = runDirectories[0];
  const runDirectory = resolve(root, runId);
  const path = resolve(runDirectory, 'playwright-results.json');
  const errors = [];
  const playwright = readJsonEvidence(path, 'playwright-results-invalid', errors);
  const structured = readJsonEvidence(
    resolve(runDirectory, 'structured-results.json'),
    'structured-results-invalid',
    errors,
  );
  const manifest = readJsonEvidence(
    resolve(runDirectory, 'evidence-manifest.json'),
    'evidence-manifest-invalid',
    errors,
  );

  if (playwright?.schemaVersion !== 1 || !Array.isArray(playwright?.tests)) {
    errors.push('playwright-tests-missing');
  }
  if (!Number.isInteger(playwright?.declaredTestCount) || playwright.declaredTestCount < 1) {
    errors.push('playwright-declared-test-count-invalid');
  }
  validateStructuredEvidence(structured, runId, playwright, errors);
  validateEvidenceManifest(manifest, runId, structured, errors);
  return {
    errors,
    path,
    tests: Array.isArray(playwright?.tests) ? playwright.tests : [],
    declaredTestCount: playwright?.declaredTestCount,
    sealedStatus: structured?.status,
  };
}

function readJsonEvidence(path, errorCode, errors) {
  try {
    return JSON.parse(readFileSync(path, 'utf8'));
  } catch {
    errors.push(errorCode);
    return null;
  }
}

function validateStructuredEvidence(structured, runId, playwright, errors) {
  if (
    structured?.schemaVersion !== 1
    || structured.runId !== runId
    || !['passed', 'failed'].includes(structured.status)
    || !Array.isArray(structured.phases)
  ) {
    errors.push('structured-results-contract-invalid');
    return;
  }

  const requiredPhases = [
    ['artifactProvisioning', true],
    ['databasePreparation', true],
    ['applicationStartup', true],
    ['testExecution', false],
  ];
  for (const [name, mustPass] of requiredPhases) {
    const matches = structured.phases.filter((phase) => phase?.name === name);
    if (
      matches.length !== 1
      || !Number.isFinite(matches[0].durationMs)
      || matches[0].durationMs < 0
      || (mustPass && matches[0].status !== 'passed')
      || (!mustPass && !['passed', 'failed'].includes(matches[0].status))
    ) {
      errors.push(`sealed-phase-invalid:${name}`);
    }
  }

  const hasTestFailure = Array.isArray(playwright?.tests)
    && playwright.tests.some((test) => test?.status !== 'passed');
  const expectedStatus = hasTestFailure ? 'failed' : 'passed';
  if (playwright?.status !== expectedStatus || structured.status !== expectedStatus) {
    errors.push('sealed-run-status-mismatch');
  }
  const testExecution = structured.phases.find((phase) => phase?.name === 'testExecution');
  if (testExecution?.status !== playwright?.status) {
    errors.push('sealed-test-execution-status-mismatch');
  }
}

function validateEvidenceManifest(manifest, runId, structured, errors) {
  if (
    manifest?.schemaVersion !== 1
    || manifest.runId !== runId
    || manifest.status !== structured?.status
    || manifest.containsDatabaseDump !== false
    || manifest.capturesRequestHeaders !== false
    || manifest.capturesRequestBodies !== false
    || manifest.traceFormat !== 'sanitized-step-events-v1'
    || manifest.screenshotPolicy !== 'text-media-masked-v1'
    || !Array.isArray(manifest.files)
  ) {
    errors.push('evidence-manifest-contract-invalid');
    return;
  }
  for (const file of [
    'evidence-manifest.json',
    'playwright-results.json',
    'structured-results.json',
  ]) {
    if (!manifest.files.includes(file)) {
      errors.push(`evidence-manifest-file-missing:${file}`);
    }
  }
  if (
    manifest.inspection?.status !== 'passed'
    || manifest.inspection.unsafeScreenshot !== false
    || !Array.isArray(manifest.inspection.invalidDiagnosticFiles)
    || manifest.inspection.invalidDiagnosticFiles.length > 0
  ) {
    errors.push('evidence-inspection-failed');
  }
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
