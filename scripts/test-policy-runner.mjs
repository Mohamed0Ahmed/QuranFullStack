import { readFileSync } from 'node:fs';
import { cpus, loadavg } from 'node:os';

const BACKEND_HEADER = [
  'FullyQualifiedClassName',
  'Feature',
  'Kind',
  'Gate',
  'Concerns',
  'BackendPolicy',
  'DataReads',
  'DataWrites',
  'DatabaseTarget',
  'DestructiveSubtype',
  'ResourceCollection',
  'MigrationState',
];

const RESOURCE_HEADER = [
  'CollectionName',
  'ResourceClassName',
  'ParallelPolicy',
  'StatePolicy',
  'SetupWrites',
  'ResetBehavior',
  'DatabaseTarget',
  'StartupEffects',
  'MigrationState',
];

const BACKEND_POLICIES = new Set([
  'FastNoDb',
  'CanonicalReader',
  'GuardedReader',
  'MutableWriter',
  'DestructiveRehearsal',
]);
const DATABASE_TARGETS = new Set(['None', 'TestDatabase', 'EmptyScratch', 'FullRehearsal']);
const DESTRUCTIVE_SUBTYPES = new Set([
  'None',
  'CanonicalImport',
  'CanonicalRebuild',
  'CanonicalGeneration',
  'Migration',
  'SystemCatalogueReconciliation',
  'SchemaDrift',
  'PhraseSearchIndexBuild',
  'Recovery',
]);
const DATA_CLASSES = new Set([
  'CanonicalQuranData',
  'SystemCatalogue',
  'MutableApplicationState',
  'SchemaState',
]);
const PROTECTED_DATA_CLASSES = new Set([
  'CanonicalQuranData',
  'SystemCatalogue',
  'SchemaState',
]);
const RESET_BEHAVIORS = new Set(['None', 'MutableApplicationState', 'ScratchDatabase']);
const STARTUP_EFFECTS = new Set(['ReadOnlyApi', 'MutableApi', 'DestructiveApi']);
const POLICY_SEVERITY = new Map([
  ['FastNoDb', 0],
  ['CanonicalReader', 1],
  ['GuardedReader', 2],
  ['MutableWriter', 3],
  ['DestructiveRehearsal', 4],
]);

export const EXECUTION_GROUPS = Object.freeze([
  'FastNoDb',
  'CanonicalReader',
  'GuardedReader',
  'MutableWriter',
  'EmptyScratchDestructiveRehearsal',
  'FullDataDestructiveRehearsal',
]);
const ALL_GROUPS = [...EXECUTION_GROUPS];

// Every planned command is attributed to exactly one phase. `provisioning` covers the manually
// provisioned capabilities the architecture keeps outside the suite execution cost; `activeGate` covers
// build, preflight, resets, application startup/shutdown, tests, and final cleanup.
export const EXECUTION_PHASES = Object.freeze(['provisioning', 'activeGate']);

// The 12-minute target applies only to active pre-PR gate time -- never to lock contention or to
// capability/manual provisioning.
export const PRE_PR_ACTIVE_GATE_TARGET_MILLISECONDS = 12 * 60 * 1000;

export const RUN_EVIDENCE_PATH_ENVIRONMENT_VARIABLE = 'QURAN_DASHBOARD_RUN_EVIDENCE_PATH';
export const RUN_EVIDENCE_COMMAND_ID_ENVIRONMENT_VARIABLE = 'QURAN_DASHBOARD_TEST_COMMAND_ID';
const RUNNER_KEEPER_LOCK_COMMANDS = new Set(['scratch-rehearsal', 'full-rehearsal']);
const FINGERPRINT_KINDS = ['full', 'verifiedCanonical'];
const LEASE_KINDS = ['exclusive', 'shared'];
const SUB_PHASE_NAMES = ['fixtureInit', 'boundaryCheck', 'perTestReset', 'testBody'];

export function captureMachineLoad(now = () => new Date()) {
  const [loadAverage1m, loadAverage5m, loadAverage15m] = loadavg();
  return {
    capturedAt: now().toISOString(),
    loadAverage1m,
    loadAverage5m,
    loadAverage15m,
    cpuCount: cpus().length,
  };
}

export function retainExecutionStatusAfterTimingFailure(executionStatus) {
  return executionStatus;
}

export function readRunEvidenceEvents(path) {
  let source;
  try {
    source = readFileSync(path, 'utf8');
  } catch (error) {
    if (error.code === 'ENOENT') {
      return [];
    }
    throw error;
  }
  return source.split('\n').filter(Boolean).map((line, index) => {
    try {
      return JSON.parse(line);
    } catch {
      throw new Error(`Run evidence line ${index + 1} is not JSON.`);
    }
  });
}

