import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { firstValueFrom } from 'rxjs';

import { CurrentUserStore } from './current-user.store';
import { PermissionCode } from './permission-codes';

// Reusable auth + permission guard factory (US5). Client-side gating for admin surfaces (e.g. permission
// administration): it hides the route when the caller lacks the effective permission. This hiding is
// DELIBERATELY non-authoritative — the backend policy (SystemOwner / permission policy) is the sole
// authority, so a hidden action invoked directly is still rejected server-side. Redirect target is the
// public home, mirroring `roleGuard`.
export function permissionGuard(required: PermissionCode): CanActivateFn {
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
    if (user?.status === 'active' && currentUserStore.hasPermission(required)) {
      return true;
    }
    return router.parseUrl('/');
  };
}
