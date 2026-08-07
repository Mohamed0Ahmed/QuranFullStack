import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { firstValueFrom } from 'rxjs';

import { DASHBOARD_ROUTE_PATH } from '../navigation/route-paths';
import { AuthReturnLocationStore } from './auth-return-location.store';
import { CurrentUserStore } from './current-user.store';

export const ownerGuard: CanActivateFn = async (_route, state): Promise<boolean | UrlTree> => {
  const oidcSecurityService = inject(OidcSecurityService);
  const currentUserStore = inject(CurrentUserStore);
  const authReturnLocationStore = inject(AuthReturnLocationStore);
  const router = inject(Router);

  const { isAuthenticated } = await firstValueFrom(oidcSecurityService.isAuthenticated$);
  if (!isAuthenticated) {
    authReturnLocationStore.remember(state.url);
    oidcSecurityService.authorize();
    return false;
  }

  await currentUserStore.ensureLoaded();
  return currentUserStore.isActive() && currentUserStore.isOwner()
    ? true
    : router.parseUrl(DASHBOARD_ROUTE_PATH);
};
