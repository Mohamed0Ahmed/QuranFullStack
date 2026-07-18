import { describe, expect, it, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { of } from 'rxjs';

import { AuthCallbackComponent } from './auth-callback.component';
import { CurrentUserStore } from '../../../../core/auth/current-user.store';
import { DASHBOARD_ROUTE_PATH } from '../../../../core/navigation/route-paths';

/**
 * Auth callback landing (Feature 033, Phase 1). On activation it reads Logto auth state
 * once: when authenticated it kicks off the (non-blocking) current-user load, and in every
 * case it lands the visitor on `/dashboard`. `OidcSecurityService`, `CurrentUserStore`, and
 * `Router` are the component's real injection boundaries; each is replaced with a boundary
 * fake so the test asserts observable behavior (load fired, navigation target) only.
 *
 * The store's `load()` is fire-and-forget and never throws (its own spec covers the
 * failure envelopes), so a failed load can never block navigation here — the load spy
 * simply records the call.
 */
describe('AuthCallbackComponent', () => {
  function mount(isAuthenticated: boolean): { load: ReturnType<typeof vi.fn>; navigateByUrl: ReturnType<typeof vi.fn> } {
    const load = vi.fn();
    const navigateByUrl = vi.fn().mockResolvedValue(true);

    TestBed.configureTestingModule({
      imports: [AuthCallbackComponent],
      providers: [
        { provide: OidcSecurityService, useValue: { isAuthenticated$: of({ isAuthenticated }) } },
        { provide: CurrentUserStore, useValue: { load } },
        { provide: Router, useValue: { navigateByUrl } },
      ],
    });

    const fixture = TestBed.createComponent(AuthCallbackComponent);
    fixture.detectChanges();

    return { load, navigateByUrl };
  }

  const cases = [
    { name: 'authenticated', isAuthenticated: true, loadCalls: 1 },
    { name: 'unauthenticated', isAuthenticated: false, loadCalls: 0 },
  ];

  it.each(cases)(
    'an $name callback loads the current user $loadCalls× and always navigates to the dashboard',
    ({ isAuthenticated, loadCalls }) => {
      const { load, navigateByUrl } = mount(isAuthenticated);

      expect(load).toHaveBeenCalledTimes(loadCalls);
      expect(navigateByUrl).toHaveBeenCalledTimes(1);
      expect(navigateByUrl).toHaveBeenCalledWith(DASHBOARD_ROUTE_PATH);
    },
  );
});
