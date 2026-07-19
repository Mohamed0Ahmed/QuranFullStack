import { EnvironmentProviders } from '@angular/core';
import { provideAuth } from 'angular-auth-oidc-client';

// Test-only auth wiring (Feature 033). Specs that mount the app shell / top navbar transitively
// inject `OidcSecurityService`, which needs the config loader `provideAuth()` registers. Provides
// the REAL library services with a static, inert config and — crucially — WITHOUT
// `withAppInitializerAuthCheck()`, so no auth-check / discovery network traffic ever fires. The
// default state is unauthenticated, which these shell/overlay specs expect.
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