export function summarizeExecutionTiming({
  mode,
  records,
  totalWallMilliseconds,
  events = [],
  machineLoad = null,
}) {
  if (!['focused', 'pre-pr'].includes(mode)) {
    throw new Error('Execution timing requires a focused or pre-pr mode.');
  }
  if (!Array.isArray(records)) {
    throw new Error('Execution timing requires an array of executed command records.');
  }
  if (!Array.isArray(events)) {
    throw new Error('Execution timing requires an array of evidence events.');
  }

  const commands = records.map((record) => {
    if (!EXECUTION_PHASES.includes(record.phase)) {
      throw new Error(`Unknown execution phase for ${record.id}: ${record.phase}`);
    }
    const durationMilliseconds = requireNonNegativeDuration(
      record.durationMilliseconds,
      `${record.id} elapsed time`,
    );
    const lockWaitMilliseconds = requireNonNegativeDuration(
      record.lockWaitMilliseconds ?? 0,
      `${record.id} lock wait`,
    );
    // A command may spend part of its elapsed time validating a manually provisioned capability even
    // when the command itself belongs to the active gate. That portion is reported as provisioning.
    const capabilityMilliseconds = requireNonNegativeDuration(
      record.capabilityMilliseconds ?? 0,
      `${record.id} capability validation`,
    );
    if (lockWaitMilliseconds > durationMilliseconds) {
      throw new Error(`${record.id} reported more lock wait than elapsed time.`);
    }
    if (lockWaitMilliseconds + capabilityMilliseconds > durationMilliseconds) {
      throw new Error(`${record.id} reported more lock wait and capability time than elapsed time.`);
    }
    const subPhases = record.subPhases ?? subPhasesForCommand(record.id, events);
    const journeys = record.journeys ?? journeysForCommand(record.id, events);
    return {
      id: record.id,
      phase: record.phase,
      group: record.group ?? null,
      status: record.status ?? null,
      durationMilliseconds,
      lockWaitMilliseconds,
      capabilityMilliseconds,
      ...(subPhases ? { subPhases } : {}),
      ...(journeys ? { journeys } : {}),
    };
  });

  const remainingMilliseconds = (entry) =>
    entry.durationMilliseconds - entry.lockWaitMilliseconds - entry.capabilityMilliseconds;
  const sumOf = (entries, project) => entries.reduce((total, entry) => total + project(entry), 0);

  const lockWaitMilliseconds = sumOf(commands, (entry) => entry.lockWaitMilliseconds);
  const provisioningMilliseconds = sumOf(commands, (entry) => entry.capabilityMilliseconds)
    + sumOf(commands.filter((entry) => entry.phase === 'provisioning'), remainingMilliseconds);
  const activeGateMilliseconds = sumOf(
    commands.filter((entry) => entry.phase === 'activeGate'),
    remainingMilliseconds,
  );
  const totalWall = requireNonNegativeDuration(totalWallMilliseconds, 'total wall time');
  const applies = mode === 'pre-pr';
  const fingerprints = summarizeFingerprints(events);
  const leases = summarizeLeases(events);
  const inChildLockWaitMilliseconds = sumOf(
    events.filter((event) => event.event === 'lease' && !RUNNER_KEEPER_LOCK_COMMANDS.has(event.command)),
    (event) => requireNonNegativeDuration(event.waitMilliseconds ?? 0, 'in-child lock wait'),
  );

  return {
    evidenceVersion: 1,
    evidenceType: 'test-execution-timing',
    mode,
    lockWaitMilliseconds,
    provisioningMilliseconds,
    activeGateMilliseconds,
    totalWallMilliseconds: totalWall,
    unattributedMilliseconds: Math.max(
      0,
      totalWall - lockWaitMilliseconds - provisioningMilliseconds - activeGateMilliseconds,
    ),
    fingerprints,
    leases,
    inChildLockWaitMilliseconds,
    testCaseIds: [...new Set(
      events
        .filter((event) => event.event === 'testCase' && typeof event.id === 'string' && event.id.length > 0)
        .map((event) => event.id),
    )].sort((left, right) => left.localeCompare(right)),
    machineLoad,
    activeGateTarget: {
      applies,
      targetMilliseconds: applies ? PRE_PR_ACTIVE_GATE_TARGET_MILLISECONDS : null,
      withinTarget: applies
        ? activeGateMilliseconds <= PRE_PR_ACTIVE_GATE_TARGET_MILLISECONDS
        : null,
    },
    commands,
  };
}

function summarizeFingerprints(events) {
  const summary = {
    full: { count: 0, totalMilliseconds: 0 },
    verifiedCanonical: { count: 0, totalMilliseconds: 0 },
  };
  for (const event of events) {
    if (event.event !== 'fingerprint') {
      continue;
    }
    if (!FINGERPRINT_KINDS.includes(event.kind)) {
      throw new Error(`Unknown fingerprint kind: ${event.kind}`);
    }
    const durationMilliseconds = requireNonNegativeDuration(
      event.durationMilliseconds,
      `${event.kind} fingerprint duration`,
    );
    summary[event.kind].count += 1;
    summary[event.kind].totalMilliseconds += durationMilliseconds;
  }
  return summary;
}

function summarizeLeases(events) {
  const summary = {
    exclusive: { count: 0, waitMilliseconds: 0 },
    shared: { count: 0, waitMilliseconds: 0 },
  };
  for (const event of events) {
    if (event.event !== 'lease') {
      continue;
    }
    if (!LEASE_KINDS.includes(event.kind)) {
      throw new Error(`Unknown lease kind: ${event.kind}`);
    }
    summary[event.kind].count += 1;
    summary[event.kind].waitMilliseconds += requireNonNegativeDuration(
      event.waitMilliseconds ?? 0,
      `${event.kind} lease wait`,
    );
  }
  return summary;
}

function subPhasesForCommand(commandId, events) {
  const matches = events.filter((event) => event.event === 'subPhase' && event.commandId === commandId);
  if (matches.length === 0) {
    return null;
  }
  const subPhases = {
    fixtureInitMilliseconds: 0,
    boundaryCheckMilliseconds: 0,
    perTestResetMilliseconds: 0,
    testBodyMilliseconds: 0,
  };
  for (const event of matches) {
    if (!SUB_PHASE_NAMES.includes(event.name)) {
      throw new Error(`Unknown command sub-phase: ${event.name}`);
    }
    subPhases[`${event.name}Milliseconds`] += requireNonNegativeDuration(
      event.durationMilliseconds,
      `${event.name} sub-phase`,
    );
  }
  return subPhases;
}

