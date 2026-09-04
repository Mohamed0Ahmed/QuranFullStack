import { test as base, expect, type BrowserContext, type TestInfo } from '@playwright/test';

import { LOGTO_ORIGIN, stubLogto } from './logto';

const LOCAL_HOSTNAMES = new Set(['localhost', '127.0.0.1', '[::1]']);
export const test = base.extend({
  page: async ({ page }, use, testInfo) => {
    await use(page);
    if (testInfo.status === testInfo.expectedStatus || page.isClosed()) return;

    try {
      await page.addStyleTag({
        content: `
          *, *::before, *::after {
            background-image: none !important;
            caret-color: transparent !important;
            color: transparent !important;
            text-shadow: none !important;
          }
          canvas, iframe, img, picture, svg, video { visibility: hidden !important; }
        `,
      });
      await testInfo.attach('sanitized-screenshot', {
        body: await page.screenshot({
          animations: 'disabled',
          caret: 'hide',
          fullPage: true,
        }),
        contentType: 'image/png',
      });
    } catch {
      // Diagnostics must never turn the originating failure into a different failure.
    }
  },
  context: async ({ context }, use, testInfo) => {
    const finalizeInstrumentation = await instrumentAppContext(context, testInfo);
    try {
      await use(context);
    } finally {
      await finalizeInstrumentation();
    }
  },
});

export { expect };

export async function instrumentAppContext(
  context: BrowserContext,
  testInfo: TestInfo,
): Promise<() => Promise<void>> {
  const leaked: string[] = [];
  const requests: Array<Record<string, unknown>> = [];
  const consoleErrors: Array<Record<string, unknown>> = [];
  let finalized = false;

  await stubLogto(context);

  context.on('request', (request) => {
    const url = new URL(request.url());
    requests.push({
      event: 'request',
      method: request.method(),
      origin: url.origin,
      path: url.pathname,
      resourceType: request.resourceType(),
    });
    if (url.protocol !== 'http:' && url.protocol !== 'https:') return;
    if (url.origin === LOGTO_ORIGIN) return;
    if (LOCAL_HOSTNAMES.has(url.hostname)) return;
    leaked.push(`${url.origin}${url.pathname}`);
  });

  context.on('response', (response) => {
    const request = response.request();
    const url = new URL(request.url());
    requests.push({
      event: 'response',
      method: request.method(),
      origin: url.origin,
      path: url.pathname,
      status: response.status(),
    });
  });

  context.on('requestfailed', (request) => {
    const url = new URL(request.url());
    requests.push({
      event: 'requestfailed',
      method: request.method(),
      origin: url.origin,
      path: url.pathname,
      error: sanitizeBrowserDiagnostic(request.failure()?.errorText ?? 'request failed'),
    });
  });

  context.on('page', (page) => {
    page.on('console', (message) => {
      if (message.type() !== 'error') return;
      const location = message.location();
      consoleErrors.push({
        type: message.type(),
        text: sanitizeBrowserDiagnostic(message.text()),
        location: stripUrlQuery(location.url),
        line: location.lineNumber,
        column: location.columnNumber,
      });
    });
    page.on('pageerror', (error) => {
      consoleErrors.push({
        type: 'pageerror',
        name: error.name,
        text: sanitizeBrowserDiagnostic(error.message),
      });
    });
  });

  return async () => {
    if (finalized) return;
    finalized = true;

    if (testInfo.status !== testInfo.expectedStatus || leaked.length > 0) {
      await testInfo.attach('request-metadata', {
        body: Buffer.from(`${JSON.stringify(requests.slice(-1000), null, 2)}\n`),
        contentType: 'application/json',
      });
      await testInfo.attach('browser-console-errors', {
        body: Buffer.from(`${JSON.stringify(consoleErrors.slice(-250), null, 2)}\n`),
        contentType: 'application/json',
      });
    }

    expect(leaked, `requests left localhost: ${leaked.join(', ')}`).toEqual([]);
  };
}

function sanitizeBrowserDiagnostic(value: string): string {
  return stripUrlQuery(value)
    .replace(/\b(?:Bearer|Basic)\s+[A-Za-z0-9._~+\/-]+=*/gi, '[REDACTED]')
    .replace(/\beyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\b/g, '[REDACTED]')
    .replace(/\b(?:cookie|password|token|secret)\s*[:=]\s*[^\s,;]+/gi, '[REDACTED]')
    .slice(0, 4000);
}

function stripUrlQuery(value: string): string {
  return value.replace(/(https?:\/\/[^\s?#]+)\?[^\s#]*/gi, '$1?[REDACTED]');
}
