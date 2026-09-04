import { spawn, spawnSync } from 'node:child_process';
import { randomBytes } from 'node:crypto';
import {
  chmodSync,
  existsSync,
  mkdirSync,
  mkdtempSync,
  readFileSync,
  rmSync,
  writeFileSync,
} from 'node:fs';
import { createConnection } from 'node:net';
import { dirname, resolve } from 'node:path';
import { tmpdir } from 'node:os';
import { fileURLToPath } from 'node:url';

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
const testRuntime = resolve(
  repositoryRoot,
  'Backend/tools/QuranDashboard.TestRuntime/bin/Debug/net10.0/QuranDashboard.TestRuntime.dll',
);
const backendAssembly = resolve(
  repositoryRoot,
  'Backend/api/QuranDashboard.Api/bin/Debug/net10.0/QuranDashboard.Api.dll',
);
const [mode = '--full', selector, interactiveMode, ...extraArguments] = process.argv.slice(2);

validateArguments(mode, selector, interactiveMode, extraArguments);
if (!existsSync(testRuntime)) {
  throw new Error(`Stateful Playwright execution requires built TestRuntime output: ${testRuntime}`);
}
if (!existsSync(backendAssembly)) {
  throw new Error(`Stateful Playwright execution requires built Backend output: ${backendAssembly}`);
}

const scenarios = mode === '--critical'
  ? selectStatefulCriticalJourneys(discover('./scripts/discover-playwright-journeys.mjs'))
  : selectStatefulPlaywrightTests(
      discover('./scripts/discover-playwright-policies.mjs'),
      mode === '--focused' ? selector : undefined,
    );
if (scenarios.length === 0) {
  throw new Error('Stateful Playwright execution discovered no guarded-read or mutating tests.');
}

const aggregateRunId = `stateful-${Date.now()}-${process.pid}`;
const aggregateRoot = resolve(frontendRoot, '.playwright/evidence', aggregateRunId);
mkdirSync(aggregateRoot, { recursive: true, mode: 0o700 });
chmodSync(aggregateRoot, 0o700);
const aggregate = {
  schemaVersion: 1,
  runId: aggregateRunId,
  status: 'passed',
  startedAt: new Date().toISOString(),
  completedAt: null,
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
  writeAggregate();
  if (result.status !== 'passed') {
    aggregate.status = 'failed';
    break;
  }
}
aggregate.completedAt = new Date().toISOString();
writeAggregate();
console.log(
  `[e2e] stateful execution status=${aggregate.status} scenarios=${aggregate.scenarios.length} evidence=${aggregateRoot}`,
);
process.exit(aggregate.status === 'passed' ? 0 : 1);

