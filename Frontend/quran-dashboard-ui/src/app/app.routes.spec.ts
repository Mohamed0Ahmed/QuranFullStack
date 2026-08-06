import { describe, expect, it } from 'vitest';
import { Route, Routes } from '@angular/router';

import { routes } from './app.routes';
import { ownerGuard } from './core/auth/owner.guard';
import { MUSHAF_ROUTES } from './features/mushaf/mushaf.routes';
import { WORDS_ROUTES } from './features/words/words.routes';
import { ABWAB_ROUTES } from './features/abwab/abwab.routes';
import { ACCESS_ADMIN_ROUTES } from './features/access-admin/access-admin.routes';

// Public-browse posture regression guard (Feature 033, Phase 2): asserts NO route carries an
// activation guard. Pure config assertion — no router harness.
//
// B1 (confirmed): do NOT resolve `route.loadChildren()` here. Executing the lazy loader during a
// test corrupts the shared Vitest fork's module graph for specs that run later in the same worker
// — paired with `selected-ayah-section.component.spec.ts` it reproduces "Cannot read properties of
// undefined (reading 'tafsir')" (that component's `mushaf.models.ts` consts read back as undefined).
// So we statically import the ROUTES arrays (never a `loadComponent` page chunk) as stand-ins;
// swapping the dynamic `import()` for these static imports, nothing else changed, turns the
// previously-failing pairing green.
const GUARD_KEYS = ['canActivate', 'canActivateChild', 'canMatch'] as const;

// Lazy child route arrays keyed by parent `path`. Every `loadChildren` route must have an entry
// here (enforced in flattenRoutes) so a future lazy feature can't drop out of guard coverage.
const STATIC_LAZY_ROUTE_ARRAYS: Readonly<Record<string, Routes>> = {
  mushaf: MUSHAF_ROUTES,
  words: WORDS_ROUTES,
  abwab: ABWAB_ROUTES,
  'settings/access': ACCESS_ADMIN_ROUTES,
};

// Flattens the route tree, substituting the statically-imported arrays for lazy `loadChildren`
// (see the header for why `loadChildren` itself is never invoked here).
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
  it('attaches the Owner guard only to the security-administration route', () => {
    const allRoutes = flattenRoutes(routes);

    const guardedPaths = allRoutes
      .filter((route) => GUARD_KEYS.some((key) => route[key] != null))
      .map((route) => route.path ?? '(pathless)');

    expect(guardedPaths).toEqual(['settings/access']);
    expect(allRoutes.find((route) => route.path === 'settings/access')?.canActivate).toEqual([
      ownerGuard,
    ]);
    expect(allRoutes.find((route) => route.path === 'settings/access')?.loadChildren).toBeTypeOf('function');
  });
});
