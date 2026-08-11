import type { Environment } from './environment.model';

export const environment: Environment = {
  production: false,
  apiBaseUrl: 'https://localhost:5015',

  logto: {
    endpoint: 'https://a8kvwi.logto.app',
    appId: 'osfceu3so056z6r762sjs',
    redirectUri: 'https://localhost:4200/callback',
    postLogoutRedirectUri: 'https://localhost:4200/',
    scope: 'email',
    resource: 'https://quranfullstack-production.up.railway.app',
  },
};
