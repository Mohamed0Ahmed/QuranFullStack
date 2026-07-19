import { EnvironmentProviders } from '@angular/core';
import { provideAuth } from 'angular-auth-oidc-client';

/**
 * Test-only auth wiring (Feature 033). Any spec that mounts the app shell / top navbar
 * transitively injects `OidcSecurityService`, whose dependency graph needs the config
 * loader that `provideAuth()` registers. This helper provides the REAL
 * angular-auth-oidc-client services with a static, inert config and — crucially — WITHOUT
 * `withAppInitializerAuthCheck()`, so no auth-check / discovery network traffic ever
 * fires. The default auth state is unauthenticated, which is what these shell/overlay
 * specs expect.
 */
export function provideAuthTesting(): EnvironmentProviders {
  return provideAuth({
    config: {
      authority: 'https://auth.test',
      redirectUrl: 'https://app.test/callback',
      clientId: 'test-client',
      scope: 'openid',
      responseType: 'code',
    },
  });
}
