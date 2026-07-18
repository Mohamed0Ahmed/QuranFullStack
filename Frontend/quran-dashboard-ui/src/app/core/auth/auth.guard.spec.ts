import { describe, expect, it, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import type { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { Observable, firstValueFrom, of } from 'rxjs';

import { authGuard } from './auth.guard';

/**
 * `authGuard` (Feature 033, Phase 1). Authentication-only: an authenticated visitor
 * activates the `/dashboard` subtree; an unauthenticated one is bounced into the Logto
 * redirect and blocked. `OidcSecurityService` is the guard's real injection boundary, so
 * a lightweight stand-in for it is a boundary fake, not an internal mock.
 */
describe('authGuard', () => {
  const cases = [
    { name: 'authenticated visitor', isAuthenticated: true, expected: true, authorizeCalls: 0 },
    { name: 'unauthenticated visitor', isAuthenticated: false, expected: false, authorizeCalls: 1 },
  ];

  it.each(cases)(
    'a $name activates=$expected and triggers the Logto redirect $authorizeCalls×',
    async ({ isAuthenticated, expected, authorizeCalls }) => {
      const authorize = vi.fn();
      TestBed.configureTestingModule({
        providers: [
          {
            provide: OidcSecurityService,
            useValue: { isAuthenticated$: of({ isAuthenticated }), authorize },
          },
        ],
      });

      const result = TestBed.runInInjectionContext(() =>
        authGuard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot),
      );
      const activated = await firstValueFrom(result as Observable<boolean>);

      expect(activated).toBe(expected);
      expect(authorize).toHaveBeenCalledTimes(authorizeCalls);
    },
  );
});
