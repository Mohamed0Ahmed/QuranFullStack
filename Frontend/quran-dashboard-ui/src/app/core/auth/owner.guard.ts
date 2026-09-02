import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';

import { DASHBOARD_ROUTE_PATH } from '../navigation/route-paths';
import { AuthSessionStore } from './auth-session.store';

export const ownerGuard: CanActivateFn = async (_route, state): Promise<boolean | UrlTree> => {
  const authSession = inject(AuthSessionStore);
  const router = inject(Router);

  await authSession.ensureResolved();
  if (!authSession.isAuthenticated()) {
    authSession.startSignIn(state.url);
    return false;
  }

  return authSession.isActiveOwner() ? true : router.parseUrl(DASHBOARD_ROUTE_PATH);
};
