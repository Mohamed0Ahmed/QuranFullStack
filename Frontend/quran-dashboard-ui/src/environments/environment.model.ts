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
