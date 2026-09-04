import assert from 'node:assert/strict';
import {
  existsSync,
  mkdtempSync,
  mkdirSync,
  readFileSync,
  rmSync,
  writeFileSync,
} from 'node:fs';
import { tmpdir } from 'node:os';
import { resolve } from 'node:path';

import {
  buildControlledBackendArguments,
  createControlledEnvironment,
  redactDiagnosticText,
  validateControlledProvisioningReceipt,
} from '../e2e/harness/controlled-execution-contract.mjs';
import {
  appendApplicationShutdownPhase,
  appendChildExecutionPhases,
  appendMissingChildFailurePhases,
  createPrivatePlaywrightRuntime,
  inspectRetainedEvidence,
  validatePlaywrightChildResult,
} from './controlled-playwright-runtime.mjs';
import { cleanupControlledPlaywrightRuntime } from './cleanup-controlled-playwright-runtime.mjs';

const outputNames = [
  'backendOutput',
  'chromiumExecutable',
  'egressGuard',
  'frontendBuild',
  'testRuntimeOutput',
  'tlsCertificate',
  'tlsPrivateKey',
];
const receipt = {
  schemaVersion: 2,
  status: 'passed',
  inputs: {
    chromiumRevision: '1234',
    chromiumVersion: '140.0.0.0',
    harnessSourceSha256: 'c'.repeat(64),
    npmLockSha256: 'a'.repeat(64),
    nugetLocksSha256: 'b'.repeat(64),
  },
  outputs: Object.fromEntries(outputNames.map((name) => [name, `/workspace/${name}`])),
  outputSha256: Object.fromEntries(outputNames.map((name) => [name, 'd'.repeat(64)])),
  phases: [
    'dependencyProvisioning',
    'chromiumProvisioning',
    'certificateProvisioning',
    'buildProvisioning',
  ].map((name) => ({ name, status: 'passed', durationMs: 1 })),
};

assert.doesNotThrow(() => validateControlledProvisioningReceipt(receipt));
assert.throws(
  () => validateControlledProvisioningReceipt({
    ...receipt,
    inputs: { ...receipt.inputs, postgresqlImage: `postgres@sha256:${'e'.repeat(64)}` },
  }),
  /unexpected.*postgresqlImage/i,
);
assert.throws(
  () => validateControlledProvisioningReceipt({
    ...receipt,
    outputs: { ...receipt.outputs, artifactVerifierOutput: '/workspace/artifacts' },
  }),
  /unexpected.*artifactVerifierOutput/i,
);
assert.throws(
  () => validateControlledProvisioningReceipt({ ...receipt, phases: receipt.phases.slice(1) }),
  /dependencyProvisioning/i,
);

const connectionString =
  'Host=127.0.0.1;Port=5432;Database=quran_dashboard_test;Username=test-runner;Password=secret';
const controlled = createControlledEnvironment(
  {
    PATH: '/usr/bin',
    HOME: '/workspace/home',
    ARTIFACT_DOWNLOAD_TOKEN: 'artifact-secret',
    AWS_SECRET_ACCESS_KEY: 'aws-secret',
    ConnectionStrings__QuranDashboardDb: 'Password=development-secret',
    ConnectionStrings__QuranDashboardTest: connectionString,
    DISPLAY: ':1',
    WAYLAND_DISPLAY: 'wayland-0',
    XAUTHORITY: '/run/user/1000/xauthority',
    XDG_RUNTIME_DIR: '/run/user/1000',
    DOCKER_AUTH_CONFIG: '{"auths":{"registry.test":{"auth":"private"}}}',
    NPM_TOKEN: 'npm-secret',
    RANDOM_PASSWORD: 'another-secret',
  },
  {
    backendAssembly: '/workspace/backend/QuranDashboard.Api.dll',
    chromiumExecutable: '/workspace/chromium',
    egressGuard: '/workspace/egress-guard.so',
    evidenceDirectory: '/workspace/evidence',
    frontendBuild: '/workspace/dist/browser',
    homeDirectory: '/private/home',
    playwrightOutputDirectory: '/private/playwright-output',
    testRuntimeAssembly: '/workspace/runtime/QuranDashboard.TestRuntime.dll',
    tlsCertificate: '/workspace/certificate.pem',
    tlsPrivateKey: '/workspace/certificate-key.pem',
  },
);

