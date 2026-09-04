import { spawn, spawnSync } from 'node:child_process';
import { randomBytes } from 'node:crypto';
import {
  appendFileSync,
  chmodSync,
  mkdirSync,
  readFileSync,
  rmSync,
} from 'node:fs';
import { createConnection } from 'node:net';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

import {
  sensitiveEnvironmentValues,
} from '../e2e/harness/controlled-execution-contract.mjs';
import {
  appendApplicationShutdownPhase,
  appendChildExecutionPhases,
  appendMissingChildFailurePhases,
  captureSanitizedStream,
  createControlledPlaywrightEnvironment,
  createPrivatePlaywrightRuntime,
  discoverControlledPlaywright,
  inspectRetainedEvidence,
  loadControlledProvisioningReceipt,
  recordSanitizedText,
  sanitizeError,
  validatePlaywrightChildResult,
  writeJson,
} from './controlled-playwright-runtime.mjs';
import {
  STATEFUL_API_PORT,
  STATEFUL_LOCK_COMMAND,
  buildMutableResetArguments,
  buildStatefulPlaywrightEnvironment,
  selectStatefulCriticalJourneys,
  selectStatefulPlaywrightTests,
  validateApiProcessReceipt,
} from './stateful-playwright-runtime.mjs';

const frontendRoot = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const repositoryRoot = resolve(frontendRoot, '../..');
const playwright = resolve(frontendRoot, 'node_modules/.bin/playwright');
const [mode = '--full', selector, interactiveMode, ...extraArguments] = process.argv.slice(2);

validateArguments(mode, selector, interactiveMode, extraArguments);
const provisioningReceipt = loadControlledProvisioningReceipt(frontendRoot, repositoryRoot);
requireTestDatabaseConnection();
const secretValues = sensitiveEnvironmentValues(process.env);
const discovered = discoverControlledPlaywright(
  frontendRoot,
  provisioningReceipt,
  mode === '--critical'
    ? './scripts/discover-playwright-journeys.mjs'
    : './scripts/discover-playwright-policies.mjs',
  process.env,
);
const scenarios = mode === '--critical'
  ? selectStatefulCriticalJourneys(discovered)
  : selectStatefulPlaywrightTests(discovered, mode === '--focused' ? selector : undefined);
if (scenarios.length === 0) {
  throw new Error('Stateful Playwright execution discovered no guarded-read or mutating tests.');
}

const aggregateRunId = `stateful-${Date.now()}-${process.pid}`;
const aggregateRoot = resolveAggregateRoot(aggregateRunId);
mkdirSync(aggregateRoot, { recursive: true, mode: 0o700 });
chmodSync(aggregateRoot, 0o700);
const aggregateStartedAt = Date.now();
const aggregate = {
  schemaVersion: 1,
  kind: 'stateful',
  runId: aggregateRunId,
  status: 'passed',
  startedAt: new Date().toISOString(),
  completedAt: null,
  durationMs: null,
  provisioningPhases: provisioningReceipt.phases,
  scenarios: [],
};
let activePlaywrightChild = null;
let interruptionSignal = null;
process.on('SIGINT', () => requestShutdown('SIGINT'));
process.on('SIGTERM', () => requestShutdown('SIGTERM'));

for (const [index, scenario] of scenarios.entries()) {
  if (interruptionSignal) {
    aggregate.status = 'failed';
    break;
  }
  const result = await runScenario(scenario, index);
  aggregate.scenarios.push(result);
  if (result.status !== 'passed') aggregate.status = 'failed';
  writeAggregate();
  if (result.status !== 'passed') break;
}
aggregate.completedAt = new Date().toISOString();
aggregate.durationMs = Date.now() - aggregateStartedAt;
writeAggregate();
console.log(
  `[e2e] controlled stateful status=${aggregate.status} scenarios=${aggregate.scenarios.length} evidence=${aggregateRoot}`,
);
process.exit(aggregate.status === 'passed' ? 0 : 1);

