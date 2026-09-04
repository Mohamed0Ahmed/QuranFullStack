import assert from 'node:assert/strict';
import { mkdtempSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

import {
  loadReleaseCandidateManifest,
  validatePrimaryEvidence,
} from './release-candidate-contract.mjs';
import { classifyReleaseCandidate, createCancellationController, createReleaseCandidateFinalizer, isolatedTemporaryEnvironment, validateCheckoutState, validateResultsLocation } from './release-candidate-orchestration.mjs';

const REPOSITORY_ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const manifest = loadReleaseCandidateManifest(resolve(REPOSITORY_ROOT, 'release-candidate-lane.json'), REPOSITORY_ROOT);
const candidate = '0123456789abcdef0123456789abcdef01234567';

assert.equal(manifest.schemaVersion, 2);
assert.equal(manifest.id, 'release-candidate');
assert.equal(manifest.executionScope, 'local-first-pre-merge');
assert.equal(manifest.providerNeutral, true);
assert.equal(manifest.externalEvidence, undefined);
assert.equal(manifest.artifact, undefined);
assert.equal(manifest.previousRelease, undefined);
assert.deepEqual(manifest.commands.map((command) => command.id), [
  'locked-backend-restore',
  'no-restore-backend-build',
  'inspect-test-runtime',
  'release-dependency-advisory',
]);
assert.deepEqual(manifest.commands.find((command) => command.id === 'release-dependency-advisory').arguments.slice(0, 3), [
  'scripts/run-dependency-advisory-evaluation.mjs', '--trigger', 'release',
]);

const missingEvidence = mkdtempSync(resolve(tmpdir(), 'qdb-release-evidence-'));
try {
  assert.equal(validatePrimaryEvidence('release-dependency-advisory', missingEvidence).status, 'failed');
} finally {
  rmSync(missingEvidence, { force: true, recursive: true });
}
verifyPrimaryEvidenceFixtures();
assert.equal(validateCheckoutState({ candidate, head: candidate, porcelain: '' }).status, 'passed');
assert.equal(validateCheckoutState({ candidate, head: candidate, porcelain: ' M scripts/a.mjs' }).reason, 'candidate-worktree-dirty');
assert.equal(validateCheckoutState({ candidate, head: candidate, porcelain: 'M  scripts/a.mjs' }).reason, 'candidate-worktree-dirty');
assert.equal(validateCheckoutState({ candidate, head: candidate, porcelain: '?? release-candidate-lane.json' }).reason, 'candidate-worktree-dirty');
assert.equal(validateCheckoutState({ candidate, head: 'fedcba9876543210fedcba9876543210fedcba98', porcelain: '' }).reason, 'candidate-head-changed');
assert.equal(validateResultsLocation({ repositoryRoot: REPOSITORY_ROOT, resultsDirectory: resolve(REPOSITORY_ROOT, 'results') }).reason, 'results-directory-inside-candidate');
assert.equal(validateResultsLocation({ repositoryRoot: REPOSITORY_ROOT, resultsDirectory: resolve(REPOSITORY_ROOT, '..results') }).reason, 'results-directory-inside-candidate');
assert.equal(validateResultsLocation({ repositoryRoot: REPOSITORY_ROOT, resultsDirectory: resolve(tmpdir(), 'qdb-release-results') }).status, 'passed');
assert.deepEqual(isolatedTemporaryEnvironment(resolve(tmpdir(), 'qdb-release-home')), {
  TMPDIR: resolve(tmpdir(), 'qdb-release-home'),
  TMP: resolve(tmpdir(), 'qdb-release-home'),
  TEMP: resolve(tmpdir(), 'qdb-release-home'),
});
const lateCancellation = createCancellationController();
lateCancellation.cancel('SIGTERM');
assert.equal(classifyReleaseCandidate({ primary: [{ status: 'passed' }], cancellation: lateCancellation }), 'cancelled');
assert.equal(classifyReleaseCandidate({ primary: [{ status: 'passed' }], cancellation: createCancellationController() }), 'passed');
assert.equal(classifyReleaseCandidate({ primary: [{ status: 'failed' }], cancellation: createCancellationController() }), 'failed');
await verifyFinalizerBoundaries();

const invalidManifest = writeInvalidManifest();
try {
  assert.throws(() => loadReleaseCandidateManifest(invalidManifest.path, REPOSITORY_ROOT), /artifact pins are not part/);
} finally {
  rmSync(invalidManifest.directory, { force: true, recursive: true });
}
await import('./verify-release-candidate-runner.mjs');
await import('./verify-release-candidate-cleanup.mjs');
console.log('Release candidate lane contract passed.');

function writeInvalidManifest() {
  const directory = mkdtempSync(resolve(tmpdir(), 'qdb-release-candidate-contract-'));
  const path = resolve(directory, 'invalid-manifest.json');
  const invalid = JSON.parse(readFileSync(resolve(REPOSITORY_ROOT, 'release-candidate-lane.json'), 'utf8'));
  invalid.artifact = { id: 'quran-canonical', sha256: '0'.repeat(64) };
  writeFileSync(path, JSON.stringify(invalid));
  return { directory, path };
}

function verifyPrimaryEvidenceFixtures() {
  const directory = mkdtempSync(resolve(tmpdir(), 'qdb-release-primary-'));
  try {
    const advisory = { schemaVersion: 1, policyId: 'dependency-advisory-evaluation', trigger: 'release', evaluatedAt: '2026-09-01T00:00:00.000Z', status: 'passed', summary: { total: 0, production: 0, development: 0, highCriticalProduction: 0, blocking: 0 }, findings: [], blockingFindings: [], expiredWaivers: [], scanErrors: null };
    writeFileSync(resolve(directory, 'evaluation.json'), JSON.stringify(advisory));
    assert.equal(validatePrimaryEvidence('release-dependency-advisory', directory).status, 'passed');
    writeFileSync(resolve(directory, 'evaluation.json'), JSON.stringify({ ...advisory, scanErrors: [{ commandId: 'synthetic' }] }));
    assert.equal(validatePrimaryEvidence('release-dependency-advisory', directory).status, 'failed');
    assert.equal(validatePrimaryEvidence('inspect-test-runtime', directory).checkId, 'no-primary-evidence-required');
  } finally { rmSync(directory, { force: true, recursive: true }); }
}

async function verifyFinalizerBoundaries() {
  const noSignal = await finalizeCase();
  assert.equal(noSignal.writes.length, 1); assert.equal(noSignal.writes.at(-1).status, 'passed'); assert.equal(noSignal.exitCode, 0);

  const queued = await finalizeCase((cancellation) => setImmediate(() => cancellation.cancel('SIGINT')));
  assert.equal(queued.writes.at(-1).status, 'cancelled'); assert.equal(queued.exitCode, 1);

  const afterWrite = await finalizeCase((_cancellation, finalizer, writes) => {
    const original = writes.push.bind(writes);
    writes.push = (value) => { original(value); if (value.status === 'passed') setImmediate(() => finalizer.cancel()); return writes.length; };
  });
  assert.equal(afterWrite.writes.at(-1).status, 'cancelled'); assert.equal(afterWrite.exitCode, 1);

  const late = await finalizeCase();
  late.finalizer.cancel();
  assert.equal(late.writes.at(-1).status, 'cancelled'); assert.equal(late.exitCode, 1);
}

async function finalizeCase(schedule) {
  const receipt = { status: 'passed', firstAttemptStatus: 'passed' };
  const cancellation = createCancellationController();
  const writes = [];
  let exitCode;
  const finalizer = createReleaseCandidateFinalizer(receipt, cancellation, (value) => writes.push({ ...value }), (code) => { exitCode = code; });
  schedule?.(cancellation, finalizer, writes);
  await finalizer.finalize();
  return { get exitCode() { return exitCode; }, finalizer, writes };
}
