import { defineConfig, devices } from '@playwright/test';

import { environment } from './src/environments/environment.development';
import {
  E2E_OWNER_SUBJECT,
  E2E_TEST_ISSUER,
  E2E_TEST_JWKS,
  e2eProfileEmail,
} from './e2e/fixtures/logto';

const UI_ORIGIN = 'https://localhost:4200';
const API_HEALTH_URL = 'https://localhost:5015/api/health';

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
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL: UI_ORIGIN,
    ignoreHTTPSErrors: true,
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    video: 'off',
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
      command: 'npm run start:https',
      url: UI_ORIGIN,
      timeout: 180_000,
    },
    {
      ...SHARED_WEB_SERVER_OPTIONS,
      command: 'node e2e/run-backend.mjs',
      gracefulShutdown: { signal: 'SIGTERM', timeout: 60_000 },
      env: {
        ASPNETCORE_ENVIRONMENT: 'Testing',
        ASPNETCORE_URLS: 'https://localhost:5015',
        Auth__Authority: `${new URL(environment.logto.endpoint).origin}/oidc`,
        Auth__Audience: environment.logto.resource,
        Auth__InteractiveClientId: environment.logto.appId,
        E2E__TestIssuer__Enabled: 'true',
        E2E__TestIssuer__Issuer: E2E_TEST_ISSUER,
        E2E__TestIssuer__Jwks: JSON.stringify(E2E_TEST_JWKS),
        OwnerBootstrap__Emails__0: e2eProfileEmail(E2E_OWNER_SUBJECT),
      },
      url: API_HEALTH_URL,
      timeout: 300_000,
    },
  ],
});
