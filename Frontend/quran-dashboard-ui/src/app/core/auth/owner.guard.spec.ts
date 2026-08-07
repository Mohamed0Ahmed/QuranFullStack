import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import type { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { UrlTree, provideRouter } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { BehaviorSubject } from 'rxjs';

import { environment } from '../../../environments/environment';
import type { ApiResponse } from '../data-access/api-response.model';
import { AuthReturnLocationStore } from './auth-return-location.store';
import { ACCESS_ME_CONTRACT_FIXTURES } from './auth.testing';
import type { AccessMeContractFixture } from './auth.testing';
import { ownerGuard } from './owner.guard';

const ME_URL = `${environment.apiBaseUrl}/api/access/me`;
const PROTECTED_DESTINATION = '/settings/access?tab=roles';

function runGuard() {
  return TestBed.runInInjectionContext(() =>
    ownerGuard(
      {} as ActivatedRouteSnapshot,
      { url: PROTECTED_DESTINATION } as RouterStateSnapshot,
    ),
  );
}

function flushCurrentUser(
  httpTesting: HttpTestingController,
  fixture: AccessMeContractFixture,
): void {
  httpTesting.expectOne(ME_URL).flush({
    isSuccess: true,
    message: 'تم',
    data: fixture,
  } satisfies ApiResponse<AccessMeContractFixture>);
}

describe('ownerGuard', () => {
  let authentication: BehaviorSubject<{ isAuthenticated: boolean }>;
  let authorize: ReturnType<typeof vi.fn>;
  let authReturnLocation: AuthReturnLocationStore;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    authentication = new BehaviorSubject<{ isAuthenticated: boolean }>({
      isAuthenticated: false,
    });
    authorize = vi.fn();
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: OidcSecurityService,
          useValue: { isAuthenticated$: authentication, authorize },
        },
      ],
    });

    authReturnLocation = TestBed.inject(AuthReturnLocationStore);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('redirects an anonymous visitor to Logto and preserves only the protected destination', async () => {
    await expect(runGuard()).resolves.toBe(false);

    expect(authorize).toHaveBeenCalledOnce();
    expect(authReturnLocation.consume()).toBe(PROTECTED_DESTINATION);
    httpTesting.expectNone(ME_URL);
  });

  it('activates an authenticated active Owner from isOwner rather than a role name', async () => {
    authentication.next({ isAuthenticated: true });
    const result = runGuard();

    flushCurrentUser(httpTesting, ACCESS_ME_CONTRACT_FIXTURES.owner);

    await expect(result).resolves.toBe(true);
    expect(authorize).not.toHaveBeenCalled();
  });

  it.each([
    ['an active non-owner', ACCESS_ME_CONTRACT_FIXTURES.readOnly],
    ['a pending account', ACCESS_ME_CONTRACT_FIXTURES.pending],
    [
      'a disabled Owner',
      { ...ACCESS_ME_CONTRACT_FIXTURES.owner, status: 'disabled' } satisfies AccessMeContractFixture,
    ],
  ])('redirects %s to the public dashboard', async (_name, fixture) => {
    authentication.next({ isAuthenticated: true });
    const result = runGuard();

    flushCurrentUser(httpTesting, fixture);

    const destination = await result;
    expect(destination).toBeInstanceOf(UrlTree);
    expect((destination as UrlTree).toString()).toBe('/dashboard');
  });
});
