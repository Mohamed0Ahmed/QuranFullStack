import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { HttpErrorResponse, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { of } from 'rxjs';

import { environment } from '../../../environments/environment';
import type { ApiResponse } from '../data-access/api-response.model';
import { AuthReturnLocationStore } from './auth-return-location.store';
import { ACCESS_ME_CONTRACT_FIXTURES } from './auth.testing';
import type { AccessMeContractFixture } from './auth.testing';
import { CurrentUserStore } from './current-user.store';
import { WriteAuthFailureCoordinator } from './write-auth-failure.coordinator';

const ME_URL = `${environment.apiBaseUrl}/api/access/me`;
const CURRENT_ROUTE = '/abwab?draft=1';

async function loadCurrentUser(
  currentUser: CurrentUserStore,
  httpTesting: HttpTestingController,
  fixture: AccessMeContractFixture,
): Promise<void> {
  const loaded = currentUser.ensureLoaded();
  httpTesting.expectOne(ME_URL).flush({
    isSuccess: true,
    message: 'تم',
    data: fixture,
  } satisfies ApiResponse<AccessMeContractFixture>);
  await loaded;
}

describe('WriteAuthFailureCoordinator', () => {
  let coordinator: WriteAuthFailureCoordinator;
  let authorize: ReturnType<typeof vi.fn>;
  let authReturnLocation: AuthReturnLocationStore;
  let currentUser: CurrentUserStore;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    authorize = vi.fn();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: Router, useValue: { url: CURRENT_ROUTE } },
        {
          provide: OidcSecurityService,
          useValue: {
            isAuthenticated$: of({ isAuthenticated: false }),
            getIdToken: () => of('signed.id.token'),
            authorize,
          },
        },
      ],
    });

    coordinator = TestBed.inject(WriteAuthFailureCoordinator);
    authReturnLocation = TestBed.inject(AuthReturnLocationStore);
    currentUser = TestBed.inject(CurrentUserStore);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('starts one login flow for concurrent write 401 responses and preserves the read location', async () => {
    await loadCurrentUser(currentUser, httpTesting, ACCESS_ME_CONTRACT_FIXTURES.exactPermission);
    const unauthorized = new HttpErrorResponse({
      status: 401,
      error: { isSuccess: false, message: 'انتهت الجلسة', data: null },
    });

    await expect(
      Promise.all([
        coordinator.handle(unauthorized),
        coordinator.handle(unauthorized),
      ]),
    ).resolves.toEqual([
      { kind: 'unauthorized', message: 'انتهت الجلسة' },
      { kind: 'unauthorized', message: 'انتهت الجلسة' },
    ]);

    expect(authorize).toHaveBeenCalledOnce();
    expect(authReturnLocation.consume()).toBe(CURRENT_ROUTE);
    expect(currentUser.currentUser()).toBeNull();
    expect(currentUser.loadState()).toBe('idle');
  });

  it('replaces the access snapshot after a write 403', async () => {
    await loadCurrentUser(currentUser, httpTesting, ACCESS_ME_CONTRACT_FIXTURES.exactPermission);

    const handled = coordinator.handle(
      new HttpErrorResponse({
        status: 403,
        error: { isSuccess: false, message: 'لم تعد لديك الصلاحية', data: null },
      }),
    );
    httpTesting.expectOne(ME_URL).flush({
      isSuccess: true,
      message: 'تم',
      data: ACCESS_ME_CONTRACT_FIXTURES.readOnly,
    } satisfies ApiResponse<AccessMeContractFixture>);

    await expect(handled).resolves.toEqual({
      kind: 'forbidden',
      message: 'لم تعد لديك الصلاحية',
    });
    expect(currentUser.currentUser()?.sub).toBe('test-read-only');
    expect(currentUser.isActive()).toBe(true);
    expect(currentUser.can('abwab.doors.create')).toBe(false);
    expect(authorize).not.toHaveBeenCalled();
  });

  it('does not coordinate normal public-request errors', async () => {
    await loadCurrentUser(currentUser, httpTesting, ACCESS_ME_CONTRACT_FIXTURES.exactPermission);

    await expect(coordinator.handle(new HttpErrorResponse({ status: 500 }))).resolves.toBeNull();

    expect(currentUser.currentUser()?.sub).toBe('test-exact-permission');
    expect(currentUser.can('abwab.doors.create')).toBe(true);
    expect(authReturnLocation.consume('/fallback')).toBe('/fallback');
    expect(authorize).not.toHaveBeenCalled();
  });
});
