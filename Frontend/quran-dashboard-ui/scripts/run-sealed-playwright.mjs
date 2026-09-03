import { execFileSync, spawn, spawnSync } from 'node:child_process';
import {
  appendFileSync,
  chmodSync,
  copyFileSync,
  mkdtempSync,
  mkdirSync,
  readFileSync,
  readdirSync,
  rmSync,
  statSync,
  writeFileSync,
} from 'node:fs';
import { tmpdir } from 'node:os';
import { dirname, join, relative, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

import { COMPACT_ARTIFACT_IDS } from '../e2e/harness/artifact-contract.mjs';
import {
  provisionDatabaseRuntime,
  removeDatabaseRuntimeState,
  writeDatabaseRuntimeState,
} from '../e2e/harness/database-runtime.mjs';
import {
  findFiles,
  harnessSourceFiles,
  sha256Files,
  sha256Path,
} from '../e2e/harness/provisioning-integrity.mjs';
import {
  createCredentialFreeEnvironment,
  createSealedEnvironment,
  redactDiagnosticText,
  sensitiveEnvironmentValues,
  validateProvisioningReceipt,
} from '../e2e/harness/sealed-execution-contract.mjs';
import {
  APPROVED_DIAGNOSTIC_FILES,
  validateRetainedDiagnostic,
} from './structured-playwright-reporter.mjs';

const SCRIPT_PATH = fileURLToPath(import.meta.url);
const FRONTEND_ROOT = resolve(dirname(SCRIPT_PATH), '..');
const REPOSITORY_ROOT = resolve(FRONTEND_ROOT, '../..');
const RECEIPT_PATH = resolve(FRONTEND_ROOT, '.playwright/provisioning/receipt.json');
const observationResultsDirectory = process.env.QDB_PR_OBSERVATION_RESULT_DIR?.trim();
const EVIDENCE_ROOT = observationResultsDirectory
  ? resolve(observationResultsDirectory, 'playwright-evidence')
  : resolve(FRONTEND_ROOT, '.playwright/evidence');
const API_PROJECT = resolve(
  REPOSITORY_ROOT,
  'Backend/api/QuranDashboard.Api/QuranDashboard.Api.csproj',
);
const [mode = '--critical', focusedSelector, ...unexpectedArguments] = process.argv.slice(2);
if (!['--critical', '--focused', '--full'].includes(mode)) {
  throw new Error('Use --critical, --focused, or --full for sealed Playwright execution.');
}
if (mode === '--focused') {
  validateFocusedSelector(focusedSelector, unexpectedArguments);
} else if (focusedSelector !== undefined || unexpectedArguments.length > 0) {
  throw new Error(`${mode} does not accept a Playwright selector.`);
}

if (process.env.E2E_ORCHESTRATOR_GUARDED !== '1') {
  const receipt = readProvisioningReceipt();
  if (
    !isFileOrDirectory(receipt.outputs.egressGuard)
    || sha256Path(receipt.outputs.egressGuard) !== receipt.outputSha256.egressGuard
  ) {
    throw new Error('The provisioned egress guard is missing or changed; provision again.');
  }
  const guardedEnvironment = createCredentialFreeEnvironment(process.env);
  mkdirSync(resolve(FRONTEND_ROOT, '.playwright'), { recursive: true });
  const sealedHome = mkdtempSync(resolve(FRONTEND_ROOT, '.playwright/sealed-home-'));
  Object.assign(guardedEnvironment, {
    CI: '1',
    E2E_DATABASE_MODE: 'artifact',
    E2E_ORCHESTRATOR_GUARDED: '1',
    E2E_SEALED_EXECUTION: '1',
    HOME: sealedHome,
    LD_PRELOAD: receipt.outputs.egressGuard,
    QDB_E2E_ALLOWED_IPV4: '192.0.2.2',
    XDG_CACHE_HOME: resolve(sealedHome, '.cache'),
    XDG_CONFIG_HOME: resolve(sealedHome, '.config'),
    XDG_DATA_HOME: resolve(sealedHome, '.local/share'),
  });
  let guarded;
  try {
    guarded = spawnSync(
      process.execPath,
      [SCRIPT_PATH, mode, ...(focusedSelector ? [focusedSelector] : [])],
      {
      cwd: FRONTEND_ROOT,
      env: guardedEnvironment,
      stdio: 'inherit',
      },
    );
  } finally {
    rmSync(sealedHome, { recursive: true, force: true });
  }
  if (guarded.error) throw guarded.error;
  process.exit(guarded.status ?? 1);
}

const runId = `${new Date().toISOString().replaceAll(/[-:.]/g, '')}-${process.pid}`;
const evidenceDirectory = resolve(EVIDENCE_ROOT, runId);
const executionLog = resolve(evidenceDirectory, 'application.log');
const playwrightOutputDirectory = mkdtempSync(resolve(tmpdir(), 'qdb-e2e-playwright-output-'));
chmodSync(playwrightOutputDirectory, 0o700);
const receiptCopy = resolve(evidenceDirectory, 'provisioning-receipt.json');
const secretValues = sensitiveEnvironmentValues(process.env);
const executionStartedAt = new Date();
const results = {
  schemaVersion: 1,
  runId,
  status: 'failed',
  startedAt: executionStartedAt.toISOString(),
  completedAt: undefined,
  durationMs: undefined,
  retention: {
    failedDiagnosticsDays: 14,
    aggregateTimingDays: 30,
  },
  phases: [],
};

mkdirSync(evidenceDirectory, { recursive: true });
chmodSync(evidenceDirectory, 0o700);
let databaseRuntime;
let exitCode = 1;

try {
  const receipt = readProvisioningReceipt();
  validateReceiptAgainstWorkspace(receipt);
  copyFileSync(RECEIPT_PATH, receiptCopy);
  chmodSync(receiptCopy, 0o600);
  results.provisioningReceipt = 'provisioning-receipt.json';
  results.provisioningReceiptSha256 = sha256Path(receiptCopy);
  results.phases.push(copyProvisioningPhase(receipt, 'artifactProvisioning'));

  process.env.E2E_SEALED_EXECUTION = '1';
  process.env.E2E_DATABASE_MODE = 'artifact';
  process.env.E2E_ARTIFACT_VERIFIER_ASSEMBLY = resolve(
    receipt.outputs.artifactVerifierOutput,
    'QuranDashboard.TestArtifacts.dll',
  );

  const databaseStartedAt = Date.now();
  databaseRuntime = await provisionDatabaseRuntime(API_PROJECT);
  secretValues.push(databaseRuntime.connection.password);
  results.phases.push({
    name: 'databasePreparation',
    status: 'passed',
    durationMs: Date.now() - databaseStartedAt,
  });
  writeDatabaseRuntimeState(databaseRuntime);

  const environment = createSealedEnvironment(process.env, {
    chromiumExecutable: receipt.outputs.chromiumExecutable,
    databaseHost: databaseRuntime.connection.host,
    egressGuard: receipt.outputs.egressGuard,
    evidenceDirectory,
    playwrightOutputDirectory,
    tlsCertificate: receipt.outputs.tlsCertificate,
    tlsPrivateKey: receipt.outputs.tlsPrivateKey,
  });
  environment.E2E_FRONTEND_BUILD = receipt.outputs.frontendBuild;
  environment.E2E_BACKEND_ASSEMBLY = resolve(
    receipt.outputs.backendOutput,
    'QuranDashboard.Api.dll',
  );

  const childStartedAt = new Date();
  const playwright = resolve(FRONTEND_ROOT, 'node_modules/.bin/playwright');
  const command = mode === '--critical'
    ? [process.execPath, [resolve(FRONTEND_ROOT, 'scripts/run-critical-playwright-journeys.mjs')]]
    : mode === '--focused'
      ? [playwright, ['test', focusedSelector, '--workers=1']]
      : [playwright, ['test', '--workers=1']];
  const child = await runWithSanitizedOutput(command[0], command[1], environment, executionLog);
  exitCode = child.exitCode;

  const playwrightResults = readPlaywrightResults();
  const applicationsReadyAt = playwrightResults?.declaredTestCount > 0
    ? new Date(playwrightResults.applicationsReadyAt)
    : undefined;
  const completedAt = playwrightResults ? new Date(playwrightResults.completedAt) : new Date();
  results.phases.push({
    name: 'applicationStartup',
    status: applicationsReadyAt ? 'passed' : 'failed',
    durationMs: applicationsReadyAt
      ? applicationsReadyAt.getTime() - childStartedAt.getTime()
      : completedAt.getTime() - childStartedAt.getTime(),
  });
  results.phases.push({
    name: 'testExecution',
    status: playwrightResults?.status ?? 'failed',
    durationMs: applicationsReadyAt
      ? completedAt.getTime() - applicationsReadyAt.getTime()
      : 0,
  });
  results.status = child.exitCode === 0 && playwrightResults?.status === 'passed'
    ? 'passed'
    : 'failed';

  if (results.status === 'passed') {
    rmSync(executionLog, { force: true });
  } else {
    captureContainerLogs(databaseRuntime.containerName, evidenceDirectory);
  }
} catch (error) {
  const message = redactDiagnosticText(
    error instanceof Error ? error.stack ?? error.message : 'Sealed execution failed.',
    secretValues,
  );
  appendFileSync(executionLog, `${message}\n`, { encoding: 'utf8', mode: 0o600 });
  console.error(message);
  if (!results.phases.some((phase) => phase.name === 'databasePreparation')) {
    results.phases.push({ name: 'databasePreparation', status: 'failed', durationMs: 0 });
  }
  captureContainerLogs(databaseRuntime?.containerName, evidenceDirectory);
} finally {
  databaseRuntime?.cleanup();
  removeDatabaseRuntimeState();
  rmSync(playwrightOutputDirectory, { recursive: true, force: true });
  results.completedAt = new Date().toISOString();
  results.durationMs = Date.now() - executionStartedAt.getTime();
  writeJson(resolve(evidenceDirectory, 'structured-results.json'), results);
  const inspection = inspectEvidence(evidenceDirectory);
  if (inspection.status !== 'passed') {
    results.status = 'failed';
    writeJson(resolve(evidenceDirectory, 'structured-results.json'), results);
  }
  writeJson(resolve(evidenceDirectory, 'evidence-manifest.json'), {
    schemaVersion: 1,
    runId,
    status: results.status,
    containsDatabaseDump: false,
    capturesRequestHeaders: false,
    capturesRequestBodies: false,
    traceFormat: 'sanitized-step-events-v1',
    screenshotPolicy: 'text-media-masked-v1',
    inspection,
    files: [...listRelativeFiles(evidenceDirectory), 'evidence-manifest.json'].sort(),
  });
}

console.log(
  `[e2e] sealed execution status=${results.status} durationMs=${results.durationMs} evidence=${evidenceDirectory}`,
);
process.exit(results.status === 'passed' ? 0 : exitCode || 1);

function validateFocusedSelector(selector, extraArguments) {
  if (!selector || extraArguments.length > 0) {
    throw new Error('--focused requires exactly one Playwright file:line selector.');
  }
  const match = /^(e2e\/[^:]+\.e2e\.ts):([1-9][0-9]*)$/.exec(selector);
  if (!match || match[1].includes('\\') || match[1].split('/').includes('..')) {
    throw new Error(`Invalid focused Playwright selector: ${selector}`);
  }
}

function readProvisioningReceipt() {
  let receipt;
  try {
    receipt = JSON.parse(readFileSync(RECEIPT_PATH, 'utf8'));
  } catch {
    throw new Error('Run npm run e2e:provision before sealed Playwright execution.');
  }
  validateProvisioningReceipt(receipt);
  return receipt;
}

function validateReceiptAgainstWorkspace(receipt) {
  const npmLock = resolve(FRONTEND_ROOT, 'package-lock.json');
  const nugetLocks = findFiles(resolve(REPOSITORY_ROOT, 'Backend'), 'packages.lock.json');
  const artifactLock = resolve(REPOSITORY_ROOT, 'test-artifacts.lock.json');
  const expected = {
    npmLockSha256: sha256Files([npmLock], REPOSITORY_ROOT),
    nugetLocksSha256: sha256Files(nugetLocks, REPOSITORY_ROOT),
    artifactLockSha256: sha256Files([artifactLock], REPOSITORY_ROOT),
    harnessSourceSha256: sha256Files(harnessSourceFiles(FRONTEND_ROOT), REPOSITORY_ROOT),
  };
  for (const [name, value] of Object.entries(expected)) {
    if (receipt.inputs[name] !== value) {
      throw new Error(`Provisioning receipt input ${name} is stale; provision again.`);
    }
  }

  for (const [name, path] of Object.entries(receipt.outputs)) {
    if (!isFileOrDirectory(path)) {
      throw new Error(`Provisioned output ${name} is missing; provision again.`);
    }
    if (sha256Path(path) !== receipt.outputSha256[name]) {
      throw new Error(`Provisioned output ${name} has changed; provision again.`);
    }
  }
  if ((statSync(receipt.outputs.tlsPrivateKey).mode & 0o077) !== 0) {
    throw new Error('The ephemeral TLS private key must not be accessible to group or other users.');
  }

  const browserLock = JSON.parse(
    readFileSync(resolve(FRONTEND_ROOT, 'node_modules/playwright-core/browsers.json'), 'utf8'),
  );
  const chromium = browserLock.browsers?.find((browser) => browser.name === 'chromium');
  if (chromium?.revision !== receipt.inputs.chromiumRevision) {
    throw new Error('The installed Playwright Chromium revision differs from provisioning.');
  }

  const lock = JSON.parse(readFileSync(artifactLock, 'utf8'));
  const artifacts = COMPACT_ARTIFACT_IDS.map((artifactId) => {
    const matches = lock.artifacts?.filter((entry) => entry.id === artifactId) ?? [];
    if (matches.length !== 1) {
      throw new Error(`Expected one locked compact artifact named ${artifactId}.`);
    }
    return matches[0];
  });
  if (artifacts.some(
    (artifact) => `postgres@${artifact.postgresql?.containerDigest}` !== receipt.inputs.postgresqlImage,
  )) {
    throw new Error('The provisioned PostgreSQL image differs from the artifact trust lock.');
  }
  execFileSync('docker', ['image', 'inspect', receipt.inputs.postgresqlImage], { stdio: 'ignore' });
}

function copyProvisioningPhase(receipt, name) {
  const phase = receipt.phases.find((candidate) => candidate.name === name);
  return { name, status: phase.status, durationMs: phase.durationMs };
}

function runWithSanitizedOutput(command, arguments_, environment, logPath) {
  return new Promise((resolvePromise, rejectPromise) => {
    const child = spawn(command, arguments_, {
      cwd: FRONTEND_ROOT,
      env: environment,
      stdio: ['ignore', 'pipe', 'pipe'],
    });
    const buffers = new Map([
      [child.stdout, ''],
      [child.stderr, ''],
    ]);

    for (const stream of buffers.keys()) {
      stream.setEncoding('utf8');
      stream.on('data', (chunk) => {
        let pending = `${buffers.get(stream)}${chunk}`;
        const lines = pending.split('\n');
        pending = lines.pop() ?? '';
        buffers.set(stream, pending);
        for (const line of lines) {
          recordSanitizedLine(line, logPath);
        }
      });
    }
    child.once('error', rejectPromise);
    child.once('close', (code) => {
      for (const pending of buffers.values()) {
        if (pending) recordSanitizedLine(pending, logPath);
      }
      resolvePromise({ exitCode: code ?? 1 });
    });
  });
}

function recordSanitizedLine(line, logPath) {
  const sanitized = redactDiagnosticText(line, secretValues);
  appendFileSync(logPath, `${sanitized}\n`, { encoding: 'utf8', mode: 0o600 });
  process.stdout.write(`${sanitized}\n`);
}

function readPlaywrightResults() {
  try {
    return JSON.parse(readFileSync(resolve(evidenceDirectory, 'playwright-results.json'), 'utf8'));
  } catch {
    return undefined;
  }
}

function captureContainerLogs(containerName, directory) {
  if (!containerName) return;
  let output;
  try {
    output = execFileSync('docker', ['logs', containerName], {
      encoding: 'utf8',
      maxBuffer: 10 * 1024 * 1024,
      stdio: ['ignore', 'pipe', 'pipe'],
    });
  } catch (error) {
    output = error instanceof Error ? error.message : 'Container log collection failed.';
  }
  writeFileSync(
    resolve(directory, 'container.log'),
    redactDiagnosticText(output, secretValues),
    { encoding: 'utf8', mode: 0o600 },
  );
}

function isFileOrDirectory(path) {
  try {
    const details = statSync(path);
    return details.isFile() || details.isDirectory();
  } catch {
    return false;
  }
}

function writeJson(path, value) {
  writeFileSync(path, `${JSON.stringify(value, null, 2)}\n`, {
    encoding: 'utf8',
    mode: 0o600,
  });
}

function inspectEvidence(directory) {
  let files = listRelativeFiles(directory);
  const removedRawFiles = files.filter((file) =>
    /\.(?:har|html|md|zip)$/i.test(file),
  );
  for (const file of removedRawFiles) rmSync(resolve(directory, file), { force: true });
  files = listRelativeFiles(directory);
  const rewritten = [];
  for (const file of files.filter((candidate) => /\.(?:json|log|txt)$/i.test(candidate))) {
    const path = resolve(directory, file);
    const content = readFileSync(path, 'utf8');
    const sanitized = redactDiagnosticText(content, secretValues);
    if (sanitized !== content) {
      writeFileSync(path, sanitized, { encoding: 'utf8', mode: 0o600 });
      rewritten.push(file);
    }
  }

  let unsafeScreenshot = false;
  try {
    const playwright = JSON.parse(
      readFileSync(resolve(directory, 'playwright-results.json'), 'utf8'),
    );
    unsafeScreenshot = playwright.tests?.some((test) =>
      test.attachments?.some(
        (attachment) => attachment.contentType === 'image/png'
          && attachment.name !== 'sanitized-screenshot',
      ),
    ) ?? false;
  } catch {
    // Startup failures can occur before Playwright creates a test result.
  }

  const invalidDiagnosticFiles = [];
  for (const file of files.filter((candidate) => candidate.startsWith('diagnostics/'))) {
    const parts = file.split('/');
    if (
      parts.length !== 3
      || !parts[1]
      || !APPROVED_DIAGNOSTIC_FILES.includes(parts[2])
    ) {
      invalidDiagnosticFiles.push(file);
      continue;
    }
    try {
      validateRetainedDiagnostic(parts[2], readFileSync(resolve(directory, file)));
    } catch {
      invalidDiagnosticFiles.push(file);
    }
  }

  return {
    status: !unsafeScreenshot && invalidDiagnosticFiles.length === 0 ? 'passed' : 'failed',
    invalidDiagnosticFiles,
    removedRawFiles,
    unsafeScreenshot,
    rewrittenTextFiles: rewritten,
  };
}

function listRelativeFiles(directory) {
  const files = [];
  for (const entry of readdirSync(directory, { withFileTypes: true })) {
    const path = join(directory, entry.name);
    if (entry.isDirectory()) files.push(...listRelativeFiles(path));
    else if (entry.isFile()) files.push(relative(evidenceDirectory, path));
  }
  return files.sort();
}
