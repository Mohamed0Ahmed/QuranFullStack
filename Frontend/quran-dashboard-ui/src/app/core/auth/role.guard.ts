import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { firstValueFrom } from 'rxjs';

import { RoleName } from './current-user.model';
import { CurrentUserStore } from './current-user.store';

// Reusable auth + role guard factory (Feature 033, Phase 2). INTENTIONALLY ATTACHED TO NOTHING
// (decision record §G1/§I4) — this phase ships roles infrastructure only and the product is
// public-browse; the Phase-1 blanket `authGuard` was removed. Not dead code: it is the hook a
// FUTURE admin feature attaches to its own admin routes. The redirect target is deliberately `/`
// (public home); the dedicated pending-activation destination for a logged-in but role-less user
// arrives with that first admin feature (§B4/§I4).
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