async function runScenario(scenario, index) {
  const startedAt = Date.now();
  const runId = randomBytes(16).toString('hex');
  const childName = `child-${String(index + 1).padStart(3, '0')}`;
  const scenarioDirectory = resolve(aggregateRoot, childName);
  const evidenceDirectory = resolve(scenarioDirectory, 'evidence');
  const applicationLog = resolve(evidenceDirectory, 'application.log');
  const apiProcessReceipt = resolve(scenarioDirectory, 'api-process.json');
  const privateRuntime = createPrivatePlaywrightRuntime(evidenceDirectory);
  const result = {
    runId,
    selector: scenario.selector,
    policy: scenario.policy,
    fixtureProfile: scenario.fixtureProfile,
    backgroundActivities: scenario.backgroundActivities,
    evidence: `${childName}/evidence/playwright-results.json`,
    status: 'failed',
    startedAt: new Date(startedAt).toISOString(),
    completedAt: null,
    durationMs: null,
    phases: [],
    cleanup: scenario.policy === 'mutating' ? 'pending' : 'not-required',
    inspection: null,
  };
  let keeper;
  let environment;
  let fingerprint;
  let childAttempted = false;
  let apiIdentityVerified = false;
  let initialResetCompleted = false;

  try {
    const controlledEnvironment = createControlledPlaywrightEnvironment(
      process.env,
      provisioningReceipt,
      privateRuntime,
    );
    environment = buildStatefulPlaywrightEnvironment(controlledEnvironment, scenario, {
      apiProcessReceipt,
      backendAssembly: controlledEnvironment.E2E_BACKEND_ASSEMBLY,
      evidenceDirectory,
      playwrightOutputDirectory: privateRuntime.playwrightOutputDirectory,
      runId,
    });
    const lockStartedAt = Date.now();
    keeper = await startKeeper(
      scenario.policy === 'mutating' ? 'exclusive' : 'shared',
      runId,
      environment,
      applicationLog,
    );
    result.phases.push({
      name: 'lockAcquisition',
      status: 'passed',
      durationMs: Date.now() - lockStartedAt,
    });
    if (scenario.policy === 'mutating') {
      const resetStartedAt = Date.now();
      fingerprint = protectedFingerprint(runTestRuntimeJson(['fingerprint'], environment, applicationLog));
      runTestRuntimeJson(buildMutableResetArguments({
        apiProcessId: null,
        expectedFingerprint: fingerprint,
        phase: 'initial',
        runId,
      }), environment, applicationLog);
      initialResetCompleted = true;
      result.phases.push({
        name: 'initialReset',
        status: 'passed',
        durationMs: Date.now() - resetStartedAt,
      });
    }

    if (interruptionSignal) {
      throw new Error(`Stateful Playwright was interrupted by ${interruptionSignal}.`);
    }
    childAttempted = true;
    const playwrightArguments = ['test', scenario.selector, '--workers=1'];
    if (interactiveMode === '--headed') playwrightArguments.push('--headed');
    if (interactiveMode === '--ui') playwrightArguments.push('--ui');
    const childStartedAt = Date.now();
    const child = await runPlaywrightChild(
      playwrightArguments,
      environment,
      keeper,
      applicationLog,
    );
    const childCompletedAt = Date.now();
    let playwrightResult;
    let playwrightEvidenceError;
    try {
      playwrightResult = validatePlaywrightChildResult(readPlaywrightResult(evidenceDirectory), {
        declaredTestCount: 1,
        runId,
      });
      appendChildExecutionPhases(result.phases, playwrightResult, childStartedAt, childCompletedAt);
    } catch (error) {
      playwrightEvidenceError = error;
    }

    const apiReceipt = readApiReceipt(apiProcessReceipt);
    const apiProcessId = validateApiProcessReceipt(apiReceipt, STATEFUL_API_PORT);
    apiIdentityVerified = true;
    let finalResetPhase;
    if (scenario.policy === 'mutating') {
      const resetStartedAt = Date.now();
      runTestRuntimeJson(buildMutableResetArguments({
        apiProcessId,
        expectedFingerprint: fingerprint,
        phase: 'final',
        runId,
      }), environment, applicationLog);
      result.cleanup = 'passed';
      finalResetPhase = {
        name: 'finalReset',
        status: 'passed',
        durationMs: Date.now() - resetStartedAt,
      };
    } else {
      await assertApiStopped(apiProcessId, STATEFUL_API_PORT);
    }
    appendApplicationShutdownPhase(
      result.phases,
      playwrightResult ?? { completedAt: new Date(childCompletedAt).toISOString() },
      childCompletedAt,
      'passed',
    );
    if (finalResetPhase) result.phases.push(finalResetPhase);

    if (child.keeperLost) {
      throw new Error('TestRuntime keeper exited while the Playwright child was still running.');
    }
    if (interruptionSignal) {
      throw new Error(`Stateful Playwright was interrupted by ${interruptionSignal}.`);
    }

    if (playwrightEvidenceError) throw playwrightEvidenceError;
    if (child.exitCode !== 0 || playwrightResult.status !== 'passed') {
      throw new Error(`Playwright child failed with status ${child.exitCode}.`);
    }
    result.status = 'passed';
  } catch (error) {
    result.error = sanitizeError(error, secretValues);
    appendMissingChildFailurePhases(result.phases);
    appendFileSync(applicationLog, `${result.error}\n`, { encoding: 'utf8', mode: 0o600 });
    if (scenario.policy === 'mutating' && childAttempted && result.cleanup === 'pending') {
      result.cleanup = apiIdentityVerified ? 'failed' : 'refused-unverified-api';
    } else if (scenario.policy === 'mutating' && initialResetCompleted && result.cleanup === 'pending') {
      result.cleanup = 'passed-no-api-started';
    }
    console.error(`[e2e] ${scenario.selector}: ${result.error}`);
  } finally {
    privateRuntime.cleanup();
    rmSync(apiProcessReceipt, { force: true });
    if (keeper) {
      const releaseStartedAt = Date.now();
      if (!keeper.stdin.destroyed) keeper.stdin.end();
      const keeperStatus = await waitForExit(keeper);
      result.phases.push({
        name: 'lockRelease',
        status: keeperStatus === 0 ? 'passed' : 'failed',
        durationMs: Date.now() - releaseStartedAt,
      });
      if (keeperStatus !== 0 && result.status === 'passed') {
        result.status = 'failed';
        result.error = `TestRuntime keeper exited with status ${keeperStatus}.`;
      }
    }
    appendMissingChildFailurePhases(result.phases, [
      'lockAcquisition',
      ...(scenario.policy === 'mutating' ? ['initialReset', 'finalReset'] : []),
      'applicationStartup',
      'testExecution',
      'applicationShutdown',
      'lockRelease',
    ]);
    result.inspection = inspectRetainedEvidence(evidenceDirectory, secretValues);
    if (result.inspection.status !== 'passed') result.status = 'failed';
    if (result.status === 'passed') rmSync(applicationLog, { force: true });
    result.completedAt = new Date().toISOString();
    result.durationMs = Date.now() - startedAt;
  }
  return result;
}

