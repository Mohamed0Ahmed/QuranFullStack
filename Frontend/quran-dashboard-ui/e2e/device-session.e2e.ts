import { environment } from '../src/environments/environment.development';
import { expect, test } from './fixtures/auth';
import { LOGTO_ORIGIN } from './fixtures/logto';

const API_ORIGIN = environment.apiBaseUrl;
const DEEP_RETURN_URL = '/dashboard?view=device-session&mode=exact#auth-return';
const SESSION_COOKIE = '__Secure-quran-dashboard-session';
const CSRF_COOKIE = 'XSRF-TOKEN';
const CSRF_HEADER = 'X-XSRF-TOKEN';
const IDENTITY_EVIDENCE_HEADER = 'x-interactive-identity-evidence';

interface BrowserCookie {
  name: string;
  value: string;
  httpOnly: boolean;
  secure: boolean;
  sameSite: 'Strict' | 'Lax' | 'None';
  path: string;
}

test(
  'the browser bootstraps, protects, and revokes its HTTPS device session',
  {
    annotation: [
      { type: 'critical' },
      { type: 'mutating' },
      { type: 'artifact', description: 'compact-cross-stack-base' },
      { type: 'journey', description: 'device-session.lifecycle' },
    ],
  },
  async ({ browser, context, page, deviceSessionPersona }) => {
    let exchangePhase: 'initial' | 'callback-failure' | 'retry' = 'initial';
    let initialExchangeCount = 0;
    let callbackFailureExchangeCount = 0;
    let retryExchangeCount = 0;
    let releaseCallbackExchange = (): void => undefined;
    const callbackExchangeRelease = new Promise<void>((resolve) => {
      releaseCallbackExchange = resolve;
    });
    let markCallbackExchangeStarted = (): void => undefined;
    const callbackExchangeStarted = new Promise<void>((resolve) => {
      markCallbackExchangeStarted = resolve;
    });

    await context.route(`${API_ORIGIN}/api/auth/sessions`, async (route) => {
      if (route.request().method() !== 'POST') {
        await route.continue();
        return;
      }
      if (exchangePhase === 'initial') {
        initialExchangeCount += 1;
        await route.fulfill({
          status: 503,
          contentType: 'application/json',
          body: JSON.stringify({ isSuccess: false, message: 'deterministic bootstrap failure' }),
        });
        return;
      }
      if (exchangePhase === 'callback-failure') {
        callbackFailureExchangeCount += 1;
        await route.fulfill({
          status: 503,
          contentType: 'application/json',
          body: JSON.stringify({ isSuccess: false, message: 'deterministic callback failure' }),
        });
        return;
      }

      retryExchangeCount += 1;
      markCallbackExchangeStarted();
      await callbackExchangeRelease;
      await route.continue();
    });
    await context.route(`${LOGTO_ORIGIN}/oidc/auth**`, (route) =>
      route.fulfill({ status: 204 }),
    );

    await page.goto(DEEP_RETURN_URL);
    await expect(page.getByRole('button', { name: 'تسجيل الدخول' })).toBeVisible();
    expect(initialExchangeCount).toBe(1);

    exchangePhase = 'callback-failure';
    const authorizationRequestPromise = page.waitForRequest((request) =>
      request.url().startsWith(`${LOGTO_ORIGIN}/oidc/auth`),
    );
    await page.getByRole('button', { name: 'تسجيل الدخول' }).click();
    await authorizationRequestPromise;

    await page.goto('/callback');
    const retrySignIn = page.getByRole('button', { name: 'إعادة المحاولة' });
    await expect(retrySignIn).toBeVisible();
    expect(callbackFailureExchangeCount).toBe(1);

    exchangePhase = 'retry';
    const retryAuthorizationRequestPromise = page.waitForRequest((request) =>
      request.url().startsWith(`${LOGTO_ORIGIN}/oidc/auth`),
    );
    await retrySignIn.click();
    await retryAuthorizationRequestPromise;

    const bootstrapResponsePromise = page.waitForResponse((response) =>
      response.url() === `${API_ORIGIN}/api/auth/sessions`
      && response.request().method() === 'POST'
      && response.status() === 200,
    );
    const cookieBackedMePromise = page.waitForResponse((response) =>
      response.url() === `${API_ORIGIN}/api/access/me`
      && response.request().method() === 'GET'
      && response.status() === 200,
    );

    await page.goto('/callback', { waitUntil: 'domcontentloaded' });
    await callbackExchangeStarted;
    await expect(page.getByText('جارٍ إكمال تسجيل الدخول…')).toBeVisible();
    releaseCallbackExchange();

    const bootstrapResponse = await bootstrapResponsePromise;
    expect(new URL(bootstrapResponse.url()).protocol).toBe('https:');
    const bootstrapHeaders = await bootstrapResponse.request().allHeaders();
    expect(Object.hasOwn(bootstrapHeaders, 'authorization')).toBe(true);
    expect(Object.hasOwn(bootstrapHeaders, IDENTITY_EVIDENCE_HEADER)).toBe(true);

    const cookieBackedMe = await cookieBackedMePromise;
    const meHeaders = await cookieBackedMe.request().allHeaders();
    expect(Object.hasOwn(meHeaders, 'authorization')).toBe(false);
    expect(await cookieBackedMe.json()).toMatchObject({
      isSuccess: true,
      data: {
        sub: deviceSessionPersona.subject,
        permissions: [],
      },
    });
    await expect(page).toHaveURL(`${new URL(DEEP_RETURN_URL, 'https://localhost:4200')}`);
    expect(retryExchangeCount).toBe(1);
    await expect(page.locator('html')).toHaveAttribute('dir', 'rtl');
    const signOut = page.getByRole('button', { name: 'تسجيل الخروج' });
    await expect(signOut).toBeVisible();

    const cookies = await context.cookies(`${API_ORIGIN}/api/access/me`);
    const sessionCookie = requireCookie(cookies, SESSION_COOKIE);
    const csrfCookie = requireCookie(cookies, CSRF_COOKIE);
    expect(sessionCookie.secure).toBe(true);
    expect(sessionCookie.httpOnly).toBe(true);
    expect(sessionCookie.sameSite).toBe('Lax');
    expect(sessionCookie.path).toBe('/api');
    expect(csrfCookie.secure).toBe(true);
    expect(csrfCookie.httpOnly).toBe(false);
    expect(csrfCookie.sameSite).toBe('Lax');
    expect(csrfCookie.path).toBe('/');

    const missingCsrfStatus = await page.evaluate(async (apiOrigin) => {
      const response = await fetch(`${apiOrigin}/api/auth/sessions/current`, {
        method: 'DELETE',
        credentials: 'include',
      });
      return response.status;
    }, API_ORIGIN);
    expect(missingCsrfStatus).toBe(403);

    const mismatchedCsrfStatus = await page.evaluate(async ({ apiOrigin, csrfHeader }) => {
      const response = await fetch(`${apiOrigin}/api/auth/sessions/current`, {
        method: 'DELETE',
        credentials: 'include',
        headers: { [csrfHeader]: 'mismatched-local-evidence' },
      });
      return response.status;
    }, { apiOrigin: API_ORIGIN, csrfHeader: CSRF_HEADER });
    expect(mismatchedCsrfStatus).toBe(403);

    const accessAfterCsrfDenials = await context.request.get(`${API_ORIGIN}/api/access/me`);
    expect(accessAfterCsrfDenials.status()).toBe(200);
    await accessAfterCsrfDenials.dispose();

    const revokeResponsePromise = page.waitForResponse((response) =>
      response.url() === `${API_ORIGIN}/api/auth/sessions/current`
      && response.request().method() === 'DELETE',
    );
    await signOut.click();
    const revokeResponse = await revokeResponsePromise;
    expect(revokeResponse.status()).toBe(204);
    const revokeHeaders = await revokeResponse.request().allHeaders();
    expect(Object.hasOwn(revokeHeaders, CSRF_HEADER.toLowerCase())).toBe(true);
    expect(Object.hasOwn(revokeHeaders, 'authorization')).toBe(false);

    const cookiesAfterLogout = await context.cookies(`${API_ORIGIN}/api/access/me`);
    expect(cookiesAfterLogout.some((cookie) => cookie.name === SESSION_COOKIE)).toBe(false);
    expect(cookiesAfterLogout.some((cookie) => cookie.name === CSRF_COOKIE)).toBe(false);

    const accessAfterLogout = await context.request.get(`${API_ORIGIN}/api/access/me`);
    expect(accessAfterLogout.status()).toBe(401);
    await accessAfterLogout.dispose();

    const replayContext = await browser.newContext({ ignoreHTTPSErrors: true });
    try {
      await replayContext.addCookies([{
        name: SESSION_COOKIE,
        value: sessionCookie.value,
        domain: 'localhost',
        path: '/api',
        httpOnly: true,
        secure: true,
        sameSite: 'Lax',
      }]);
      const replayedRevokedSession = await replayContext.request.get(
        `${API_ORIGIN}/api/access/me`,
      );
      expect(replayedRevokedSession.status()).toBe(401);
      await replayedRevokedSession.dispose();
    } finally {
      await replayContext.close();
    }
  },
);

function requireCookie(
  cookies: readonly BrowserCookie[],
  name: string,
): BrowserCookie {
  const cookie = cookies.find((candidate) => candidate.name === name);
  if (!cookie) {
    throw new Error(`Expected browser cookie ${name} to exist.`);
  }
  return cookie;
}