function journeysForCommand(commandId, events) {
  const matches = events.filter((event) => event.event === 'journeyPhase' && event.commandId === commandId);
  if (matches.length === 0) {
    return null;
  }
  const journeys = [];
  for (const event of matches) {
    let journey = journeys.find((candidate) => candidate.selector === event.journey);
    if (!journey) {
      journey = { selector: event.journey, phases: [] };
      journeys.push(journey);
    }
    journey.phases.push({
      name: event.name,
      durationMilliseconds: requireNonNegativeDuration(
        event.durationMilliseconds,
        `${event.journey} ${event.name}`,
      ),
    });
  }
  return journeys;
}

function requireNonNegativeDuration(value, subject) {
  if (typeof value !== 'number' || !Number.isFinite(value)) {
    throw new Error(`${subject} must be a finite number of milliseconds.`);
  }
  if (value < 0) {
    throw new Error(`${subject} must not be negative.`);
  }
  return Math.round(value);
}

export function assessScratchLifecycleResult({
  action,
  runId,
  subtype = null,
  processStatus,
  report,
  durationMilliseconds,
  processError = false,
  parseError = false,
}) {
  const reportObject = report !== null && typeof report === 'object' ? report : null;
  const evidenceShapeValid = reportObject !== null
    && typeof reportObject.succeeded === 'boolean'
    && Array.isArray(reportObject.violations)
    && reportObject.scratch !== null
    && typeof reportObject.scratch === 'object'
    && reportObject.scratch.mode === action
    && sanitizeDatabaseIdentity(reportObject.scratch.database) !== null
    && typeof reportObject.scratch.validated === 'boolean'
    && typeof reportObject.scratch.removed === 'boolean'
    && Number.isInteger(reportObject.scratch.dumpFilesRetained)
    && reportObject.scratch.dumpFilesRetained >= 0;
  const identityMatches = evidenceShapeValid
    && /^[0-9a-f]{32}$/.test(runId)
    && reportObject.scratch.runId === runId
    && reportObject.scratch.database === `quran_test_scratch_${runId}`
    && reportObject.scratch.subtype === (action === 'reap' ? null : subtype);
  const evidenceValid = evidenceShapeValid && identityMatches;
  const successfulEvidence = evidenceValid
    && reportObject.scratch.validated === true
    && reportObject.scratch.dumpFilesRetained === 0
    && (action !== 'create' || reportObject.scratch.receiptRecorded === true)
    && (action !== 'cleanup' || reportObject.scratch.removed === true);
  const failureCategory = processError
    ? 'process-start-failed'
    : parseError
      ? 'invalid-json-evidence'
      : reportObject === null
        ? 'missing-evidence'
        : !evidenceShapeValid
          ? 'invalid-evidence'
          : !identityMatches
            ? 'identity-mismatch'
            : reportObject.succeeded !== true
              ? 'lifecycle-failed'
              : !successfulEvidence
                ? 'invalid-evidence'
                : null;
  const reportedStatus = Number.isInteger(processStatus) ? processStatus : 1;

  return {
    status: reportedStatus === 0 && failureCategory !== null ? 1 : reportedStatus,
    report: reportObject,
    evidenceValid,
    failureCategory,
    durationMilliseconds: normalizeDuration(durationMilliseconds),
  };
}

export function createEmptyScratchExecutionEvidence({
  command,
  runId,
  keeperStatus,
  keeperExitStatus = null,
  keeperDurationMilliseconds = null,
  reap = null,
  create = null,
  testStatus = null,
  testDurationMilliseconds = null,
  cleanup = null,
  totalDurationMilliseconds = null,
  finalStatus,
}) {
  const createScratch = create?.report?.scratch;
  const cleanupScratch = cleanup?.report?.scratch;
  const succeeded = keeperStatus === 'acquired'
    && keeperExitStatus === 0
    && reap?.status === 0
    && reap?.evidenceValid === true
    && reap?.report?.succeeded === true
    && lifecycleIdentityMatches(reap, 'reap', runId, null)
    && create?.status === 0
    && create?.evidenceValid === true
    && create?.report?.succeeded === true
    && lifecycleIdentityMatches(create, 'create', runId, command.scratchSubtype)
    && testStatus === 0
    && cleanup?.status === 0
    && cleanup?.evidenceValid === true
    && cleanup?.report?.succeeded === true
    && lifecycleIdentityMatches(cleanup, 'cleanup', runId, command.scratchSubtype)
    && cleanupScratch?.removed === true
    && cleanupScratch?.dumpFilesRetained === 0
    && finalStatus === 0;

  return {
    evidenceVersion: 1,
    evidenceType: 'empty-scratch-test-execution',
    scope: command.selection ?? {
      group: command.group,
    },
    scratch: {
      runId,
      subtype: command.scratchSubtype ?? null,
      database: sanitizeDatabaseIdentity(createScratch?.database)
        ?? sanitizeDatabaseIdentity(cleanupScratch?.database),
    },
    lifecycle: {
      keeper: {
        status: keeperStatus,
        exitStatus: keeperExitStatus,
        durationMilliseconds: normalizeDuration(keeperDurationMilliseconds),
      },
      reap: summarizeScratchLifecycleResult(reap),
      create: summarizeScratchLifecycleResult(create),
      test: testStatus === null
        ? null
        : {
            status: testStatus,
            succeeded: testStatus === 0,
            durationMilliseconds: normalizeDuration(testDurationMilliseconds),
          },
      cleanup: summarizeScratchLifecycleResult(cleanup),
    },
    timings: {
      keeperMilliseconds: normalizeDuration(keeperDurationMilliseconds),
      reapMilliseconds: reap?.durationMilliseconds ?? null,
      createMilliseconds: create?.durationMilliseconds ?? null,
      testMilliseconds: normalizeDuration(testDurationMilliseconds),
      cleanupMilliseconds: cleanup?.durationMilliseconds ?? null,
      totalMilliseconds: normalizeDuration(totalDurationMilliseconds),
    },
    finalStatus,
    succeeded,
  };
}

