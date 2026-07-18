import { describe, expect, it, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { of } from 'rxjs';

import { AuthCallbackComponent } from './auth-callback.component';
import { CurrentUserStore } from '../../../../core/auth/current-user.store';
import { DASHBOARD_ROUTE_PATH } from '../../../../core/navigation/route-paths';

/**
 * Auth callback landing (Feature 033). On activation it reads Logto auth state once and
 * distinguishes three outcomes:
 * - authenticated → fires the (non-blocking) current-user load and navigates to
 *   `/dashboard`.
 * - not authenticated with an `error` or `code` query param on the callback URL → a
 *   genuine login FAILURE: stays on the page and renders the calm error state instead
 *   of navigating.
 * - not authenticated with neither param → an ABANDONED login: still navigates to
 *   `/dashboard` (browsing is public).
 *
 * `OidcSecurityService`, `CurrentUserStore`, `Router`, and `ActivatedRoute` are the
 * component's real injection boundaries; each is replaced with a boundary fake so the
 * tests assert observable behavior only (rendered DOM, load/navigate/authorize calls).
 *
 * The store's `load()` is fire-and-forget and never throws (its own spec covers the
 * failure envelopes), so a failed load can never block navigation here — the load spy
 * simply records the call.
 */
describe('AuthCallbackComponent', () => {
  function mount(isAuthenticated: boolean, queryParams: Record<string, string> = {}) {
    const load = vi.fn();
    const navigateByUrl = vi.fn().mockResolvedValue(true);
    const authorize = vi.fn();

    TestBed.configureTestingModule({
      imports: [AuthCallbackComponent],
      providers: [
        {
          provide: OidcSecurityService,
          useValue: { isAuthenticated$: of({ isAuthenticated }), authorize },
        },
        { provide: CurrentUserStore, useValue: { load } },
        { provide: Router, useValue: { navigateByUrl } },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { queryParamMap: convertToParamMap(queryParams) } },
        },
      ],
    });

    const fixture = TestBed.createComponent(AuthCallbackComponent);
    fixture.detectChanges();

    return { fixture, load, navigateByUrl, authorize };
  }

  function errorState(fixture: ReturnType<typeof mount>['fixture']) {
    return (fixture.nativeElement as HTMLElement).querySelector('[data-testid="qd-state-error"]');
  }

  it('renders the error state and does not navigate when Logto returns an `error` param', () => {
    const { fixture, navigateByUrl } = mount(false, { error: 'access_denied' });

    expect(errorState(fixture)).toBeTruthy();
    expect(navigateByUrl).not.toHaveBeenCalled();
  });

  it('renders the error state when a `code` param is present but the exchange did not authenticate', () => {
    const { fixture, navigateByUrl } = mount(false, { code: 'xyz' });

    expect(errorState(fixture)).toBeTruthy();
    expect(navigateByUrl).not.toHaveBeenCalled();
  });

  it('loads the current user and navigates to the dashboard when authenticated', () => {
    const { fixture, load, navigateByUrl } = mount(true);

    expect(load).toHaveBeenCalledTimes(1);
    expect(navigateByUrl).toHaveBeenCalledTimes(1);
    expect(navigateByUrl).toHaveBeenCalledWith(DASHBOARD_ROUTE_PATH);
    expect(errorState(fixture)).toBeNull();
  });

  it('navigates to the dashboard as an abandoned login when unauthenticated with no error/code params', () => {
    const { fixture, load, navigateByUrl } = mount(false);

    expect(load).not.toHaveBeenCalled();
    expect(navigateByUrl).toHaveBeenCalledTimes(1);
    expect(navigateByUrl).toHaveBeenCalledWith(DASHBOARD_ROUTE_PATH);
    expect(errorState(fixture)).toBeNull();
  });

  it('restarts the login flow when the error state’s retry action is activated', () => {
    const { fixture, authorize } = mount(false, { error: 'access_denied' });

    errorState(fixture)?.querySelector<HTMLButtonElement>('[data-testid="qd-state-action"]')?.click();

    expect(authorize).toHaveBeenCalledTimes(1);
  });
});
