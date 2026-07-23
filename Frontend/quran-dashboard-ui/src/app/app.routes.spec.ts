import { describe, expect, it } from 'vitest';
import { Route, Routes } from '@angular/router';

import { routes } from './app.routes';
import { MUSHAF_ROUTES } from './features/mushaf/mushaf.routes';
import { WORDS_ROUTES } from './features/words/words.routes';
import { PERMISSIONS_ROUTES } from './features/permissions/permissions.routes';
import { ABWAB_ROUTES } from './features/abwab/abwab.routes';

const GUARD_KEYS = ['canActivate', 'canActivateChild', 'canMatch'] as const;

const STATIC_LAZY_ROUTE_ARRAYS: Readonly<Record<string, Routes>> = {
  mushaf: MUSHAF_ROUTES,
  words: WORDS_ROUTES,
  permissions: PERMISSIONS_ROUTES,
  gates: ABWAB_ROUTES,
};

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
  it('guards ONLY the owner-only permission-administration route', () => {
    const allRoutes = flattenRoutes(routes);

    const guarded = allRoutes.filter((route) => GUARD_KEYS.some((key) => route[key] != null));

    expect(guarded).toHaveLength(1);
    expect(guarded[0].canActivate).toBeDefined();
  });
});