function summarizeScratchLifecycleResult(result) {
  if (result === null) {
    return null;
  }

  const scratch = result.report?.scratch;
  return {
    status: result.status,
    succeeded: result.report?.succeeded === true,
    evidenceValid: result.evidenceValid === true,
    failureCategory: sanitizeEvidenceCode(result.failureCategory),
    failureType: sanitizeEvidenceCode(result.report?.failureType),
    violationCodes: Array.isArray(result.report?.violations)
      ? [...new Set(result.report.violations
          .map(({ code }) => sanitizeEvidenceCode(code))
          .filter((code) => code !== null))].sort()
      : [],
    durationMilliseconds: normalizeDuration(result.durationMilliseconds),
    mode: sanitizeEvidenceCode(scratch?.mode),
    database: sanitizeDatabaseIdentity(scratch?.database),
    receiptRecorded: normalizeBoolean(scratch?.receiptRecorded),
    validated: normalizeBoolean(scratch?.validated),
    removed: normalizeBoolean(scratch?.removed),
    dumpFilesRetained: normalizeNonnegativeInteger(scratch?.dumpFilesRetained),
  };
}

function sanitizeEvidenceCode(value) {
  return typeof value === 'string' && /^[a-z0-9][a-z0-9._-]{0,127}$/i.test(value)
    ? value
    : null;
}

function lifecycleIdentityMatches(result, mode, runId, subtype) {
  const scratch = result?.report?.scratch;
  return scratch?.mode === mode
    && scratch?.runId === runId
    && scratch?.database === `quran_test_scratch_${runId}`
    && scratch?.subtype === subtype;
}

function sanitizeDatabaseIdentity(value) {
  return typeof value === 'string' && /^[a-z_][a-z0-9_]{0,62}$/.test(value)
    ? value
    : null;
}

function normalizeBoolean(value) {
  return typeof value === 'boolean' ? value : null;
}

function normalizeNonnegativeInteger(value) {
  return Number.isInteger(value) && value >= 0 ? value : null;
}

function normalizeDuration(value) {
  return Number.isFinite(value) && value >= 0 ? Math.round(value) : null;
}

export function parseBackendPolicyCatalog(source) {
  const lines = source.replace(/\r/g, '').trimEnd().split('\n');
  if (lines.length < 2) {
    throw new Error('Backend policy catalog must contain a header and at least one row.');
  }
  const actualHeader = lines[0].split('\t');
  if (!sameValues(actualHeader, BACKEND_HEADER)) {
    throw new Error(`Unexpected Backend policy catalog header: ${actualHeader.join(', ')}`);
  }

  const entries = lines.slice(1).filter(Boolean).map((line, index) => {
    const columns = line.split('\t');
    const lineNumber = index + 2;
    if (columns.length !== BACKEND_HEADER.length) {
      throw invalidRow(lineNumber, `expected ${BACKEND_HEADER.length} columns, found ${columns.length}`);
    }
    if (columns.slice(0, 4).some((value) => value.length === 0)) {
      throw invalidRow(lineNumber, 'class, feature, kind, and gate are required');
    }

    const [
      className,
      feature,
      kind,
      gate,
      concerns,
      backendPolicy,
      dataReads,
      dataWrites,
      databaseTarget,
      destructiveSubtype,
      resourceCollection,
      migrationState,
    ] = columns;
    const policyValues = columns.slice(5, 11);
    if (migrationState === 'Migrated') {
      if (policyValues.some((value) => value.length === 0)) {
        throw invalidRow(lineNumber, 'migrated classes require complete policy metadata');
      }
      validateMigratedPolicy(
        { backendPolicy, dataReads, dataWrites, databaseTarget, destructiveSubtype },
        lineNumber,
      );
    } else {
      throw invalidRow(lineNumber, `unsupported MigrationState ${migrationState}`);
    }

    return {
      className,
      feature,
      kind,
      gate,
      concerns: splitSet(concerns),
      policy: {
            backendPolicy,
            dataReads: splitSet(dataReads),
            dataWrites: splitSet(dataWrites),
            databaseTarget,
            destructiveSubtype,
          },
      resourceCollection: resourceCollection !== 'None' ? resourceCollection : null,
      migrationState,
    };
  });

  const classNames = new Set();
  for (const entry of entries) {
    if (classNames.has(entry.className)) {
      throw new Error(`Duplicate Backend policy class: ${entry.className}`);
    }
    classNames.add(entry.className);
  }
  return entries;
}

