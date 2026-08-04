import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { firstValueFrom } from 'rxjs';

import { RoleName } from './current-user.model';
import { CurrentUserStore } from './current-user.store';

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
