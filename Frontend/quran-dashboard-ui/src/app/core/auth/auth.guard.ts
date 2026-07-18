import { inject } from '@angular/core';
import { CanActivateFn } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { map, take } from 'rxjs';

/**
 * Guards the `/dashboard` subtree (Feature 033, Phase 1). Phase-1 behavior is LOCKED to
 * authentication only — no role / pending-status logic yet (that is Phase 2).
 *
 * `checkAuth` already ran via the app-initializer (`withAppInitializerAuthCheck()` in
 * `app.config.ts`), so `isAuthenticated$` has a settled current value. We read it once:
 * authenticated ⇒ activate; otherwise kick off the Logto redirect and block activation.
 *
 * Note: `canActivate` on the parent fires on entry/deep-link into the subtree but NOT on
 * child-to-child navigation once inside. Fine for Phase-1 auth-only; Phase 2's role/status
 * gating must account for this (e.g. `canActivateChild` or route-level checks).
 *
 * Functional guard idiom (first guard in the codebase; mirrors the functional
 * interceptors): an exported `const` that calls `inject()` inside the factory.
 */
export const authGuard: CanActivateFn = () => {
  const oidcSecurityService = inject(OidcSecurityService);

  return oidcSecurityService.isAuthenticated$.pipe(
    take(1),
    map(({ isAuthenticated }) => {
      if (isAuthenticated) {
        return true;
      }
      oidcSecurityService.authorize();
      return false;
    }),
  );
};
