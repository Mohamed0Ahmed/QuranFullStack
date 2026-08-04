import { EnvironmentProviders } from '@angular/core';
import { provideAuth } from 'angular-auth-oidc-client';

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
