import { HttpInterceptorFn } from '@angular/common/http';

import { withDevApiLatency } from './dev-api-latency';

export const devLatencyInterceptor: HttpInterceptorFn = (req, next) =>
  withDevApiLatency(next(req));
