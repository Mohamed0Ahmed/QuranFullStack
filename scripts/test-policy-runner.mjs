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
const RESET_BEHAVIORS = new Set(['None', 'MutableApplicationState']);
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
const ALL_GROUPS = [...EXECUTION_GROUPS, 'LegacyUnmigrated'];

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
    } else if (migrationState === 'Unmigrated') {
      if (policyValues.some((value) => value.length !== 0)) {
        throw invalidRow(lineNumber, 'unmigrated classes must keep blank policy metadata');
      }
    } else {
      throw invalidRow(lineNumber, `unsupported MigrationState ${migrationState}`);
    }

    return {
      className,
      feature,
      kind,
      gate,
      concerns: splitSet(concerns),
      policy: migrationState === 'Migrated'
        ? {
            backendPolicy,
            dataReads: splitSet(dataReads),
            dataWrites: splitSet(dataWrites),
            databaseTarget,
            destructiveSubtype,
          }
        : null,
      resourceCollection: migrationState === 'Migrated' && resourceCollection !== 'None'
        ? resourceCollection
        : null,
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
    } else if (migrationState === 'Unmigrated') {
      if (policyValues.some((value) => value.length !== 0)) {
        throw invalidResourceRow(lineNumber, 'unmigrated resources must keep blank policy metadata');
      }
    } else {
      throw invalidResourceRow(lineNumber, `unsupported MigrationState ${migrationState}`);
    }

    return {
      collectionName,
      resourceClassName,
      parallelPolicy,
      statePolicy,
      policy: migrationState === 'Migrated'
        ? {
            setupWrites: parseExplicitSet(setupWrites, DATA_CLASSES, lineNumber, 'SetupWrites'),
            resetBehavior,
            databaseTarget,
            startupEffects: parseExplicitSet(startupEffects, STARTUP_EFFECTS, lineNumber, 'StartupEffects'),
          }
        : null,
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
    : backendCatalog.filter((entry) => entry.kind === 'Release' || entry.gate === 'Release');
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
    command(
      'playwright-canonical-critical',
      'Frontend/quran-dashboard-ui',
      'npm',
      ['run', 'e2e:canonical:critical'],
      'CanonicalReader',
    ),
    command('playwright-critical', 'Frontend/quran-dashboard-ui', 'npm', ['run', 'e2e:critical']),
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
  if (entry.migrationState === 'Unmigrated') {
    return entry.kind === 'Canonical' || entry.kind === 'Release';
  }
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
    group: backendExecutionGroup(entry, resourceCatalog),
    destructiveSubtype: entry.policy?.destructiveSubtype ?? null,
    legacyLane: entry.migrationState === 'Unmigrated'
      ? legacyReleaseLane(entry.className)
      : null,
  };
}

function backendExecutionGroup(entry, resourceCatalog) {
  if (entry.migrationState === 'Unmigrated') {
    return entry.kind === 'Canonical' || entry.kind === 'Release'
      ? 'FullDataDestructiveRehearsal'
      : 'LegacyUnmigrated';
  }

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

  if (selection.legacyLane) {
    const arguments_ = [selection.legacyLane];
    if (selection.selectorType === 'method') {
      arguments_.push('--test', selection.selector);
    }
    arguments_.push('--no-build');
    return command(
      `backend-${selection.selectorType}-${selection.selector}`,
      '.',
      'Backend/scripts/test-backend',
      arguments_,
      selection.group,
    );
  }

  const option = selection.selectorType === 'method' ? '--test' : '--class';
  return command(
    `backend-${selection.selectorType}-${selection.selector}`,
    '.',
    'Backend/scripts/test-backend',
    ['feature', option, selection.selector, '--no-build'],
    selection.group,
    selection.group === 'EmptyScratchDestructiveRehearsal'
      ? { scratchSubtype: toScratchSubtype(selection.destructiveSubtype) }
      : selection.group === 'FullDataDestructiveRehearsal'
          && ['PhraseSearchIndexBuild', 'Recovery'].includes(selection.destructiveSubtype)
        ? { rehearsalSubtype: toScratchSubtype(selection.destructiveSubtype) }
        : {},
  );
}

function toScratchSubtype(subtype) {
  return subtype.replace(/[A-Z]/g, (letter, offset) =>
    `${offset === 0 ? '' : '-'}${letter.toLowerCase()}`,
  );
}

function legacyReleaseLane(className) {
  const lanes = new Map([
    [
      'QuranDashboard.Tests.Quran.PhraseSearch.PhraseIndexFullCanonicalRehearsalTests',
      'phrase-index-rehearsal',
    ],
    [
      'QuranDashboard.Tests.TestSupport.Artifacts.FullCanonicalRecoveryRehearsalTests',
      'full-canonical-recovery',
    ],
    [
      'QuranDashboard.Tests.TestSupport.Artifacts.PreviousReleaseMigrationUpgradeRehearsalTests',
      'previous-release-upgrade',
    ],
  ]);
  return lanes.get(className) ?? null;
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
  return { id, cwd, executable, arguments: arguments_, group, ...metadata };
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
