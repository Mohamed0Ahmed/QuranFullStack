import { defineConfig, devices, type ReporterDescription } from '@playwright/test';
import { resolve } from 'node:path';

import { environment } from './src/environments/environment.development';
import {
  E2E_OWNER_SUBJECT,
  E2E_TEST_ISSUER,
  E2E_TEST_JWKS,
  e2eProfileEmail,
} from './e2e/fixtures/logto';

const UI_ORIGIN = 'https://localhost:4200';
const API_HEALTH_URL = 'https://localhost:5015/api/health';
const evidenceDirectory = process.env['E2E_EVIDENCE_DIRECTORY'];
const sealedExecution = process.env['E2E_SEALED_EXECUTION'] === '1';
const playwrightOutputDirectory = process.env['E2E_PLAYWRIGHT_OUTPUT_DIRECTORY'];
const tlsCertificate = process.env['E2E_TLS_CERTIFICATE'];
const tlsPrivateKey = process.env['E2E_TLS_PRIVATE_KEY'];
const chromiumExecutable = process.env['E2E_CHROMIUM_EXECUTABLE'];
const canonicalReadExecution = process.env['E2E_DATABASE_MODE'] === 'persistent-read-only';
const statefulExecution = process.env['E2E_DATABASE_MODE'] === 'persistent-stateful';
const databaseActivityProfile = resolveDatabaseActivityProfile();
const frontendCommand = sealedExecution ? 'node e2e/run-frontend.mjs' : 'npm run start:https';
const backendCommand = canonicalReadExecution
  ? 'node e2e/run-canonical-backend.mjs'
  : 'node e2e/run-backend.mjs';
if (sealedExecution && !playwrightOutputDirectory) {
  throw new Error('Sealed execution requires a private Playwright output directory.');
}
const reporters: ReporterDescription[] = evidenceDirectory
  ? [
      ['list'],
      ['./scripts/structured-playwright-reporter.mjs'],
    ]
  : [['list'], ['html', { open: 'never' }]];

// Both servers are self-signed localhost, and the backend CORS policy admits exactly
// https://localhost:4200 (Backend/api/QuranDashboard.Api/appsettings.Development.json), so neither
// origin nor port is configurable here.
const SHARED_WEB_SERVER_OPTIONS = {
  cwd: __dirname,
  reuseExistingServer: false,
  ignoreHTTPSErrors: true,
  stdout: 'pipe',
  stderr: 'pipe',
} as const;

export default defineConfig({
  testDir: './e2e',
  testMatch: /.*\.e2e\.ts$/,
  fullyParallel: true,
  workers: 2,
  retries: 0,
  timeout: 60_000,
  expect: { timeout: 15_000 },
  reporter: reporters,
  outputDir: playwrightOutputDirectory ? resolve(playwrightOutputDirectory) : undefined,
  use: {
    baseURL: UI_ORIGIN,
    ignoreHTTPSErrors: true,
    trace: sealedExecution ? 'off' : 'retain-on-failure',
    screenshot: sealedExecution ? 'off' : 'only-on-failure',
    video: 'off',
    ...(sealedExecution && chromiumExecutable
      ? { launchOptions: { executablePath: chromiumExecutable } }
      : {}),
  },
  // Projects retain their ownership boundary, while `npm run e2e` uses one worker and one shared
  // provisioned stack. A Global-scope Abwab reorder resequences every live root, so two Abwab
  // specs in different workers could race the same rows — see e2e/README.md.
  projects: [
    { name: 'default', testIgnore: /abwab-.*\.e2e\.ts$/, use: { ...devices['Desktop Chrome'] } },
    { name: 'abwab', testMatch: /abwab-.*\.e2e\.ts$/, use: { ...devices['Desktop Chrome'] } },
  ],
  webServer: [
    {
      ...SHARED_WEB_SERVER_OPTIONS,
      command: frontendCommand,
      url: UI_ORIGIN,
      timeout: 180_000,
    },
    {
      ...SHARED_WEB_SERVER_OPTIONS,
      command: backendCommand,
      gracefulShutdown: { signal: 'SIGTERM', timeout: 60_000 },
      env: {
        ASPNETCORE_ENVIRONMENT: 'Testing',
        ASPNETCORE_URLS: 'https://localhost:5015',
        Testing__DatabaseActivity__Profile: databaseActivityProfile,
        ...(canonicalReadExecution || statefulExecution
          ? {}
          : {
              Testing__DatabaseActivity__EnabledBackgroundActivities__0:
                'LinkingPreparedPreflightProcessor',
              Testing__DatabaseActivity__EnabledBackgroundActivities__1:
                'LinkingConfirmationJobProcessor',
            }),
        Auth__Authority: `${new URL(environment.logto.endpoint).origin}/oidc`,
        Auth__Audience: environment.logto.resource,
        Auth__InteractiveClientId: environment.logto.appId,
        E2E__TestIssuer__Enabled: 'true',
        E2E__TestIssuer__Issuer: E2E_TEST_ISSUER,
        E2E__TestIssuer__Jwks: JSON.stringify(E2E_TEST_JWKS),
        OwnerBootstrap__Emails__0: e2eProfileEmail(E2E_OWNER_SUBJECT),
        ...(tlsCertificate && tlsPrivateKey
          ? {
              ASPNETCORE_Kestrel__Certificates__Default__Path: tlsCertificate,
              ASPNETCORE_Kestrel__Certificates__Default__KeyPath: tlsPrivateKey,
            }
          : {}),
      },
      url: API_HEALTH_URL,
      timeout: 300_000,
    },
  ],
});

function resolveDatabaseActivityProfile(): 'ReadOnly' | 'Mutable' {
  if (!statefulExecution) return canonicalReadExecution ? 'ReadOnly' : 'Mutable';
  const configured = process.env['Testing__DatabaseActivity__Profile'];
  if (configured !== 'ReadOnly' && configured !== 'Mutable') {
    throw new Error('Stateful Playwright requires a ReadOnly or Mutable API activity profile.');
  }
  return configured;
}