function runTestRuntimeJson(arguments_, environment, logPath) {
  const result = spawnSync('dotnet', [environment.E2E_TEST_RUNTIME_ASSEMBLY, ...arguments_], {
    cwd: repositoryRoot,
    encoding: 'utf8',
    env: environment,
  });
  recordSanitizedText(result.stdout, logPath, secretValues);
  recordSanitizedText(result.stderr, logPath, secretValues);
  if (result.error) throw result.error;
  let report;
  try {
    report = JSON.parse(result.stdout);
  } catch (error) {
    throw new Error(`TestRuntime ${arguments_[0]} returned invalid JSON: ${error.message}`);
  }
  if (result.status !== 0 || report.succeeded !== true) {
    throw new Error(`TestRuntime ${arguments_[0]} failed with status ${result.status ?? 1}.`);
  }
  return report;
}

function runPlaywrightChild(arguments_, environment, keeper, logPath) {
  return new Promise((resolveChild, rejectChild) => {
    let keeperLost = false;
    let settled = false;
    const child = spawn(playwright, arguments_, {
      cwd: frontendRoot,
      detached: true,
      env: environment,
      stdio: ['ignore', 'pipe', 'pipe'],
    });
    activePlaywrightChild = child;
    captureSanitizedStream(child.stdout, { logPath, secretValues });
    captureSanitizedStream(child.stderr, { logPath, secretValues });

    const finish = (callback) => {
      if (settled) return;
      settled = true;
      activePlaywrightChild = null;
      keeper.off('close', onKeeperClose);
      callback();
    };
    const onKeeperClose = () => {
      keeperLost = true;
      terminatePlaywrightTree(child, 'SIGTERM');
    };
    keeper.once('close', onKeeperClose);
    if (keeper.exitCode !== null || keeper.signalCode !== null) onKeeperClose();

    child.once('error', (error) => finish(() => rejectChild(error)));
    child.once('close', (code, signal) => finish(() => resolveChild({
      exitCode: code ?? (signal ? 1 : 0),
      keeperLost,
    })));
  });
}

