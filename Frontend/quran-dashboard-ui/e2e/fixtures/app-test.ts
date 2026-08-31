import { execFileSync } from 'node:child_process';
import { resolve } from 'node:path';
import { test as base, expect } from '@playwright/test';

import { LOGTO_ORIGIN, stubLogto } from './logto';

const LOCAL_HOSTNAMES = new Set(['localhost', '127.0.0.1', '[::1]']);
const FRONTEND_ROOT = process.cwd();
const RESET_DATABASE = resolve(FRONTEND_ROOT, 'e2e/reset-database.mjs');

export const test = base.extend<{ mutableDatabaseState: void }>({
  mutableDatabaseState: [
    async ({}, use, testInfo) => {
      const mutating = testInfo.annotations.some((annotation) => annotation.type === 'mutating');
      if (mutating) {
        resetMutableDatabase();
      }

      try {
        await use();
      } finally {
        if (mutating) {
          resetMutableDatabase();
        }
      }
    },
    { auto: true },
  ],
  context: async ({ context }, use) => {
    const leaked: string[] = [];

    await stubLogto(context);

    context.on('request', (request) => {
      const url = new URL(request.url());
      if (url.protocol !== 'http:' && url.protocol !== 'https:') return;
      if (url.origin === LOGTO_ORIGIN) return;
      if (LOCAL_HOSTNAMES.has(url.hostname)) return;
      leaked.push(request.url());
    });

    await use(context);

    expect(leaked, `requests left localhost: ${leaked.join(', ')}`).toEqual([]);
  },
});

export { expect };

function resetMutableDatabase(): void {
  execFileSync(process.execPath, [RESET_DATABASE], {
    cwd: FRONTEND_ROOT,
    stdio: 'inherit',
  });
}
