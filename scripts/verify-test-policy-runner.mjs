import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';

import {
  EXECUTION_GROUPS,
  EXECUTION_PHASES,
  PRE_PR_ACTIVE_GATE_TARGET_MILLISECONDS,
  assessScratchLifecycleResult,
  captureMachineLoad,
  createEmptyScratchExecutionEvidence,
  parseBackendPolicyCatalog,
  parseBackendResourceCatalog,
  planFocusedSelection,
  planPrePrSelection,
  retainExecutionStatusAfterTimingFailure,
  summarizeExecutionTiming,
} from './test-policy-runner.mjs';

const header = [
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
].join('\t');

const catalog = parseBackendPolicyCatalog(`${header}\n${[
  row('Tests.Fast', 'ApiBehavior', 'Fast', 'TierB', '', 'FastNoDb', 'None', 'None', 'None', 'None', 'None', 'Migrated'),
  row('Tests.Reader', 'MushafReader', 'Database', 'TierB', '', 'CanonicalReader', 'CanonicalQuranData', 'None', 'TestDatabase', 'None', 'None', 'Migrated'),
  row('Tests.Guarded', 'PhraseSearch', 'Database', 'TierB', '', 'GuardedReader', 'CanonicalQuranData', 'None', 'TestDatabase', 'None', 'None', 'Migrated'),
  row('Tests.Writer', 'Access', 'Database', 'TierB', '', 'MutableWriter', 'SystemCatalogue', 'MutableApplicationState', 'TestDatabase', 'None', 'None', 'Migrated'),
  row('Tests.FixtureUpgraded', 'Access', 'Database', 'TierB', '', 'CanonicalReader', 'SystemCatalogue', 'None', 'TestDatabase', 'None', 'WriterCollection', 'Migrated'),
  row('Tests.EmptyMigration', 'Access', 'Migration', 'TierB', 'Schema', 'DestructiveRehearsal', 'None', 'SchemaState', 'EmptyScratch', 'Migration', 'ScratchCollection', 'Migrated'),
  row('Tests.EmptyCanonicalGenerator', 'WordsMorphology', 'Database', 'Pipeline', 'Contract,Safety,Schema,Source', 'DestructiveRehearsal', 'CanonicalQuranData,SchemaState', 'CanonicalQuranData', 'EmptyScratch', 'CanonicalImport', 'ScratchCollection', 'Migrated'),
  row('Tests.EmptyOtherGenerator', 'WordsDisplay', 'Database', 'Pipeline', 'Contract,Safety,Schema,Source', 'DestructiveRehearsal', 'CanonicalQuranData,SchemaState', 'CanonicalQuranData', 'EmptyScratch', 'CanonicalRebuild', 'ScratchCollection', 'Migrated'),
  row('Tests.FullImport', 'FoundationImport', 'Canonical', 'Pipeline', 'Cli,Source,Safety', 'DestructiveRehearsal', 'CanonicalQuranData', 'CanonicalQuranData', 'FullRehearsal', 'CanonicalImport', 'None', 'Migrated'),
  row('Tests.OtherFullImport', 'Tafsirs', 'Canonical', 'Pipeline', 'Cli,Source', 'DestructiveRehearsal', 'CanonicalQuranData', 'CanonicalQuranData', 'FullRehearsal', 'CanonicalImport', 'None', 'Migrated'),
  row('Tests.FullIndex', 'PhraseSearch', 'Release', 'Release', 'Cli,Execution', 'DestructiveRehearsal', 'CanonicalQuranData', 'CanonicalQuranData', 'FullRehearsal', 'PhraseSearchIndexBuild', 'None', 'Migrated'),
  row('Tests.FullRecovery', 'ApiBehavior', 'Release', 'Release', 'Cli,Execution,Schema', 'DestructiveRehearsal', 'CanonicalQuranData,SchemaState', 'CanonicalQuranData,SchemaState', 'FullRehearsal', 'Recovery', 'None', 'Migrated'),
  row('Tests.LegacyFull', 'Smoke', 'Canonical', 'Smoke', 'Cli', 'DestructiveRehearsal', 'CanonicalQuranData', 'CanonicalQuranData', 'FullRehearsal', 'CanonicalImport', 'None', 'Migrated'),
  row('Tests.FastUnclassified', 'Linking', 'Fast', 'TierB', '', 'FastNoDb', 'None', 'None', 'None', 'None', 'None', 'Migrated'),
].join('\n')}\n`);
const resourceCatalog = parseBackendResourceCatalog(`${[
  'CollectionName\tResourceClassName\tParallelPolicy\tStatePolicy\tSetupWrites\tResetBehavior\tDatabaseTarget\tStartupEffects\tMigrationState',
  'WriterCollection\tTests.WriterFixture\tNonParallel\tResetPerTest\tMutableApplicationState\tMutableApplicationState\tTestDatabase\tMutableApi\tMigrated',
  'ScratchCollection\tTests.ScratchFixture\tNonParallel\tFreshLeasePerCase\tSchemaState\tNone\tEmptyScratch\tNone\tMigrated',
].join('\n')}\n`);

assert.deepEqual(EXECUTION_GROUPS, [
  'FastNoDb',
  'CanonicalReader',
  'GuardedReader',
  'MutableWriter',
  'EmptyScratchDestructiveRehearsal',
  'FullDataDestructiveRehearsal',
]);

