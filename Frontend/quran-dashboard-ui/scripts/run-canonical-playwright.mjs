import { spawnSync } from 'node:child_process';
import {
  appendFileSync,
  chmodSync,
  mkdirSync,
  mkdtempSync,
  readFileSync,
  rmSync,
} from 'node:fs';
import { tmpdir } from 'node:os';
import { dirname, relative, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

import {
  buildCanonicalPlaywrightEnvironment,
  selectCanonicalCriticalJourneys,
  selectCanonicalPlaywrightTests,
} from './canonical-playwright-runtime.mjs';
import {
  createControlledPlaywrightEnvironment,
  inspectRetainedEvidence,
  loadControlledProvisioningReceipt,
  runWithSanitizedOutput,
  sanitizeError,
  validatePlaywrightChildResult,
  writeJson,
} from './controlled-playwright-runtime.mjs';
import {
  redactDiagnosticText,
  sensitiveEnvironmentValues,
} from '../e2e/harness/controlled-execution-contract.mjs';

const frontendRoot = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const repositoryRoot = resolve(frontendRoot, '../..');
const playwright = resolve(frontendRoot, 'node_modules/.bin/playwright');
const [mode = '--full', selector, interactiveMode, ...extraArguments] = process.argv.slice(2);

validateArguments();
const receipt = loadControlledProvisioningReceipt(frontendRoot, repositoryRoot);
requireTestDatabaseConnection();
const secretValues = sensitiveEnvironmentValues(process.env);
const runId = `canonical-${Date.now()}-${process.pid}`;
const partitionRoot = resolvePartitionRoot(runId);
const evidenceDirectory = resolve(partitionRoot, 'evidence');
const applicationLog = resolve(evidenceDirectory, 'application.log');
const playwrightOutputDirectory = mkdtempSync(resolve(tmpdir(), 'qdb-controlled-playwright-output-'));
const homeDirectory = mkdtempSync(resolve(tmpdir(), 'qdb-controlled-playwright-home-'));
mkdirSync(evidenceDirectory, { recursive: true, mode: 0o700 });
chmodSync(partitionRoot, 0o700);
chmodSync(evidenceDirectory, 0o700);
chmodSync(playwrightOutputDirectory, 0o700);
chmodSync(homeDirectory, 0o700);

const controlledEnvironment = createControlledPlaywrightEnvironment(process.env, receipt, {
  evidenceDirectory,
  homeDirectory,
  playwrightOutputDirectory,
});
const environment = buildCanonicalPlaywrightEnvironment(controlledEnvironment);
environment.QURAN_DASHBOARD_TEST_RUN_ID = runId;
const report = {
  schemaVersion: 1,
  kind: 'canonical-read',
  runId,
  status: 'failed',
  startedAt: new Date().toISOString(),
  completedAt: null,
  durationMs: null,
  evidence: relative(partitionRoot, resolve(evidenceDirectory, 'playwright-results.json')),
  phases: [],
  inspection: null,
};
const startedAt = Date.now();
let exitCode = 1;
let activePlaywrightChild = null;
let interruptionSignal = null;
process.on('SIGINT', () => requestShutdown('SIGINT'));
process.on('SIGTERM', () => requestShutdown('SIGTERM'));

try {
  const selectors = mode === '--critical'
    ? selectCanonicalCriticalJourneys(discover('./scripts/discover-playwright-journeys.mjs'))
    : selectCanonicalPlaywrightTests(
        discover('./scripts/discover-playwright-policies.mjs'),
        mode === '--focused' ? selector : undefined,
      );
  if (selectors.length === 0) {
    throw new Error('Canonical Playwright execution discovered no canonical-read tests.');
  }

  const inspectionStartedAt = Date.now();
  const inspection = await runWithSanitizedOutput(
    'dotnet',
    [environment.E2E_TEST_RUNTIME_ASSEMBLY, 'inspect'],
    {
      cwd: repositoryRoot,
      environment,
      logPath: applicationLog,
      secretValues,
    },
  );
  report.phases.push({
    name: 'capabilityInspection',
    status: inspection.exitCode === 0 ? 'passed' : 'failed',
    durationMs: Date.now() - inspectionStartedAt,
  });
  if (inspection.exitCode !== 0) throw new Error('TestRuntime capability inspection failed.');

  const childStartedAt = Date.now();
  const playwrightArguments = ['test', ...selectors, `--workers=${interactiveMode ? 1 : 2}`];
  if (interactiveMode) playwrightArguments.push(interactiveMode);
  const child = await runWithSanitizedOutput(playwright, playwrightArguments, {
    cwd: frontendRoot,
    environment,
    logPath: applicationLog,
    secretValues,
    onSpawn: (spawned) => {
      activePlaywrightChild = spawned;
    },
  });
  activePlaywrightChild = null;
  exitCode = child.exitCode;
  const childCompletedAt = Date.now();
  const playwrightResult = validatePlaywrightChildResult(readPlaywrightResult(), {
    declaredTestCount: selectors.length,
    runId,
  });
  appendChildPhases(report.phases, playwrightResult, childStartedAt, childCompletedAt);
  if (child.exitCode !== 0 || playwrightResult.status !== 'passed') {
    throw new Error(`Canonical Playwright child failed with status ${child.exitCode}.`);
  }
  if (interruptionSignal) {
    throw new Error(`Canonical Playwright was interrupted by ${interruptionSignal}.`);
  }
  report.status = 'passed';
} catch (error) {
  report.error = sanitizeError(error, secretValues);
  appendMissingChildFailurePhases(report.phases);
  appendFileSync(applicationLog, `${report.error}\n`, { encoding: 'utf8', mode: 0o600 });
  console.error(report.error);
} finally {
  rmSync(playwrightOutputDirectory, { recursive: true, force: true });
  rmSync(homeDirectory, { recursive: true, force: true });
  report.inspection = inspectRetainedEvidence(evidenceDirectory, secretValues);
  if (report.inspection.status !== 'passed') report.status = 'failed';
  if (report.status === 'passed') rmSync(applicationLog, { force: true });
  report.completedAt = new Date().toISOString();
  report.durationMs = Date.now() - startedAt;
  writeJson(resolve(partitionRoot, 'canonical-results.json'), report);
}

console.log(
  `[e2e] controlled canonical status=${report.status} durationMs=${report.durationMs} evidence=${partitionRoot}`,
);
process.exit(report.status === 'passed' ? 0 : exitCode || 1);

function discover(reporter) {
  const result = spawnSync(playwright, ['test', '--list', `--reporter=${reporter}`], {
    cwd: frontendRoot,
    encoding: 'utf8',
    env: environment,
    maxBuffer: 10 * 1024 * 1024,
  });
  if (result.error) throw result.error;
  if (result.status !== 0) {
    throw new Error(redactDiagnosticText(result.stderr ?? 'Playwright discovery failed.', secretValues));
  }
  try {
    return JSON.parse(result.stdout);
  } catch (error) {
    throw new Error(`Playwright discovery returned invalid JSON: ${error.message}`);
  }
}

function readPlaywrightResult() {
  try {
    return JSON.parse(readFileSync(resolve(evidenceDirectory, 'playwright-results.json'), 'utf8'));
  } catch {
    throw new Error('Canonical Playwright child returned no valid structured result.');
  }
}

function appendChildPhases(phases, childResult, childStartedAt, childCompletedAt) {
  const applicationsReadyAt = Date.parse(childResult.applicationsReadyAt);
  const testsCompletedAt = Date.parse(childResult.completedAt);
  phases.push({
    name: 'applicationStartup',
    status: Number.isFinite(applicationsReadyAt) ? 'passed' : 'failed',
    durationMs: Number.isFinite(applicationsReadyAt)
      ? Math.max(0, applicationsReadyAt - childStartedAt)
      : Math.max(0, childCompletedAt - childStartedAt),
  });
  phases.push({
    name: 'testExecution',
    status: childResult.status,
    durationMs: Number.isFinite(applicationsReadyAt) && Number.isFinite(testsCompletedAt)
      ? Math.max(0, testsCompletedAt - applicationsReadyAt)
      : 0,
  });
  phases.push({
    name: 'applicationShutdown',
    status: 'passed',
    durationMs: Number.isFinite(testsCompletedAt)
      ? Math.max(0, childCompletedAt - testsCompletedAt)
      : 0,
  });
}

function appendMissingChildFailurePhases(phases) {
  for (const name of ['applicationStartup', 'testExecution', 'applicationShutdown']) {
    if (!phases.some((phase) => phase.name === name)) {
      phases.push({ name, status: 'failed', durationMs: 0 });
    }
  }
}

function requestShutdown(signal) {
  interruptionSignal ??= signal;
  if (!activePlaywrightChild?.pid) return;
  try {
    process.kill(-activePlaywrightChild.pid, 'SIGTERM');
  } catch (error) {
    if (error?.code !== 'ESRCH') activePlaywrightChild.kill('SIGTERM');
  }
}

function resolvePartitionRoot(id) {
  const aggregateDirectory = process.env.QDB_PLAYWRIGHT_AGGREGATE_DIRECTORY?.trim();
  if (aggregateDirectory) return resolve(aggregateDirectory, 'canonical');
  const observationDirectory = process.env.QDB_PR_OBSERVATION_RESULT_DIR?.trim();
  const evidenceRoot = observationDirectory
    ? resolve(observationDirectory, 'playwright-evidence')
    : resolve(frontendRoot, '.playwright/evidence');
  return resolve(evidenceRoot, id);
}

function validateArguments() {
  if (!['--critical', '--focused', '--full'].includes(mode)) {
    throw new Error('Use --critical, --focused, or --full for canonical Playwright execution.');
  }
  if (mode === '--focused' && (!selector || extraArguments.length > 0)) {
    throw new Error('--focused requires exactly one Playwright file:line selector.');
  }
  if (mode === '--focused' && interactiveMode !== undefined && !['--headed', '--ui'].includes(interactiveMode)) {
    throw new Error('Focused canonical Playwright supports only --headed or --ui interactive mode.');
  }
  if (mode !== '--focused' && (selector !== undefined || interactiveMode !== undefined || extraArguments.length > 0)) {
    throw new Error(`${mode} does not accept a Playwright selector.`);
  }
}

function requireTestDatabaseConnection() {
  if (!process.env.ConnectionStrings__QuranDashboardTest?.trim()) {
    throw new Error(
      'Controlled canonical Playwright execution requires ConnectionStrings__QuranDashboardTest.',
    );
  }
}
