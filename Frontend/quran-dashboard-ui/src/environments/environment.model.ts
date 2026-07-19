// Both `environment.ts` (production) and `environment.development.ts` must satisfy
// this, so field drift between the two files is caught at build time.
export interface Environment {
  production: boolean;
  apiBaseUrl: string;
  devApiLatencyMs: number;
  logto: {
    endpoint: string;
    appId: string;
    redirectUri: string;
    postLogoutRedirectUri: string;
    scope: string;
    resource: string;
  };
}