function requestShutdown(signal) {
  interruptionSignal ??= signal;
  if (activePlaywrightChild) terminatePlaywrightTree(activePlaywrightChild, 'SIGTERM');
}

function terminatePlaywrightTree(child, signal) {
  if (child.exitCode !== null || child.signalCode !== null || !child.pid) return;
  try {
    process.kill(-child.pid, signal);
  } catch (error) {
    if (error?.code !== 'ESRCH') child.kill(signal);
  }
}

function protectedFingerprint(report) {
  const fingerprint = report.protectedStateFingerprint?.fingerprint;
  if (!/^[a-f0-9]{64}$/i.test(fingerprint ?? '')) {
    throw new Error('TestRuntime fingerprint returned no verified Protected State fingerprint.');
  }
  return fingerprint;
}

async function startKeeper(lockMode, runId, environment, logPath) {
  const keeper = spawn('dotnet', [
    environment.E2E_TEST_RUNTIME_ASSEMBLY,
    'lock',
    'hold',
    '--mode',
    lockMode,
    '--run-id',
    runId,
    '--command',
    STATEFUL_LOCK_COMMAND,
    '--release-on-stdin-close',
  ], {
    cwd: repositoryRoot,
    detached: true,
    env: environment,
    stdio: ['pipe', 'pipe', 'pipe'],
  });
  captureSanitizedStream(keeper.stderr, { logPath, secretValues });
  const firstLine = await readFirstLine(keeper.stdout, keeper);
  if (firstLine === null) {
    await waitForExit(keeper);
    throw new Error('TestRuntime keeper returned no acquisition evidence.');
  }
  recordSanitizedText(firstLine, logPath, secretValues);
  let report;
  try {
    report = JSON.parse(firstLine);
  } catch {
    keeper.stdin.end();
    await waitForExit(keeper);
    throw new Error('TestRuntime keeper returned invalid JSON.');
  }
  if (!report.succeeded || report.advisoryLock?.status !== 'acquired') {
    keeper.stdin.end();
    await waitForExit(keeper);
    throw new Error(`TestRuntime ${lockMode} keeper did not acquire its lock.`);
  }
  captureSanitizedStream(keeper.stdout, { logPath, secretValues });
  return keeper;
}

