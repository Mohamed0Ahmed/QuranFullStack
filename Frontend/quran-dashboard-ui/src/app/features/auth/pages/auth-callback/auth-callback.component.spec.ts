import { describe, expect, it, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { BehaviorSubject, of } from 'rxjs';

import { AuthCallbackComponent } from './auth-callback.component';
import { AuthReturnLocationStore } from '../../../../core/auth/auth-return-location.store';
import { CurrentUserStore } from '../../../../core/auth/current-user.store';
import { DASHBOARD_ROUTE_PATH } from '../../../../core/navigation/route-paths';

describe('AuthCallbackComponent', () => {
  function mount(isAuthenticated: boolean, queryParams: Record<string, string> = {}) {
    const ensureLoaded = vi.fn().mockResolvedValue(undefined);
    const consume = vi.fn(() => DASHBOARD_ROUTE_PATH);
    const clear = vi.fn();
    const navigateByUrl = vi.fn().mockResolvedValue(true);
    const authorize = vi.fn();

    TestBed.configureTestingModule({
      imports: [AuthCallbackComponent],
      providers: [
        {
          provide: OidcSecurityService,
          useValue: { isAuthenticated$: of({ isAuthenticated }), authorize },
        },
        { provide: CurrentUserStore, useValue: { ensureLoaded } },
        { provide: AuthReturnLocationStore, useValue: { consume, clear } },
        { provide: Router, useValue: { navigateByUrl } },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { queryParamMap: convertToParamMap(queryParams) } },
        },
      ],
    });

    const fixture = TestBed.createComponent(AuthCallbackComponent);
    fixture.detectChanges();

    return { fixture, ensureLoaded, consume, clear, navigateByUrl, authorize };
  }

  function errorState(fixture: ReturnType<typeof mount>['fixture']) {
    return (fixture.nativeElement as HTMLElement).querySelector('[data-testid="auth-callback-error"]');
  }

  function setupCallbackRetry() {
    const authentication = new BehaviorSubject({ isAuthenticated: false });
    const ensureLoaded = vi.fn().mockResolvedValue(undefined);
    const navigateByUrl = vi.fn().mockResolvedValue(true);
    const authorize = vi.fn();
    const route = { snapshot: { queryParamMap: convertToParamMap({ error: 'access_denied' }) } };

    TestBed.configureTestingModule({
      imports: [AuthCallbackComponent],
      providers: [
        {
          provide: OidcSecurityService,
          useValue: { isAuthenticated$: authentication, authorize },
        },
        { provide: CurrentUserStore, useValue: { ensureLoaded } },
        { provide: Router, useValue: { navigateByUrl } },
        { provide: ActivatedRoute, useValue: route },
      ],
    });

    return {
      authentication,
      authorize,
      ensureLoaded,
      navigateByUrl,
      returnLocationStore: TestBed.inject(AuthReturnLocationStore),
      route,
    };
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

  it('loads the current user and restores the saved destination when authenticated', () => {
    const { fixture, ensureLoaded, consume, navigateByUrl } = mount(true);

    expect(ensureLoaded).toHaveBeenCalledTimes(1);
    expect(consume).toHaveBeenCalledWith(DASHBOARD_ROUTE_PATH);
    expect(navigateByUrl).toHaveBeenCalledTimes(1);
    expect(navigateByUrl).toHaveBeenCalledWith(DASHBOARD_ROUTE_PATH);
    expect(errorState(fixture)).toBeNull();
  });

  it('navigates to the dashboard as an abandoned login when unauthenticated with no error/code params', () => {
    const { fixture, ensureLoaded, navigateByUrl } = mount(false);

    expect(ensureLoaded).not.toHaveBeenCalled();
    expect(navigateByUrl).toHaveBeenCalledTimes(1);
    expect(navigateByUrl).toHaveBeenCalledWith(DASHBOARD_ROUTE_PATH);
    expect(errorState(fixture)).toBeNull();
  });

  it('restarts the login flow when the error state’s retry action is activated', () => {
    const { fixture, authorize } = mount(false, { error: 'access_denied' });

    errorState(fixture)?.querySelector<HTMLButtonElement>('[data-testid="auth-callback-retry"]')?.click();

    expect(authorize).toHaveBeenCalledTimes(1);
  });

  it('renders the pending state through the F12 skeleton owner, never the qd-state adapter', () => {
    const { fixture } = mount(false);
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('qd-state')).toBeNull();
    const loading = root.querySelector('[data-testid="auth-callback-loading"]');
    expect(loading?.getAttribute('role')).toBe('status');
    expect(loading?.getAttribute('aria-busy')).toBe('true');
  });

  it('restores a saved 401 deep link after a callback error, retry, and successful callback', () => {
    const { authentication, authorize, ensureLoaded, navigateByUrl, returnLocationStore, route } = setupCallbackRetry();
    const savedDeepLink = '/settings/access?tab=roles#owner';
    returnLocationStore.remember(savedDeepLink);

    const failedCallback = TestBed.createComponent(AuthCallbackComponent);
    failedCallback.detectChanges();
    errorState(failedCallback)
      ?.querySelector<HTMLButtonElement>('[data-testid="auth-callback-retry"]')
      ?.click();

    expect(authorize).toHaveBeenCalledOnce();
    expect(navigateByUrl).not.toHaveBeenCalled();

    route.snapshot.queryParamMap = convertToParamMap({});
    authentication.next({ isAuthenticated: true });

    const successfulCallback = TestBed.createComponent(AuthCallbackComponent);
    successfulCallback.detectChanges();

    expect(ensureLoaded).toHaveBeenCalledOnce();
    expect(navigateByUrl).toHaveBeenCalledOnce();
    expect(navigateByUrl).toHaveBeenCalledWith(savedDeepLink);
  });
});
