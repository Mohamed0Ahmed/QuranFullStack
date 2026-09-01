import assert from 'node:assert/strict';
import { existsSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { resolve } from 'node:path';

import {
  createSealedEnvironment,
  redactDiagnosticText,
  validateProvisioningReceipt,
} from '../e2e/harness/sealed-execution-contract.mjs';

const receipt = {
  schemaVersion: 1,
  status: 'passed',
  inputs: {
    npmLockSha256: 'a'.repeat(64),
    nugetLocksSha256: 'b'.repeat(64),
    artifactLockSha256: 'c'.repeat(64),
    harnessSourceSha256: 'f'.repeat(64),
    chromiumRevision: '1234',
    postgresqlImage: `postgres@sha256:${'d'.repeat(64)}`,
  },
  outputs: {
    artifactVerifierOutput: '/workspace/artifact-verifier',
    chromiumExecutable: '/workspace/chromium',
    frontendBuild: '/workspace/dist/browser',
    backendOutput: '/workspace/backend',
    tlsCertificate: '/workspace/certificate.pem',
    tlsPrivateKey: '/workspace/certificate-key.pem',
    egressGuard: '/workspace/egress-guard.so',
  },
  outputSha256: Object.fromEntries(
    [
      'artifactVerifierOutput',
      'chromiumExecutable',
      'frontendBuild',
      'backendOutput',
      'tlsCertificate',
      'tlsPrivateKey',
      'egressGuard',
    ].map((name) => [name, 'e'.repeat(64)]),
  ),
  phases: [
    'dependencyProvisioning',
    'chromiumProvisioning',
    'postgresqlProvisioning',
    'artifactProvisioning',
    'certificateProvisioning',
    'buildProvisioning',
  ].map((name) => ({ name, status: 'passed', durationMs: 1 })),
};

assert.doesNotThrow(() => validateProvisioningReceipt(receipt));
assert.throws(
  () => validateProvisioningReceipt({ ...receipt, phases: receipt.phases.slice(1) }),
  /dependencyProvisioning/,
);
assert.throws(
  () => validateProvisioningReceipt({ ...receipt, status: 'failed' }),
  /passed provisioning receipt/,
);

const sealed = createSealedEnvironment(
  {
    PATH: '/usr/bin',
    HOME: '/workspace/home',
    CI: 'true',
    ARTIFACT_DOWNLOAD_TOKEN: 'artifact-secret',
    AWS_SECRET_ACCESS_KEY: 'aws-secret',
    NPM_TOKEN: 'npm-secret',
    ConnectionStrings__QuranDashboardDb: 'Password=database-secret',
    RANDOM_PASSWORD: 'another-secret',
    SYSTEM_ACCESSTOKEN: 'system-token',
    DOCKER_AUTH_CONFIG: '{"auths":{"registry.test":{"auth":"private"}}}',
    QDB_PR_OBSERVATION_RESULT_DIR: '/workspace/observation-results',
    SAFE_SETTING: 'retained',
  },
  {
    chromiumExecutable: '/workspace/chromium',
    databaseHost: '172.20.0.2',
    egressGuard: '/workspace/egress-guard.so',
    evidenceDirectory: '/workspace/evidence',
    playwrightOutputDirectory: '/private/playwright-output',
    tlsCertificate: '/workspace/certificate.pem',
    tlsPrivateKey: '/workspace/certificate-key.pem',
  },
);

assert.equal(sealed.HOME, '/workspace/home');
assert.equal(sealed.SAFE_SETTING, undefined);
assert.equal(sealed.E2E_CHROMIUM_EXECUTABLE, '/workspace/chromium');
assert.equal(sealed.E2E_DATABASE_MODE, 'artifact');
assert.equal(sealed.E2E_PREPARED_DATABASE, '1');
assert.equal(sealed.E2E_PLAYWRIGHT_OUTPUT_DIRECTORY, '/private/playwright-output');
assert.equal(sealed.E2E_SEALED_EXECUTION, '1');
assert.equal(sealed.QDB_E2E_ALLOWED_IPV4, '172.20.0.2');
assert.equal(sealed.QDB_PR_OBSERVATION_RESULT_DIR, '/workspace/observation-results');
assert.equal(sealed.LD_PRELOAD, '/workspace/egress-guard.so');
assert.equal(sealed.ARTIFACT_DOWNLOAD_TOKEN, undefined);
assert.equal(sealed.AWS_SECRET_ACCESS_KEY, undefined);
assert.equal(sealed.NPM_TOKEN, undefined);
assert.equal(sealed.ConnectionStrings__QuranDashboardDb, undefined);
assert.equal(sealed.RANDOM_PASSWORD, undefined);
assert.equal(sealed.SYSTEM_ACCESSTOKEN, undefined);
assert.equal(sealed.DOCKER_AUTH_CONFIG, undefined);

const redacted = redactDiagnosticText(
  [
    'Authorization: Bearer abc.def.ghi',
    'Cookie: session=private-value',
    'Password=database-secret',
    'https://example.test/path?token=secret&safe=no',
    '-----BEGIN PRIVATE KEY----- private -----END PRIVATE KEY-----',
    'known-value',
  ].join('\n'),
  ['known-value'],
);

assert.doesNotMatch(redacted, /abc\.def\.ghi|private-value|database-secret|token=secret|known-value/);
assert.match(redacted, /Authorization: \[REDACTED\]/);
assert.match(redacted, /https:\/\/example\.test\/path\?\[REDACTED\]/);
assert.match(redacted, /-----BEGIN PRIVATE KEY-----\[REDACTED\]-----END PRIVATE KEY-----/);

const frontendRoot = process.cwd();
const packageManifest = JSON.parse(readFileSync(resolve(frontendRoot, 'package.json'), 'utf8'));
assert.equal(packageManifest.scripts.e2e, 'node scripts/run-sealed-playwright.mjs --full');
assert.equal(
  packageManifest.scripts['e2e:critical'],
  'node scripts/run-sealed-playwright.mjs --critical',
);
assert.equal(
  packageManifest.scripts['e2e:provision'],
  'node scripts/provision-sealed-playwright.mjs',
);
const playwrightConfiguration = readFileSync(resolve(frontendRoot, 'playwright.config.ts'), 'utf8');
assert.match(playwrightConfiguration, /E2E_PLAYWRIGHT_OUTPUT_DIRECTORY/);
assert.doesNotMatch(playwrightConfiguration, /resolve\(evidenceDirectory, 'test-results'\)/);
const accessibilityFixture = readFileSync(
  resolve(frontendRoot, 'e2e/fixtures/accessibility.ts'),
  'utf8',
);
assert.doesNotMatch(accessibilityFixture, /node\.html|\bhtml\s*:/);

const reporterDirectory = mkdtempSync(resolve(tmpdir(), 'qdb-e2e-reporter-contract-'));
try {
  process.env.E2E_EVIDENCE_DIRECTORY = reporterDirectory;
  process.env.E2E_ATTACHMENT_TEST_TOKEN = 'attachment-secret';
  const { default: StructuredPlaywrightReporter } = await import(
    './structured-playwright-reporter.mjs'
  );
  const reporter = new StructuredPlaywrightReporter();
  const test = {
    annotations: [{ type: 'journey', description: 'quran-fidelity.contract' }],
    id: 'contract-failure',
    location: { file: '/workspace/contract.e2e.ts', line: 1 },
    titlePath: () => ['', 'default', 'contract failure'],
  };
  reporter.onBegin({}, { allTests: () => [test] });
  reporter.onTestEnd(test, {
    attachments: [
      {
        name: 'sanitized-screenshot',
        contentType: 'image/png',
        body: Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]),
      },
      {
        name: 'request-metadata',
        contentType: 'application/json',
        body: Buffer.from('[{"event":"request","method":"GET","origin":"https://localhost:4200","path":"/api/health","resourceType":"fetch"}]'),
      },
      {
        name: 'browser-console-errors',
        contentType: 'application/json',
        body: Buffer.from('[{"type":"pageerror","name":"Error","text":"attachment-secret"}]'),
      },
      {
        name: 'accessibility-observations',
        contentType: 'application/json',
        body: Buffer.from('[{"id":"color-contrast","impact":"serious","help":"Elements must meet minimum color contrast ratio thresholds","nodeCount":1,"targets":["[data-testid=sample]"]}]'),
      },
      {
        name: 'error-context',
        contentType: 'text/markdown',
        body: Buffer.from('raw page text'),
      },
    ],
    duration: 1,
    errors: [{
      message: 'serious/critical accessibility violations: [{"id":"color-contrast","impact":"serious","help":"Elements must meet contrast thresholds","nodeCount":1,"targets":["[data-testid=sample]"]}]',
    }],
    retry: 0,
    status: 'failed',
  });
  reporter.onEnd({ duration: 1, status: 'failed' });

  const diagnosticRoot = resolve(reporterDirectory, 'diagnostics/contract-failure');
  assert.equal(existsSync(resolve(diagnosticRoot, 'sanitized-screenshot.png')), true);
  assert.equal(existsSync(resolve(diagnosticRoot, 'request-metadata.json')), true);
  assert.equal(existsSync(resolve(diagnosticRoot, 'browser-console-errors.json')), true);
  assert.equal(existsSync(resolve(diagnosticRoot, 'accessibility-observations.json')), true);
  assert.equal(existsSync(resolve(diagnosticRoot, 'error-context.md')), false);
  assert.doesNotMatch(
    readFileSync(resolve(diagnosticRoot, 'browser-console-errors.json'), 'utf8'),
    /attachment-secret/,
  );
  const reporterResults = JSON.parse(
    readFileSync(resolve(reporterDirectory, 'playwright-results.json'), 'utf8'),
  );
  assert.deepEqual(
    reporterResults.tests[0].attachments.map((attachment) => attachment.name).sort(),
    ['accessibility-observations', 'browser-console-errors', 'request-metadata', 'sanitized-screenshot'],
  );
  assert.equal(reporterResults.tests[0].journey, 'quran-fidelity.contract');
  assert.doesNotMatch(JSON.stringify(reporterResults.tests[0].errors), /<[^>]+>/);

  const rawPath = resolve(reporterDirectory, 'raw-request.json');
  writeFileSync(rawPath, '[{"event":"request","method":"GET","origin":"https://localhost:4200","path":"/"}]');
  const invalidCases = [
    {
      label: 'wrong MIME',
      attachments: [{
        name: 'sanitized-screenshot',
        contentType: 'application/octet-stream',
        body: Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]),
      }],
    },
    {
      label: 'duplicate approved name',
      attachments: [
        { name: 'request-metadata', contentType: 'application/json', body: Buffer.from('[]') },
        { name: 'request-metadata', contentType: 'application/json', body: Buffer.from('[]') },
      ],
    },
    {
      label: 'path-backed raw data',
      attachments: [{ name: 'request-metadata', contentType: 'application/json', path: rawPath }],
    },
    {
      label: 'invalid request metadata schema',
      attachments: [{
        name: 'request-metadata',
        contentType: 'application/json',
        body: Buffer.from('[{"headers":{"authorization":"secret"}}]'),
      }],
    },
    {
      label: 'invalid accessibility observation schema',
      attachments: [{
        name: 'accessibility-observations',
        contentType: 'application/json',
        body: Buffer.from('[{"html":"raw page content"}]'),
      }],
    },
  ];
  for (const invalidCase of invalidCases) {
    const invalidReporter = new StructuredPlaywrightReporter();
    invalidReporter.onBegin({}, { allTests: () => [test] });
    assert.throws(
      () => invalidReporter.onTestEnd(test, {
        attachments: invalidCase.attachments,
        duration: 1,
        errors: [],
        retry: 0,
        status: 'failed',
      }),
      undefined,
      invalidCase.label,
    );
  }
} finally {
  delete process.env.E2E_EVIDENCE_DIRECTORY;
  delete process.env.E2E_ATTACHMENT_TEST_TOKEN;
  rmSync(reporterDirectory, { recursive: true, force: true });
}

console.log('Sealed Playwright provisioning, credential, and sanitization contract passed.');