export function parseBackendResourceCatalog(source) {
  const lines = source.replace(/\r/g, '').trimEnd().split('\n');
  if (lines.length < 1) {
    throw new Error('Backend resource policy catalog must contain a header.');
  }
  const actualHeader = lines[0].split('\t');
  if (!sameValues(actualHeader, RESOURCE_HEADER)) {
    throw new Error(`Unexpected Backend resource policy catalog header: ${actualHeader.join(', ')}`);
  }

  const resources = lines.slice(1).filter(Boolean).map((line, index) => {
    const columns = line.split('\t');
    const lineNumber = index + 2;
    if (columns.length !== RESOURCE_HEADER.length) {
      throw invalidResourceRow(lineNumber, `expected ${RESOURCE_HEADER.length} columns, found ${columns.length}`);
    }
    if (columns.slice(0, 4).some((value) => value.length === 0)) {
      throw invalidResourceRow(lineNumber, 'collection, resource class, parallel policy, and state policy are required');
    }

    const [
      collectionName,
      resourceClassName,
      parallelPolicy,
      statePolicy,
      setupWrites,
      resetBehavior,
      databaseTarget,
      startupEffects,
      migrationState,
    ] = columns;
    const policyValues = columns.slice(4, 8);
    if (migrationState === 'Migrated') {
      if (policyValues.some((value) => value.length === 0)) {
        throw invalidResourceRow(lineNumber, 'migrated resources require complete policy metadata');
      }
      validateResourcePolicy(
        { setupWrites, resetBehavior, databaseTarget, startupEffects },
        lineNumber,
      );
    } else {
      throw invalidResourceRow(lineNumber, `unsupported MigrationState ${migrationState}`);
    }

    return {
      collectionName,
      resourceClassName,
      parallelPolicy,
      statePolicy,
      policy: {
            setupWrites: parseExplicitSet(setupWrites, DATA_CLASSES, lineNumber, 'SetupWrites'),
            resetBehavior,
            databaseTarget,
            startupEffects: parseExplicitSet(startupEffects, STARTUP_EFFECTS, lineNumber, 'StartupEffects'),
          },
      migrationState,
    };
  });

  const collectionNames = new Set();
  for (const resource of resources) {
    if (collectionNames.has(resource.collectionName)) {
      throw new Error(`Duplicate Backend resource collection: ${resource.collectionName}`);
    }
    collectionNames.add(resource.collectionName);
  }
  return resources;
}

export function planFocusedSelection({
  backendCatalog,
  backendResources = [],
  backendClasses,
  backendMethods,
  buildMode,
  playwrightSelections,
  authorizeFullData = false,
}) {
  if (!['build', 'no-build'].includes(buildMode)) {
    throw new Error('Focused selection requires exactly one build mode: build or no-build.');
  }

  const selections = [];
  for (const className of backendClasses) {
    const entry = requireBackendClass(backendCatalog, className);
    selections.push(backendSelection(entry, backendResources, 'class', className));
  }
  for (const methodName of backendMethods) {
    const entry = requireBackendMethodClass(backendCatalog, methodName);
    selections.push(backendSelection(entry, backendResources, 'method', methodName));
  }
  for (const selection of playwrightSelections) {
    requireExactPlaywrightSelection(selection);
    selections.push({
      kind: 'playwright',
      selectorType: 'file-line',
      selector: `${selection.file}:${selection.line}`,
      group: selection.effectiveGroup,
    });
  }
  if (selections.length === 0) {
    throw new Error('Focused selection requires at least one Backend class/method or Playwright file:line.');
  }

  const uniqueSelections = deduplicateSelections(selections);
  const partitions = partitionSelections(uniqueSelections);
  const authorizationRequired = uniqueSelections
    .filter(({ group }) => group === 'FullDataDestructiveRehearsal')
    .map(({ selector }) => selector)
    .sort((left, right) => left.localeCompare(right));
  const executableSelections = authorizeFullData
    ? uniqueSelections
    : uniqueSelections.filter(({ group }) => group !== 'FullDataDestructiveRehearsal');
  const executablePartitions = partitionSelections(executableSelections);
  const commands = [];
  if (buildMode === 'build' && executableSelections.some(({ kind }) => kind === 'backend')) {
    commands.push(backendBuildCommand());
  }
  commands.push(...executablePartitions.flatMap(
    ({ selections: partition }) => partition.map(selectionCommand),
  ));

  return {
    mode: 'focused',
    buildMode,
    authorizeFullData,
    authorizationRequired: authorizeFullData ? [] : authorizationRequired,
    partitions,
    commands,
  };
}

