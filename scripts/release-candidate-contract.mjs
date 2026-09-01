import { existsSync, lstatSync, readFileSync, readdirSync } from 'node:fs';
import { isAbsolute, relative, resolve, sep } from 'node:path';

export const RELEASE_ARTIFACT_SHA256 = '3d4038d561a2b4b048e72c05f0cc472b2b1bcf0f2af0d09d0c054cff38e9b29d';
export const PREVIOUS_RELEASE_REFERENCE = 'df07306b5a5ebe08ff205c0d2f6cd5a10af87f2d';
export const EXTERNAL_EVIDENCE_DOCUMENTS = [
  'isolated-staging-critical-journeys',
  'real-logto-sentinel',
  'manual-release-charter',
];

const RELEASE_RESULTS_PLACEHOLDER = '{RELEASE_RESULTS_DIR}';
const REQUIRED_COMMANDS = new Map([
  ['locked-backend-restore', ['dotnet', ['restore', 'Backend/QuranDashboard.sln', '--locked-mode', '--disable-parallel', '-m:1', '-p:BuildInParallel=false', '-p:RestoreDisableParallel=true']]],
  ['no-restore-backend-build', ['dotnet', ['build', 'Backend/QuranDashboard.sln', '--no-restore', '-m:1', '-p:BuildInParallel=false']]],
  ['verify-full-canonical-artifact', ['Backend/scripts/test-artifacts', ['verify-content-addressed', '--artifact', 'quran-canonical']]],
  ['previous-release-upgrade', ['Backend/scripts/test-backend', ['previous-release-upgrade', '--no-build', '--results-dir', '{RELEASE_RESULTS_DIR}/previous-release-upgrade']]],
  ['full-canonical-recovery', ['Backend/scripts/test-backend', ['full-canonical-recovery', '--no-build', '--results-dir', '{RELEASE_RESULTS_DIR}/full-canonical-recovery']]],
  ['release-dependency-advisory', ['node', ['scripts/run-dependency-advisory-evaluation.mjs', '--trigger', 'release', '--results-dir', '{RELEASE_RESULTS_DIR}/dependency-advisory']]],
]);

export function loadReleaseCandidateManifest(path, repositoryRoot) {
  let manifest;
  try {
    manifest = JSON.parse(readFileSync(path, 'utf8'));
  } catch (error) {
    throw new Error(`Cannot read release candidate manifest: ${error.message}`);
  }
  validateReleaseCandidateManifest(manifest, repositoryRoot);
  return manifest;
}

