import { execFileSync } from 'node:child_process';
import { createPrivateKey, sign } from 'node:crypto';
import { resolve } from 'node:path';
import type { APIRequestContext, APIResponse, BrowserContext, Page } from '@playwright/test';

import { ABWAB_PERMISSION_CODES } from '../../src/app/core/auth/permission-codes.generated';
import { environment } from '../../src/environments/environment.development';
import { instrumentAppContext, test as appTest } from './app-test';
import {
  E2E_OWNER_SUBJECT,
  E2E_TEST_ISSUER,
  e2eProfileEmail,
} from './logto';

const PRIVATE_KEY_PEM = `-----BEGIN PRIVATE KEY-----
MIIEvAIBADANBgkqhkiG9w0BAQEFAASCBKYwggSiAgEAAoIBAQC1oTCn9fPhTCBd
MiM4uxmDFTGpd0BbIVnnyi5LbPQ+m6SYIIRo6xoktG8O6aW7rnjMW8dbQdXBJbXP
D6Xrn467gHE2Rx5BQkemRDlrJ9n9p0vn9KWQ5PIKryGkTnzS/L1jH7aJyenntnMo
qDFFeqJzpBInqER6KXxoFiBe7uJs1TV7GYzqI649Dhh0Am33g8s754ZjKzsJnrU9
1uiiaXVlMt6Tj9Qizln/8JvpM6MyjaUO7OEs6JOGagpQD85OjxugUGZ735YSowk3
AqUr8EHTliWUrc0BoDUttXVLmzeEiL2N2qN4/54VdR8p05ByCb9wi7cfAOxNE8mL
qxmlKIDxAgMBAAECgf9GiYkLEBt+SNxjwQeA80OkU+eksu7Eq2UJM/UXi7IL/NH8
yWC95RbxRTyY7FpxAlEq8WxSYuCuSqYNm8LGvyMou+8F1H3eAqm12zTdMsVNr7iV
C9nHoakbA9tCvmD/I8ozY/Uh3s9WmZI6lj+0yXJonycPgxPsk3Ne3hCL20fDTLId
z73q9Gkoc/Wicks+mQuALVblzeR3A3aePvenIjMPKsjc6qk96YrmR5zVEui3wzXV
SJBhuc1TKB47qo5vaGzqfs6mcb2ZrgbeAqhMuhtiImyGrzzYDi9stOt9eGaFN3vs
BqTpFLpfO7ceaXHoObGBGm07e/pUcHVyFRtrZr0CgYEA9UtiTGh+AkKO1PFpRKa/
PzuqzS36kNnhiFAfEw/i20P7BPy6W293ghnD5HozGwz5lXVtjxk2A9x/Ze90Max5
AkICDNBg35fxH9ZLSfJOQnLEPx+jCj4ziB0eL2hRnMmGWUEFHsp1WCaP1DOmVqdq
uX467FJ8PBimafmnxRtsg/MCgYEAvY5+j/EDjiT+wf9RHNSAlTvWRjqDD2fQ4BZy
BhQYZZfN3h6VGshcAZUMXQX3k2Ot/sB502nSEi0Sbde+k5AnZerGUmI1M9t1ewKR
jNcz/b0qfETseFtEWdaYSH9815qFbfi3mdnv5X7Im8ZH9m3TJLp2X6+cRRRRl/j4
jlUytIsCgYA1nMhbUutXqxx0xl7vtlJOi8gFGGuPhU+Z74kbLXmz2uPeby58FZzV
PrRmF59b5jYWyJetkyEPLv5ZVUDcKoo4SB0Sl+jUde0pvDiwbNlWBKeH9G46KuYw
dczHJ9HOpu1KTL/FvxJutd7xzmgowGa/LCvMwyqMKAcXWo2ksx3AqQKBgQCr2MEY
kbFdbmCfU24fqu8fW+34IResIbwVS4P3ysQLZkI2upcGftoafRuGQeCe+GFHcQuB
BJBz9fSdLFgTwz5UgTFmgq4k4zJwYHW/r2HtCU+49WtD+cnPgGLyZtGxTN7mQfOB
QTjhX71Sq8GVwR8ITxr9yGDtp2wJUKdVshuT3QKBgQDrG/NnZEJLS5wfBK6jgJ3o
rcuGKySR7Chd+8Ra+pYsU+Mu9Agk/kxKe3t4QmQHjYblQ9Un3INMBMgYszZjq4BW
+XJVRmwxSg/i6QcfO0sIBjRRp8oJ5T5Byig14GwgQzYGDVJjXsslY6lg/OOl/9uK
D/87qVxDKhIVRcV/AxdZ3Q==
-----END PRIVATE KEY-----`;
const API_ORIGIN = environment.apiBaseUrl;
const OIDC_SESSION_KEY = `0-${environment.logto.appId}`;
const PREPARE_ACCESS_ADMIN = resolve(process.cwd(), 'e2e/prepare-access-admin.mjs');
const IDENTITY_EVIDENCE_HEADER = 'X-Interactive-Identity-Evidence';
const SETUP_REASON = 'Authenticated E2E fixture setup.';
const TEARDOWN_REASON = 'Authenticated E2E fixture teardown.';
const TEST_PERMISSION = ABWAB_PERMISSION_CODES.doors.create;
const PRIVATE_KEY = createPrivateKey(PRIVATE_KEY_PEM);