export function planPrePrSelection({
  backendCatalog,
  backendResources = [],
  affectedFeatures,
  affectedConcerns,
  authorizeFullData,
  explicitPolicy = null,
}) {
  if (explicitPolicy !== null && !['scheduled', 'release'].includes(explicitPolicy)) {
    throw new Error(`Unsupported explicit test policy: ${explicitPolicy}`);
  }
  const featureSet = new Set(affectedFeatures);
  const concernSet = new Set(affectedConcerns);
  const knownFeatures = new Set(backendCatalog.map(({ feature }) => feature));
  const knownConcerns = new Set(backendCatalog.flatMap(({ concerns }) => concerns));
  for (const feature of featureSet) {
    if (!knownFeatures.has(feature)) {
      throw new Error(`Unknown affected Backend feature: ${feature}`);
    }
  }
  for (const concern of concernSet) {
    if (!knownConcerns.has(concern)) {
      throw new Error(`Unknown affected Backend concern: ${concern}`);
    }
  }
  const requiredCandidates = backendCatalog.filter((entry) =>
    entry.kind !== 'Release'
    && entry.gate !== 'Pipeline'
    && !isEmptyScratchEntry(entry, backendResources)
    && !isFullDataEntry(entry, backendResources),
  );
  const affectedCandidates = backendCatalog.filter((entry) =>
    (entry.gate === 'Pipeline' || isEmptyScratchEntry(entry, backendResources))
    && (featureSet.has(entry.feature) || entry.concerns.some((concern) => concernSet.has(concern))),
  );
  const explicitCandidates = explicitPolicy === null
    ? []
    : backendCatalog.filter((entry) =>
        entry.kind === 'Release'
        || entry.gate === 'Release'
        || (entry.gate === 'Pipeline'
          && (isEmptyScratchEntry(entry, backendResources)
            || isFullDataEntry(entry, backendResources))),
      );
  const candidates = deduplicateEntries([
    ...requiredCandidates,
    ...affectedCandidates,
    ...explicitCandidates,
  ]);

  const authorizationRequired = [];
  const plannedSelections = [];
  const permittedSelections = [];
  for (const entry of candidates) {
    const fullData = isFullDataEntry(entry, backendResources);
    const selection = backendSelection(entry, backendResources, 'class', entry.className);
    plannedSelections.push(selection);
    if (fullData && !authorizeFullData) {
      authorizationRequired.push(entry.className);
      continue;
    }
    permittedSelections.push(selection);
  }

  authorizationRequired.sort((left, right) => left.localeCompare(right));
  const partitions = partitionSelections(deduplicateSelections(plannedSelections));
  const executablePartitions = partitionSelections(deduplicateSelections(permittedSelections));
  const commands = [
    backendBuildCommand(),
    ...executablePartitions.flatMap(({ selections }) => selections.map(selectionCommand)),
    command('frontend-pre-pr', 'Frontend/quran-dashboard-ui', 'npm', ['run', 'test:pre-pr']),
    command('playwright-typecheck', 'Frontend/quran-dashboard-ui', 'npm', ['run', 'e2e:typecheck']),
    // Controlled provisioning hashes the built Backend and Frontend outputs, so it must run after
    // `backend-build` and `frontend-pre-pr` have finished rebuilding them and before any controlled
    // Playwright lane validates the receipt.
    command(
      'playwright-provision',
      'Frontend/quran-dashboard-ui',
      'npm',
      ['run', 'e2e:provision'],
      null,
      { phase: 'provisioning' },
    ),
    command(
      'playwright-canonical-critical',
      'Frontend/quran-dashboard-ui',
      'npm',
      ['run', 'e2e:canonical:critical'],
      'CanonicalReader',
    ),
    command(
      'playwright-stateful-critical',
      'Frontend/quran-dashboard-ui',
      'npm',
      ['run', 'e2e:stateful:critical'],
    ),
  ];

  return {
    mode: 'pre-pr',
    affectedFeatures: [...featureSet].sort(),
    affectedConcerns: [...concernSet].sort(),
    authorizeFullData,
    explicitPolicy,
    authorizationRequired,
    requiredGates: ['backend-risk', 'frontend-policy-build', 'playwright-critical'],
    partitions,
    commands,
  };
}

function isFullDataEntry(entry, resourceCatalog) {
  return effectiveBackendPolicy(entry, resourceCatalog).target === 'FullRehearsal';
}

function isEmptyScratchEntry(entry, resourceCatalog) {
  return entry.migrationState === 'Migrated'
    && effectiveBackendPolicy(entry, resourceCatalog).target === 'EmptyScratch';
}

function deduplicateEntries(entries) {
  const classNames = new Set();
  return entries.filter((entry) => {
    if (classNames.has(entry.className)) {
      return false;
    }
    classNames.add(entry.className);
    return true;
  });
}

function partitionSelections(selections) {
  const groups = new Map(ALL_GROUPS.map((group) => [group, []]));
  for (const selection of selections) {
    if (!groups.has(selection.group)) {
      throw new Error(`Unsupported execution group: ${selection.group}`);
    }
    groups.get(selection.group).push(selection);
  }

  return ALL_GROUPS.flatMap((group) => {
    const groupSelections = groups.get(group).sort((left, right) =>
      `${left.kind}\0${left.selector}`.localeCompare(`${right.kind}\0${right.selector}`),
    );
    return groupSelections.length === 0 ? [] : [{ group, selections: groupSelections }];
  });
}

function backendSelection(entry, resourceCatalog, selectorType, selector) {
  return {
    kind: 'backend',
    selectorType,
    selector,
    className: entry.className,
    feature: entry.feature,
    concerns: [...entry.concerns].sort(),
    group: backendExecutionGroup(entry, resourceCatalog),
    destructiveSubtype: entry.policy?.destructiveSubtype ?? null,
  };
}

function backendExecutionGroup(entry, resourceCatalog) {
  const effective = effectiveBackendPolicy(entry, resourceCatalog);
  if (effective.policy !== 'DestructiveRehearsal') {
    return effective.policy;
  }
  return effective.target === 'EmptyScratch'
    ? 'EmptyScratchDestructiveRehearsal'
    : 'FullDataDestructiveRehearsal';
}

