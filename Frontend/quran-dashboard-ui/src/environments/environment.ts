import type { Environment } from './environment.model';

export const environment: Environment = {
  production: true,
  apiBaseUrl: 'https://quranfullstack-production.up.railway.app',
  devApiLatencyMs: 0,

  logto: {
    endpoint: 'https://a8kvwi.logto.app',
    appId: 'osfceu3so056z6r762sjs',
    redirectUri: 'https://manhag-qurany-ui.vercel.app/callback',
    postLogoutRedirectUri: 'https://manhag-qurany-ui.vercel.app/',
    scope: '',
    resource: 'https://quranfullstack-production.up.railway.app',
  },
};