interface TokenPair {
  accessToken: string;
  idToken: string;
}

interface ApiEnvelope<T> {
  isSuccess: boolean;
  message: string | null;
  data: T | null;
}

interface CurrentUser {
  status: string;
  isOwner: boolean;
  permissions: string[];
}

interface AccessUserSummary {
  id: number;
  email: string;
  status: string;
  version: number;
}

interface AccessUserDetail extends AccessUserSummary {
  permissionCodes: string[];
}

interface AccessUserPermissions {
  status: string;
  version: number;
  permissionCodes: string[];
}

interface AccessUserPage {
  items: AccessUserSummary[];
}

interface AuthenticatedPersona {
  subject: string;
  permission: typeof TEST_PERMISSION;
}

interface DeviceSessionPersona {
  subject: string;
}

interface PermissionLifecyclePersona {
  subject: string;
  email: string;
  userId: number;
  permission: typeof TEST_PERMISSION;
  accessToken: string;
  ownerAccessToken: string;
  ownerPage: Page;
}

export const test = appTest.extend<{
  authenticatedPersona: AuthenticatedPersona;
  deviceSessionPersona: DeviceSessionPersona;
  permissionLifecyclePersona: PermissionLifecyclePersona;
}>({
  deviceSessionPersona: async ({ context }, use, testInfo) => {
    const subject = `e2e-device-session-${testInfo.parallelIndex}`;
    const tokens = mintTokenPair(subject);
    await installOidcSession(context, tokens);

    await use({ subject });
  },
  authenticatedPersona: async ({ context, request }, use, testInfo) => {
    const ownerTokens = mintTokenPair(E2E_OWNER_SUBJECT);
    const personaSubject = `e2e-permission-author-${testInfo.parallelIndex}`;
    const personaTokens = mintTokenPair(personaSubject);
    let ownerReady = false;

    try {
      const owner = await provisionCurrentUser(request, ownerTokens);
      if (owner.status !== 'active' || !owner.isOwner) {
        throw new Error('The E2E Owner was not provisioned as an active Owner.');
      }
      ownerReady = true;

      await provisionCurrentUser(request, personaTokens);
      await activatePersona(request, ownerTokens.accessToken, personaSubject);
      await installOidcSession(context, personaTokens);

      await use({ subject: personaSubject, permission: TEST_PERMISSION });
    } finally {
      if (ownerReady) {
        await disablePersona(request, ownerTokens.accessToken, personaSubject);
      }
    }
  },
  permissionLifecyclePersona: async ({ browser, context, request }, use, testInfo) => {
    const ownerTokens = mintTokenPair(E2E_OWNER_SUBJECT);
    const personaSubject = `e2e-permission-lifecycle-${testInfo.parallelIndex}`;
    const personaTokens = mintTokenPair(personaSubject);
    let ownerContext: BrowserContext | null = null;
    let finalizeOwnerInstrumentation: (() => Promise<void>) | null = null;

    try {
      prepareAccessAdministration();
      const owner = await provisionCurrentUser(request, ownerTokens);
      if (owner.status !== 'active' || !owner.isOwner) {
        throw new Error('The E2E Owner was not provisioned as an active Owner.');
      }

      await provisionCurrentUser(request, personaTokens);
      const persona = await activatePersonaWithoutPermissions(
        request,
        ownerTokens.accessToken,
        personaSubject,
      );
      await installOidcSession(context, personaTokens);

      ownerContext = await browser.newContext({ ignoreHTTPSErrors: true });
      finalizeOwnerInstrumentation = await instrumentAppContext(ownerContext, testInfo);
      await installOidcSession(ownerContext, ownerTokens);
      const ownerPage = await ownerContext.newPage();

      await use({
        subject: personaSubject,
        email: e2eProfileEmail(personaSubject),
        userId: persona.id,
        permission: TEST_PERMISSION,
        accessToken: personaTokens.accessToken,
        ownerAccessToken: ownerTokens.accessToken,
        ownerPage,
      });
    } finally {
      try {
        await finalizeOwnerInstrumentation?.();
      } finally {
        await ownerContext?.close();
      }
    }
  },
});

