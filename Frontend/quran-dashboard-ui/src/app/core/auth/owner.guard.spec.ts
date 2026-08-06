import { describe, expect, it, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import type { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { UrlTree, provideRouter } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { of } from 'rxjs';

import { AuthReturnLocationStore } from './auth-return-location.store';
import { CurrentUserStore } from './current-user.store';
import { ownerGuard } from './owner.guard';

async function runGuard(isAuthenticated: boolean, isActive: boolean, isOwner: boolean) {
  const authorize = vi.fn();
  const ensureLoaded = vi.fn().mockResolvedValue(undefined);
  const remember = vi.fn();

  TestBed.configureTestingModule({
    providers: [
      provideRouter([]),
      {
        provide: OidcSecurityService,
        useValue: { isAuthenticated$: of({ isAuthenticated }), authorize },
      },
      { provide: CurrentUserStore, useValue: { ensureLoaded, isActive: () => isActive, isOwner: () => isOwner } },
      { provide: AuthReturnLocationStore, useValue: { remember } },
    ],
  });

  const result = await TestBed.runInInjectionContext(() =>
    ownerGuard({} as ActivatedRouteSnapshot, { url: '/settings/access?tab=roles' } as RouterStateSnapshot),
  );

  return { result, authorize, ensureLoaded, remember };
}

describe('ownerGuard', () => {
  it('redirects an anonymous visitor to Logto and preserves only the protected destination', async () => {
    const { result, authorize, ensureLoaded, remember } = await runGuard(false, false, false);

    expect(result).toBe(false);
    expect(authorize).toHaveBeenCalledOnce();
    expect(ensureLoaded).not.toHaveBeenCalled();
    expect(remember).toHaveBeenCalledWith('/settings/access?tab=roles');
  });

  it('activates an authenticated active Owner from isOwner rather than a role name', async () => {
    const { result, authorize, ensureLoaded } = await runGuard(true, true, true);

    expect(result).toBe(true);
    expect(authorize).not.toHaveBeenCalled();
    expect(ensureLoaded).toHaveBeenCalledOnce();
  });

  it.each([
    ['a non-owner', true, false],
    ['a pending account', false, true],
    ['a disabled account', false, false],
  ])('redirects %s to the public dashboard', async (_name, isActive, isOwner) => {
    const { result } = await runGuard(true, isActive, isOwner);

    expect(result).toBeInstanceOf(UrlTree);
    expect((result as UrlTree).toString()).toBe('/dashboard');
  });
});
