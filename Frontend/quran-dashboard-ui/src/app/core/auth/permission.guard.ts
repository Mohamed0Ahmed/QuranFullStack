import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { firstValueFrom } from 'rxjs';

import { CurrentUserStore } from './current-user.store';
import { PermissionCode } from './permission-codes';

// Non-authoritative hiding only: the backend policy is the sole authority, so a hidden action invoked
// directly is still rejected server-side.
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
