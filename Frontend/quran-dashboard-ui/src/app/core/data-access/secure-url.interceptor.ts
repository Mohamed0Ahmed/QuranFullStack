import { HttpInterceptorFn } from '@angular/common/http';
import { throwError } from 'rxjs';

import { environment } from '../../../environments/environment';

export class SecureUrlBlockedError extends Error {
  constructor(url: string) {
    const target = environment.apiBaseUrl || 'نطاق التطبيق الحالي';
    super(
      `تم حظر الطلب: يجب أن تستهدف جميع طلبات البيانات ${target} عبر HTTPS فقط. (الرابط: ${url})`
    );
    this.name = 'SecureUrlBlockedError';
  }
}

export function isUrlUnderApiBase(url: string, apiBaseUrl: string): boolean {
  let requestUrl: URL;

  try {
    const browserOrigin = globalThis.location?.origin ?? 'https://localhost';
    requestUrl = new URL(url, browserOrigin);
    const baseUrl = apiBaseUrl ? new URL(apiBaseUrl) : new URL(browserOrigin);
    return requestUrl.origin === baseUrl.origin && requestUrl.pathname.startsWith('/api/');
  } catch {
    return false;
  }
}

function isUrlUnderOrigin(url: string, baseUrl: string): boolean {
  try {
    return new URL(url).origin === new URL(baseUrl).origin;
  } catch {
    return false;
  }
}

export function isApiRequestUrl(url: string): boolean {
  return isUrlUnderApiBase(url, environment.apiBaseUrl);
}

function isIdentityProviderUrl(url: string): boolean {
  return isUrlUnderOrigin(url, environment.logto.endpoint);
}

export const secureUrlInterceptor: HttpInterceptorFn = (req, next) => {
  if (!isApiRequestUrl(req.url) && !isIdentityProviderUrl(req.url)) {
    return throwError(() => new SecureUrlBlockedError(req.url));
  }

  return next(req);
};
