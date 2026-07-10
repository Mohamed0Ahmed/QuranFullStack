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

export const secureUrlInterceptor: HttpInterceptorFn = (req, next) => {
  if (!isAllowedApiUrl(req.url)) {
    return throwError(() => new SecureUrlBlockedError(req.url));
  }

  return next(req);
};
