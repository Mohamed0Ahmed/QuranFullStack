import { describe, expect, it, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import type { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { UrlTree, provideRouter } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { of } from 'rxjs';

import { CurrentUser, RoleName } from './current-user.model';
import { CurrentUserStore } from './current-user.store';
import { roleGuard } from './role.guard';

function ownerUser(overrides: Partial<CurrentUser> = {}): CurrentUser {
  return {
    sub: 'logto-subject-1',
    email: 'owner@example.test',
    displayName: 'المالك',
    status: 'active',
    roleId: 1,
    roleName: 'Owner',
    permissions: [],
    ...overrides,
  };
}

async function runGuard(
  requiredRole: RoleName,
  isAuthenticated: boolean,
  user: CurrentUser | null,
): Promise<{ result: boolean | UrlTree; authorize: ReturnType<typeof vi.fn>; ensureLoaded: ReturnType<typeof vi.fn> }> {
  const authorize = vi.fn();
  const ensureLoaded = vi.fn().mockResolvedValue(undefined);

  TestBed.configureTestingModule({
    providers: [
      provideRouter([]),
      {
        provide: OidcSecurityService,
        useValue: { isAuthenticated$: of({ isAuthenticated }), authorize },
      },
      { provide: CurrentUserStore, useValue: { ensureLoaded, currentUser: () => user } },
    ],
  });

  const result = await TestBed.runInInjectionContext(() =>
    roleGuard(requiredRole)({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot),
  );

  return { result: result as boolean | UrlTree, authorize, ensureLoaded };
}

describe('roleGuard', () => {
  it('blocks an anonymous visitor and kicks off the Logto redirect without loading the user', async () => {
    const { result, authorize, ensureLoaded } = await runGuard('Owner', false, null);

    expect(result).toBe(false);
    expect(authorize).toHaveBeenCalledOnce();
    expect(ensureLoaded).not.toHaveBeenCalled();
  });

  it('activates an authenticated, active user holding the required role', async () => {
    const { result, authorize, ensureLoaded } = await runGuard('Owner', true, ownerUser());

    expect(result).toBe(true);
    expect(authorize).not.toHaveBeenCalled();
    expect(ensureLoaded).toHaveBeenCalledOnce();
  });

  const redirectCases: { name: string; user: CurrentUser | null }[] = [
    { name: 'an authenticated user with a different role', user: ownerUser({ roleName: 'Editor' }) },
    {
      name: 'an authenticated but pending, role-less user',
      user: ownerUser({ status: 'pending', roleId: null, roleName: null }),
    },
    { name: 'an authenticated user whose account failed to load', user: null },
  ];

  it.each(redirectCases)('redirects $name to /', async ({ user }) => {
    const { result, authorize } = await runGuard('Owner', true, user);

    expect(result).toBeInstanceOf(UrlTree);
    expect((result as UrlTree).toString()).toBe('/');
    expect(authorize).not.toHaveBeenCalled();
  });
});
