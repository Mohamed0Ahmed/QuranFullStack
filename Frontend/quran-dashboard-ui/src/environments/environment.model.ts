export interface Environment {
  production: boolean;
  apiBaseUrl: string;
  logto: {
    endpoint: string;
    appId: string;
    redirectUri: string;
    postLogoutRedirectUri: string;
    scope: string;
    resource: string;
  };
}