export function validateReleaseCandidateManifest(manifest, repositoryRoot) {
  const pins = resolveAuthoritativePins(repositoryRoot);
  requireCondition(manifest?.schemaVersion === 1, 'schemaVersion must be 1.');
  requireCondition(manifest.id === 'release-candidate', 'id must be release-candidate.');
  requireNonEmptyString(manifest.title, 'title');
  requireCondition(manifest.providerNeutral === true, 'providerNeutral must be true.');
  requirePositiveInteger(manifest.timeoutSeconds, 'timeoutSeconds');
  requireCondition(manifest.artifact?.id === 'quran-canonical', 'artifact id must be quran-canonical.');
  requireCondition(manifest.artifact?.sha256 === pins.artifactSha256, 'artifact SHA-256 does not match the authoritative lock.');
  requireCondition(manifest.previousRelease?.reference === pins.previousReleaseReference, 'previous-release reference does not match the authoritative declaration.');
  requirePositiveInteger(manifest.externalEvidence?.maxAgeHours, 'externalEvidence.maxAgeHours');
  requireCondition(
    JSON.stringify(manifest.externalEvidence?.documents) === JSON.stringify(EXTERNAL_EVIDENCE_DOCUMENTS),
    'external evidence documents must be complete and ordered.',
  );
  requireCondition(Array.isArray(manifest.commands) && manifest.commands.length === REQUIRED_COMMANDS.size, 'commands are incomplete.');

  const ids = new Set();
  for (const command of manifest.commands) {
    requireNonEmptyString(command.id, 'command.id');
    requireCondition(!ids.has(command.id), `duplicate command id: ${command.id}.`);
    ids.add(command.id);
    requirePositiveInteger(command.timeoutSeconds, `${command.id}.timeoutSeconds`);
    requireNonEmptyString(command.cwd, `${command.id}.cwd`);
    requireCondition(isInside(repositoryRoot, resolve(repositoryRoot, command.cwd)), `${command.id}.cwd must stay inside the repository.`);
    requireNonEmptyString(command.executable, `${command.id}.executable`);
    requireCondition(Array.isArray(command.arguments) && command.arguments.every((argument) => typeof argument === 'string'), `${command.id}.arguments must be strings.`);
    const expected = REQUIRED_COMMANDS.get(command.id);
    requireCondition(expected !== undefined, `unexpected command: ${command.id}.`);
    requireCondition(command.executable === expected[0] && JSON.stringify(command.arguments) === JSON.stringify(expected[1]), `${command.id} does not use its existing executable seam.`);
  }
  for (const command of manifest.commands) {
    requireCondition(
      command.dependsOn === undefined || (Array.isArray(command.dependsOn) && command.dependsOn.every((dependency) => ids.has(dependency) && dependency !== command.id)),
      `${command.id}.dependsOn is invalid.`,
    );
  }
  requireCondition(
    JSON.stringify(manifest.commands.find((command) => command.id === 'no-restore-backend-build').dependsOn) === JSON.stringify(['locked-backend-restore']),
    'the no-restore build must depend on locked restore.',
  );
  requireCondition(
    JSON.stringify(manifest.commands.find((command) => command.id === 'verify-full-canonical-artifact').dependsOn) === JSON.stringify(['no-restore-backend-build']),
    'artifact verification must depend on the controlled build.',
  );
  requireCondition(
    JSON.stringify(manifest.commands.find((command) => command.id === 'previous-release-upgrade').dependsOn) === JSON.stringify(['verify-full-canonical-artifact']),
    'previous-release-upgrade must depend on complete artifact verification.',
  );
  requireCondition(
    JSON.stringify(manifest.commands.find((command) => command.id === 'full-canonical-recovery').dependsOn) === JSON.stringify(['verify-full-canonical-artifact']),
    'full-canonical-recovery must depend on complete artifact verification.',
  );
  for (const id of ['previous-release-upgrade', 'full-canonical-recovery']) {
    requireCondition(manifest.commands.find((command) => command.id === id).databaseOwning === true, `${id} must declare database ownership.`);
  }
}

export function resolveAuthoritativePins(repositoryRoot, paths = {}) {
  const artifactLock = readJson(paths.artifactLockPath ?? resolve(repositoryRoot, 'test-artifacts.lock.json'), 'test artifact lock');
  const declaration = readJson(paths.declarationPath ?? resolve(repositoryRoot, 'docs/testing/previous-release-migration-upgrade.json'), 'previous-release declaration');
  const artifacts = (artifactLock?.artifacts ?? []).filter((artifact) => artifact?.id === 'quran-canonical');
  requireCondition(artifacts.length === 1, 'authoritative quran-canonical artifact is missing or ambiguous.');
  const artifact = artifacts[0];
  const payloads = (artifact.stagedFiles ?? []).filter((file) => file?.role === 'payload');
  requireCondition(payloads.length === 1 && /^[a-f0-9]{64}$/.test(payloads[0].sha256), 'authoritative payload is missing or ambiguous.');
  const artifactSha256 = payloads[0].sha256;
  requireCondition(artifact.immutableStorageId === `local://quran-canonical@sha256:${artifactSha256}`, 'authoritative immutable storage identity is invalid.');
  requireCondition(artifactSha256 === RELEASE_ARTIFACT_SHA256, 'authoritative artifact lock drifted from the adopted SHA-256.');
  requireCondition(declaration?.status === 'adopted' && declaration?.authoritativePreviousRelease?.role === 'authoritative-previous-release', 'authoritative previous-release declaration is invalid.');
  const previousReleaseReference = declaration.authoritativePreviousRelease.sha;
  requireCondition(/^[a-f0-9]{40}$/.test(previousReleaseReference), 'authoritative previous-release reference is invalid.');
  requireCondition(previousReleaseReference === PREVIOUS_RELEASE_REFERENCE, 'authoritative previous-release declaration drifted from the adopted reference.');
  requireCondition(declaration?.artifact?.id === 'quran-canonical' && declaration.artifact.payloadSha256 === artifactSha256, 'authoritative declarations disagree on the canonical artifact.');
  return { artifact, artifactSha256, declaration, previousReleaseReference };
}