async function runScenario(scenario, index) {
  const runId = randomBytes(16).toString('hex');
  const scenarioDirectory = resolve(aggregateRoot, `child-${String(index + 1).padStart(3, '0')}`);
  const evidenceDirectory = resolve(scenarioDirectory, 'evidence');
  const apiProcessReceipt = resolve(scenarioDirectory, 'api-process.json');
  const playwrightOutputDirectory = mkdtempSync(resolve(tmpdir(), 'qdb-stateful-playwright-'));
  mkdirSync(evidenceDirectory, { recursive: true, mode: 0o700 });
  chmodSync(scenarioDirectory, 0o700);
  chmodSync(evidenceDirectory, 0o700);
  chmodSync(playwrightOutputDirectory, 0o700);
  const result = {
    selector: scenario.selector,
    policy: scenario.policy,
    fixtureProfile: scenario.fixtureProfile,
    backgroundActivities: scenario.backgroundActivities,
    evidence: `child-${String(index + 1).padStart(3, '0')}/evidence/playwright-results.json`,
    status: 'failed',
    cleanup: scenario.policy === 'mutating' ? 'pending' : 'not-required',
  };
  let keeper;
  let environment;
  let fingerprint;
  let childAttempted = false;
  let apiIdentityVerified = false;
  let initialResetCompleted = false;

  try {
    environment = buildStatefulPlaywrightEnvironment(process.env, scenario, {
      apiProcessReceipt,
      backendAssembly,
      evidenceDirectory,
      playwrightOutputDirectory,
      runId,
    });
    keeper = await startKeeper(scenario.policy === 'mutating' ? 'exclusive' : 'shared', runId);
    if (scenario.policy === 'mutating') {
      fingerprint = protectedFingerprint(runTestRuntimeJson(['fingerprint'], environment));
      runTestRuntimeJson(buildMutableResetArguments({
        apiProcessId: null,
        expectedFingerprint: fingerprint,
        phase: 'initial',
        runId,
      }), environment);
      initialResetCompleted = true;
    }

    if (interruptionSignal) {
      throw new Error(`Stateful Playwright was interrupted by ${interruptionSignal}.`);
    }
    childAttempted = true;
    const playwrightArguments = ['test', scenario.selector, '--workers=1'];
    if (interactiveMode === '--headed') playwrightArguments.push('--headed');
    if (interactiveMode === '--ui') playwrightArguments.push('--ui');
    const child = await runPlaywrightChild(playwrightArguments, environment, keeper);

    const receipt = readApiReceipt(apiProcessReceipt);
    const apiProcessId = validateApiProcessReceipt(receipt, STATEFUL_API_PORT);
    apiIdentityVerified = true;
    if (scenario.policy === 'mutating') {
      runTestRuntimeJson(buildMutableResetArguments({
        apiProcessId,
        expectedFingerprint: fingerprint,
        phase: 'final',
        runId,
      }), environment);
      result.cleanup = 'passed';
    } else {
      await assertApiStopped(apiProcessId, STATEFUL_API_PORT);
    }

    if (child.keeperLost) {
      throw new Error('TestRuntime keeper exited while the Playwright child was still running.');
    }
    if (interruptionSignal) {
      throw new Error(`Stateful Playwright was interrupted by ${interruptionSignal}.`);
    }

    const playwrightResult = readPlaywrightResult(evidenceDirectory);
    if (child.exitCode !== 0 || playwrightResult.status !== 'passed') {
      throw new Error(`Playwright child failed with status ${child.exitCode}.`);
    }
    if (playwrightResult.declaredTestCount !== 1) {
      throw new Error(
        `Exact Playwright child ${scenario.selector} declared ${playwrightResult.declaredTestCount} tests.`,
      );
    }
    result.status = 'passed';
  } catch (error) {
    result.error = error instanceof Error ? error.message : 'Stateful Playwright scenario failed.';
    if (scenario.policy === 'mutating' && childAttempted && result.cleanup === 'pending') {
      result.cleanup = apiIdentityVerified ? 'failed' : 'refused-unverified-api';
    } else if (scenario.policy === 'mutating' && initialResetCompleted && result.cleanup === 'pending') {
      result.cleanup = 'passed-no-api-started';
    }
    console.error(`[e2e] ${scenario.selector}: ${result.error}`);
  } finally {
    rmSync(playwrightOutputDirectory, { recursive: true, force: true });
    if (keeper) {
      if (!keeper.stdin.destroyed) keeper.stdin.end();
      const keeperStatus = await waitForExit(keeper);
      if (keeperStatus !== 0 && result.status === 'passed') {
        result.status = 'failed';
        result.error = `TestRuntime keeper exited with status ${keeperStatus}.`;
      }
    }
  }
  return result;
}

function discover(reporter) {
  const result = spawnSync(
    playwright,
    ['test', '--list', `--reporter=${reporter}`],
    { cwd: frontendRoot, encoding: 'utf8', env: process.env, maxBuffer: 10 * 1024 * 1024 },
  );
  if (result.error) throw result.error;
  if (result.status !== 0) {
    process.stderr.write(result.stderr ?? '');
    process.exit(result.status ?? 1);
  }
  try {
    return JSON.parse(result.stdout);
  } catch (error) {
    throw new Error(`Playwright discovery returned invalid JSON: ${error.message}`);
  }
}

function runTestRuntimeJson(arguments_, environment) {
  const result = spawnSync('dotnet', [testRuntime, ...arguments_], {
    cwd: repositoryRoot,
    encoding: 'utf8',
    env: environment,
  });
  if (result.stdout) process.stdout.write(result.stdout);
  if (result.stderr) process.stderr.write(result.stderr);
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

function runPlaywrightChild(arguments_, environment, keeper) {
  return new Promise((resolveChild, rejectChild) => {
    let keeperLost = false;
    let settled = false;
    const child = spawn(playwright, arguments_, {
      cwd: frontendRoot,
      detached: true,
      env: environment,
      stdio: 'inherit',
    });
    activePlaywrightChild = child;

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

async function startKeeper(lockMode, runId) {
  const keeper = spawn('dotnet', [
    testRuntime,
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
    env: process.env,
    stdio: ['pipe', 'pipe', 'pipe'],
  });
  keeper.stderr.pipe(process.stderr);
  const firstLine = await readFirstLine(keeper.stdout, keeper);
  if (firstLine === null) {
    await waitForExit(keeper);
    throw new Error('TestRuntime keeper returned no acquisition evidence.');
  }
  process.stdout.write(`${firstLine}\n`);
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
  keeper.stdout.pipe(process.stdout);
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
  writeFileSync(
    resolve(aggregateRoot, 'stateful-results.json'),
    `${JSON.stringify(aggregate, null, 2)}\n`,
    { encoding: 'utf8', mode: 0o600 },
  );
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
