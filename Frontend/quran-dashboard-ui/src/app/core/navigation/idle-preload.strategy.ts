import { Injectable } from '@angular/core';
import { PreloadingStrategy, Route } from '@angular/router';
import { from, Observable, switchMap } from 'rxjs';

// The timeout bounds how long a busy main thread can starve preloading; the fallback covers
// browsers without requestIdleCallback (Safari).
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

// Preloads every lazy route (58 chunks, ~1.03 MB raw / ~252 kB transfer — trivial for a
// desktop admin dashboard, so no exclusions), but never during bootstrap: Angular starts
// preloading only after the initial navigation completes, and each chunk additionally waits
// for an idle period so preloading cannot compete with the landing page's own work.
@Injectable({ providedIn: 'root' })
export class IdlePreloadStrategy implements PreloadingStrategy {
  preload(route: Route, load: () => Observable<unknown>): Observable<unknown> {
    return from(whenIdle()).pipe(switchMap(() => load()));
  }
}
