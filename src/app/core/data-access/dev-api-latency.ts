import { Observable, delay } from 'rxjs';

import { environment } from '../../../environments/environment';

/** Dev-only artificial latency so loading states and transitions stay visible locally. */
export function devApiLatencyMs(): number {
  return environment.devApiLatencyMs ?? 0;
}

export function withDevApiLatency<T>(source: Observable<T>): Observable<T> {
  const ms = devApiLatencyMs();
  if (ms <= 0) {
    return source;
  }

  return source.pipe(delay(ms));
}
