import type { Environment } from './environment.model';

export const environment: Environment = {
  production: true,
  apiBaseUrl: 'https://quranfullstack-production.up.railway.app',
  devApiLatencyMs: 0,

  logto: {
    endpoint: 'https://REPLACE-WITH-YOUR-TENANT.logto.app',
    appId: 'REPLACE_WITH_LOGTO_SPA_APP_ID',
    redirectUri: 'https://REPLACE-WITH-PRODUCTION-ORIGIN/callback',
    postLogoutRedirectUri: 'https://REPLACE-WITH-PRODUCTION-ORIGIN',
    scope: '',
    resource: 'https://REPLACE-WITH-YOUR-API-RESOURCE-INDICATOR',
  },
};