export function materializeReleaseCandidateCommand(command, repositoryRoot, resultsDirectory) {
  return {
    ...command,
    cwd: resolve(repositoryRoot, command.cwd),
    arguments: command.arguments.map((argument) => argument.replaceAll(RELEASE_RESULTS_PLACEHOLDER, resultsDirectory)),
  };
}

export function readExternalEvidence(directory) {
  const documents = {};
  if (!directory || !existsSync(directory) || !lstatSync(directory).isDirectory()) return documents;
  const expectedFiles = new Set(EXTERNAL_EVIDENCE_DOCUMENTS.map((id) => `${id}.json`));
  let entries;
  try {
    entries = readdirSync(directory, { withFileTypes: true });
  } catch {
    return documents;
  }
  for (const entry of entries) {
    if (!entry.isFile() || !expectedFiles.has(entry.name)) {
      documents.__invalid = 'external-evidence-directory-invalid';
      return documents;
    }
  }
  for (const id of EXTERNAL_EVIDENCE_DOCUMENTS) {
    const path = resolve(directory, `${id}.json`);
    if (!existsSync(path) || !lstatSync(path).isFile()) continue;
    try {
      documents[id] = JSON.parse(readFileSync(path, 'utf8'));
    } catch {
      documents[id] = null;
    }
  }
  return documents;
}

export function evaluateExternalEvidence(documents, { candidate, now = new Date(), maxAgeHours = 24 } = {}) {
  const nowMs = now.getTime();
  const components = EXTERNAL_EVIDENCE_DOCUMENTS.map((id) => evaluateDocument(id, documents?.[id], candidate, nowMs, maxAgeHours));
  if (documents?.__invalid) components.push({ id: 'external-evidence-directory', status: 'failed', checkId: documents.__invalid });
  const status = components.every((component) => component.status === 'passed')
    ? 'passed'
    : components.some((component) => component.status === 'failed') ? 'failed'
      : components.some((component) => component.status === 'stale') ? 'stale' : 'unavailable';
  return { status, components };
}

function evaluateDocument(id, document, candidate, nowMs, maxAgeHours) {
  if (document === undefined) return { id, status: 'unavailable', checkId: 'evidence-missing' };
  if (!isValidBaseDocument(document, id)) return { id, status: 'failed', checkId: 'evidence-invalid-or-unsanitized' };
  if (document.status !== 'passed') return { id, status: 'failed', checkId: 'evidence-reported-failure' };
  if (!isCommit(candidate) || document.candidate !== candidate) return { id, status: 'failed', checkId: 'candidate-binding-mismatch' };
  if (!isValidDocumentDetails(id, document)) return { id, status: 'failed', checkId: 'evidence-contract-incomplete' };
  const completedAt = Date.parse(document.completedAt);
  if (!Number.isFinite(completedAt) || completedAt > nowMs || nowMs - completedAt > maxAgeHours * 3_600_000) {
    return { id, status: 'stale', checkId: 'evidence-stale' };
  }
  return { id, status: 'passed', checkId: 'evidence-current-and-complete' };
}