const focused = planFocusedSelection({
  backendCatalog: catalog,
  backendResources: resourceCatalog,
  backendClasses: ['Tests.Reader', 'Tests.FixtureUpgraded', 'Tests.FastUnclassified'],
  backendMethods: ['Tests.Fast.Contract_case'],
  buildMode: 'build',
  playwrightSelections: [
    { file: 'e2e/mushaf-reader.e2e.ts', line: 42, effectiveGroup: 'CanonicalReader' },
    { file: 'e2e/guarded.e2e.ts', line: 41, effectiveGroup: 'GuardedReader' },
  ],
  authorizeFullData: false,
});

assert.deepEqual(
  focused.partitions.map(({ group }) => group),
  ['FastNoDb', 'CanonicalReader', 'GuardedReader', 'MutableWriter'],
);
assert.deepEqual(
  focused.partitions.flatMap(({ selections }) => selections.map(({ selector }) => selector)),
  [
    'Tests.Fast.Contract_case',
    'Tests.FastUnclassified',
    'Tests.Reader',
    'e2e/mushaf-reader.e2e.ts:42',
    'e2e/guarded.e2e.ts:41',
    'Tests.FixtureUpgraded',
  ],
);
assert.equal(focused.commands[0].id, 'backend-build');
assert.deepEqual(
  focused.commands.find(({ id }) => id === 'backend-method-Tests.Fast.Contract_case').arguments,
  ['feature', '--test', 'Tests.Fast.Contract_case', '--no-build'],
);
assert.deepEqual(
  focused.commands.find(({ id }) => id === 'playwright-e2e/mushaf-reader.e2e.ts:42').arguments,
  ['run', 'e2e:canonical:focused', '--', 'e2e/mushaf-reader.e2e.ts:42'],
);
assert.deepEqual(
  focused.commands.find(({ id }) => id === 'playwright-e2e/guarded.e2e.ts:41').arguments,
  ['run', 'e2e:focused', '--', 'e2e/guarded.e2e.ts:41'],
);

assert.throws(
  () =>
    planFocusedSelection({
      backendCatalog: catalog,
      backendResources: resourceCatalog,
      backendClasses: [],
      backendMethods: [],
      buildMode: 'no-build',
      playwrightSelections: [],
      authorizeFullData: false,
    }),
  /focused selection requires/i,
);

const blockedFocusedFullData = planFocusedSelection({
  backendCatalog: catalog,
  backendResources: resourceCatalog,
  backendClasses: ['Tests.FullImport'],
  backendMethods: [],
  buildMode: 'no-build',
  playwrightSelections: [],
  authorizeFullData: false,
});
assert.deepEqual(blockedFocusedFullData.authorizationRequired, ['Tests.FullImport']);
assert.equal(blockedFocusedFullData.commands.length, 0);

const authorizedFocusedFullData = planFocusedSelection({
  backendCatalog: catalog,
  backendResources: resourceCatalog,
  backendClasses: ['Tests.FullImport'],
  backendMethods: [],
  buildMode: 'no-build',
  playwrightSelections: [],
  authorizeFullData: true,
});
assert.deepEqual(authorizedFocusedFullData.authorizationRequired, []);
assert.equal(authorizedFocusedFullData.commands.length, 1);
assert.equal(authorizedFocusedFullData.commands[0].rehearsalSubtype, undefined);

const authorizedIndexAndRecovery = planFocusedSelection({
  backendCatalog: catalog,
  backendResources: resourceCatalog,
  backendClasses: ['Tests.FullIndex', 'Tests.FullRecovery'],
  backendMethods: [],
  buildMode: 'no-build',
  playwrightSelections: [],
  authorizeFullData: true,
});
assert.deepEqual(
  authorizedIndexAndRecovery.commands.map(({ rehearsalSubtype }) => rehearsalSubtype),
  ['phrase-search-index-build', 'recovery'],
);

const blockedLegacyFullData = planFocusedSelection({
  backendCatalog: catalog,
  backendResources: resourceCatalog,
  backendClasses: ['Tests.LegacyFull'],
  backendMethods: [],
  buildMode: 'no-build',
  playwrightSelections: [],
  authorizeFullData: false,
});
assert.deepEqual(blockedLegacyFullData.authorizationRequired, ['Tests.LegacyFull']);

const focusedReleaseMethodName = 'Tests.FullRecovery.Recovery_case';
const focusedReleaseMethod = planFocusedSelection({
  backendCatalog: catalog,
  backendResources: resourceCatalog,
  backendClasses: [],
  backendMethods: [focusedReleaseMethodName],
  buildMode: 'no-build',
  playwrightSelections: [],
  authorizeFullData: true,
});
assert.deepEqual(focusedReleaseMethod.commands[0].arguments, [
  'feature',
  '--test',
  focusedReleaseMethodName,
  '--no-build',
]);
assert.equal(focusedReleaseMethod.commands[0].rehearsalSubtype, 'recovery');