assert.equal(controlled.ConnectionStrings__QuranDashboardTest, connectionString);
assert.equal(controlled.ConnectionStrings__QuranDashboardDb, undefined);
assert.equal(controlled.E2E_CONTROLLED_EXECUTION, '1');
assert.equal(controlled.E2E_SEALED_EXECUTION, undefined);
assert.equal(controlled.E2E_DATABASE_MODE, undefined);
assert.equal(controlled.E2E_CHROMIUM_EXECUTABLE, '/workspace/chromium');
assert.equal(controlled.E2E_FRONTEND_BUILD, '/workspace/dist/browser');
assert.equal(controlled.E2E_BACKEND_ASSEMBLY, '/workspace/backend/QuranDashboard.Api.dll');
assert.equal(
  controlled.E2E_TEST_RUNTIME_ASSEMBLY,
  '/workspace/runtime/QuranDashboard.TestRuntime.dll',
);
assert.equal(controlled.E2E_PLAYWRIGHT_OUTPUT_DIRECTORY, '/private/playwright-output');
assert.equal(controlled.HOME, '/private/home');
assert.equal(controlled.XDG_CACHE_HOME, '/private/home/.cache');
assert.equal(controlled.LD_PRELOAD, '/workspace/egress-guard.so');
assert.equal(controlled.QDB_E2E_ALLOWED_IPV4, undefined);
assert.equal(controlled.ARTIFACT_DOWNLOAD_TOKEN, undefined);
assert.equal(controlled.AWS_SECRET_ACCESS_KEY, undefined);
assert.equal(controlled.NPM_TOKEN, undefined);
assert.equal(controlled.RANDOM_PASSWORD, undefined);
assert.equal(controlled.DOCKER_AUTH_CONFIG, undefined);
assert.equal(controlled.DISPLAY, ':1');
assert.equal(controlled.WAYLAND_DISPLAY, 'wayland-0');
assert.equal(controlled.XAUTHORITY, '/run/user/1000/xauthority');
assert.equal(controlled.XDG_RUNTIME_DIR, '/run/user/1000');

assert.deepEqual(
  buildControlledBackendArguments('/workspace/QuranDashboard.Api.dll', '/workspace/api.csproj', true),
  [
    '/workspace/QuranDashboard.Api.dll',
    '--Logging:LogLevel:Microsoft.EntityFrameworkCore.Database.Command=Warning',
  ],
);
assert.deepEqual(
  buildControlledBackendArguments(undefined, '/workspace/api.csproj', false),
  ['run', '--project', '/workspace/api.csproj', '--no-build', '--no-restore', '--no-launch-profile'],
);

const redacted = redactDiagnosticText(
  [
    'Authorization: Bearer abc.def.ghi',
    'Cookie: session=private-value',
    'Password=database-secret',
    'https://example.test/path?token=secret&safe=no',
    '-----BEGIN PRIVATE KEY----- private -----END PRIVATE KEY-----',
    connectionString,
  ].join('\n'),
  [connectionString],
);
assert.doesNotMatch(
  redacted,
  /abc\.def\.ghi|private-value|database-secret|token=secret|test-runner|BEGIN PRIVATE KEY----- private/,
);

const frontendRoot = process.cwd();
const packageManifest = JSON.parse(readFileSync(resolve(frontendRoot, 'package.json'), 'utf8'));
assert.equal(
  packageManifest.scripts['check:controlled-playwright-runtime'],
  'node scripts/verify-controlled-playwright-runtime.mjs',
);
assert.equal(
  packageManifest.scripts['e2e:provision'],
  'node scripts/provision-controlled-playwright.mjs',
);
assert.equal(packageManifest.scripts['check:sealed-e2e-contract'], undefined);

