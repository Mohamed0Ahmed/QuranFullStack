import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { firstValueFrom } from 'rxjs';

import { RoleName } from './current-user.model';
import { CurrentUserStore } from './current-user.store';

/**
 * Reusable auth + role guard factory (Feature 033, Phase 2).
 *
 * INTENTIONALLY ATTACHED TO NOTHING in Phase 2, per the decision record (§G1, §I4): this
 * phase ships roles *infrastructure only* and the product is public-browse — no route is
 * protected. The Phase-1 blanket `authGuard` on the `/dashboard` tree was removed; this
 * factory is the hook a FUTURE admin feature will attach to its own admin routes. Until
 * then it lives here exercised only by its spec.
 *
 * Behavior once a future admin route wires `roleGuard('Owner')` (etc.):
 *  - not authenticated → kick off the Logto redirect (`authorize()`, login on demand) and
 *    block activation.
 *  - authenticated → ensure the current user is loaded (single cached
 *    `GET /api/access/me`), then activate iff the account is `active` AND holds the
 *    required role name; otherwise redirect to `/`.
 *
 * The redirect target is deliberately `/` (the public home). The dedicated
 * pending-activation destination for a logged-in-but-role-less user arrives with the
 * first admin feature that attaches this guard (decision record §B4, §I4).
 */
export function roleGuard(requiredRole: RoleName): CanActivateFn {
  return async (): Promise<boolean | UrlTree> => {
    const oidcSecurityService = inject(OidcSecurityService);
    const currentUserStore = inject(CurrentUserStore);
    const router = inject(Router);

    const { isAuthenticated } = await firstValueFrom(oidcSecurityService.isAuthenticated$);
    if (!isAuthenticated) {
      oidcSecurityService.authorize();
      return false;
    }

    await currentUserStore.ensureLoaded();
    const user = currentUserStore.currentUser();
    if (user?.status === 'active' && user.roleName === requiredRole) {
      return true;
    }
    return router.parseUrl('/');
  };
}