const ordinaryPrePr = planPrePrSelection({
  backendCatalog: catalog,
  backendResources: resourceCatalog,
  affectedFeatures: [],
  affectedConcerns: [],
  authorizeFullData: false,
});
assert.deepEqual(ordinaryPrePr.authorizationRequired, []);
assert.deepEqual(ordinaryPrePr.requiredGates, ['backend-risk', 'frontend-policy-build', 'playwright-critical']);
assert.ok(ordinaryPrePr.commands.some(({ id }) => id === 'backend-class-Tests.Fast'));
assert.ok(ordinaryPrePr.commands.some(({ id }) => id === 'backend-class-Tests.FastUnclassified'));
assert.ok(ordinaryPrePr.commands.some(({ id }) => id === 'frontend-pre-pr'));
assert.ok(ordinaryPrePr.commands.some(({ id }) => id === 'playwright-canonical-critical'));
assert.deepEqual(
  ordinaryPrePr.commands.find(({ id }) => id === 'playwright-stateful-critical').arguments,
  ['run', 'e2e:stateful:critical'],
);
assert.ok(ordinaryPrePr.commands.some(({ id }) => id === 'playwright-provision'));
assert.deepEqual(
  ordinaryPrePr.commands.find(({ id }) => id === 'playwright-provision').arguments,
  ['run', 'e2e:provision'],
);

// Controlled Playwright provisioning hashes the built Backend and Frontend outputs, so every command
// that rebuilds them must run before the receipt is written, and nothing may rebuild them afterwards.
const prePrIds = ordinaryPrePr.commands.map(({ id }) => id);
const provisionIndex = prePrIds.indexOf('playwright-provision');
for (const rebuildingId of ['backend-build', 'frontend-pre-pr']) {
  assert.ok(
    prePrIds.indexOf(rebuildingId) < provisionIndex,
    `${rebuildingId} must run before playwright-provision invalidates its receipt`,
  );
}
for (const controlledId of ['playwright-canonical-critical', 'playwright-stateful-critical']) {
  assert.ok(
    prePrIds.indexOf(controlledId) > provisionIndex,
    `${controlledId} must run after playwright-provision`,
  );
}

// Every planned command declares the phase its elapsed time is attributed to, so the runner can report
// provisioning separately from the active gate the 12-minute target measures.
assert.deepEqual(EXECUTION_PHASES, ['provisioning', 'activeGate']);
for (const planned of [...ordinaryPrePr.commands, ...focused.commands]) {
  assert.ok(
    EXECUTION_PHASES.includes(planned.phase),
    `${planned.id} must declare an execution phase`,
  );
}
assert.equal(
  ordinaryPrePr.commands.find(({ id }) => id === 'playwright-provision').phase,
  'provisioning',
);
assert.equal(
  ordinaryPrePr.commands.find(({ id }) => id === 'backend-build').phase,
  'activeGate',
);

assert.ok(!ordinaryPrePr.commands.some(({ id }) => id.includes('FullImport')));
assert.ok(!ordinaryPrePr.commands.some(({ id }) => id.includes('LegacyFull')));
assert.ok(!ordinaryPrePr.commands.some(({ id }) => id.includes('EmptyMigration')));
assert.ok(!ordinaryPrePr.commands.some(({ id }) => id.includes('EmptyCanonicalGenerator')));
assert.ok(!ordinaryPrePr.commands.some(({ id }) => id.includes('EmptyOtherGenerator')));
assert.deepEqual(
  ordinaryPrePr.partitions.map(({ group }) => group),
  ['FastNoDb', 'CanonicalReader', 'GuardedReader', 'MutableWriter'],
);

const affectedPipeline = planPrePrSelection({
  backendCatalog: catalog,
  backendResources: resourceCatalog,
  affectedFeatures: ['FoundationImport'],
  affectedConcerns: [],
  authorizeFullData: false,
});
assert.deepEqual(affectedPipeline.authorizationRequired, ['Tests.FullImport']);
assert.ok(!affectedPipeline.commands.some(({ id }) => id.includes('Tests.FullImport')));
assert.ok(!affectedPipeline.commands.some(({ id }) => id.includes('Tests.OtherFullImport')));

const affectedEmptyPipeline = planPrePrSelection({
  backendCatalog: catalog,
  backendResources: resourceCatalog,
  affectedFeatures: ['WordsMorphology'],
  affectedConcerns: [],
  authorizeFullData: false,
});
assert.ok(affectedEmptyPipeline.commands.some(({ id }) =>
  id.includes('EmptyCanonicalGenerator')));
assert.ok(!affectedEmptyPipeline.commands.some(({ id }) =>
  id.includes('EmptyOtherGenerator')));

const affectedSafety = planPrePrSelection({
  backendCatalog: catalog,
  backendResources: resourceCatalog,
  affectedFeatures: [],
  affectedConcerns: ['Safety'],
  authorizeFullData: false,
});
assert.deepEqual(affectedSafety.authorizationRequired, ['Tests.FullImport']);
assert.ok(affectedSafety.commands.some(({ id }) => id.includes('EmptyCanonicalGenerator')));

const authorizedPipeline = planPrePrSelection({
  backendCatalog: catalog,
  backendResources: resourceCatalog,
  affectedFeatures: ['FoundationImport'],
  affectedConcerns: [],
  authorizeFullData: true,
});
assert.deepEqual(authorizedPipeline.authorizationRequired, []);
assert.ok(authorizedPipeline.commands.some(({ id }) => id.includes('Tests.FullImport')));
assert.ok(!authorizedPipeline.commands.some(({ id }) => id.includes('Tests.OtherFullImport')));
assert.equal(
  authorizedPipeline.commands.find(({ id }) => id.includes('Tests.FullImport')).rehearsalSubtype,
  undefined,
);