function isValidBaseDocument(document, id) {
  if (!isExactObject(document, ['schemaVersion', 'id', 'status', 'completedAt', 'runId', 'candidate', 'sanitization', ...detailKeys(id)])) return false;
  return document.schemaVersion === 1 && document.id === id && typeof document.status === 'string'
    && isIdentifier(document.runId) && isCommit(document.candidate) && isIsoTimestamp(document.completedAt)
    && isExactObject(document.sanitization, ['credentials', 'rawUrls', 'requestBodies', 'responseBodies', 'databaseDumps'])
    && Object.values(document.sanitization).every((value) => value === false);
}

function detailKeys(id) {
  if (id === 'isolated-staging-critical-journeys') return ['isolation', 'deployment', 'artifact', 'journeys'];
  if (id === 'real-logto-sentinel') return ['serialization', 'identities', 'checks'];
  return ['manual'];
}

function isValidDocumentDetails(id, document) {
  if (id === 'isolated-staging-critical-journeys') {
    return isExactObject(document.isolation, ['dedicatedState', 'sharedState'])
      && document.isolation.dedicatedState === true && document.isolation.sharedState === false
      && isExactObject(document.deployment, ['id', 'immutable']) && isIdentifier(document.deployment.id) && document.deployment.immutable === true
      && isExactObject(document.artifact, ['sha256', 'verification'])
      && document.artifact.sha256 === RELEASE_ARTIFACT_SHA256 && document.artifact.verification === 'passed'
      && isExactObject(document.journeys, ['catalogue', 'allDeclared', 'firstAttempt', 'retryCount'])
      && document.journeys.catalogue === 'critical-playwright' && document.journeys.allDeclared === true
      && document.journeys.firstAttempt === true && document.journeys.retryCount === 0;
  }
  if (id === 'real-logto-sentinel') {
    return isExactObject(document.serialization, ['serialized', 'concurrentRuns'])
      && document.serialization.serialized === true && document.serialization.concurrentRuns === 1
      && isExactObject(document.identities, ['dedicated', 'count'])
      && document.identities.dedicated === true && Number.isInteger(document.identities.count) && document.identities.count >= 2
      && isExactObject(document.checks, ['redirect', 'callback', 'logout', 'identityMapping', 'sessionBootstrap', 'approvedProfileReconciliation'])
      && Object.values(document.checks).every((value) => value === true);
  }
  return isExactObject(document.manual, ['typography', 'assistiveTechnology', 'restore', 'providerConfiguration'])
    && Object.values(document.manual).every((value) => value === true);
}

function isExactObject(value, keys) {
  return value !== null && typeof value === 'object' && !Array.isArray(value)
    && JSON.stringify(Object.keys(value).sort()) === JSON.stringify([...keys].sort());
}

function isIdentifier(value) {
  return typeof value === 'string' && /^[a-z0-9]+(?:[._-][a-z0-9]+)*$/.test(value);
}

function isCommit(value) {
  return typeof value === 'string' && /^[a-f0-9]{40}$/.test(value);
}

export function validatePrimaryEvidence(command, resultsDirectory, repositoryRoot) {
  if (command === 'release-dependency-advisory') return validateAdvisoryEvidence(resultsDirectory);
  const expectedClass = command === 'previous-release-upgrade'
    ? 'QuranDashboard.Tests.TestSupport.Artifacts.PreviousReleaseMigrationUpgradeRehearsalTests'
    : command === 'full-canonical-recovery'
      ? 'QuranDashboard.Tests.TestSupport.Artifacts.FullCanonicalRecoveryRehearsalTests' : null;
  if (!expectedClass) return { status: 'passed', checkId: 'no-primary-evidence-required' };
  const files = approvedRehearsalFiles(resultsDirectory);
  if (!files) return { status: 'failed', checkId: 'rehearsal-evidence-inventory-invalid' };
  const trx = files.filter((file) => file.endsWith('.trx'));
  if (trx.length !== 1 || !trxPassedForClass(readText(trx[0]), expectedClass)) return { status: 'failed', checkId: 'trx-missing-or-invalid' };
  const receipt = readJsonOrNull(resolve(resultsDirectory, 'nightly-test-evidence.json'));
  if (!isExactObject(receipt, ['schemaVersion', 'lane', 'status']) || receipt.schemaVersion !== 1 || receipt.lane !== command || receipt.status !== 'passed') return { status: 'failed', checkId: 'lane-receipt-missing-or-invalid' };
  const evidenceFiles = files.filter((file) => file.endsWith('.json') && file !== resolve(resultsDirectory, 'nightly-test-evidence.json'));
  if (evidenceFiles.length !== 1) return { status: 'failed', checkId: 'sanitized-evidence-count-invalid' };
  const evidence = readJsonOrNull(evidenceFiles[0]);
  const pins = repositoryRoot ? resolveAuthoritativePins(repositoryRoot) : null;
  const declaration = pins?.declaration ?? null;
  return command === 'previous-release-upgrade'
    ? validPreviousReleaseEvidence(evidence, declaration) ? { status: 'passed', checkId: 'previous-release-evidence-validated' } : { status: 'failed', checkId: 'previous-release-evidence-invalid' }
    : validRecoveryEvidence(evidence, pins) ? { status: 'passed', checkId: 'recovery-evidence-validated' } : { status: 'failed', checkId: 'recovery-evidence-invalid' };
}

