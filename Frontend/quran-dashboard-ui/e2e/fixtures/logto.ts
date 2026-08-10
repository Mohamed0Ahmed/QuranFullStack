import type { BrowserContext } from '@playwright/test';

import { environment } from '../../src/environments/environment.development';

// src/app/app.config.ts installs withAppInitializerAuthCheck(), so the app performs OIDC discovery
// against this origin before it renders — every page load, even though no flow here signs in.
// Serving a static discovery document locally keeps the boot path deterministic and offline.
export const LOGTO_ORIGIN = new URL(environment.logto.endpoint).origin;
export const E2E_TEST_ISSUER = 'https://e2e.quran-dashboard.test/oidc';
export const E2E_OWNER_SUBJECT = 'e2e-owner';
export const E2E_TEST_JWKS = {
  keys: [
    {
      kty: 'RSA',
      n: 'taEwp_Xz4UwgXTIjOLsZgxUxqXdAWyFZ58ouS2z0PpukmCCEaOsaJLRvDumlu654zFvHW0HVwSW1zw-l65-Ou4BxNkceQUJHpkQ5ayfZ_adL5_SlkOTyCq8hpE580vy9Yx-2icnp57ZzKKgxRXqic6QSJ6hEeil8aBYgXu7ibNU1exmM6iOuPQ4YdAJt94PLO-eGYys7CZ61Pdbooml1ZTLek4_UIs5Z__Cb6TOjMo2lDuzhLOiThmoKUA_OTo8boFBme9-WEqMJNwKlK_BB05YllK3NAaA1LbV1S5s3hIi9jdqjeP-eFXUfKdOQcgm_cIu3HwDsTRPJi6sZpSiA8Q',
      e: 'AQAB',
      kid: 'quran-dashboard-e2e',
      use: 'sig',
      alg: 'RS256',
    },
  ],
} as const;

export function e2eProfileEmail(subject: string): string {
  return `${subject}@example.test`;
}

const DISCOVERY_DOCUMENT = {
  issuer: `${LOGTO_ORIGIN}/oidc`,
  authorization_endpoint: `${LOGTO_ORIGIN}/oidc/auth`,
  token_endpoint: `${LOGTO_ORIGIN}/oidc/token`,
  userinfo_endpoint: `${LOGTO_ORIGIN}/oidc/me`,
  jwks_uri: `${LOGTO_ORIGIN}/oidc/jwks`,
  end_session_endpoint: `${LOGTO_ORIGIN}/oidc/session/end`,
  response_types_supported: ['code'],
  subject_types_supported: ['public'],
  id_token_signing_alg_values_supported: ['RS256'],
  scopes_supported: ['openid', 'offline_access', 'profile', 'email'],
  token_endpoint_auth_methods_supported: ['none'],
  code_challenge_methods_supported: ['S256'],
  grant_types_supported: ['authorization_code', 'refresh_token'],
};

const JWKS_PATH = new URL(DISCOVERY_DOCUMENT.jwks_uri).pathname;

// Routed on the context rather than a page so pages opened mid-test (popups, context.newPage())
// are stubbed too instead of reaching the real tenant.
export async function stubLogto(context: BrowserContext): Promise<void> {
  await context.route(`${LOGTO_ORIGIN}/**`, async (route) => {
    const path = new URL(route.request().url()).pathname;

    if (path.endsWith('/.well-known/openid-configuration')) {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(DISCOVERY_DOCUMENT),
      });
      return;
    }

    if (path === JWKS_PATH) {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(E2E_TEST_JWKS),
      });
      return;
    }

    await route.fulfill({
      status: 404,
      contentType: 'application/json',
      body: JSON.stringify({ error: `unstubbed Logto path: ${path}` }),
    });
  });
}