const cheapScratch = planPrePrSelection({
  backendCatalog: catalog,
  backendResources: resourceCatalog,
  affectedFeatures: [],
  affectedConcerns: ['Schema'],
  authorizeFullData: false,
});
assert.ok(cheapScratch.commands.some(({ id }) => id.includes('Tests.EmptyMigration')));
assert.deepEqual(cheapScratch.authorizationRequired, []);
assert.equal(
  cheapScratch.commands.find(({ id }) => id.includes('Tests.EmptyMigration')).scratchSubtype,
  'migration',
);

assert.deepEqual(
  planPrePrSelection({
    backendCatalog: catalog,
    backendResources: resourceCatalog,
    affectedFeatures: ['FoundationImport'],
    affectedConcerns: [],
    authorizeFullData: true,
  }),
  authorizedPipeline,
);

const scheduledWithoutAuthorization = planPrePrSelection({
  backendCatalog: catalog,
  backendResources: resourceCatalog,
  affectedFeatures: [],
  affectedConcerns: [],
  authorizeFullData: false,
  explicitPolicy: 'scheduled',
});
assert.ok(scheduledWithoutAuthorization.commands.some(({ id }) =>
  id.includes('EmptyCanonicalGenerator')));
assert.ok(scheduledWithoutAuthorization.commands.some(({ id }) =>
  id.includes('EmptyOtherGenerator')));
assert.deepEqual(scheduledWithoutAuthorization.authorizationRequired, [
  'Tests.FullImport',
  'Tests.FullIndex',
  'Tests.FullRecovery',
  'Tests.OtherFullImport',
]);

const authorizedRelease = planPrePrSelection({
  backendCatalog: catalog,
  backendResources: resourceCatalog,
  affectedFeatures: [],
  affectedConcerns: [],
  authorizeFullData: true,
  explicitPolicy: 'release',
});
const releaseCommand = authorizedRelease.commands.find(({ id }) =>
  id.includes('Tests.FullRecovery'),
);
assert.deepEqual(releaseCommand.arguments, [
  'feature',
  '--class',
  'Tests.FullRecovery',
  '--no-build',
]);
assert.equal(releaseCommand.rehearsalSubtype, 'recovery');

assert.throws(
  () => parseBackendPolicyCatalog(`${header}\n${row('Tests.Bad', 'ApiBehavior', 'Fast', 'TierB', '', '', '', '', '', '', '', 'Migrated')}\n`),
  /migrated.*complete policy metadata/i,
);
assert.throws(
  () => parseBackendPolicyCatalog(`${header}\n${row('Tests.Bad', 'ApiBehavior', 'Fast', 'TierB', '', 'FastNoDb', 'None', 'None', 'None', 'None', 'None', 'Unmigrated')}\n`),
  /unsupported MigrationState Unmigrated/i,
);
assert.throws(
  () => parseBackendPolicyCatalog(`${header}\n${row('Tests.BadReader', 'ApiBehavior', 'Database', 'TierB', '', 'CanonicalReader', 'CanonicalQuranData', 'MutableApplicationState', 'TestDatabase', 'None', 'None', 'Migrated')}\n`),
  /reader policies.*no writes/i,
);
assert.throws(
  () => parseBackendResourceCatalog(`${[
    'CollectionName\tResourceClassName\tParallelPolicy\tStatePolicy\tSetupWrites\tResetBehavior\tDatabaseTarget\tStartupEffects\tMigrationState',
    'BadCollection\tTests.BadFixture\tParallel\tImmutableSeed\tSchemaState\tNone\tTestDatabase\tReadOnlyApi\tMigrated',
  ].join('\n')}\n`),
  /Protected State setup writes require a rehearsal target/i,
);
assert.throws(
  () => planPrePrSelection({
    backendCatalog: catalog,
    backendResources: resourceCatalog,
    affectedFeatures: ['TypoFeature'],
    affectedConcerns: [],
    authorizeFullData: false,
  }),
  /unknown affected Backend feature/i,
);

const repositoryCatalog = parseBackendPolicyCatalog(readFileSync(
  new URL('../Backend/tests/QuranDashboard.Tests/TestSupport/Execution/test-gates.tsv', import.meta.url),
  'utf8',
));
const repositoryResources = parseBackendResourceCatalog(readFileSync(
  new URL('../Backend/tests/QuranDashboard.Tests/TestSupport/Execution/test-resources.tsv', import.meta.url),
  'utf8',
));
const declaredLegacyPhraseIndex = repositoryCatalog.find(({ className }) =>
  className === 'QuranDashboard.Tests.Quran.PhraseSearch.PhraseIndexFullCanonicalRehearsalTests');
const declaredLegacyPhraseIndexResource = repositoryResources.find(({ collectionName }) =>
  collectionName === 'PhraseIndexFullCanonicalRehearsalCollection');
assert.equal(declaredLegacyPhraseIndex.migrationState, 'Migrated');
assert.equal(declaredLegacyPhraseIndex.policy.databaseTarget, 'FullRehearsal');
assert.equal(declaredLegacyPhraseIndex.policy.destructiveSubtype, 'PhraseSearchIndexBuild');
assert.equal(declaredLegacyPhraseIndexResource.migrationState, 'Migrated');
assert.equal(declaredLegacyPhraseIndexResource.policy.databaseTarget, 'FullRehearsal');

