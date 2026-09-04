import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';

import {
  buildMutableResetArguments,
  buildStatefulPlaywrightEnvironment,
  classifyInteractivePlaywrightSelector,
  selectStatefulCriticalJourneys,
  selectStatefulPlaywrightTests,
  validateApiProcessReceipt,
} from './stateful-playwright-runtime.mjs';

const connectionString =
  'Host=localhost;Port=5432;Database=quran_dashboard_test;Username=test-runner;Password=secret';
const discovered = [
  {
    backgroundActivities: [],
    declaredPolicy: 'canonical-read',
    effectiveGroup: 'CanonicalReader',
    file: 'e2e/mushaf-reader.e2e.ts',
    fixtureProfile: 'canonical-read-only',
    line: 42,
  },
  {
    backgroundActivities: [],
    declaredPolicy: 'guarded-read',
    effectiveGroup: 'GuardedReader',
    file: 'e2e/abwab-permissions.e2e.ts',
    fixtureProfile: 'guarded-read-only',
    line: 18,
  },
  {
    backgroundActivities: [
      'LinkingPreparedPreflightProcessor',
      'LinkingConfirmationJobProcessor',
    ],
    declaredPolicy: 'mutating',
    effectiveGroup: 'MutableWriter',
    file: 'e2e/linking-success.e2e.ts',
    fixtureProfile: 'mutable-linking-authenticated',
    line: 90,
  },
];

assert.deepEqual(selectStatefulPlaywrightTests(discovered), [
  {
    backgroundActivities: [],
    fixtureProfile: 'guarded-read-only',
    policy: 'guarded-read',
    selector: 'e2e/abwab-permissions.e2e.ts:18',
  },
  {
    backgroundActivities: [
      'LinkingPreparedPreflightProcessor',
      'LinkingConfirmationJobProcessor',
    ],
    fixtureProfile: 'mutable-linking-authenticated',
    policy: 'mutating',
    selector: 'e2e/linking-success.e2e.ts:90',
  },
]);
assert.deepEqual(
  selectStatefulPlaywrightTests(discovered, 'e2e/abwab-permissions.e2e.ts:18'),
  [
    {
      backgroundActivities: [],
      fixtureProfile: 'guarded-read-only',
      policy: 'guarded-read',
      selector: 'e2e/abwab-permissions.e2e.ts:18',
    },
  ],
);
assert.throws(
  () => selectStatefulPlaywrightTests(discovered, 'e2e/mushaf-reader.e2e.ts:42'),
  /not stateful/i,
);
assert.throws(
  () => selectStatefulPlaywrightTests([discovered[1], discovered[1]]),
  /exactly one.*file:line/i,
);

assert.deepEqual(
  selectStatefulCriticalJourneys([
    {
      backgroundActivities: [],
      file: 'mushaf-reader.e2e.ts',
      fixtureProfile: 'canonical-read-only',
      line: 42,
      state: 'canonical-read',
    },
    {
      backgroundActivities: [],
      file: 'abwab-permissions.e2e.ts',
      fixtureProfile: 'guarded-read-only',
      line: 18,
      state: 'guarded-read',
    },
    {
      backgroundActivities: ['LinkingConfirmationJobProcessor'],
      file: 'linking-success.e2e.ts',
      fixtureProfile: 'mutable-linking-authenticated',
      line: 90,
      state: 'mutating',
    },
  ]),
  [
    {
      backgroundActivities: [],
      fixtureProfile: 'guarded-read-only',
      policy: 'guarded-read',
      selector: 'e2e/abwab-permissions.e2e.ts:18',
    },
    {
      backgroundActivities: ['LinkingConfirmationJobProcessor'],
      fixtureProfile: 'mutable-linking-authenticated',
      policy: 'mutating',
      selector: 'e2e/linking-success.e2e.ts:90',
    },
  ],
);

const guardedEnvironment = buildStatefulPlaywrightEnvironment(
  {
    ConnectionStrings__QuranDashboardTest: connectionString,
    E2E_DATABASE_MODE: 'artifact',
    E2E_PREPARED_DATABASE: '1',
    E2E_SEALED_EXECUTION: '1',
    Testing__DatabaseActivity__EnabledBackgroundActivities__0: 'unsafe-stale-value',
  },
  selectStatefulPlaywrightTests(discovered)[0],
  {
    apiProcessReceipt: '/private/guarded/api-process.json',
    backendAssembly: '/private/build/QuranDashboard.Api.dll',
    evidenceDirectory: '/private/guarded/evidence',
    playwrightOutputDirectory: '/private/guarded/output',
    runId: 'guarded-run',
  },
);
assert.equal(guardedEnvironment.E2E_DATABASE_MODE, 'persistent-stateful');
assert.equal(guardedEnvironment.E2E_PLAYWRIGHT_POLICY_PARTITION, 'guarded-read');
assert.equal(guardedEnvironment.Testing__DatabaseActivity__Profile, 'ReadOnly');
assert.equal(guardedEnvironment.QURAN_DASHBOARD_TEST_RUNTIME_READER_CONTEXT, 'verified-v1');
assert.equal(guardedEnvironment.QURAN_DASHBOARD_TEST_RUNTIME_WRITER_CONTEXT, undefined);
assert.equal(guardedEnvironment.Testing__DatabaseActivity__EnabledBackgroundActivities__0, undefined);