function effectiveBackendPolicy(entry, resourceCatalog) {
  if (!entry.resourceCollection) {
    return { policy: entry.policy.backendPolicy, target: entry.policy.databaseTarget };
  }

  const resource = resourceCatalog.find(
    ({ collectionName }) => collectionName === entry.resourceCollection,
  );
  if (!resource) {
    throw new Error(
      `Migrated Backend class ${entry.className} references unknown resource ${entry.resourceCollection}.`,
    );
  }
  if (resource.migrationState !== 'Migrated' || !resource.policy) {
    throw new Error(
      `Migrated Backend class ${entry.className} references unmigrated resource ${entry.resourceCollection}.`,
    );
  }
  const effectiveTarget = entry.policy.databaseTarget === 'None'
    ? resource.policy.databaseTarget
    : resource.policy.databaseTarget === 'None'
      ? entry.policy.databaseTarget
      : entry.policy.databaseTarget === resource.policy.databaseTarget
        ? entry.policy.databaseTarget
        : null;
  if (!effectiveTarget) {
    throw new Error(
      `Backend class ${entry.className} and resource ${entry.resourceCollection} declare contradictory database targets.`,
    );
  }

  const resourceMinimum = minimumResourcePolicy(resource.policy);
  const effectivePolicy = POLICY_SEVERITY.get(resourceMinimum)
      > POLICY_SEVERITY.get(entry.policy.backendPolicy)
    ? resourceMinimum
    : entry.policy.backendPolicy;
  if (effectivePolicy === 'DestructiveRehearsal'
      && entry.policy.backendPolicy !== 'DestructiveRehearsal') {
    throw new Error(
      `Backend class ${entry.className} is under-classified for destructive fixture/resource effects.`,
    );
  }
  return { policy: effectivePolicy, target: effectiveTarget };
}

function minimumResourcePolicy(policy) {
  if (policy.setupWrites.some((dataClass) => PROTECTED_DATA_CLASSES.has(dataClass))
      || policy.startupEffects.includes('DestructiveApi')
      || policy.resetBehavior === 'ScratchDatabase'
      || ['EmptyScratch', 'FullRehearsal'].includes(policy.databaseTarget)) {
    return 'DestructiveRehearsal';
  }
  if (policy.setupWrites.length > 0
      || policy.resetBehavior === 'MutableApplicationState'
      || policy.startupEffects.includes('MutableApi')) {
    return 'MutableWriter';
  }
  return policy.databaseTarget === 'None' ? 'FastNoDb' : 'CanonicalReader';
}

function selectionCommand(selection) {
  if (selection.kind === 'playwright') {
    const script = selection.group === 'CanonicalReader'
      ? 'e2e:canonical:focused'
      : 'e2e:focused';
    return command(
      `playwright-${selection.selector}`,
      'Frontend/quran-dashboard-ui',
      'npm',
      ['run', script, '--', selection.selector],
      selection.group,
    );
  }

  const option = selection.selectorType === 'method' ? '--test' : '--class';
  const executionMetadata = {
    selection: backendSelectionEvidence(selection),
    ...(selection.group === 'EmptyScratchDestructiveRehearsal'
      ? { scratchSubtype: toScratchSubtype(selection.destructiveSubtype) }
      : selection.group === 'FullDataDestructiveRehearsal'
          && ['PhraseSearchIndexBuild', 'Recovery'].includes(selection.destructiveSubtype)
        ? { rehearsalSubtype: toScratchSubtype(selection.destructiveSubtype) }
        : {}),
  };
  return command(
    `backend-${selection.selectorType}-${selection.selector}`,
    '.',
    'Backend/scripts/test-backend',
    ['feature', option, selection.selector, '--no-build'],
    selection.group,
    executionMetadata,
  );
}

function backendSelectionEvidence(selection) {
  return {
    kind: selection.kind,
    selectorType: selection.selectorType,
    selector: selection.selector,
    className: selection.className,
    feature: selection.feature,
    concerns: selection.concerns,
    group: selection.group,
  };
}

function toScratchSubtype(subtype) {
  return subtype.replace(/[A-Z]/g, (letter, offset) =>
    `${offset === 0 ? '' : '-'}${letter.toLowerCase()}`,
  );
}

function backendBuildCommand() {
  return command('backend-build', 'Backend', 'dotnet', [
    'build',
    'QuranDashboard.sln',
    '--disable-build-servers',
    '-m:1',
    '-p:BuildInParallel=false',
    '-v',
    'minimal',
  ]);
}

function command(id, cwd, executable, arguments_, group = null, metadata = {}) {
  return { id, cwd, executable, arguments: arguments_, group, phase: 'activeGate', ...metadata };
}

function requireBackendClass(catalog, className) {
  const entry = catalog.find((candidate) => candidate.className === className);
  if (!entry) {
    throw new Error(`Unknown Backend test class: ${className}`);
  }
  return entry;
}

function requireBackendMethodClass(catalog, methodName) {
  const candidates = catalog
    .filter(({ className }) => methodName.startsWith(`${className}.`))
    .sort((left, right) => right.className.length - left.className.length);
  if (candidates.length === 0) {
    throw new Error(`Backend method does not belong to a cataloged class: ${methodName}`);
  }
  return candidates[0];
}

function requireExactPlaywrightSelection(selection) {
  if (
    typeof selection?.file !== 'string'
    || !selection.file.endsWith('.e2e.ts')
    || !Number.isInteger(selection.line)
    || selection.line <= 0
    || !ALL_GROUPS.includes(selection.effectiveGroup)
  ) {
    throw new Error('Playwright selections require an exact file, positive line, and policy group.');
  }
}

function deduplicateSelections(selections) {
  const keys = new Set();
  const result = [];
  for (const selection of selections) {
    const key = `${selection.kind}\0${selection.selector}`;
    if (!keys.has(key)) {
      keys.add(key);
      result.push(selection);
    }
  }
  return result;
}

