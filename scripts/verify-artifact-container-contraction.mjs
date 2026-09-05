import assert from 'node:assert/strict';
import { execFileSync } from 'node:child_process';
import { existsSync, readFileSync, readdirSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

import {
  parseBackendPolicyCatalog,
  parseBackendResourceCatalog,
  planFocusedSelection,
  planPrePrSelection,
} from './test-policy-runner.mjs';

const REPOSITORY_ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '..');

function read(relativePath) {
  return readFileSync(resolve(REPOSITORY_ROOT, relativePath), 'utf8');
}

// The contraction is a repository-content claim, so it is asserted against tracked files. Untracked
// build residue (a stale `bin`/`obj` left by a checkout that predates the cutover) is a local artifact
// of the developer's own machine, not a retired path this repository still ships.
const trackedPaths = execFileSync('git', ['ls-files', '-z'], {
  cwd: REPOSITORY_ROOT,
  encoding: 'utf8',
  maxBuffer: 64 * 1024 * 1024,
}).split('\0').filter(Boolean);

function assertNotTracked(relativePath) {
  const retained = trackedPaths.filter((tracked) =>
    tracked === relativePath || tracked.startsWith(`${relativePath}/`),
  );
  assert.deepEqual(
    retained,
    [],
    `${relativePath} must be removed with the artifact/container lifecycle`,
  );
}

const retiredPaths = [
  'test-artifacts.lock.json',
  'docs/testing/test-artifacts.md',
  'docs/testing/test-artifact-manifest.schema.json',
  'docs/testing/test-artifacts-lock.schema.json',
  'docs/testing/compact-phrase-search-ready-candidate.md',
  'docs/testing/quran-fidelity-oracle-candidate.md',
  'docs/testing/previous-release-migration-upgrade.json',
  'docs/testing/previous-release-migration-upgrade.schema.json',
  'Backend/tools/QuranDashboard.TestArtifacts',
  'Backend/scripts/test-artifacts',
  'Backend/tests/QuranDashboard.Tests/TestSupport/Artifacts',
  'Frontend/quran-dashboard-ui/e2e/harness/database-runtime.mjs',
  'Frontend/quran-dashboard-ui/e2e/harness/database-contract.mjs',
];
for (const relativePath of retiredPaths) {
  assertNotTracked(relativePath);
}

assert.equal(
  existsSync(resolve(REPOSITORY_ROOT, 'test-artifacts')),
  false,
  'test-artifacts dumps and manifests must be removed',
);

const solution = read('Backend/QuranDashboard.sln');
assert.doesNotMatch(solution, /TestArtifacts/);

const testProject = read('Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj');
assert.doesNotMatch(testProject, /Testcontainers/);
assert.doesNotMatch(testProject, /TestArtifacts/);

const testBackend = read('Backend/scripts/test-backend');
for (const retired of [
  'full-canonical-recovery',
  'previous-release-upgrade',
  'phrase-index-rehearsal',
  'QURAN_DASHBOARD_ARTIFACT_EXECUTION',
  'EXCLUSIVE_POSTGRES_CLASSES',
  'await_free_postgres_runtime',
  'test-artifacts',
]) {
  assert.doesNotMatch(
    testBackend,
    new RegExp(retired),
    `Backend/scripts/test-backend must not retain ${retired}`,
  );
}
assert.match(testBackend, /ROOT_TEST_RUNNER|scripts\/test/);
assert.match(testBackend, /feature/);

const policyRunner = read('scripts/test-policy-runner.mjs');
assert.doesNotMatch(policyRunner, /legacyReleaseLane|LegacyUnmigrated|full-canonical-recovery|phrase-index-rehearsal/);

const frontendPackage = read('Frontend/quran-dashboard-ui/package.json');
assert.doesNotMatch(frontendPackage, /clone-local|test-artifacts|database-runtime/);

const advisoryPolicy = JSON.parse(read('dependency-advisory-policy.json'));
const developmentProjects = advisoryPolicy.ecosystems.nuget.projectScopes.development;
assert.ok(!developmentProjects.some((path) => path.includes('TestArtifacts')));
assert.ok(developmentProjects.some((path) => path.includes('TestRuntime')));

const nightly = JSON.parse(read('nightly-risk-lane.json'));
assert.ok(!JSON.stringify(nightly).includes('test-artifacts'));
assert.equal(nightly.inputContract.artifactRoot, undefined);
assert.ok(!nightly.commands.some(({ executable, id }) =>
  executable?.includes('test-artifacts') || id === 'verify-full-canonical-artifact'));