export { expect } from './app-test';

function mintTokenPair(subject: string): TokenPair {
  const email = e2eProfileEmail(subject);
  return {
    accessToken: mintToken(subject, environment.logto.resource),
    idToken: mintToken(subject, environment.logto.appId, {
      email,
      email_verified: true,
    }),
  };
}

function mintToken(
  subject: string,
  audience: string,
  additionalClaims: Readonly<Record<string, string | boolean>> = {},
): string {
  const issuedAt = Math.floor(Date.now() / 1000);
  const header = encodeJson({ alg: 'RS256', kid: 'quran-dashboard-e2e', typ: 'JWT' });
  const payload = encodeJson({
    iss: E2E_TEST_ISSUER,
    aud: audience,
    sub: subject,
    iat: issuedAt,
    exp: issuedAt + 3600,
    ...additionalClaims,
  });
  const signingInput = `${header}.${payload}`;
  const signature = sign('RSA-SHA256', Buffer.from(signingInput), PRIVATE_KEY).toString('base64url');
  return `${signingInput}.${signature}`;
}

function encodeJson(value: object): string {
  return Buffer.from(JSON.stringify(value)).toString('base64url');
}

function prepareAccessAdministration(): void {
  execFileSync(process.execPath, [PREPARE_ACCESS_ADMIN], {
    cwd: process.cwd(),
    stdio: 'inherit',
  });
}

function oidcSession(tokens: TokenPair): Record<string, unknown> {
  return {
    authzData: tokens.accessToken,
    authnResult: {
      access_token: tokens.accessToken,
      id_token: tokens.idToken,
      expires_in: 3600,
      token_type: 'Bearer',
    },
    access_token_expires_at: Date.now() + 3_600_000,
  };
}

async function installOidcSession(context: BrowserContext, tokens: TokenPair): Promise<void> {
  await context.addInitScript(
    ({ key, session }) => sessionStorage.setItem(key, JSON.stringify(session)),
    {
      key: OIDC_SESSION_KEY,
      session: oidcSession(tokens),
    },
  );
}

async function provisionCurrentUser(
  request: APIRequestContext,
  tokens: TokenPair,
): Promise<CurrentUser> {
  const response = await request.get(`${API_ORIGIN}/api/access/me`, {
    headers: {
      Authorization: `Bearer ${tokens.accessToken}`,
      [IDENTITY_EVIDENCE_HEADER]: tokens.idToken,
    },
  });
  return readData<CurrentUser>(response, 'provision current user');
}

async function activatePersona(
  request: APIRequestContext,
  ownerAccessToken: string,
  subject: string,
): Promise<void> {
  const user = await findUser(request, ownerAccessToken, subject);
  let detail: AccessUserDetail;

  if (user.status === 'pending') {
    detail = await postOwnerData<AccessUserDetail>(
      request,
      ownerAccessToken,
      `/api/access/users/${user.id}/accept`,
      {
        expectedVersion: user.version,
        permissionCodes: [TEST_PERMISSION],
        reason: SETUP_REASON,
      },
      'accept E2E persona',
    );
  } else {
    detail = user.status === 'disabled'
      ? await postOwnerData<AccessUserDetail>(
          request,
          ownerAccessToken,
          `/api/access/users/${user.id}/reactivate`,
          { expectedVersion: user.version, reason: SETUP_REASON },
          'reactivate E2E persona',
        )
      : await getOwnerData<AccessUserDetail>(
          request,
          ownerAccessToken,
          `/api/access/users/${user.id}`,
          'load E2E persona',
        );

    const permissions = await putOwnerData<AccessUserPermissions>(
      request,
      ownerAccessToken,
      `/api/access/users/${user.id}/permissions`,
      {
        expectedVersion: detail.version,
        permissionCodes: [TEST_PERMISSION],
        reason: SETUP_REASON,
      },
      'grant E2E permission',
    );
    detail = { ...detail, ...permissions };
  }

  if (detail.status !== 'active' || !detail.permissionCodes.includes(TEST_PERMISSION)) {
    throw new Error('The E2E persona did not receive its active direct permission grant.');
  }
}

