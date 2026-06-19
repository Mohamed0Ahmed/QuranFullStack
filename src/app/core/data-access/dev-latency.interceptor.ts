import { HttpInterceptorFn } from '@angular/common/http';

import { withDevApiLatency } from './dev-api-latency';

/**
 * Dev-only response delay so local API calls do not complete instantly.
 * Disabled in production and when `environment.devApiLatencyMs` is 0.
 */
export const devLatencyInterceptor: HttpInterceptorFn = (req, next) =>
  withDevApiLatency(next(req));
