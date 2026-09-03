import assert from 'node:assert/strict';

import {
  EXECUTION_GROUPS,
  parseBackendPolicyCatalog,
  parseBackendResourceCatalog,
  planFocusedSelection,
  planPrePrSelection,
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
  row('Tests.EmptyMigration', 'Access', 'Migration', 'Pipeline', 'Schema', 'DestructiveRehearsal', 'None', 'SchemaState', 'EmptyScratch', 'Migration', 'ScratchCollection', 'Migrated'),
  row('Tests.FullImport', 'FoundationImport', 'Canonical', 'Pipeline', 'Cli,Source,Safety', 'DestructiveRehearsal', 'CanonicalQuranData', 'CanonicalQuranData', 'FullRehearsal', 'CanonicalImport', 'None', 'Migrated'),
  row('Tests.OtherFullImport', 'Tafsirs', 'Canonical', 'Pipeline', 'Cli,Source', 'DestructiveRehearsal', 'CanonicalQuranData', 'CanonicalQuranData', 'FullRehearsal', 'CanonicalImport', 'None', 'Migrated'),
  row('Tests.LegacyFull', 'Smoke', 'Canonical', 'Smoke', '', '', '', '', '', '', '', 'Unmigrated'),
  row('QuranDashboard.Tests.TestSupport.Artifacts.FullCanonicalRecoveryRehearsalTests', 'ApiBehavior', 'Release', 'Release', 'Safety', '', '', '', '', '', '', 'Unmigrated'),
  row('Tests.Legacy', 'Linking', 'Database', 'TierB', '', '', '', '', '', '', '', 'Unmigrated'),
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
  backendClasses: ['Tests.Reader', 'Tests.FixtureUpgraded', 'Tests.Legacy'],
  backendMethods: ['Tests.Fast.Contract_case'],
  buildMode: 'build',
  playwrightSelections: [
    { file: 'e2e/guarded.e2e.ts', line: 41, effectiveGroup: 'GuardedReader' },
  ],
  authorizeFullData: false,
});

assert.deepEqual(
  focused.partitions.map(({ group }) => group),
  ['FastNoDb', 'CanonicalReader', 'GuardedReader', 'MutableWriter', 'LegacyUnmigrated'],
);
assert.deepEqual(
  focused.partitions.flatMap(({ selections }) => selections.map(({ selector }) => selector)),
  [
    'Tests.Fast.Contract_case',
    'Tests.Reader',
    'e2e/guarded.e2e.ts:41',
    'Tests.FixtureUpgraded',
    'Tests.Legacy',
  ],
);
assert.equal(focused.commands[0].id, 'backend-build');
assert.deepEqual(
  focused.commands.find(({ id }) => id === 'backend-method-Tests.Fast.Contract_case').arguments,
  ['feature', '--test', 'Tests.Fast.Contract_case', '--no-build'],
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

const focusedReleaseMethodName =
  'QuranDashboard.Tests.TestSupport.Artifacts.FullCanonicalRecoveryRehearsalTests.Recovery_case';
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
  'full-canonical-recovery',
  '--test',
  focusedReleaseMethodName,
  '--no-build',
]);

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
assert.ok(ordinaryPrePr.commands.some(({ id }) => id === 'backend-class-Tests.Legacy'));
assert.ok(ordinaryPrePr.commands.some(({ id }) => id === 'frontend-pre-pr'));
assert.ok(ordinaryPrePr.commands.some(({ id }) => id === 'playwright-critical'));
assert.ok(!ordinaryPrePr.commands.some(({ id }) => id.includes('FullImport')));
assert.ok(!ordinaryPrePr.commands.some(({ id }) => id.includes('LegacyFull')));
assert.deepEqual(
  ordinaryPrePr.partitions.map(({ group }) => group),
  ['FastNoDb', 'CanonicalReader', 'GuardedReader', 'MutableWriter', 'LegacyUnmigrated'],
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

const affectedSafety = planPrePrSelection({
  backendCatalog: catalog,
  backendResources: resourceCatalog,
  affectedFeatures: [],
  affectedConcerns: ['Safety'],
  authorizeFullData: false,
});
assert.deepEqual(affectedSafety.authorizationRequired, ['Tests.FullImport']);

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
assert.deepEqual(scheduledWithoutAuthorization.authorizationRequired, [
  'QuranDashboard.Tests.TestSupport.Artifacts.FullCanonicalRecoveryRehearsalTests',
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
  id.includes('FullCanonicalRecoveryRehearsalTests'),
);
assert.deepEqual(releaseCommand.arguments, ['full-canonical-recovery', '--no-build']);

assert.throws(
  () => parseBackendPolicyCatalog(`${header}\n${row('Tests.Bad', 'ApiBehavior', 'Fast', 'TierB', '', '', '', '', '', '', '', 'Migrated')}\n`),
  /migrated.*complete policy metadata/i,
);
assert.throws(
  () => parseBackendPolicyCatalog(`${header}\n${row('Tests.Bad', 'ApiBehavior', 'Fast', 'TierB', '', 'FastNoDb', 'None', 'None', 'None', 'None', 'None', 'Unmigrated')}\n`),
  /unmigrated.*blank policy metadata/i,
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

console.log('Repository test policy runner contract passed.');

function row(...columns) {
  return columns.join('\t');
}