for (const file of [
  'scripts/provision-controlled-playwright.mjs',
  'e2e/harness/controlled-execution-contract.mjs',
]) {
  const source = readFileSync(resolve(frontendRoot, file), 'utf8');
  assert.doesNotMatch(
    source,
    /postgresqlImage|artifactVerifier|COMPACT_ARTIFACT|database-runtime|docker(?:\s|', \[')(?:pull|image|network)/i,
    file,
  );
}

const playwrightConfiguration = readFileSync(resolve(frontendRoot, 'playwright.config.ts'), 'utf8');
assert.match(playwrightConfiguration, /E2E_CONTROLLED_EXECUTION/);
assert.doesNotMatch(playwrightConfiguration, /E2E_SEALED_EXECUTION/);
for (const backendWrapper of ['e2e/run-canonical-backend.mjs', 'e2e/run-backend.mjs']) {
  assert.match(
    readFileSync(resolve(frontendRoot, backendWrapper), 'utf8'),
    /buildControlledBackendArguments/,
    backendWrapper,
  );
}

const evidenceDirectory = mkdtempSync(resolve(tmpdir(), 'qdb-controlled-evidence-'));
try {
  const childResult = {
    schemaVersion: 1,
    runId: 'child-run',
    status: 'failed',
    applicationsReadyAt: '2026-09-04T10:00:01.000Z',
    completedAt: '2026-09-04T10:00:03.000Z',
    durationMs: 2_000,
    declaredTestCount: 1,
    counts: { failed: 1 },
    tests: [{
      id: 'test-id',
      journey: null,
      title: 'failure',
      file: '/workspace/test.e2e.ts',
      line: 1,
      status: 'failed',
      durationMs: 2_000,
      retry: 0,
      errors: ['Password=secret'],
      attachments: [],
    }],
  };
  writeFileSync(
    resolve(evidenceDirectory, 'playwright-results.json'),
    `${JSON.stringify(childResult)}\n`,
  );
  writeFileSync(resolve(evidenceDirectory, 'application.log'), 'Password=secret\n');
  writeFileSync(resolve(evidenceDirectory, 'raw-trace.zip'), 'unsafe');
  mkdirSync(resolve(evidenceDirectory, 'diagnostics/test-id'), { recursive: true });
  writeFileSync(
    resolve(evidenceDirectory, 'diagnostics/test-id/browser-console-errors.json'),
    '[{"type":"pageerror","name":"Error","text":"Password=secret"}]',
  );

  assert.equal(
    validatePlaywrightChildResult(childResult, { declaredTestCount: 1, runId: 'child-run' }),
    childResult,
  );
  assert.throws(
    () => validatePlaywrightChildResult(childResult, { declaredTestCount: 2, runId: 'child-run' }),
    /declared test count/i,
  );
  assert.throws(
    () => validatePlaywrightChildResult(childResult, { declaredTestCount: 1, runId: 'other-run' }),
    /run ID/i,
  );

  const failedShutdownPhases = [];
  appendChildExecutionPhases(
    failedShutdownPhases,
    childResult,
    Date.parse('2026-09-04T10:00:00.000Z'),
    Date.parse('2026-09-04T10:00:04.000Z'),
  );
  appendMissingChildFailurePhases(failedShutdownPhases);
  assert.deepEqual(
    failedShutdownPhases.map(({ name, status }) => ({ name, status })),
    [
      { name: 'applicationStartup', status: 'passed' },
      { name: 'testExecution', status: 'failed' },
      { name: 'applicationShutdown', status: 'failed' },
    ],
  );
  const passedShutdownPhases = failedShutdownPhases.slice(0, 2);
  appendApplicationShutdownPhase(
    passedShutdownPhases,
    childResult,
    Date.parse('2026-09-04T10:00:04.000Z'),
    'passed',
  );
  assert.deepEqual(passedShutdownPhases.at(-1), {
    name: 'applicationShutdown',
    status: 'passed',
    durationMs: 1_000,
  });

  const inspection = inspectRetainedEvidence(evidenceDirectory, ['secret']);
  assert.equal(inspection.status, 'passed', JSON.stringify(inspection));
  assert.deepEqual(inspection.removedRawFiles, ['raw-trace.zip']);
  assert.equal(existsSync(resolve(evidenceDirectory, 'raw-trace.zip')), false);
  assert.doesNotMatch(readFileSync(resolve(evidenceDirectory, 'application.log'), 'utf8'), /secret/);
  assert.doesNotMatch(
    readFileSync(
      resolve(evidenceDirectory, 'diagnostics/test-id/browser-console-errors.json'),
      'utf8',
    ),
    /secret/,
  );
} finally {
  rmSync(evidenceDirectory, { recursive: true, force: true });
}

