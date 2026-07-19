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
  // Fail closed: an empty apiBaseUrl is a misconfiguration, not "same origin for
  // everything". Both environments always set apiBaseUrl, so allowing every origin
  // here would silently defeat this guard on the most likely misconfiguration.
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

// angular-auth-oidc-client performs OIDC discovery/token calls through Angular's HttpClient,
// so the identity provider's origin must pass this guard. It is exempt from the block only —
// the Bearer token still attaches exclusively to `apiBaseUrl` (authInterceptor `secureRoutes`).
function isIdentityProviderUrl(url: string): boolean {
  return isUrlUnderApiBase(url, environment.logto.endpoint);
}

export const secureUrlInterceptor: HttpInterceptorFn = (req, next) => {
  if (!isAllowedApiUrl(req.url) && !isIdentityProviderUrl(req.url)) {
    return throwError(() => new SecureUrlBlockedError(req.url));
  }

  return next(req);
};