function approvedRehearsalFiles(resultsDirectory) {
  try {
    const entries = readdirSync(resultsDirectory, { withFileTypes: true });
    if (entries.length !== 3 || entries.some((entry) => !entry.isFile())) return null;
    const files = entries.map((entry) => resolve(resultsDirectory, entry.name));
    const trx = files.filter((file) => file.endsWith('.trx'));
    const receipt = files.filter((file) => file === resolve(resultsDirectory, 'nightly-test-evidence.json'));
    const evidence = files.filter((file) => file.endsWith('.json') && file !== resolve(resultsDirectory, 'nightly-test-evidence.json'));
    return trx.length === 1 && receipt.length === 1 && evidence.length === 1 ? files : null;
  } catch { return null; }
}

function validateAdvisoryEvidence(resultsDirectory) {
  const evaluation = readJsonOrNull(resolve(resultsDirectory, 'evaluation.json'));
  return isExactObject(evaluation, ['schemaVersion', 'policyId', 'trigger', 'evaluatedAt', 'status', 'summary', 'findings', 'blockingFindings', 'expiredWaivers', 'scanErrors'])
    && evaluation.schemaVersion === 1 && evaluation.policyId === 'dependency-advisory-evaluation' && evaluation.trigger === 'release'
    && ['passed', 'passed-with-notes'].includes(evaluation.status) && isExactObject(evaluation.summary, ['total', 'production', 'development', 'highCriticalProduction', 'blocking'])
    && Object.values(evaluation.summary).every((count) => Number.isInteger(count) && count >= 0)
    && Array.isArray(evaluation.findings) && evaluation.summary.total === evaluation.findings.length && evaluation.summary.blocking === 0
    && Array.isArray(evaluation.blockingFindings) && evaluation.blockingFindings.length === 0
    && Array.isArray(evaluation.expiredWaivers) && evaluation.expiredWaivers.length === 0
    && evaluation.scanErrors === null
    ? { status: 'passed', checkId: 'release-advisory-evaluation-validated' }
    : { status: 'failed', checkId: 'release-advisory-evaluation-missing-or-invalid' };
}

function trxPassedForClass(text, expectedClass) {
  if (typeof text !== 'string') return false;
  const results = [...text.matchAll(/<UnitTestResult\b([^>]*)\/>/g)].map((match) => match[1]);
  const counters = /<Counters\b([^>]*)\/>/.exec(text)?.[1];
  if (!counters || results.length === 0 || !results.every((attributes) => attribute(attributes, 'outcome') === 'Passed' && attribute(attributes, 'testName')?.startsWith(`${expectedClass}.`))) return false;
  const positive = ['total', 'executed', 'passed'];
  const forbidden = ['failed', 'error', 'timeout', 'aborted', 'inconclusive', 'notRunnable', 'notExecuted', 'disconnected', 'warning', 'inProgress', 'pending'];
  return positive.every((name) => integerAttribute(counters, name) > 0)
    && integerAttribute(counters, 'total') === results.length
    && integerAttribute(counters, 'executed') === results.length
    && integerAttribute(counters, 'passed') === results.length
    && forbidden.every((name) => integerAttribute(counters, name) === 0);
}

