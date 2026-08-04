import { Injectable } from '@angular/core';
import { PreloadingStrategy, Route } from '@angular/router';
import { from, Observable, switchMap } from 'rxjs';

const IDLE_TIMEOUT_MS = 3000;
const FALLBACK_DELAY_MS = 1500;

function whenIdle(): Promise<void> {
  return new Promise((resolve) => {
    if (typeof requestIdleCallback === 'function') {
      requestIdleCallback(() => resolve(), { timeout: IDLE_TIMEOUT_MS });
    } else {
      setTimeout(resolve, FALLBACK_DELAY_MS);
    }
  });
}

@Injectable({ providedIn: 'root' })
export class IdlePreloadStrategy implements PreloadingStrategy {
  preload(route: Route, load: () => Observable<unknown>): Observable<unknown> {
    return from(whenIdle()).pipe(switchMap(() => load()));
  }
}
