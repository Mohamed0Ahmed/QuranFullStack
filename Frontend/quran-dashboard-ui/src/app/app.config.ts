import {
  ApplicationConfig,
  provideBrowserGlobalErrorListeners,
  provideZoneChangeDetection,
} from '@angular/core';
import { provideRouter, TitleStrategy, withPreloading } from '@angular/router';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import {
  authInterceptor,
  provideAuth,
  withAppInitializerAuthCheck,
} from 'angular-auth-oidc-client';
import { buildAngularAuthConfig } from '@logto/angular';

import { routes } from './app.routes';
import { environment } from '../environments/environment';
import { AppTitleStrategy } from './core/navigation/app-title.strategy';
import { IdlePreloadStrategy } from './core/navigation/idle-preload.strategy';
import { devLatencyInterceptor } from './core/data-access/dev-latency.interceptor';
import { secureUrlInterceptor } from './core/data-access/secure-url.interceptor';

const { endpoint, appId, redirectUri, postLogoutRedirectUri, scope, resource } = environment.logto;

export const oidcConfig = {
  ...buildAngularAuthConfig({
    endpoint,
    appId,
    redirectUri,
    postLogoutRedirectUri,
    resource,
    ...(scope ? { scopes: scope.split(/\s+/).filter(Boolean) } : {}),
  }),
  secureRoutes: [environment.apiBaseUrl],
  triggerAuthorizationResultEvent: true,
};

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes, withPreloading(IdlePreloadStrategy)),
    provideHttpClient(
      withFetch(),
      withInterceptors([secureUrlInterceptor, authInterceptor(), devLatencyInterceptor]),
    ),
    { provide: TitleStrategy, useClass: AppTitleStrategy },
    provideAuth({ config: oidcConfig }, withAppInitializerAuthCheck()),
  ],
};
