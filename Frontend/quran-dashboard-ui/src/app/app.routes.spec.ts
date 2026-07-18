import { describe, expect, it } from 'vitest';
import { Route, Routes } from '@angular/router';

import { routes } from './app.routes';

/**
 * Public-browse posture regression guard (Feature 033, Phase 2). The headline decision is
 * that NO route is protected — the Phase-1 blanket `authGuard` was removed and the reusable
 * `roleGuard` is attached to nothing until the first admin feature (decision record §G1/§I4).
 *
 * This is a pure config assertion — no router harness — mirroring the backend's
 * `FallbackPolicy`-is-null test: flatten the whole route tree (static `children` plus the
 * awaited lazy `loadChildren` arrays) and assert no route declares any activation guard.
 */
const GUARD_KEYS = ['canActivate', 'canActivateChild', 'canMatch'] as const;

/** Depth-first flatten of the route tree, resolving lazy `loadChildren` route arrays. */
async function flattenRoutes(routeList: Routes): Promise<Route[]> {
  const collected: Route[] = [];

  for (const route of routeList) {
    collected.push(route);

    if (route.children) {
      collected.push(...(await flattenRoutes(route.children)));
    }

    if (route.loadChildren) {
      const loaded = await route.loadChildren();
      if (Array.isArray(loaded)) {
        collected.push(...(await flattenRoutes(loaded)));
      }
    }
  }

  return collected;
}

describe('app routes (public-browse posture)', () => {
  it('declares no activation guard anywhere on the route tree', async () => {
    const allRoutes = await flattenRoutes(routes);

    const guardedPaths = allRoutes
      .filter((route) => GUARD_KEYS.some((key) => route[key] != null))
      .map((route) => route.path ?? '(pathless)');

    expect(guardedPaths).toEqual([]);
  });
});
