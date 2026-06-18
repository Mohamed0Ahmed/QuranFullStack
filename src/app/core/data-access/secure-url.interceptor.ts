import { HttpInterceptorFn } from '@angular/common/http';
import { throwError } from 'rxjs';

import { environment } from '../../../environments/environment';

/** Thrown when a dev-time HTTP request is not under the configured HTTPS API base URL. */
export class SecureUrlBlockedError extends Error {
  constructor(url: string) {
    super(
      `تم حظر الطلب: يجب أن تستهدف جميع طلبات البيانات ${environment.apiBaseUrl} عبر HTTPS فقط. (الرابط: ${url})`
    );
    this.name = 'SecureUrlBlockedError';
  }
}

/**
 * Returns true when `url` targets the same origin as `apiBaseUrl` (and is a valid absolute URL).
 * When `apiBaseUrl` is empty (production same-origin), all URLs are allowed — the guard is a
 * local-dev convenience only, not a production security control.
 */
export function isUrlUnderApiBase(url: string, apiBaseUrl: string): boolean {
  if (!apiBaseUrl) {
    return true;
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

/**
 * Dev-time guard: allow only requests whose URL shares `environment.apiBaseUrl`'s origin
 * (HTTPS in local development). Never rewrites blocked URLs to HTTP.
 */
export const secureUrlInterceptor: HttpInterceptorFn = (req, next) => {
  if (!isAllowedApiUrl(req.url)) {
    return throwError(() => new SecureUrlBlockedError(req.url));
  }

  return next(req);
};
