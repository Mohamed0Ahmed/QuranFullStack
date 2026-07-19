import { describe, expect, it } from 'vitest';
import { Route, Routes } from '@angular/router';

import { routes } from './app.routes';
import { MUSHAF_ROUTES } from './features/mushaf/mushaf.routes';
import { WORDS_ROUTES } from './features/words/words.routes';

/**
 * Public-browse posture regression guard (Feature 033, Phase 2). The headline decision is
 * that NO route is protected — the Phase-1 blanket `authGuard` was removed and the reusable
 * `roleGuard` is attached to nothing until the first admin feature (decision record §G1/§I4).
 *
 * This is a pure config assertion — no router harness.
 *
 * B1 (confirmed): do NOT resolve `route.loadChildren()` here. Executing the lazy loader inside
 * a running test — even only for the `mushaf`/`words` route-config modules, never touching
 * their lazy `loadComponent` page chunks — corrupts the shared Vitest fork's module graph for
 * specs that run later in the same worker: paired with
 * `selected-ayah-section.component.spec.ts`, it reproduces
 * "Cannot read properties of undefined (reading 'tafsir')" (that component's
 * `AYAH_STUDY_TAB_LABELS` — and other `mushaf.models.ts` consts — read back as undefined).
 * Statically importing the same `*.routes.ts` modules up front — the ROUTES array only, never
 * a `loadComponent` page chunk — avoids whatever runtime module-graph invalidation the
 * *dynamic* `import()` call triggers when it fires during test execution: swapping the dynamic
 * call for this static import, with nothing else changed, turns the previously-failing pairing
 * green. `mushaf.routes.ts` / `words.routes.ts` only use `loadComponent` (no further
 * `loadChildren`), so importing them cannot force-evaluate any page/component module.
 */
const GUARD_KEYS = ['canActivate', 'canActivateChild', 'canMatch'] as const;

/**
 * Registered lazy child route arrays, keyed by the parent route's static `path`. Every route in
 * `routes` that declares `loadChildren` must have an entry here (enforced below) so a future
 * lazy feature can't silently drop out of guard coverage.
 */
const STATIC_LAZY_ROUTE_ARRAYS: Readonly<Record<string, Routes>> = {
  mushaf: MUSHAF_ROUTES,
  words: WORDS_ROUTES,
};

/**
 * Depth-first flatten of the route tree: static `children` plus the statically-imported arrays
 * standing in for lazy `loadChildren` (see the comment above for why `loadChildren` itself is
 * never invoked here).
 */
function flattenRoutes(routeList: Routes): Route[] {
  const collected: Route[] = [];

  for (const route of routeList) {
    collected.push(route);

    if (route.children) {
      collected.push(...flattenRoutes(route.children));
    }

    if (route.loadChildren) {
      const staticArray = route.path != null ? STATIC_LAZY_ROUTE_ARRAYS[route.path] : undefined;
      if (!staticArray) {
        throw new Error(
          `Route "${route.path}" declares loadChildren but has no statically-imported array ` +
            'registered in STATIC_LAZY_ROUTE_ARRAYS — register it so this guard assertion keeps ' +
            'covering it (see the file header comment for why loadChildren() is never executed).',
        );
      }
      collected.push(...flattenRoutes(staticArray));
    }
  }

  return collected;
}

describe('app routes (public-browse posture)', () => {
  it('declares no activation guard anywhere on the route tree', () => {
    const allRoutes = flattenRoutes(routes);

    const guardedPaths = allRoutes
      .filter((route) => GUARD_KEYS.some((key) => route[key] != null))
      .map((route) => route.path ?? '(pathless)');

    expect(guardedPaths).toEqual([]);
  });
});
