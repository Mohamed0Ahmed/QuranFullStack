import { HttpInterceptorFn } from '@angular/common/http';
import { throwError } from 'rxjs';

import { environment } from '../../../environments/environment';

export class SecureUrlBlockedError extends Error {
  constructor(url: string) {
    super(
      `تم حظر الطلب: يجب أن تستهدف جميع طلبات البيانات ${environment.apiBaseUrl} عبر HTTPS فقط. (الرابط: ${url})`
    );
    this.name = 'SecureUrlBlockedError';
  }
}

export function isUrlUnderApiBase(url: string, apiBaseUrl: string): boolean {
  // Fail closed: empty apiBaseUrl is a misconfiguration; allowing every origin would silently defeat this guard.
  if (!apiBaseUrl) {
    return false;
  }

  let requestUrl: URL;
  let baseUrl: URL;

  try {
    requestUrl = new URL(url);
    baseUrl = new URL(apiBaseUrl);
  } catch {
    return false;
  }

  return requestUrl.origin === baseUrl.origin;
}

function isAllowedApiUrl(url: string): boolean {
  return isUrlUnderApiBase(url, environment.apiBaseUrl);
}

// The IdP origin is exempt from the block (OIDC uses HttpClient); the bearer token still attaches only to apiBaseUrl.
function isIdentityProviderUrl(url: string): boolean {
  return isUrlUnderApiBase(url, environment.logto.endpoint);
}

export const secureUrlInterceptor: HttpInterceptorFn = (req, next) => {
  if (!isAllowedApiUrl(req.url) && !isIdentityProviderUrl(req.url)) {
    return throwError(() => new SecureUrlBlockedError(req.url));
  }

  return next(req);
};
