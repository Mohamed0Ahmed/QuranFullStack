import { defineConfig, devices } from '@playwright/test';

const UI_ORIGIN = 'https://localhost:4200';
const API_HEALTH_URL = 'https://localhost:5015/api/health';

// Both servers are self-signed localhost, and the backend CORS policy admits exactly
// https://localhost:4200 (Backend/api/QuranDashboard.Api/appsettings.Development.json), so neither
// origin nor port is configurable here.
const SHARED_WEB_SERVER_OPTIONS = {
  cwd: __dirname,
  reuseExistingServer: true,
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
  // Split so `npm run e2e` can run Abwab at --workers=1 (T502): a Global-scope reorder
  // resequences every live root in the database, so two Abwab specs in different workers can
  // race the same rows — see e2e/README.md. Every other spec stays read-only and unaffected.
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
      // /api/health is DbContext-backed and answers 503 when the database is unreachable, which
      // Playwright refuses to accept as ready — a broken DB fails the boot instead of producing a
      // suite of red UI tests.
      command:
        'dotnet run --project ../../Backend/api/QuranDashboard.Api --launch-profile https --no-build',
      url: API_HEALTH_URL,
      timeout: 120_000,
    },
  ],
});
