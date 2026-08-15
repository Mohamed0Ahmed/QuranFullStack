import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';

import { DASHBOARD_ROUTE_PATH } from '../navigation/route-paths';
import { AuthReturnLocationStore } from './auth-return-location.store';
import { CurrentUserStore } from './current-user.store';

export const ownerGuard: CanActivateFn = async (_route, state): Promise<boolean | UrlTree> => {
  const oidcSecurityService = inject(OidcSecurityService);
  const currentUserStore = inject(CurrentUserStore);
  const authReturnLocationStore = inject(AuthReturnLocationStore);
  const router = inject(Router);

  await currentUserStore.ensureLoaded();
  if (!currentUserStore.isAuthenticated()) {
    authReturnLocationStore.remember(state.url);
    oidcSecurityService.authorize();
    return false;
  }

  return currentUserStore.isActive() && currentUserStore.isOwner()
    ? true
    : router.parseUrl(DASHBOARD_ROUTE_PATH);
};