const authorizedLegacyPhraseIndex = planFocusedSelection({
  backendCatalog: repositoryCatalog,
  backendResources: repositoryResources,
  backendClasses: [declaredLegacyPhraseIndex.className],
  backendMethods: [],
  buildMode: 'no-build',
  playwrightSelections: [],
  authorizeFullData: true,
});
assert.deepEqual(
  authorizedLegacyPhraseIndex.commands[0].arguments,
  ['feature', '--class', declaredLegacyPhraseIndex.className, '--no-build'],
);
assert.equal(authorizedLegacyPhraseIndex.commands[0].rehearsalSubtype, 'phrase-search-index-build');
const foundationClasses = repositoryCatalog
  .filter(({ feature }) => feature === 'FoundationImport')
  .map(({ className }) => className)
  .sort();
const navigationClasses = repositoryCatalog
  .filter(({ feature }) => feature === 'Navigation')
  .map(({ className }) => className)
  .sort();

const focusedFoundationRebuild = planFocusedSelection({
  backendCatalog: repositoryCatalog,
  backendResources: repositoryResources,
  backendClasses: ['QuranDashboard.Tests.Quran.Import.ForceReloadTests'],
  backendMethods: [],
  buildMode: 'no-build',
  playwrightSelections: [],
});
assert.equal(focusedFoundationRebuild.commands.length, 1);
assert.equal(focusedFoundationRebuild.commands[0].group, 'EmptyScratchDestructiveRehearsal');
assert.equal(focusedFoundationRebuild.commands[0].scratchSubtype, 'canonical-rebuild');

const focusedNavigationRule = planFocusedSelection({
  backendCatalog: repositoryCatalog,
  backendResources: repositoryResources,
  backendClasses: ['QuranDashboard.Tests.Quran.Navigation.NavigationValidationRuleTests'],
  backendMethods: [],
  buildMode: 'no-build',
  playwrightSelections: [],
});
assert.equal(focusedNavigationRule.commands.length, 1);
assert.equal(focusedNavigationRule.commands[0].group, 'FastNoDb');

const ordinaryRepositoryPrePr = planPrePrSelection({
  backendCatalog: repositoryCatalog,
  backendResources: repositoryResources,
  affectedFeatures: [],
  affectedConcerns: [],
  authorizeFullData: false,
});
assert.ok(!ordinaryRepositoryPrePr.partitions
  .flatMap(({ selections }) => selections)
  .some(({ className, group }) =>
    group === 'EmptyScratchDestructiveRehearsal'
    && (foundationClasses.includes(className) || navigationClasses.includes(className))));

for (const [feature, expectedClasses] of [
  ['FoundationImport', foundationClasses],
  ['Navigation', navigationClasses],
]) {
  const affected = planPrePrSelection({
    backendCatalog: repositoryCatalog,
    backendResources: repositoryResources,
    affectedFeatures: [feature],
    affectedConcerns: [],
    authorizeFullData: false,
  });
  const selectedFeatureClasses = affected.partitions
    .flatMap(({ selections }) => selections)
    .filter(({ className }) => expectedClasses.includes(className))
    .map(({ className }) => className)
    .sort();

  assert.deepEqual(selectedFeatureClasses, expectedClasses);
  assert.deepEqual(affected.authorizationRequired, []);
}

const expectedScratchClasses = [...foundationClasses, ...navigationClasses]
  .filter((className) => repositoryCatalog.find(
    (entry) => entry.className === className,
  ).policy?.backendPolicy === 'DestructiveRehearsal');
for (const concern of ['Source', 'Schema', 'Contract', 'Safety']) {
  const concernPrePr = planPrePrSelection({
    backendCatalog: repositoryCatalog,
    backendResources: repositoryResources,
    affectedFeatures: [],
    affectedConcerns: [concern],
    authorizeFullData: false,
  });
  const scratchClasses = concernPrePr.partitions
    .flatMap(({ selections }) => selections)
    .filter(({ group }) => group === 'EmptyScratchDestructiveRehearsal')
    .map(({ className }) => className);
  assert.ok(expectedScratchClasses.every((className) => scratchClasses.includes(className)));
}

const credentialSentinel = 'do-not-retain-this-password';
const scratchEvidence = createEmptyScratchExecutionEvidence({
  command: focusedFoundationRebuild.commands[0],
  runId: '0123456789abcdef0123456789abcdef',
  keeperStatus: 'acquired',
  keeperExitStatus: 0,
  keeperDurationMilliseconds: 11,
  reap: lifecycleResult('reap', false),
  create: lifecycleResult('create', false),
  testStatus: 0,
  testDurationMilliseconds: 31,
  cleanup: lifecycleResult('cleanup', true),
  totalDurationMilliseconds: 73,
  finalStatus: 0,
});
assert.equal(scratchEvidence.evidenceType, 'empty-scratch-test-execution');
assert.equal(scratchEvidence.scope.className, 'QuranDashboard.Tests.Quran.Import.ForceReloadTests');
assert.equal(scratchEvidence.scratch.runId, '0123456789abcdef0123456789abcdef');
assert.equal(scratchEvidence.scratch.subtype, 'canonical-rebuild');
assert.equal(scratchEvidence.lifecycle.test.status, 0);
assert.equal(scratchEvidence.lifecycle.cleanup.removed, true);
assert.equal(scratchEvidence.lifecycle.cleanup.dumpFilesRetained, 0);
assert.equal(scratchEvidence.timings.totalMilliseconds, 73);
assert.equal(scratchEvidence.lifecycle.create.durationMilliseconds, 10);
assert.equal(scratchEvidence.succeeded, true);
assert.ok(!JSON.stringify(scratchEvidence).includes(credentialSentinel));