function validPreviousReleaseEvidence(value, declaration) {
  return isExactObject(value, ['status', 'authoritativePreviousRelease', 'supplementalRehearsalBaseline', 'payloadSha256', 'manifestSha256', 'preUpgradeCanonicalSentinel', 'postUpgradeCanonicalSentinel', 'phraseSearch', 'applicationBoot', 'criticalReadSentinels', 'phases'])
    && declaration?.artifact?.payloadSha256 === RELEASE_ARTIFACT_SHA256 && declaration?.authoritativePreviousRelease?.sha === PREVIOUS_RELEASE_REFERENCE
    && value.status === 'passed' && migrationEvidenceMatches(value.authoritativePreviousRelease, declaration.authoritativePreviousRelease, declaration.expectations.authoritativeForwardMigrationIds)
    && migrationEvidenceMatches(value.supplementalRehearsalBaseline, declaration.supplementalRehearsalBaseline, declaration.expectations.supplementalForwardMigrationIds)
    && value.payloadSha256 === declaration.artifact.payloadSha256 && value.manifestSha256 === declaration.artifact.manifestSha256
    && sentinelMatches(value.preUpgradeCanonicalSentinel, declaration.expectations.preUpgradeSentinel)
    && sentinelMatches(value.postUpgradeCanonicalSentinel, declaration.expectations.postUpgradeSentinel)
    && phraseSearchMatches(value.phraseSearch, declaration.expectations.phraseSearch)
    && checkMatches(value.applicationBoot) && checkMatches(value.criticalReadSentinels)
    && Array.isArray(value.phases) && JSON.stringify(value.phases.map((phase) => phase.name)) === JSON.stringify(['artifact', 'historical-schema', 'restore', 'forward-migrations', 'application-boot', 'critical-read-sentinels', 'post-upgrade-sentinels'])
    && value.phases.every((phase) => isExactObject(phase, ['name', 'status', 'durationMilliseconds', 'detail']) && phase.status === 'passed' && Number.isInteger(phase.durationMilliseconds) && phase.durationMilliseconds >= 0 && phase.detail === 'completed');
}

function migrationEvidenceMatches(value, expected, forwardMigrationIds) {
  return isExactObject(value, ['commit', 'forwardMigrationIds']) && value.commit === expected.sha && JSON.stringify(value.forwardMigrationIds) === JSON.stringify(forwardMigrationIds);
}

function sentinelMatches(value, expected) {
  return isExactObject(value, ['table', 'expectedRows', 'actualRows']) && value.table === expected.table && value.expectedRows === expected.expectedCount && value.actualRows === expected.expectedCount;
}

function phraseSearchMatches(value, expected) {
  return isExactObject(value, ['stateTable', 'expectedRows', 'expectedActiveBuild', 'actualRows', 'actualActiveBuildRows'])
    && value.stateTable === expected.stateTable && value.expectedRows === expected.expectedRows && value.expectedActiveBuild === expected.activeBuild
    && value.actualRows === expected.expectedRows && value.actualActiveBuildRows === 0;
}

function checkMatches(value) {
  return isExactObject(value, ['expected', 'actual']) && value.expected === 'succeeded' && value.actual === 'succeeded';
}