async function activatePersonaWithoutPermissions(
  request: APIRequestContext,
  ownerAccessToken: string,
  subject: string,
): Promise<AccessUserDetail> {
  const user = await findUser(request, ownerAccessToken, subject);
  let detail: AccessUserDetail;

  if (user.status === 'pending') {
    detail = await postOwnerData<AccessUserDetail>(
      request,
      ownerAccessToken,
      `/api/access/users/${user.id}/accept`,
      {
        expectedVersion: user.version,
        permissionCodes: [],
        reason: SETUP_REASON,
      },
      'accept least-privilege E2E persona',
    );
  } else {
    detail = user.status === 'disabled'
      ? await postOwnerData<AccessUserDetail>(
          request,
          ownerAccessToken,
          `/api/access/users/${user.id}/reactivate`,
          { expectedVersion: user.version, reason: SETUP_REASON },
          'reactivate least-privilege E2E persona',
        )
      : await getOwnerData<AccessUserDetail>(
          request,
          ownerAccessToken,
          `/api/access/users/${user.id}`,
          'load least-privilege E2E persona',
        );

    if (detail.permissionCodes.length > 0) {
      const permissions = await putOwnerData<AccessUserPermissions>(
        request,
        ownerAccessToken,
        `/api/access/users/${detail.id}/permissions`,
        {
          expectedVersion: detail.version,
          permissionCodes: [],
          reason: SETUP_REASON,
        },
        'clear least-privilege E2E persona permissions',
      );
      detail = { ...detail, ...permissions };
    }
  }

  if (detail.status !== 'active' || detail.permissionCodes.length !== 0) {
    throw new Error('The least-privilege E2E persona was not active without direct permissions.');
  }

  return detail;
}

async function disablePersona(
  request: APIRequestContext,
  ownerAccessToken: string,
  subject: string,
): Promise<void> {
  const summary = await findUser(request, ownerAccessToken, subject);
  let detail = await getOwnerData<AccessUserDetail>(
    request,
    ownerAccessToken,
    `/api/access/users/${summary.id}`,
    'load E2E persona for teardown',
  );

  if (detail.status === 'active' && detail.permissionCodes.length > 0) {
    const permissions = await putOwnerData<AccessUserPermissions>(
      request,
      ownerAccessToken,
      `/api/access/users/${detail.id}/permissions`,
      {
        expectedVersion: detail.version,
        permissionCodes: [],
        reason: TEARDOWN_REASON,
      },
      'remove E2E permissions',
    );
    detail = { ...detail, ...permissions };
  }

  if (detail.status !== 'disabled') {
    await postOwnerData<AccessUserDetail>(
      request,
      ownerAccessToken,
      `/api/access/users/${detail.id}/disable`,
      { expectedVersion: detail.version, reason: TEARDOWN_REASON },
      'disable E2E persona',
    );
  }
}

async function findUser(
  request: APIRequestContext,
  ownerAccessToken: string,
  subject: string,
): Promise<AccessUserSummary> {
  const email = e2eProfileEmail(subject);
  const page = await getOwnerData<AccessUserPage>(
    request,
    ownerAccessToken,
    `/api/access/users?search=${encodeURIComponent(email)}&page=1&pageSize=25`,
    'find E2E persona',
  );
  const matches = page.items.filter((user) => user.email === email);
  if (matches.length !== 1) {
    throw new Error(`Expected one E2E persona for ${email}, found ${matches.length}.`);
  }
  return matches[0];
}

function getOwnerData<T>(
  request: APIRequestContext,
  accessToken: string,
  path: string,
  operation: string,
): Promise<T> {
  return request
    .get(`${API_ORIGIN}${path}`, { headers: ownerHeaders(accessToken) })
    .then((response) => readData<T>(response, operation));
}

function postOwnerData<T>(
  request: APIRequestContext,
  accessToken: string,
  path: string,
  data: object,
  operation: string,
): Promise<T> {
  return request
    .post(`${API_ORIGIN}${path}`, { headers: ownerHeaders(accessToken), data })
    .then((response) => readData<T>(response, operation));
}

function putOwnerData<T>(
  request: APIRequestContext,
  accessToken: string,
  path: string,
  data: object,
  operation: string,
): Promise<T> {
  return request
    .put(`${API_ORIGIN}${path}`, { headers: ownerHeaders(accessToken), data })
    .then((response) => readData<T>(response, operation));
}

function ownerHeaders(accessToken: string): Record<string, string> {
  return { Authorization: `Bearer ${accessToken}` };
}

async function readData<T>(response: APIResponse, operation: string): Promise<T> {
  const status = response.status();
  const body = await response.text();
  await response.dispose();
  if (status < 200 || status >= 300) {
    throw new Error(`${operation} failed with HTTP ${status}: ${body}`);
  }

  const envelope = JSON.parse(body) as ApiEnvelope<T>;
  if (!envelope.isSuccess || envelope.data === null) {
    throw new Error(`${operation} returned an unsuccessful API envelope: ${envelope.message ?? body}`);
  }
  return envelope.data;
}