const missingLifecycleEvidence = assessScratchLifecycleResult({
  action: 'create',
  runId: '0123456789abcdef0123456789abcdef',
  subtype: 'canonical-rebuild',
  processStatus: 0,
  report: null,
  durationMilliseconds: 4,
});
assert.equal(missingLifecycleEvidence.status, 1);
assert.equal(missingLifecycleEvidence.evidenceValid, false);
assert.equal(missingLifecycleEvidence.failureCategory, 'missing-evidence');

const failedLifecycleEvidence = assessScratchLifecycleResult({
  action: 'cleanup',
  runId: '0123456789abcdef0123456789abcdef',
  subtype: 'canonical-rebuild',
  processStatus: 0,
  durationMilliseconds: 12,
  report: {
    succeeded: false,
    failureType: 'scratch-cleanup-failed',
    connectionString: `Password=${credentialSentinel}`,
    violations: [{
      code: 'scratch.receipt.mismatch',
      message: credentialSentinel,
    }],
    scratch: {
      mode: 'cleanup',
      database: 'quran_test_scratch_0123456789abcdef0123456789abcdef',
      runId: '0123456789abcdef0123456789abcdef',
      subtype: 'canonical-rebuild',
      receiptRecorded: true,
      validated: true,
      removed: false,
      dumpFilesRetained: 0,
    },
  },
});
assert.equal(failedLifecycleEvidence.status, 1);
assert.equal(failedLifecycleEvidence.evidenceValid, true);
assert.equal(failedLifecycleEvidence.failureCategory, 'lifecycle-failed');

const mismatchedLifecycleReport = lifecycleResult('create', false).report;
mismatchedLifecycleReport.scratch.runId = 'fedcba9876543210fedcba9876543210';
const mismatchedLifecycleEvidence = assessScratchLifecycleResult({
  action: 'create',
  runId: '0123456789abcdef0123456789abcdef',
  subtype: 'canonical-rebuild',
  processStatus: 0,
  durationMilliseconds: 5,
  report: mismatchedLifecycleReport,
});
assert.equal(mismatchedLifecycleEvidence.status, 1);
assert.equal(mismatchedLifecycleEvidence.evidenceValid, false);
assert.equal(mismatchedLifecycleEvidence.failureCategory, 'identity-mismatch');

const mismatchedCleanup = lifecycleResult('cleanup', true);
mismatchedCleanup.report.scratch.runId = 'fedcba9876543210fedcba9876543210';
mismatchedCleanup.report.scratch.database =
  'quran_test_scratch_fedcba9876543210fedcba9876543210';
const crossPhaseMismatchEvidence = createEmptyScratchExecutionEvidence({
  command: focusedFoundationRebuild.commands[0],
  runId: '0123456789abcdef0123456789abcdef',
  keeperStatus: 'acquired',
  keeperExitStatus: 0,
  reap: lifecycleResult('reap', false),
  create: lifecycleResult('create', false),
  testStatus: 0,
  cleanup: mismatchedCleanup,
  finalStatus: 0,
});
assert.equal(crossPhaseMismatchEvidence.succeeded, false);

const failedScratchEvidence = createEmptyScratchExecutionEvidence({
  command: focusedFoundationRebuild.commands[0],
  runId: '0123456789abcdef0123456789abcdef',
  keeperStatus: 'acquired',
  keeperExitStatus: 0,
  reap: lifecycleResult('reap', false),
  create: lifecycleResult('create', false),
  testStatus: 0,
  cleanup: failedLifecycleEvidence,
  finalStatus: 0,
});
assert.equal(failedScratchEvidence.succeeded, false);
assert.equal(failedScratchEvidence.lifecycle.cleanup.failureType, 'scratch-cleanup-failed');
assert.deepEqual(
  failedScratchEvidence.lifecycle.cleanup.violationCodes,
  ['scratch.receipt.mismatch'],
);
assert.ok(!JSON.stringify(failedScratchEvidence).includes(credentialSentinel));

assert.equal(PRE_PR_ACTIVE_GATE_TARGET_MILLISECONDS, 12 * 60 * 1000);

const timing = summarizeExecutionTiming({
  mode: 'pre-pr',
  totalWallMilliseconds: 900_000,
  records: [
    { id: 'backend-build', phase: 'activeGate', status: 0, durationMilliseconds: 60_000 },
    {
      id: 'backend-class-Tests.EmptyMigration',
      phase: 'activeGate',
      group: 'EmptyScratchDestructiveRehearsal',
      status: 0,
      durationMilliseconds: 100_000,
      lockWaitMilliseconds: 40_000,
    },
    { id: 'playwright-provision', phase: 'provisioning', status: 0, durationMilliseconds: 300_000 },
    { id: 'playwright-canonical-critical', phase: 'activeGate', status: 0, durationMilliseconds: 200_000 },
  ],
});

assert.equal(timing.evidenceType, 'test-execution-timing');
assert.equal(timing.mode, 'pre-pr');
assert.equal(timing.lockWaitMilliseconds, 40_000);
assert.equal(timing.provisioningMilliseconds, 300_000);
// Active gate time excludes both provisioning and the lock wait inside an active-gate command.
assert.equal(timing.activeGateMilliseconds, 320_000);
assert.equal(timing.totalWallMilliseconds, 900_000);
assert.equal(timing.unattributedMilliseconds, 240_000);
assert.deepEqual(timing.activeGateTarget, {
  applies: true,
  targetMilliseconds: 720_000,
  withinTarget: true,
});
assert.equal(timing.commands.length, 4);

