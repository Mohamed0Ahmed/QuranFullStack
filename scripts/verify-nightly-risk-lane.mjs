import assert from 'node:assert/strict';
import { execFileSync, spawn } from 'node:child_process';
import { existsSync, mkdtempSync, readFileSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

import { loadNightlyRiskManifest } from './nightly-risk-contract.mjs';

const REPOSITORY_ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const MANIFEST_PATH = resolve(REPOSITORY_ROOT, 'nightly-risk-lane.json');
const RUNNER_PATH = resolve(REPOSITORY_ROOT, 'scripts/run-nightly-risk-lane.mjs');
const manifest = loadNightlyRiskManifest(MANIFEST_PATH, REPOSITORY_ROOT);

assert.equal(manifest.id, 'nightly-risk');
assert.equal(manifest.providerNeutral, true);
assert.equal(manifest.evidenceRetention.externalUploadConfigured, false);
assert.deepEqual(manifest.excludedWork, ['dependency-advisory-evaluation']);
assert.deepEqual(manifest.requiredBrowserJourneys, [
  'quran-fidelity.mushaf-mobile',
  'linking.successful-owner-mobile',
]);
assert.equal(command('full-chromium-suite').executable, 'npm');
assert.deepEqual(command('full-chromium-suite').arguments, ['run', 'e2e']);
assert.equal(command('full-chromium-suite').diagnosticRetry, true);
assert.deepEqual(command('phrase-index-build-activation').arguments, [
  'scripts/test', 'focused', '--backend-class',
  'QuranDashboard.Tests.Quran.PhraseSearch.PhraseIndexFullCanonicalRehearsalTests',
  '--no-build', '--authorize-full-data',
]);
assert.ok(command('abwab-snapshot-protections').arguments.includes('QuranDashboard.Tests.Abwab.AbwabSnapshotWorkflowTests'));
assert.ok(command('quran-topics-import-protections').arguments.includes('QuranDashboard.Tests.Quran.QuranTopicsBook.QuranTopicsBookImportTests'));
assert.equal(manifest.commands.some((entry) => JSON.stringify(entry).toLowerCase().includes('advisory')), false);

verifyDiagnosticRetryCannotPassTheLane();
verifySkippedMobileEvidenceCannotPassTheLane();
verifyProvisioningFailureCannotRetryBrowser();
verifyBrowserCleanupPrecedesDiagnosticAndDatabaseWork();
verifyUnsafeEvidenceIsRejectedAndRemoved();
verifyCleanupFailureBlocksDatabaseWorkAndRetry();
verifyTimeoutEscalatesBeforeCleanup();
verifyOrphanDescendantIsSweptBeforeCleanup();
verifyDiagnosticCleanupFailureCannotPassClassification();
await verifySignalCancellationCleansUp();

console.log('Nightly risk lane contract passed.');

function command(id) {
  const entry = manifest.commands.find((candidate) => candidate.id === id);
  assert.ok(entry, `missing command ${id}`);
  return entry;
}

function verifyDiagnosticRetryCannotPassTheLane() {
  const temporaryRoot = mkdtempSync(resolve(tmpdir(), 'qdb-nightly-risk-contract-'));
  const resultsDirectory = resolve(temporaryRoot, 'results');
  try {
    let exitCode = 0;
    try {
      execFileSync(process.execPath, [
        RUNNER_PATH,
        '--contract-test', 'diagnostic-retry',
        '--results-dir', resultsDirectory,
        '--diagnostic-retry',
      ], {
        cwd: REPOSITORY_ROOT,
        stdio: 'pipe',
      });
    } catch (error) {
      exitCode = error.status ?? 1;
    }
    const result = JSON.parse(readFileSync(resolve(resultsDirectory, 'nightly-risk-result.json'), 'utf8'));
    assert.equal(exitCode, 1, 'a passing diagnostic retry must not make the lane successful');
    assert.equal(result.status, 'failed');
    assert.equal(result.primary[0].status, 'failed');
    assert.equal(result.primary[0].exitCode, 7);
    assert.equal(result.diagnostic.classificationOnly, true);
    assert.equal(result.diagnostic.status, 'passed');
    assert.equal(result.diagnostic.result.exitCode, 0);
    let reuseExitCode = 0;
    try {
      execFileSync(process.execPath, [
        RUNNER_PATH,
        '--contract-test', 'diagnostic-retry',
        '--results-dir', resultsDirectory,
      ], {
        cwd: REPOSITORY_ROOT,
        stdio: 'pipe',
      });
    } catch (error) {
      reuseExitCode = error.status ?? 1;
    }
    assert.equal(reuseExitCode, 2, 'stale nightly results must be rejected');
  } finally {
    rmSync(temporaryRoot, { force: true, recursive: true });
  }
}

function verifySkippedMobileEvidenceCannotPassTheLane() {
  const temporaryRoot = mkdtempSync(resolve(tmpdir(), 'qdb-nightly-browser-evidence-'));
  const resultsDirectory = resolve(temporaryRoot, 'results');
  try {
    let exitCode = 0;
    try {
      execFileSync(process.execPath, [
        RUNNER_PATH,
        '--contract-test', 'skipped-mobile-evidence',
        '--results-dir', resultsDirectory,
      ], {
        cwd: REPOSITORY_ROOT,
        stdio: 'pipe',
      });
    } catch (error) {
      exitCode = error.status ?? 1;
    }
    const result = JSON.parse(readFileSync(resolve(resultsDirectory, 'nightly-risk-result.json'), 'utf8'));
    assert.equal(exitCode, 1, 'skipped designated mobile evidence must fail the lane');
    assert.equal(result.status, 'failed');
    assert.deepEqual(result.primary[0].reporterEvidence, {
      status: 'failed',
      checkIds: ['designated-mobile-journey-not-first-attempt-passed'],
    });
  } finally {
    rmSync(temporaryRoot, { force: true, recursive: true });
  }
}

function verifyProvisioningFailureCannotRetryBrowser() {
  const { exitCode, result, temporaryRoot } = runFixture('provisioning-blocks-browser', true);
  try {
    assert.equal(exitCode, 1);
    assert.equal(result.primary[0].status, 'failed');
    assert.equal(result.primary[1].status, 'not-run');
    assert.equal(result.diagnostic.status, 'not-eligible');
    assert.equal(existsSync(resolve(temporaryRoot, 'results', 'attempts')), false, 'the browser command must never start');
  } finally {
    rmSync(temporaryRoot, { force: true, recursive: true });
  }
}

function verifyBrowserCleanupPrecedesDiagnosticAndDatabaseWork() {
  const { exitCode, result, temporaryRoot } = runFixture('browser-cleanup-lifecycle', true);
  try {
    assert.equal(exitCode, 1, 'the failed primary browser attempt remains authoritative');
    assert.equal(result.primary[0].runtimeCleanup.status, 'passed');
    assert.equal(result.primary[1].status, 'passed');
    assert.equal(result.diagnostic.status, 'passed');
    assert.deepEqual(
      readFileSync(resolve(temporaryRoot, 'results', 'lifecycle-order.log'), 'utf8').trim().split('\n'),
      ['primary:failed', 'primary:cleanup', 'database-work', 'diagnostic:failed', 'diagnostic:cleanup'],
    );
  } finally {
    rmSync(temporaryRoot, { force: true, recursive: true });
  }
}

function verifyUnsafeEvidenceIsRejectedAndRemoved() {
  for (const name of [
    'evidence-extra-file',
    'evidence-run-id-mismatch',
    'evidence-unsafe-screenshot',
    'evidence-invalid-diagnostics',
    'evidence-symlink',
  ]) {
    const { exitCode, result, temporaryRoot } = runFixture(name);
    try {
      assert.equal(exitCode, 1, `${name} must fail the lane`);
      assert.equal(result.primary[0].status, 'failed');
      assert.equal(result.primary[0].reporterEvidence.status, 'failed');
      assert.equal(existsSync(resolve(temporaryRoot, 'results', 'attempts', 'primary')), false, `${name} evidence must be removed`);
    } finally {
      rmSync(temporaryRoot, { force: true, recursive: true });
    }
  }
}

function verifyCleanupFailureBlocksDatabaseWorkAndRetry() {
  const { exitCode, result, temporaryRoot } = runFixture('browser-cleanup-failure', true);
  try {
    assert.equal(exitCode, 1);
    assert.equal(result.primary[0].runtimeCleanup.status, 'failed');
    assert.equal(result.primary[1].status, 'not-run');
    assert.equal(result.primary[1].reason, 'blocked-by-browser-cleanup');
    assert.equal(result.diagnostic.status, 'not-eligible');
    assert.equal(existsSync(resolve(temporaryRoot, 'results', 'lifecycle-order.log')), true);
    assert.equal(readFileSync(resolve(temporaryRoot, 'results', 'lifecycle-order.log'), 'utf8').includes('database-work'), false);
  } finally {
    rmSync(temporaryRoot, { force: true, recursive: true });
  }
}

function verifyTimeoutEscalatesBeforeCleanup() {
  const { exitCode, result, temporaryRoot } = runFixture('browser-timeout');
  try {
    assert.equal(exitCode, 1);
    assert.equal(result.primary[0].status, 'timed-out');
    assert.equal(result.primary[0].runtimeCleanup.status, 'passed');
    assert.deepEqual(
      readFileSync(resolve(temporaryRoot, 'results', 'lifecycle-order.log'), 'utf8').trim().split('\n'),
      ['timeout:start', 'timeout:term', 'primary:cleanup'],
    );
  } finally {
    rmSync(temporaryRoot, { force: true, recursive: true });
  }
}

function verifyOrphanDescendantIsSweptBeforeCleanup() {
  const { exitCode, result, temporaryRoot } = runFixture('orphan-descendant-timeout');
  try {
    assert.equal(exitCode, 1);
    assert.equal(result.primary[0].status, 'timed-out');
    assert.equal(result.primary[0].runtimeCleanup.status, 'passed');
    const order = readFileSync(resolve(temporaryRoot, 'results', 'lifecycle-order.log'), 'utf8').trim().split('\n');
    const cleanup = order.indexOf('primary:cleanup');
    assert.ok(cleanup > order.indexOf('orphan:parent-term'));
    assert.ok(order.slice(cleanup + 1).includes('database-work'));
    assert.equal(order.slice(cleanup + 1).some((entry) => entry === 'orphan:descendant-alive'), false);
  } finally {
    rmSync(temporaryRoot, { force: true, recursive: true });
  }
}

function verifyDiagnosticCleanupFailureCannotPassClassification() {
  const { exitCode, result, temporaryRoot } = runFixture('diagnostic-cleanup-failure', true);
  try {
    assert.equal(exitCode, 1);
    assert.equal(result.primary[0].status, 'failed');
    assert.equal(result.diagnostic.status, 'failed');
    assert.equal(result.diagnostic.result.cleanupClassification, 'cleanup-unverified');
    assert.equal(result.diagnostic.result.runtimeCleanup.status, 'failed');
  } finally {
    rmSync(temporaryRoot, { force: true, recursive: true });
  }
}

async function verifySignalCancellationCleansUp() {
  const temporaryRoot = mkdtempSync(resolve(tmpdir(), 'qdb-nightly-signal-'));
  const resultsDirectory = resolve(temporaryRoot, 'results');
  try {
    const child = spawn(process.execPath, [
      RUNNER_PATH,
      '--contract-test', 'browser-timeout',
      '--results-dir', resultsDirectory,
    ], { cwd: REPOSITORY_ROOT, stdio: 'ignore' });
    await waitFor(() => existsSync(resolve(resultsDirectory, 'lifecycle-order.log')));
    child.kill('SIGTERM');
    const exitCode = await new Promise((resolvePromise) => child.once('close', resolvePromise));
    const result = JSON.parse(readFileSync(resolve(resultsDirectory, 'nightly-risk-result.json'), 'utf8'));
    assert.equal(exitCode, 1);
    assert.equal(result.primary[0].status, 'cancelled');
    assert.equal(result.primary[0].runtimeCleanup.status, 'passed');
    assert.deepEqual(
      readFileSync(resolve(resultsDirectory, 'lifecycle-order.log'), 'utf8').trim().split('\n'),
      ['timeout:start', 'timeout:term', 'primary:cleanup'],
    );
  } finally {
    rmSync(temporaryRoot, { force: true, recursive: true });
  }
}

function waitFor(condition) {
  return new Promise((resolvePromise, rejectPromise) => {
    const deadline = Date.now() + 2_000;
    const timer = setInterval(() => {
      if (condition()) {
        clearInterval(timer);
        resolvePromise();
      } else if (Date.now() >= deadline) {
        clearInterval(timer);
        rejectPromise(new Error('Timed out waiting for the fake child process.'));
      }
    }, 10);
  });
}

function runFixture(name, diagnosticRetry = false) {
  const temporaryRoot = mkdtempSync(resolve(tmpdir(), 'qdb-nightly-fixture-'));
  const resultsDirectory = resolve(temporaryRoot, 'results');
  let exitCode = 0;
  try {
    execFileSync(process.execPath, [
      RUNNER_PATH,
      '--contract-test', name,
      '--results-dir', resultsDirectory,
      ...(diagnosticRetry ? ['--diagnostic-retry'] : []),
    ], { cwd: REPOSITORY_ROOT, stdio: 'pipe' });
  } catch (error) {
    exitCode = error.status ?? 1;
  }
  return {
    exitCode,
    result: JSON.parse(readFileSync(resolve(resultsDirectory, 'nightly-risk-result.json'), 'utf8')),
    temporaryRoot,
  };
}
