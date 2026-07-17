import { ApplicationConfig, provideBrowserGlobalErrorListeners, provideZoneChangeDetection } from '@angular/core';
import { provideRouter, TitleStrategy } from '@angular/router';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';

import { routes } from './app.routes';
import { AppTitleStrategy } from './core/navigation/app-title.strategy';
import { devLatencyInterceptor } from './core/data-access/dev-latency.interceptor';
import { secureUrlInterceptor } from './core/data-access/secure-url.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(withFetch(), withInterceptors([secureUrlInterceptor, devLatencyInterceptor])),
    { provide: TitleStrategy, useClass: AppTitleStrategy },
  ]
};
