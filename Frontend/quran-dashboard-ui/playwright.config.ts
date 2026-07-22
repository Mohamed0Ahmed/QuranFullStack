import { defineConfig, devices } from '@playwright/test';

// Reusable Playwright harness for the Abwab UI Kits (§15.2 gate 6, FR-004). This config only provides
// the shared runner + the locked-scenario projects; domain specs land under e2e/ in later specs
// (029+). No webServer/baseURL is fixed here because the harness scenarios drive inline content and
// each later domain spec owns its own app target. Arabic locale + RTL is the default the product runs in.
export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  forbidOnly: !!process.env['CI'],
  retries: process.env['CI'] ? 1 : 0,
  reporter: process.env['CI'] ? [['github'], ['list']] : [['list']],
  use: {
    locale: 'ar',
    trace: 'on-first-retry',
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
});