function validRecoveryEvidence(value, pins) {
  const artifact = pins?.artifact;
  return isExactObject(value, ['status', 'classification', 'applicationRollback', 'lockedCriticalReads', 'lockedOracles', 'backup', 'receipt', 'source', 'target', 'sourceCriticalReads', 'targetCriticalReads', 'targetSequences', 'durationMilliseconds'])
    && value.status === 'passed' && value.classification === 'data-recovery' && value.applicationRollback === 'application-rollback-not-requested'
    && isExactObject(value.receipt, ['status', 'classification', 'applicationRollback']) && value.receipt.status === 'rehearsed'
    && value.receipt.classification === 'data-recovery' && value.receipt.applicationRollback === 'application-rollback-not-requested'
    && artifact && validRecoveryDescriptor(value.source, artifact) && validRecoveryDescriptor(value.target, artifact) && value.source.role === 'source' && value.target.role === 'target'
    && value.source.serverInstanceId !== value.target.serverInstanceId && value.source.imageDigest === value.target.imageDigest
    && value.source.migrationHead === value.target.migrationHead && value.source.migrationCount === value.target.migrationCount
    && JSON.stringify(value.lockedOracles) === JSON.stringify(artifact.sentinels)
    && validRecoveryBackup(value.backup, artifact, value.targetSequences) && exactLockedReads(value.lockedCriticalReads, artifact)
    && exactFingerprintReads(value.sourceCriticalReads, artifact) && JSON.stringify(value.sourceCriticalReads) === JSON.stringify(value.targetCriticalReads)
    && exactSequenceStates(value.targetSequences, artifact) && exactReconciliations(value.backup.sequenceReconciliations, value.targetSequences, artifact)
    && Number.isInteger(value.durationMilliseconds) && value.durationMilliseconds > 0;
}

function validRecoveryDescriptor(value, artifact) {
  return isExactObject(value, ['role', 'serverInstanceId', 'imageDigest', 'postgreSqlVersion', 'migrationHead', 'migrationCount'])
    && /^[a-f0-9]{32}$/.test(value.serverInstanceId) && value.imageDigest === artifact.postgresql.containerDigest
    && (value.postgreSqlVersion === artifact.postgresql.producerVersion || value.postgreSqlVersion.startsWith(`${artifact.postgresql.producerVersion} `) || value.postgreSqlVersion.startsWith(`${artifact.postgresql.producerVersion} (`))
    && value.migrationHead === artifact.migration.head && value.migrationCount === artifact.migration.count;
}

function validRecoveryBackup(value, artifact, targetSequences) {
  return value !== null && isExactObject(value, ['fileName', 'size', 'sha256', 'repositoryMigration', 'tables', 'ownedSequences', 'sequenceReconciliations', 'artifacts'])
    && value.fileName === 'quran-canonical-recovery.dump' && Number.isInteger(value.size) && value.size > 0 && /^[a-f0-9]{64}$/.test(value.sha256)
    && isExactObject(value.repositoryMigration, ['head', 'count']) && value.repositoryMigration.head === artifact.migration.head && value.repositoryMigration.count === artifact.migration.count
    && JSON.stringify(value.tables) === JSON.stringify(artifact.tableScope.tables) && JSON.stringify(value.ownedSequences) === JSON.stringify(artifact.tableScope.ownedSequences)
    && Array.isArray(value.sequenceReconciliations) && value.sequenceReconciliations.length > 0 && Array.isArray(value.artifacts)
    && value.artifacts.length === 1 && validRecoveredArtifact(value.artifacts[0], artifact, targetSequences);
}

function validRecoveredArtifact(value, artifact, targetSequences) {
  return isExactObject(value, ['id', 'immutableStorageId', 'tables', 'sentinels', 'stagedFiles', 'sources', 'criticalReads', 'sequences'])
    && value.id === artifact.id && value.immutableStorageId === artifact.immutableStorageId
    && JSON.stringify(value.tables) === JSON.stringify(artifact.tableCounts)
    && JSON.stringify(value.stagedFiles) === JSON.stringify(artifact.stagedFiles)
    && JSON.stringify(value.sources) === JSON.stringify(artifact.sources)
    && JSON.stringify(value.sentinels) === JSON.stringify(restoreSentinels(artifact))
    && JSON.stringify(value.criticalReads) === JSON.stringify(lockedReads(artifact))
    && exactSequenceStates(value.sequences, artifact) && JSON.stringify(value.sequences) === JSON.stringify(targetSequences);
}

