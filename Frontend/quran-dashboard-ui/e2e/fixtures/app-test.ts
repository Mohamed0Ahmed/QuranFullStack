import { test as base, expect } from '@playwright/test';

import { LOGTO_ORIGIN, stubLogto } from './logto';

const LOCAL_HOSTNAMES = new Set(['localhost', '127.0.0.1', '[::1]']);

export const test = base.extend({
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
