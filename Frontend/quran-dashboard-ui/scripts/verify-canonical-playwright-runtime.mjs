import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';

import {
  buildCanonicalPlaywrightEnvironment,
  classifyFocusedPlaywrightSelector,
  selectCanonicalCriticalJourneys,
  selectNonCanonicalPlaywrightTests,
  selectCanonicalPlaywrightTests,
} from './canonical-playwright-runtime.mjs';

const connectionString =
  'Host=localhost;Port=5432;Database=quran_dashboard_test;Username=test-runner;Password=secret';
const environment = buildCanonicalPlaywrightEnvironment({
  ConnectionStrings__QuranDashboardTest: connectionString,
  E2E_ARTIFACT_VERIFIER_ASSEMBLY: '/tmp/artifact-verifier.dll',
  E2E_DATABASE_MODE: 'artifact',
  E2E_ORCHESTRATOR_GUARDED: '1',
  E2E_PREPARED_DATABASE: '1',
  E2E_SEALED_EXECUTION: '1',
  QURAN_TEST_ARTIFACT_ROOT: '/tmp/artifacts',
  Testing__DatabaseActivity__EnabledBackgroundActivities__0: 'LinkingPreparedPreflightProcessor',
  Testing__DatabaseActivity__Profile: 'Mutable',
});

assert.equal(environment.ConnectionStrings__QuranDashboardTest, connectionString);
assert.equal(environment.E2E_DATABASE_MODE, 'persistent-read-only');
assert.equal(environment.E2E_PLAYWRIGHT_POLICY_PARTITION, 'canonical-read');
assert.equal(environment.QURAN_DASHBOARD_TEST_RUNTIME_READER_CONTEXT, 'verified-v1');
assert.equal(environment.Testing__DatabaseActivity__Profile, 'ReadOnly');
assert.equal(environment.E2E_ARTIFACT_VERIFIER_ASSEMBLY, undefined);
assert.equal(environment.E2E_ORCHESTRATOR_GUARDED, undefined);
assert.equal(environment.E2E_PREPARED_DATABASE, undefined);
assert.equal(environment.E2E_SEALED_EXECUTION, undefined);
assert.equal(environment.QURAN_TEST_ARTIFACT_ROOT, undefined);
assert.equal(environment.Testing__DatabaseActivity__EnabledBackgroundActivities__0, undefined);

assert.throws(
  () => buildCanonicalPlaywrightEnvironment({}),
  /ConnectionStrings__QuranDashboardTest/,
);

const discovered = [
  {
    effectiveGroup: 'CanonicalReader',
    file: 'e2e/mushaf-reader.e2e.ts',
    line: 42,
    title: 'renders canonical Quran data',
  },
  {
    effectiveGroup: 'MutableWriter',
    file: 'e2e/linking-success.e2e.ts',
    line: 90,
    title: 'persists a link',
  },
];

assert.deepEqual(selectCanonicalPlaywrightTests(discovered), [
  'e2e/mushaf-reader.e2e.ts:42',
]);
assert.deepEqual(
  selectCanonicalPlaywrightTests(discovered, 'e2e/mushaf-reader.e2e.ts:42'),
  ['e2e/mushaf-reader.e2e.ts:42'],
);
assert.throws(
  () => selectCanonicalPlaywrightTests(discovered, 'e2e/linking-success.e2e.ts:90'),
  /is not canonical-read/,
);
assert.throws(
  () => selectCanonicalPlaywrightTests(discovered, 'e2e/missing.e2e.ts:1'),
  /Unknown Playwright selector/,
);
assert.equal(
  classifyFocusedPlaywrightSelector(discovered, 'e2e/mushaf-reader.e2e.ts:42'),
  'canonical-read',
);
assert.equal(
  classifyFocusedPlaywrightSelector(discovered, 'e2e/linking-success.e2e.ts:90'),
  'non-canonical',
);
assert.deepEqual(selectNonCanonicalPlaywrightTests(discovered), [
  'e2e/linking-success.e2e.ts:90',
]);

assert.deepEqual(
  selectCanonicalCriticalJourneys([
    { file: 'mushaf-reader.e2e.ts', line: 100, state: 'canonical-read' },
    { file: 'mushaf-reader.e2e.ts', line: 100, state: 'canonical-read', project: 'mobile' },
    { file: 'linking-success.e2e.ts', line: 90, state: 'mutating' },
    { file: 'shell-nav.e2e.ts', line: 25, state: 'read-only' },
  ]),
  ['e2e/mushaf-reader.e2e.ts:100'],
);

const canonicalBackendSource = readFileSync(
  resolve(process.cwd(), 'e2e/run-canonical-backend.mjs'),
  'utf8',
);
assert.match(canonicalBackendSource, /Testing__DatabaseActivity__Profile !== 'ReadOnly'/);
assert.match(canonicalBackendSource, /ConnectionStrings__QuranDashboardTest/);
assert.doesNotMatch(
  canonicalBackendSource,
  /artifact|provisionDatabase|databaseRuntime|advisory|lock hold|reset-database/i,
);

console.log('Canonical Playwright runtime contract passed.');
