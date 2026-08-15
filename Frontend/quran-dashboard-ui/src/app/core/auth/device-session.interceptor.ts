import { DOCUMENT } from '@angular/common';
import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';

import { isApiRequestUrl } from '../data-access/secure-url.interceptor';

const CSRF_COOKIE_NAME = 'XSRF-TOKEN';
const CSRF_HEADER_NAME = 'X-XSRF-TOKEN';
const SAFE_METHODS = new Set(['GET', 'HEAD', 'OPTIONS', 'TRACE']);

export const deviceSessionInterceptor: HttpInterceptorFn = (request, next) => {
  if (!isApiRequestUrl(request.url)) {
    return next(request);
  }

  const document = inject(DOCUMENT);
  const csrfToken = SAFE_METHODS.has(request.method.toUpperCase())
    ? null
    : readCookie(document.cookie, CSRF_COOKIE_NAME);
  const headers = csrfToken && !request.headers.has(CSRF_HEADER_NAME)
    ? request.headers.set(CSRF_HEADER_NAME, csrfToken)
    : request.headers;

  return next(request.clone({ headers, withCredentials: true }));
};

function readCookie(cookieHeader: string, name: string): string | null {
  const prefix = `${encodeURIComponent(name)}=`;
  const part = cookieHeader.split(';').map((value) => value.trim()).find((value) => value.startsWith(prefix));
  return part ? decodeURIComponent(part.slice(prefix.length)) : null;
}