// Validating a manually provisioned capability is provisioning time even when the command that waits
// for it belongs to the active gate: it must never be charged to the 12-minute target.
const capabilityTiming = summarizeExecutionTiming({
  mode: 'pre-pr',
  totalWallMilliseconds: 100_000,
  records: [
    {
      id: 'backend-class-Tests.FullIndex',
      phase: 'activeGate',
      group: 'FullDataDestructiveRehearsal',
      status: 0,
      durationMilliseconds: 100_000,
      lockWaitMilliseconds: 5_000,
      capabilityMilliseconds: 30_000,
    },
  ],
});
assert.equal(capabilityTiming.lockWaitMilliseconds, 5_000);
assert.equal(capabilityTiming.provisioningMilliseconds, 30_000);
assert.equal(capabilityTiming.activeGateMilliseconds, 65_000);
assert.equal(capabilityTiming.unattributedMilliseconds, 0);

assert.throws(
  () => summarizeExecutionTiming({
    mode: 'pre-pr',
    totalWallMilliseconds: 10,
    records: [{
      id: 'backend-build',
      phase: 'activeGate',
      status: 0,
      durationMilliseconds: 10,
      lockWaitMilliseconds: 4,
      capabilityMilliseconds: 7,
    }],
  }),
  /more lock wait and capability time than elapsed/i,
);

const overTarget = summarizeExecutionTiming({
  mode: 'pre-pr',
  totalWallMilliseconds: 2_000_000,
  records: [
    { id: 'playwright-provision', phase: 'provisioning', status: 0, durationMilliseconds: 900_000 },
    { id: 'backend-build', phase: 'activeGate', status: 0, durationMilliseconds: 720_001 },
  ],
});
assert.equal(overTarget.activeGateTarget.withinTarget, false);
// Provisioning alone never pushes the reported active gate over the target.
assert.equal(overTarget.provisioningMilliseconds, 900_000);

// The 12-minute target is a pre-PR statement; focused selections report the same times without it.
const focusedTiming = summarizeExecutionTiming({
  mode: 'focused',
  totalWallMilliseconds: 5_000,
  records: [{ id: 'backend-build', phase: 'activeGate', status: 0, durationMilliseconds: 4_000 }],
});
assert.deepEqual(focusedTiming.activeGateTarget, {
  applies: false,
  targetMilliseconds: null,
  withinTarget: null,
});

assert.throws(
  () => summarizeExecutionTiming({
    mode: 'pre-pr',
    totalWallMilliseconds: 10,
    records: [{ id: 'backend-build', phase: 'warmup', status: 0, durationMilliseconds: 10 }],
  }),
  /unknown execution phase/i,
);
assert.throws(
  () => summarizeExecutionTiming({
    mode: 'pre-pr',
    totalWallMilliseconds: 10,
    records: [{
      id: 'backend-build',
      phase: 'activeGate',
      status: 0,
      durationMilliseconds: 10,
      lockWaitMilliseconds: 11,
    }],
  }),
  /more lock wait than elapsed/i,
);
assert.throws(
  () => summarizeExecutionTiming({
    mode: 'pre-pr',
    totalWallMilliseconds: 10,
    records: [{ id: 'backend-build', phase: 'activeGate', status: 0, durationMilliseconds: -1 }],
  }),
  /must not be negative/i,
);

const mutableWriterClassCount = repositoryCatalog.filter((row) => row.policy.backendPolicy === 'MutableWriter').length;
const ordinaryPrePrFullFingerprintCallSites = (mutableWriterClassCount * 2) + (8 * 5);
assert.equal(mutableWriterClassCount, 28);
assert.equal(ordinaryPrePrFullFingerprintCallSites, 96);