function validateMigratedPolicy(policy, lineNumber) {
  if (!BACKEND_POLICIES.has(policy.backendPolicy)) {
    throw invalidRow(lineNumber, `unsupported BackendPolicy ${policy.backendPolicy}`);
  }
  if (!DATABASE_TARGETS.has(policy.databaseTarget)) {
    throw invalidRow(lineNumber, `unsupported DatabaseTarget ${policy.databaseTarget}`);
  }
  if (!DESTRUCTIVE_SUBTYPES.has(policy.destructiveSubtype)) {
    throw invalidRow(lineNumber, `unsupported DestructiveSubtype ${policy.destructiveSubtype}`);
  }
  if (policy.dataReads.length === 0 || policy.dataWrites.length === 0) {
    throw invalidRow(lineNumber, 'DataReads and DataWrites must use None instead of a blank value');
  }
  const reads = parseExplicitSet(policy.dataReads, DATA_CLASSES, lineNumber, 'DataReads');
  const writes = parseExplicitSet(policy.dataWrites, DATA_CLASSES, lineNumber, 'DataWrites');
  const writesProtectedState = writes.some((dataClass) => PROTECTED_DATA_CLASSES.has(dataClass));
  if (policy.backendPolicy === 'FastNoDb'
      && (policy.dataReads !== 'None'
        || policy.dataWrites !== 'None'
        || policy.databaseTarget !== 'None'
        || policy.destructiveSubtype !== 'None')) {
    throw invalidRow(lineNumber, 'FastNoDb cannot declare database effects');
  }
  if (policy.backendPolicy === 'DestructiveRehearsal'
      && (policy.destructiveSubtype === 'None'
        || !['EmptyScratch', 'FullRehearsal'].includes(policy.databaseTarget))) {
    throw invalidRow(lineNumber, 'DestructiveRehearsal requires an approved subtype and rehearsal target');
  }
  if (writesProtectedState && policy.backendPolicy !== 'DestructiveRehearsal') {
    throw invalidRow(lineNumber, 'Protected State writes require DestructiveRehearsal');
  }
  if (['CanonicalReader', 'GuardedReader'].includes(policy.backendPolicy)
      && (writes.length !== 0
        || policy.databaseTarget !== 'TestDatabase'
        || policy.destructiveSubtype !== 'None')) {
    throw invalidRow(lineNumber, 'reader policies require the Test Database and no writes');
  }
  if (policy.backendPolicy === 'MutableWriter'
      && (writes.some((dataClass) => dataClass !== 'MutableApplicationState')
        || policy.databaseTarget !== 'TestDatabase'
        || policy.destructiveSubtype !== 'None')) {
    throw invalidRow(lineNumber, 'MutableWriter may write only Mutable Application State on the Test Database');
  }
  if (policy.backendPolicy !== 'FastNoDb' && policy.databaseTarget === 'None') {
    throw invalidRow(lineNumber, 'database-aware policies require an explicit target');
  }
  void reads;
}

function validateResourcePolicy(policy, lineNumber) {
  const setupWrites = parseExplicitSet(policy.setupWrites, DATA_CLASSES, lineNumber, 'SetupWrites');
  const startupEffects = parseExplicitSet(
    policy.startupEffects,
    STARTUP_EFFECTS,
    lineNumber,
    'StartupEffects',
  );
  if (!RESET_BEHAVIORS.has(policy.resetBehavior)) {
    throw invalidResourceRow(lineNumber, `unsupported ResetBehavior ${policy.resetBehavior}`);
  }
  if (!DATABASE_TARGETS.has(policy.databaseTarget)) {
    throw invalidResourceRow(lineNumber, `unsupported DatabaseTarget ${policy.databaseTarget}`);
  }
  if (setupWrites.some((dataClass) => PROTECTED_DATA_CLASSES.has(dataClass))
      && !['EmptyScratch', 'FullRehearsal'].includes(policy.databaseTarget)) {
    throw invalidResourceRow(lineNumber, 'Protected State setup writes require a rehearsal target');
  }
  if ((setupWrites.length > 0
        || policy.resetBehavior !== 'None'
        || startupEffects.length > 0)
      && policy.databaseTarget === 'None') {
    throw invalidResourceRow(lineNumber, 'fixture/resource effects require an explicit database target');
  }
  if (startupEffects.includes('DestructiveApi') && policy.databaseTarget === 'TestDatabase') {
    throw invalidResourceRow(lineNumber, 'a destructive API cannot target the persistent Test Database');
  }
  if (policy.resetBehavior === 'ScratchDatabase' && policy.databaseTarget !== 'EmptyScratch') {
    throw invalidResourceRow(lineNumber, 'a full scratch reset requires the EmptyScratch target');
  }
}

function parseExplicitSet(value, allowed, lineNumber, column) {
  if (value === 'None') {
    return [];
  }
  const values = value.split(',').map((item) => item.trim()).filter(Boolean);
  if (values.length === 0 || values.some((item) => !allowed.has(item))) {
    throw invalidRow(lineNumber, `${column} contains an unsupported value`);
  }
  if (new Set(values).size !== values.length) {
    throw invalidRow(lineNumber, `${column} contains duplicate values`);
  }
  return values;
}

function splitSet(value) {
  return value === '' || value === 'None'
    ? []
    : value.split(',').map((item) => item.trim()).filter(Boolean);
}

function sameValues(left, right) {
  return left.length === right.length && left.every((value, index) => value === right[index]);
}

function invalidRow(lineNumber, message) {
  return new Error(`Invalid Backend policy catalog line ${lineNumber}: ${message}.`);
}

function invalidResourceRow(lineNumber, message) {
  return new Error(`Invalid Backend resource policy catalog line ${lineNumber}: ${message}.`);
}