function exactLockedReads(reads, artifact) {
  return Array.isArray(reads) && JSON.stringify(reads) === JSON.stringify(lockedReads(artifact));
}

function lockedReads(artifact) {
  return (artifact.restore?.sentinelTables ?? []).map(({ id, criticalReadSha256 }) => ({ id, sha256: criticalReadSha256 }));
}

function restoreSentinels(artifact) {
  return (artifact.restore?.sentinelTables ?? []).map(({ id, table, expectedCount }) => ({ id, table, expectedCount, actualCount: expectedCount }));
}

function exactFingerprintReads(reads, artifact) {
  return Array.isArray(reads) && JSON.stringify(reads) === JSON.stringify((artifact.restore?.sentinelTables ?? []).map(({ id, criticalReadSha256 }) => ({ key: id, value: criticalReadSha256 })));
}

function exactSequenceStates(states, artifact, reconciled = true) {
  return Array.isArray(states) && states.length === artifact.tableScope.ownedSequences.length && states.every((state, index) => isExactObject(state, ['ownership', 'highWaterMark', 'lastValue', 'isCalled', 'incrementBy', 'nextValue'])
    && JSON.stringify(state.ownership) === JSON.stringify(artifact.tableScope.ownedSequences[index])
    && (state.highWaterMark === null || Number.isInteger(state.highWaterMark)) && Number.isInteger(state.lastValue) && typeof state.isCalled === 'boolean'
    && Number.isSafeInteger(state.incrementBy) && state.incrementBy > 0 && Number.isSafeInteger(state.lastValue) && Number.isSafeInteger(state.nextValue)
    && state.nextValue === (state.isCalled ? state.lastValue + state.incrementBy : state.lastValue)
    && (!reconciled || state.highWaterMark === null || state.nextValue > state.highWaterMark));
}

function exactReconciliations(reconciliations, targetSequences, artifact) {
  return Array.isArray(reconciliations) && reconciliations.length === artifact.tableScope.ownedSequences.length && reconciliations.every((entry, index) => isExactObject(entry, ['original', 'reconciled'])
    && exactSequenceStates([entry.original], { tableScope: { ownedSequences: [artifact.tableScope.ownedSequences[index]] } }, false)
    && exactSequenceStates([entry.reconciled], { tableScope: { ownedSequences: [artifact.tableScope.ownedSequences[index]] } })
    && entry.original.highWaterMark === entry.reconciled.highWaterMark && JSON.stringify(entry.reconciled) === JSON.stringify(targetSequences[index]));
}

function attribute(attributes, name) {
  return new RegExp(`${name}="([^"]*)"`).exec(attributes)?.[1];
}

function integerAttribute(attributes, name) {
  const value = attribute(attributes, name);
  return value !== undefined && /^\d+$/.test(value) ? Number(value) : -1;
}

function readText(path) {
  try { return readFileSync(path, 'utf8'); } catch { return ''; }
}

function readJsonOrNull(path) {
  try { return JSON.parse(readFileSync(path, 'utf8')); } catch { return null; }
}

function readJson(path, name) {
  try { return JSON.parse(readFileSync(path, 'utf8')); } catch { throw new Error(`Cannot read ${name}.`); }
}

function isIsoTimestamp(value) {
  return typeof value === 'string' && /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}Z$/.test(value);
}

function isInside(parent, child) {
  const pathFromParent = relative(resolve(parent), resolve(child));
  return pathFromParent === ''
    || (pathFromParent !== '..' && !pathFromParent.startsWith(`..${sep}`) && !isAbsolute(pathFromParent));
}

function requirePositiveInteger(value, name) {
  requireCondition(Number.isInteger(value) && value > 0, `${name} must be a positive integer.`);
}

function requireNonEmptyString(value, name) {
  requireCondition(typeof value === 'string' && value.length > 0, `${name} must be a non-empty string.`);
}

function requireCondition(condition, message) {
  if (!condition) throw new Error(`Invalid release candidate manifest: ${message}`);
}