const fullFingerprintEvents = Array.from({ length: 96 }, (_, index) => ({
  event: 'fingerprint',
  kind: 'full',
  durationMilliseconds: 1_000 + index,
}));
const verifiedCanonicalEvents = [
  { event: 'fingerprint', kind: 'verifiedCanonical', durationMilliseconds: 40 },
  { event: 'fingerprint', kind: 'verifiedCanonical', durationMilliseconds: 60 },
];
const instrumentedTiming = summarizeExecutionTiming({
  mode: 'pre-pr',
  totalWallMilliseconds: 900_000,
  records: [
    { id: 'backend-build', phase: 'activeGate', status: 0, durationMilliseconds: 60_000 },
    {
      id: 'backend-class-Tests.AccessMutable',
      phase: 'activeGate',
      group: 'MutableWriter',
      status: 0,
      durationMilliseconds: 120_000,
      lockWaitMilliseconds: 0,
      subPhases: {
        fixtureInitMilliseconds: 80_000,
        boundaryCheckMilliseconds: 42_000,
        perTestResetMilliseconds: 5_000,
        testBodyMilliseconds: 3_000,
      },
    },
    {
      id: 'backend-class-Tests.SmokeDataReadTests',
      phase: 'activeGate',
      group: 'GuardedReader',
      status: 0,
      durationMilliseconds: 51_100,
      lockWaitMilliseconds: 0,
      subPhases: {
        fixtureInitMilliseconds: 8_000,
        boundaryCheckMilliseconds: 0,
        perTestResetMilliseconds: 0,
        testBodyMilliseconds: 43_100,
      },
    },
    {
      id: 'playwright-stateful-critical',
      phase: 'activeGate',
      status: 0,
      durationMilliseconds: 200_000,
      journeys: [
        {
          selector: 'mutating-one',
          phases: [
            { name: 'applicationStartup', durationMilliseconds: 12_000 },
            { name: 'testExecution', durationMilliseconds: 8_000 },
          ],
        },
      ],
    },
  ],
  events: [
    ...fullFingerprintEvents,
    ...verifiedCanonicalEvents,
    { event: 'lease', kind: 'exclusive', waitMilliseconds: 25, command: 'access-mutable' },
    { event: 'lease', kind: 'shared', waitMilliseconds: 7, command: 'guarded-reader' },
    { event: 'lease', kind: 'exclusive', waitMilliseconds: 40_000, command: 'scratch-rehearsal' },
    { event: 'testCase', id: 'QuranDashboard.Tests.Smoke.Data.SmokeDataReadTests.PersistentDatabase_MatchesTheIndependentReaderOracle' },
    { event: 'testCase', id: 'QuranDashboard.Tests.Api.Access.AccessRolesTests.Owner_CanReadRoles' },
    {
      event: 'journeyPhase',
      journey: 'mutating-one',
      name: 'applicationStartup',
      durationMilliseconds: 12_000,
    },
    {
      event: 'journeyPhase',
      journey: 'mutating-one',
      name: 'testExecution',
      durationMilliseconds: 8_000,
    },
  ],
  machineLoad: {
    capturedAt: '2026-09-05T00:00:00.000Z',
    loadAverage1m: 1.5,
    loadAverage5m: 1.25,
    loadAverage15m: 1.1,
    cpuCount: 8,
  },
});

assert.equal(instrumentedTiming.lockWaitMilliseconds, 0);
assert.equal(instrumentedTiming.provisioningMilliseconds, 0);
assert.equal(instrumentedTiming.activeGateMilliseconds, 431_100);
assert.equal(instrumentedTiming.totalWallMilliseconds, 900_000);
assert.deepEqual(instrumentedTiming.fingerprints, {
  full: { count: 96, totalMilliseconds: 96_000 + ((95 * 96) / 2) },
  verifiedCanonical: { count: 2, totalMilliseconds: 100 },
});
assert.equal(instrumentedTiming.fingerprints.full.count, ordinaryPrePrFullFingerprintCallSites);
assert.deepEqual(instrumentedTiming.leases, {
  exclusive: { count: 2, waitMilliseconds: 40_025 },
  shared: { count: 1, waitMilliseconds: 7 },
});
assert.equal(instrumentedTiming.inChildLockWaitMilliseconds, 32);
assert.deepEqual(instrumentedTiming.testCaseIds, [
  'QuranDashboard.Tests.Api.Access.AccessRolesTests.Owner_CanReadRoles',
  'QuranDashboard.Tests.Smoke.Data.SmokeDataReadTests.PersistentDatabase_MatchesTheIndependentReaderOracle',
]);
assert.deepEqual(
  instrumentedTiming.commands.find((command) => command.id === 'backend-class-Tests.SmokeDataReadTests').subPhases,
  {
    fixtureInitMilliseconds: 8_000,
    boundaryCheckMilliseconds: 0,
    perTestResetMilliseconds: 0,
    testBodyMilliseconds: 43_100,
  },
);
assert.ok(
  instrumentedTiming.commands.find((command) => command.id === 'backend-class-Tests.SmokeDataReadTests')
    .subPhases.testBodyMilliseconds > 3_660,
);
assert.deepEqual(
  instrumentedTiming.commands.find((command) => command.id === 'playwright-stateful-critical').journeys,
  [
    {
      selector: 'mutating-one',
      phases: [
        { name: 'applicationStartup', durationMilliseconds: 12_000 },
        { name: 'testExecution', durationMilliseconds: 8_000 },
      ],
    },
  ],
);
assert.deepEqual(instrumentedTiming.machineLoad, {
  capturedAt: '2026-09-05T00:00:00.000Z',
  loadAverage1m: 1.5,
  loadAverage5m: 1.25,
  loadAverage15m: 1.1,
  cpuCount: 8,
});

const load = captureMachineLoad();
assert.equal(typeof load.capturedAt, 'string');
assert.equal(typeof load.loadAverage1m, 'number');
assert.equal(typeof load.cpuCount, 'number');
assert.ok(load.cpuCount > 0);

assert.equal(retainExecutionStatusAfterTimingFailure(0), 0);
assert.equal(retainExecutionStatusAfterTimingFailure(4), 4);

console.log('Repository test policy runner contract passed.');

function row(...columns) {
  return columns.join('\t');
}

function lifecycleResult(mode, removed) {
  return {
    status: 0,
    evidenceValid: true,
    failureCategory: null,
    durationMilliseconds: 10,
    report: {
      succeeded: true,
      failureType: null,
      violations: [],
      connectionString: `Password=${credentialSentinel}`,
      scratch: {
        mode,
        database: 'quran_test_scratch_0123456789abcdef0123456789abcdef',
        runId: '0123456789abcdef0123456789abcdef',
        subtype: mode === 'reap' ? null : 'canonical-rebuild',
        receiptRecorded: mode !== 'reap',
        validated: true,
        removed,
        dumpFilesRetained: 0,
        payload: credentialSentinel,
      },
    },
  };
}
