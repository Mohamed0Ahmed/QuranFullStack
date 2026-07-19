import { describe, expect, it } from 'vitest';

import { assertProductionAuthConfigured } from './environment-guard';
import type { Environment } from './environment.model';

describe('assertProductionAuthConfigured', () => {
  function environment(overrides: Partial<Environment>): Environment {
    return {
      production: true,
      apiBaseUrl: 'https://example.test',
      devApiLatencyMs: 0,
      logto: {
        endpoint: 'https://tenant.logto.app',
        appId: 'app-id',
        redirectUri: 'https://example.test/callback',
        postLogoutRedirectUri: 'https://example.test',
        scope: '',
        resource: 'https://example.test/api',
      },
      ...overrides,
    };
  }

  it('throws naming the offending key(s) when production ships placeholder Logto config', () => {
    const env = environment({
      logto: {
        endpoint: 'https://REPLACE-WITH-YOUR-TENANT.logto.app',
        appId: 'REPLACE_WITH_LOGTO_SPA_APP_ID',
        redirectUri: 'https://example.test/callback',
        postLogoutRedirectUri: 'https://example.test',
        scope: '',
        resource: 'https://example.test/api',
      },
    });

    expect(() => assertProductionAuthConfigured(env)).toThrowError(/endpoint.*appId|appId.*endpoint/);
  });

  it('does not throw when production ships real Logto config', () => {
    const env = environment({});

    expect(() => assertProductionAuthConfigured(env)).not.toThrow();
  });

  it('does not throw in development even with placeholder Logto config', () => {
    const env = environment({
      production: false,
      logto: {
        endpoint: 'https://REPLACE-WITH-YOUR-TENANT.logto.app',
        appId: 'REPLACE_WITH_LOGTO_SPA_APP_ID',
        redirectUri: 'https://REPLACE-WITH-PRODUCTION-ORIGIN/callback',
        postLogoutRedirectUri: 'https://REPLACE-WITH-PRODUCTION-ORIGIN',
        scope: '',
        resource: 'https://REPLACE-WITH-YOUR-API-RESOURCE-INDICATOR',
      },
    });

    expect(() => assertProductionAuthConfigured(env)).not.toThrow();
  });
});