const mutableEnvironment = buildStatefulPlaywrightEnvironment(
  { ConnectionStrings__QuranDashboardTest: connectionString },
  selectStatefulPlaywrightTests(discovered)[1],
  {
    apiProcessReceipt: '/private/mutable/api-process.json',
    backendAssembly: '/private/build/QuranDashboard.Api.dll',
    evidenceDirectory: '/private/mutable/evidence',
    playwrightOutputDirectory: '/private/mutable/output',
    runId: 'mutable-run',
  },
);
assert.equal(mutableEnvironment.Testing__DatabaseActivity__Profile, 'Mutable');
assert.equal(mutableEnvironment.QURAN_DASHBOARD_TEST_RUNTIME_WRITER_CONTEXT, 'verified-v1');
assert.equal(mutableEnvironment.QURAN_DASHBOARD_TEST_RUNTIME_READER_CONTEXT, undefined);
assert.equal(
  mutableEnvironment.Testing__DatabaseActivity__EnabledBackgroundActivities__0,
  'LinkingPreparedPreflightProcessor',
);
assert.equal(
  mutableEnvironment.Testing__DatabaseActivity__EnabledBackgroundActivities__1,
  'LinkingConfirmationJobProcessor',
);
assert.equal(mutableEnvironment.E2E_API_PROCESS_RECEIPT, '/private/mutable/api-process.json');
assert.equal(mutableEnvironment.E2E_BACKEND_ASSEMBLY, '/private/build/QuranDashboard.Api.dll');
assert.equal(mutableEnvironment.E2E_EVIDENCE_DIRECTORY, '/private/mutable/evidence');
assert.equal(mutableEnvironment.E2E_PLAYWRIGHT_OUTPUT_DIRECTORY, '/private/mutable/output');
assert.equal(mutableEnvironment.QURAN_DASHBOARD_TEST_RUN_ID, 'mutable-run');

assert.throws(
  () => buildStatefulPlaywrightEnvironment({}, selectStatefulPlaywrightTests(discovered)[0], {}),
  /ConnectionStrings__QuranDashboardTest/,
);

assert.deepEqual(
  buildMutableResetArguments({
    apiProcessId: null,
    expectedFingerprint: 'a'.repeat(64),
    phase: 'initial',
    runId: 'mutable-run',
  }),
  [
    'reset',
    '--run-id',
    'mutable-run',
    '--command',
    'playwright-stateful',
    '--expected-fingerprint',
    'a'.repeat(64),
    '--api-port',
    '5015',
    '--api-process-id',
    'none',
    '--phase',
    'initial',
  ],
);
assert.deepEqual(
  buildMutableResetArguments({
    apiProcessId: 4123,
    expectedFingerprint: 'b'.repeat(64),
    phase: 'final',
    runId: 'mutable-run',
  }).slice(-4),
  ['--api-process-id', '4123', '--phase', 'final'],
);
assert.throws(
  () => buildMutableResetArguments({
    apiProcessId: null,
    expectedFingerprint: 'b'.repeat(64),
    phase: 'final',
    runId: 'mutable-run',
  }),
  /final.*process/i,
);

assert.equal(
  validateApiProcessReceipt({ schemaVersion: 1, processId: 4123, port: 5015 }, 5015),
  4123,
);
assert.throws(
  () => validateApiProcessReceipt({ schemaVersion: 1, processId: 4123, port: 9999 }, 5015),
  /port/i,
);
assert.throws(() => validateApiProcessReceipt(null, 5015), /unverified.*API process/i);

assert.equal(
  classifyInteractivePlaywrightSelector(
    discovered,
    'read-only',
    'e2e/mushaf-reader.e2e.ts:42',
  ),
  'canonical-read',
);
assert.equal(
  classifyInteractivePlaywrightSelector(
    discovered,
    'read-only',
    'e2e/abwab-permissions.e2e.ts:18',
  ),
  'guarded-read',
);
assert.equal(
  classifyInteractivePlaywrightSelector(
    discovered,
    'mutating',
    'e2e/linking-success.e2e.ts:90',
  ),
  'mutating',
);
assert.throws(
  () => classifyInteractivePlaywrightSelector(
    discovered,
    'read-only',
    'e2e/linking-success.e2e.ts:90',
  ),
  /does not match.*read-only/i,
);
assert.throws(
  () => classifyInteractivePlaywrightSelector(
    discovered,
    'mutating',
    'e2e/abwab-permissions.e2e.ts:18',
  ),
  /does not match.*mutating/i,
);

const statefulRunnerSource = readFileSync(
  resolve(process.cwd(), 'scripts/run-stateful-playwright.mjs'),
  'utf8',
);
assert.match(statefulRunnerSource, /process\.on\('SIGINT'/);
assert.match(statefulRunnerSource, /process\.on\('SIGTERM'/);
assert.match(statefulRunnerSource, /keeper\.once\('close'/);
assert.match(statefulRunnerSource, /await runPlaywrightChild/);
assert.doesNotMatch(statefulRunnerSource, /spawnSync\(playwright/);

console.log('Stateful Playwright runtime contract passed.');