function readFirstLine(stream, child) {
  return new Promise((resolveLine) => {
    let buffered = '';
    const onData = (chunk) => {
      buffered += chunk.toString();
      const newline = buffered.indexOf('\n');
      if (newline >= 0) {
        cleanup();
        resolveLine(buffered.slice(0, newline));
      }
    };
    const onClose = () => {
      cleanup();
      resolveLine(buffered.length > 0 ? buffered : null);
    };
    const cleanup = () => {
      stream.off('data', onData);
      child.off('close', onClose);
    };
    stream.on('data', onData);
    child.on('close', onClose);
  });
}

function waitForExit(child) {
  if (child.exitCode !== null) return Promise.resolve(child.exitCode);
  return new Promise((resolveExit) => child.once('close', (code) => resolveExit(code ?? 1)));
}

function readApiReceipt(path) {
  try {
    return JSON.parse(readFileSync(path, 'utf8'));
  } catch {
    return null;
  }
}

function readPlaywrightResult(evidenceDirectory) {
  try {
    return JSON.parse(readFileSync(resolve(evidenceDirectory, 'playwright-results.json'), 'utf8'));
  } catch {
    throw new Error('Playwright child returned no valid structured result.');
  }
}

async function assertApiStopped(processId, port) {
  try {
    process.kill(processId, 0);
    throw new Error(`API process ${processId} is still live after the guarded-read child.`);
  } catch (error) {
    if (error?.code !== 'ESRCH') throw error;
  }
  await new Promise((resolveFree, rejectUnverified) => {
    const socket = createConnection({ host: '127.0.0.1', port });
    socket.setTimeout(1_000);
    socket.once('connect', () => {
      socket.destroy();
      rejectUnverified(new Error(`API port ${port} remains occupied after the guarded-read child.`));
    });
    socket.once('error', (error) => {
      socket.destroy();
      if (error.code === 'ECONNREFUSED') resolveFree();
      else rejectUnverified(error);
    });
    socket.once('timeout', () => {
      socket.destroy();
      rejectUnverified(new Error(`API port ${port} could not be proven free.`));
    });
  });
}

function writeAggregate() {
  writeJson(resolve(aggregateRoot, 'stateful-results.json'), aggregate);
}

function resolveAggregateRoot(runId) {
  const aggregateDirectory = process.env.QDB_PLAYWRIGHT_AGGREGATE_DIRECTORY?.trim();
  if (aggregateDirectory) return resolve(aggregateDirectory, 'stateful');
  const observationDirectory = process.env.QDB_PR_OBSERVATION_RESULT_DIR?.trim();
  const evidenceRoot = observationDirectory
    ? resolve(observationDirectory, 'playwright-evidence')
    : resolve(frontendRoot, '.playwright/evidence');
  return resolve(evidenceRoot, runId);
}

function validateArguments(selectedMode, selectedSelector, selectedInteractiveMode, extras) {
  if (!['--critical', '--focused', '--full'].includes(selectedMode)) {
    throw new Error('Use --critical, --focused, or --full for stateful Playwright execution.');
  }
  if (selectedMode === '--focused') {
    if (!selectedSelector || extras.length > 0) {
      throw new Error('--focused requires exactly one Playwright file:line selector.');
    }
    if (selectedInteractiveMode !== undefined && !['--headed', '--ui'].includes(selectedInteractiveMode)) {
      throw new Error('Focused Playwright supports only --headed or --ui interactive mode.');
    }
  } else if (selectedSelector !== undefined || selectedInteractiveMode !== undefined || extras.length > 0) {
    throw new Error(`${selectedMode} does not accept a Playwright selector or interactive mode.`);
  }
}

function requireTestDatabaseConnection() {
  if (!process.env.ConnectionStrings__QuranDashboardTest?.trim()) {
    throw new Error(
      'Controlled stateful Playwright execution requires ConnectionStrings__QuranDashboardTest.',
    );
  }
}
