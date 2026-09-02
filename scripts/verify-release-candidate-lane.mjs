import assert from 'node:assert/strict';
import { mkdirSync, mkdtempSync, readFileSync, rmSync, symlinkSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

import {
  PREVIOUS_RELEASE_REFERENCE,
  RELEASE_ARTIFACT_SHA256,
  loadReleaseCandidateManifest,
  resolveAuthoritativePins,
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
assert.equal(manifest.artifact.sha256, RELEASE_ARTIFACT_SHA256);
assert.equal(manifest.previousRelease.reference, PREVIOUS_RELEASE_REFERENCE);
assert.deepEqual(manifest.commands.map((command) => command.id), [
  'locked-backend-restore',
  'no-restore-backend-build',
  'verify-full-canonical-artifact',
  'previous-release-upgrade',
  'full-canonical-recovery',
  'release-dependency-advisory',
]);
assert.deepEqual(manifest.commands.find((command) => command.id === 'release-dependency-advisory').arguments.slice(0, 3), [
  'scripts/run-dependency-advisory-evaluation.mjs', '--trigger', 'release',
]);

const missingEvidence = mkdtempSync(resolve(tmpdir(), 'qdb-release-evidence-'));
try {
  assert.equal(validatePrimaryEvidence('previous-release-upgrade', missingEvidence).status, 'failed');
  writeFileSync(resolve(missingEvidence, 'backend-tests.trx'), '<TestRun><ResultSummary><Counters total="1" executed="1" passed="1" failed="0" error="0" timeout="0" aborted="0" inconclusive="0" notRunnable="0" notExecuted="0" disconnected="0" warning="0" inProgress="0" pending="0" /></ResultSummary><Results><UnitTestResult testName="QuranDashboard.Tests.TestSupport.Artifacts.PreviousReleaseMigrationUpgradeRehearsalTests.Run" outcome="Passed" /></Results></TestRun>');
  writeFileSync(resolve(missingEvidence, 'nightly-test-evidence.json'), '{"schemaVersion":1,"lane":"previous-release-upgrade","status":"passed"}');
  writeFileSync(resolve(missingEvidence, 'rehearsal.json'), '{}');
  assert.equal(validatePrimaryEvidence('previous-release-upgrade', missingEvidence, REPOSITORY_ROOT).status, 'failed');
  const emptyBlameDirectory = resolve(missingEvidence, '31ac7138-b257-4571-9226-1f8824b4ef55');
  mkdirSync(emptyBlameDirectory);
  assert.equal(validatePrimaryEvidence('previous-release-upgrade', missingEvidence, REPOSITORY_ROOT).checkId, 'previous-release-evidence-invalid');
  writeFileSync(resolve(emptyBlameDirectory, 'unexpected.txt'), 'x');
  assert.equal(validatePrimaryEvidence('previous-release-upgrade', missingEvidence, REPOSITORY_ROOT).checkId, 'rehearsal-evidence-inventory-invalid');
  rmSync(emptyBlameDirectory, { recursive: true });
  writeFileSync(resolve(missingEvidence, 'extra.txt'), 'x');
  assert.equal(validatePrimaryEvidence('previous-release-upgrade', missingEvidence, REPOSITORY_ROOT).checkId, 'rehearsal-evidence-inventory-invalid');
  rmSync(resolve(missingEvidence, 'extra.txt'));
  mkdirSync(resolve(missingEvidence, 'extra-directory'));
  assert.equal(validatePrimaryEvidence('previous-release-upgrade', missingEvidence, REPOSITORY_ROOT).checkId, 'rehearsal-evidence-inventory-invalid');
  rmSync(resolve(missingEvidence, 'extra-directory'), { recursive: true });
  symlinkSync(resolve(missingEvidence, 'rehearsal.json'), resolve(missingEvidence, 'extra-link'));
  assert.equal(validatePrimaryEvidence('previous-release-upgrade', missingEvidence, REPOSITORY_ROOT).checkId, 'rehearsal-evidence-inventory-invalid');
} finally {
  rmSync(missingEvidence, { force: true, recursive: true });
}
const pins = resolveAuthoritativePins(REPOSITORY_ROOT);
assert.equal(pins.artifactSha256, RELEASE_ARTIFACT_SHA256);
assert.equal(pins.previousReleaseReference, PREVIOUS_RELEASE_REFERENCE);
verifyPrimaryEvidenceFixtures(pins);
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
  assert.throws(() => loadReleaseCandidateManifest(invalidManifest.path, REPOSITORY_ROOT), /artifact SHA-256 does not match/);
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
  invalid.artifact.sha256 = '0'.repeat(64);
  writeFileSync(path, JSON.stringify(invalid));
  return { directory, path };
}

function verifyPrimaryEvidenceFixtures(authority) {
  const directory = mkdtempSync(resolve(tmpdir(), 'qdb-release-primary-'));
  try {
    const previous = previousEvidence(authority.declaration);
    writeRehearsal(directory, 'previous-release-upgrade', 'QuranDashboard.Tests.TestSupport.Artifacts.PreviousReleaseMigrationUpgradeRehearsalTests', previous);
    assert.equal(validatePrimaryEvidence('previous-release-upgrade', directory, REPOSITORY_ROOT).status, 'passed');
    for (const mutate of [
      (value) => { value.authoritativePreviousRelease.commit = '0'.repeat(40); },
      (value) => { value.supplementalRehearsalBaseline.forwardMigrationIds = []; },
      (value) => { value.payloadSha256 = '0'.repeat(64); },
      (value) => { value.preUpgradeCanonicalSentinel.actualRows = 0; },
      (value) => { value.phraseSearch.actualRows = 0; },
      (value) => { value.phases.reverse(); },
    ]) {
      const invalid = clone(previous); mutate(invalid); writeRehearsal(directory, 'previous-release-upgrade', 'QuranDashboard.Tests.TestSupport.Artifacts.PreviousReleaseMigrationUpgradeRehearsalTests', invalid);
      assert.equal(validatePrimaryEvidence('previous-release-upgrade', directory, REPOSITORY_ROOT).status, 'failed');
    }

    const recovery = recoveryEvidence(authority.artifact);
    writeRehearsal(directory, 'full-canonical-recovery', 'QuranDashboard.Tests.TestSupport.Artifacts.FullCanonicalRecoveryRehearsalTests', recovery);
    assert.equal(validatePrimaryEvidence('full-canonical-recovery', directory, REPOSITORY_ROOT).status, 'passed');
    for (const mutate of [
      (value) => { value.backup.artifacts[0].tables[0].rows = 0; },
      (value) => { value.backup.artifacts[0].sentinels[0].actualCount = 0; },
      (value) => { value.lockedOracles = []; },
      (value) => { value.lockedOracles[0].oracleSha256 = '0'.repeat(64); },
      (value) => { value.lockedOracles[0].extra = true; },
      (value) => { value.lockedCriticalReads[0].sha256 = '0'.repeat(64); },
      (value) => { value.targetCriticalReads[0].value = '0'.repeat(64); },
      (value) => { value.targetSequences[0].ownership.name = 'other'; },
      (value) => { value.backup.sequenceReconciliations[0].reconciled.nextValue += 1; },
      (value) => { value.backup.artifacts[0].sequences[0].nextValue += 1; },
      (value) => { value.targetSequences[0].nextValue = value.targetSequences[0].lastValue; },
      (value) => { value.targetSequences[0].isCalled = false; value.targetSequences[0].nextValue += 1; },
      (value) => { value.backup.sequenceReconciliations[0].reconciled.lastValue += 1; },
      (value) => { value.targetSequences[0].incrementBy = 0; },
      (value) => { value.targetSequences[0].incrementBy = -1; },
      (value) => { value.targetSequences[0].nextValue = value.targetSequences[0].highWaterMark; },
      (value) => { value.backup.sequenceReconciliations[0].reconciled.highWaterMark += 1; },
      (value) => { value.source.imageDigest = 'sha256:0'.padEnd(71, '0'); },
      (value) => { value.source.serverInstanceId = 'not-a-guid'; },
      (value) => { value.source.postgreSqlVersion = '18.60 arbitrary'; },
      (value) => { value.target.migrationHead = 'other'; },
      (value) => { value.backup.fileName = '../recovery.dump'; },
      (value) => { value.backup.extra = true; },
      (value) => { value.durationMilliseconds = 0; },
    ]) {
      const invalid = clone(recovery); mutate(invalid); writeRehearsal(directory, 'full-canonical-recovery', 'QuranDashboard.Tests.TestSupport.Artifacts.FullCanonicalRecoveryRehearsalTests', invalid);
      assert.equal(validatePrimaryEvidence('full-canonical-recovery', directory, REPOSITORY_ROOT).status, 'failed');
    }

    rmSync(directory, { force: true, recursive: true }); mkdirSync(directory);
    const advisory = { schemaVersion: 1, policyId: 'dependency-advisory-evaluation', trigger: 'release', evaluatedAt: '2026-09-01T00:00:00.000Z', status: 'passed', summary: { total: 0, production: 0, development: 0, highCriticalProduction: 0, blocking: 0 }, findings: [], blockingFindings: [], expiredWaivers: [], scanErrors: null };
    writeFileSync(resolve(directory, 'evaluation.json'), JSON.stringify(advisory));
    assert.equal(validatePrimaryEvidence('release-dependency-advisory', directory, REPOSITORY_ROOT).status, 'passed');
    writeFileSync(resolve(directory, 'evaluation.json'), JSON.stringify({ ...advisory, scanErrors: [{ commandId: 'synthetic' }] }));
    assert.equal(validatePrimaryEvidence('release-dependency-advisory', directory, REPOSITORY_ROOT).status, 'failed');
  } finally { rmSync(directory, { force: true, recursive: true }); }
}

function writeRehearsal(directory, lane, className, evidence) {
  rmSync(directory, { force: true, recursive: true }); mkdirSync(directory);
  writeFileSync(resolve(directory, 'backend-tests.trx'), `<TestRun><ResultSummary><Counters total="1" executed="1" passed="1" failed="0" error="0" timeout="0" aborted="0" inconclusive="0" notRunnable="0" notExecuted="0" disconnected="0" warning="0" inProgress="0" pending="0" /></ResultSummary><Results><UnitTestResult testName="${className}.Run" outcome="Passed" /></Results></TestRun>`);
  writeFileSync(resolve(directory, 'nightly-test-evidence.json'), JSON.stringify({ schemaVersion: 1, lane, status: 'passed' }));
  writeFileSync(resolve(directory, 'rehearsal.json'), JSON.stringify(evidence));
}

function previousEvidence(declaration) {
  const sentinel = (expected) => ({ table: expected.table, expectedRows: expected.expectedCount, actualRows: expected.expectedCount });
  return { status: 'passed', authoritativePreviousRelease: { commit: declaration.authoritativePreviousRelease.sha, forwardMigrationIds: declaration.expectations.authoritativeForwardMigrationIds }, supplementalRehearsalBaseline: { commit: declaration.supplementalRehearsalBaseline.sha, forwardMigrationIds: declaration.expectations.supplementalForwardMigrationIds }, payloadSha256: declaration.artifact.payloadSha256, manifestSha256: declaration.artifact.manifestSha256, preUpgradeCanonicalSentinel: sentinel(declaration.expectations.preUpgradeSentinel), postUpgradeCanonicalSentinel: sentinel(declaration.expectations.postUpgradeSentinel), phraseSearch: { stateTable: declaration.expectations.phraseSearch.stateTable, expectedRows: 1, expectedActiveBuild: 'none', actualRows: 1, actualActiveBuildRows: 0 }, applicationBoot: { expected: 'succeeded', actual: 'succeeded' }, criticalReadSentinels: { expected: 'succeeded', actual: 'succeeded' }, phases: ['artifact', 'historical-schema', 'restore', 'forward-migrations', 'application-boot', 'critical-read-sentinels', 'post-upgrade-sentinels'].map((name) => ({ name, status: 'passed', durationMilliseconds: 1, detail: 'completed' })) };
}

function recoveryEvidence(artifact) {
  const states = artifact.tableScope.ownedSequences.map((ownership, index) => ({ ownership, highWaterMark: index + 1, lastValue: index + 1, isCalled: true, incrementBy: 1, nextValue: index + 2 }));
  const reads = artifact.restore.sentinelTables.map((sentinel) => ({ id: sentinel.id, sha256: sentinel.criticalReadSha256 }));
  const fingerprints = reads.map(({ id, sha256 }) => ({ key: id, value: sha256 }));
  const descriptor = (role, instance) => ({ role, serverInstanceId: instance, imageDigest: artifact.postgresql.containerDigest, postgreSqlVersion: `${artifact.postgresql.producerVersion} (PostgreSQL)`, migrationHead: artifact.migration.head, migrationCount: artifact.migration.count });
  const recovered = { id: artifact.id, immutableStorageId: artifact.immutableStorageId, tables: artifact.tableCounts, sentinels: artifact.restore.sentinelTables.map(({ id, table, expectedCount }) => ({ id, table, expectedCount, actualCount: expectedCount })), stagedFiles: artifact.stagedFiles, sources: artifact.sources, criticalReads: reads, sequences: states };
  const originalStates = states.map((state) => ({ ...state, lastValue: state.highWaterMark - 1, isCalled: false, nextValue: state.highWaterMark - 1 }));
  return { status: 'passed', classification: 'data-recovery', applicationRollback: 'application-rollback-not-requested', lockedCriticalReads: reads, lockedOracles: artifact.sentinels, backup: { fileName: 'quran-canonical-recovery.dump', size: 1, sha256: 'a'.repeat(64), repositoryMigration: artifact.migration, tables: artifact.tableScope.tables, ownedSequences: artifact.tableScope.ownedSequences, sequenceReconciliations: states.map((state, index) => ({ original: originalStates[index], reconciled: state })), artifacts: [recovered] }, receipt: { status: 'rehearsed', classification: 'data-recovery', applicationRollback: 'application-rollback-not-requested' }, source: descriptor('source', '0123456789abcdef0123456789abcdef'), target: descriptor('target', 'fedcba9876543210fedcba9876543210'), sourceCriticalReads: fingerprints, targetCriticalReads: fingerprints, targetSequences: states, durationMilliseconds: 1 };
}

function clone(value) { return JSON.parse(JSON.stringify(value)); }

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