const release = JSON.parse(read('release-candidate-lane.json'));
assert.equal(release.artifact, undefined);
assert.ok(!JSON.stringify(release).includes('test-artifacts'));
assert.ok(!release.commands.some(({ id }) =>
  id === 'verify-full-canonical-artifact'
  || id === 'full-canonical-recovery'
  || id === 'previous-release-upgrade'));

const observation = JSON.parse(read('pr-observation-matrix.json'));
assert.ok(!JSON.stringify(observation).includes('full-canonical'));
assert.ok(!JSON.stringify(observation).includes('compactFixture'));
const backendPr = observation.jobs.find(({ id }) => id === 'backend-pr');
assert.ok(backendPr.commands.some(({ executable, arguments: args }) =>
  executable.includes('scripts/test') || (Array.isArray(args) && args.includes('pre-pr'))));

const catalog = parseBackendPolicyCatalog(read(
  'Backend/tests/QuranDashboard.Tests/TestSupport/Execution/test-gates.tsv',
));
const resources = parseBackendResourceCatalog(read(
  'Backend/tests/QuranDashboard.Tests/TestSupport/Execution/test-resources.tsv',
));
assert.ok(catalog.every(({ migrationState }) => migrationState === 'Migrated'));
assert.ok(resources.every(({ migrationState }) => migrationState === 'Migrated'));
assert.ok(!catalog.some(({ className }) => className.includes('.Artifacts.')));
assert.ok(!catalog.some(({ className }) => className.includes('PostgreSqlDatabaseSlot')));
assert.ok(!catalog.some(({ className }) => className.includes('PostgreSqlTestProcess')));

const ordinaryPrePr = planPrePrSelection({
  backendCatalog: catalog,
  backendResources: resources,
  affectedFeatures: [],
  affectedConcerns: [],
  authorizeFullData: false,
});
assert.ok(!ordinaryPrePr.partitions.some(({ group }) => group === 'FullDataDestructiveRehearsal'
  && ordinaryPrePr.commands.some(({ group: commandGroup }) => commandGroup === 'FullDataDestructiveRehearsal')));
assert.ok(!ordinaryPrePr.commands.some(({ arguments: args }) =>
  args.some((value) =>
    value === 'full-canonical-recovery'
    || value === 'phrase-index-rehearsal'
    || value === 'previous-release-upgrade')));
assert.ok(!ordinaryPrePr.commands.some(({ group }) => group === 'EmptyScratchDestructiveRehearsal'
  && !ordinaryPrePr.affectedFeatures.length && !ordinaryPrePr.affectedConcerns.length));
assert.deepEqual(ordinaryPrePr.authorizationRequired, []);

const phraseIndex = catalog.find(({ className }) =>
  className === 'QuranDashboard.Tests.Quran.PhraseSearch.PhraseIndexFullCanonicalRehearsalTests');
if (phraseIndex) {
  assert.equal(phraseIndex.migrationState, 'Migrated');
  assert.equal(phraseIndex.policy.databaseTarget, 'FullRehearsal');
  const focused = planFocusedSelection({
    backendCatalog: catalog,
    backendResources: resources,
    backendClasses: [phraseIndex.className],
    backendMethods: [],
    buildMode: 'no-build',
    playwrightSelections: [],
    authorizeFullData: true,
  });
  assert.deepEqual(focused.commands[0].arguments, [
    'feature',
    '--class',
    phraseIndex.className,
    '--no-build',
  ]);
  assert.equal(focused.commands[0].rehearsalSubtype, 'phrase-search-index-build');
}

const sourceFiles = [
  'Backend/scripts/test-backend',
  'scripts/test',
  'scripts/test-policy-runner.mjs',
  'Frontend/quran-dashboard-ui/package.json',
];
for (const relativePath of sourceFiles) {
  const text = read(relativePath);
  assert.doesNotMatch(text, /clone-local/);
  assert.doesNotMatch(text, /LeaseMigratedDatabaseAsync/);
  assert.doesNotMatch(text, /PostgreSqlBuilder/);
}

const testSources = walkFiles(resolve(REPOSITORY_ROOT, 'Backend/tests/QuranDashboard.Tests'))
  .filter((path) => path.endsWith('.cs'));
assert.ok(!testSources.some((path) => read(path).includes('Testcontainers')));
assert.ok(!testSources.some((path) => read(path).includes('LeaseMigratedDatabaseAsync')));

function walkFiles(directory) {
  return readdirSync(directory, { withFileTypes: true }).flatMap((entry) => {
    const path = resolve(directory, entry.name);
    return entry.isDirectory() ? walkFiles(path) : [path];
  });
}