const cleanupResults = mkdtempSync(resolve(tmpdir(), 'qdb-controlled-cleanup-contract-'));
const cleanupOwner = resolve(cleanupResults, 'attempts/primary/playwright-evidence/run/evidence');
const orphanedRuntime = createPrivatePlaywrightRuntime(cleanupOwner);
try {
  rmSync(orphanedRuntime.playwrightOutputDirectory, { recursive: true, force: true });
  mkdirSync(orphanedRuntime.playwrightOutputDirectory, { mode: 0o700 });
  assert.equal(cleanupControlledPlaywrightRuntime(cleanupResults, 'primary'), 2);
  assert.equal(existsSync(orphanedRuntime.homeDirectory), false);
  assert.equal(existsSync(orphanedRuntime.playwrightOutputDirectory), false);
} finally {
  orphanedRuntime.cleanup();
  rmSync(cleanupResults, { recursive: true, force: true });
}

const canonicalRunner = readFileSync(resolve(frontendRoot, 'scripts/run-canonical-playwright.mjs'), 'utf8');
const statefulRunner = readFileSync(resolve(frontendRoot, 'scripts/run-stateful-playwright.mjs'), 'utf8');
const allRunner = readFileSync(resolve(frontendRoot, 'scripts/run-all-playwright.mjs'), 'utf8');
for (const [name, source] of [
  ['canonical runner', canonicalRunner],
  ['stateful runner', statefulRunner],
]) {
  assert.match(source, /loadControlledProvisioningReceipt/, name);
  assert.match(source, /createControlledPlaywrightEnvironment/, name);
  assert.match(source, /createPrivatePlaywrightRuntime/, name);
  assert.match(source, /discoverControlledPlaywright/, name);
  assert.match(source, /inspectRetainedEvidence/, name);
  assert.doesNotMatch(source, /stdio:\s*'inherit'/, name);
}
for (const router of ['scripts/run-focused-playwright.mjs', 'scripts/run-interactive-playwright.mjs']) {
  assert.match(
    readFileSync(resolve(frontendRoot, router), 'utf8'),
    /discoverControlledPlaywright/,
    router,
  );
}
const controlledRuntimeSource = readFileSync(
  resolve(frontendRoot, 'scripts/controlled-playwright-runtime.mjs'),
  'utf8',
);
assert.match(controlledRuntimeSource, /qdb-controlled-playwright-home-/);
assert.match(controlledRuntimeSource, /applicationStartup/);
assert.match(controlledRuntimeSource, /applicationShutdown/);
assert.match(controlledRuntimeSource, /testExecution/);
assert.match(allRunner, /playwright-run\.json/);
assert.match(allRunner, /QDB_PLAYWRIGHT_AGGREGATE_DIRECTORY/);

console.log('Controlled Playwright provisioning and execution contract passed.');
