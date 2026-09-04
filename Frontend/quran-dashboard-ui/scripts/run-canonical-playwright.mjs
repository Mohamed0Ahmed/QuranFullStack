import { appendFileSync, readFileSync, rmSync } from 'node:fs';
import { dirname, relative, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

import {
  buildCanonicalPlaywrightEnvironment,
  selectCanonicalCriticalJourneys,
  selectCanonicalPlaywrightTests,
} from './canonical-playwright-runtime.mjs';
import {
  appendApplicationShutdownPhase,
  appendChildExecutionPhases,
  appendMissingChildFailurePhases,
  createPrivatePlaywrightRuntime,
  createControlledPlaywrightEnvironment,
  discoverControlledPlaywright,
  inspectRetainedEvidence,
  loadControlledProvisioningReceipt,
  runWithSanitizedOutput,
  sanitizeError,
  validatePlaywrightChildResult,
  writeJson,
} from './controlled-playwright-runtime.mjs';
import {
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
const discovered = discoverControlledPlaywright(
  frontendRoot,
  receipt,
  mode === '--critical'
    ? './scripts/discover-playwright-journeys.mjs'
    : './scripts/discover-playwright-policies.mjs',
  process.env,
);
const selectors = mode === '--critical'
  ? selectCanonicalCriticalJourneys(discovered)
  : selectCanonicalPlaywrightTests(discovered, mode === '--focused' ? selector : undefined);
if (selectors.length === 0) {
  throw new Error('Canonical Playwright execution discovered no canonical-read tests.');
}
const runId = `canonical-${Date.now()}-${process.pid}`;
const partitionRoot = resolvePartitionRoot(runId);
const evidenceDirectory = resolve(partitionRoot, 'evidence');
const applicationLog = resolve(evidenceDirectory, 'application.log');
const privateRuntime = createPrivatePlaywrightRuntime(evidenceDirectory);
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
let environment;
let activePlaywrightChild = null;
let interruptionSignal = null;
process.on('SIGINT', () => requestShutdown('SIGINT'));
process.on('SIGTERM', () => requestShutdown('SIGTERM'));

try {
  const controlledEnvironment = createControlledPlaywrightEnvironment(
    process.env,
    receipt,
    privateRuntime,
  );
  environment = buildCanonicalPlaywrightEnvironment(controlledEnvironment);
  environment.QURAN_DASHBOARD_TEST_RUN_ID = runId;

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
  appendChildExecutionPhases(report.phases, playwrightResult, childStartedAt, childCompletedAt);
  appendApplicationShutdownPhase(
    report.phases,
    playwrightResult,
    childCompletedAt,
    interruptionSignal ? 'failed' : 'passed',
  );
  if (child.exitCode !== 0 || playwrightResult.status !== 'passed') {
    throw new Error(`Canonical Playwright child failed with status ${child.exitCode}.`);
  }
  if (interruptionSignal) {
    throw new Error(`Canonical Playwright was interrupted by ${interruptionSignal}.`);
  }
  report.status = 'passed';
} catch (error) {
  report.error = sanitizeError(error, secretValues);
  appendMissingChildFailurePhases(report.phases, [
    'capabilityInspection',
    'applicationStartup',
    'testExecution',
    'applicationShutdown',
  ]);
  appendFileSync(applicationLog, `${report.error}\n`, { encoding: 'utf8', mode: 0o600 });
  console.error(report.error);
} finally {
  privateRuntime.cleanup();
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

function readPlaywrightResult() {
  try {
    return JSON.parse(readFileSync(resolve(evidenceDirectory, 'playwright-results.json'), 'utf8'));
  } catch {
    throw new Error('Canonical Playwright child returned no valid structured result.');
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
