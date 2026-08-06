import { describe, expect, it, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';

import { AuthReturnLocationStore } from './auth-return-location.store';
import { CurrentUserStore } from './current-user.store';
import { WriteAuthFailureCoordinator } from './write-auth-failure.coordinator';

function setup() {
  const authorize = vi.fn();
  const clear = vi.fn();
  const refresh = vi.fn().mockResolvedValue(undefined);
  const remember = vi.fn();

  TestBed.configureTestingModule({
    providers: [
      { provide: Router, useValue: { url: '/abwab?draft=1' } },
      { provide: OidcSecurityService, useValue: { authorize } },
      { provide: CurrentUserStore, useValue: { clear, refresh } },
      { provide: AuthReturnLocationStore, useValue: { remember } },
    ],
  });

  return {
    coordinator: TestBed.inject(WriteAuthFailureCoordinator),
    authorize,
    clear,
    refresh,
    remember,
  };
}

describe('WriteAuthFailureCoordinator', () => {
  it('starts one login flow for concurrent write 401 responses and preserves the read location', async () => {
    const { coordinator, authorize, clear, remember } = setup();
    const unauthorized = new HttpErrorResponse({
      status: 401,
      error: { isSuccess: false, message: 'انتهت الجلسة', data: null },
    });

    await expect(Promise.all([coordinator.handle(unauthorized), coordinator.handle(unauthorized)])).resolves.toEqual([
      { kind: 'unauthorized', message: 'انتهت الجلسة' },
      { kind: 'unauthorized', message: 'انتهت الجلسة' },
    ]);

    expect(authorize).toHaveBeenCalledOnce();
    expect(clear).toHaveBeenCalledOnce();
    expect(remember).toHaveBeenCalledWith('/abwab?draft=1');
  });

  it('refreshes the snapshot after a write 403 without retrying the request', async () => {
    const { coordinator, authorize, refresh } = setup();

    await expect(
      coordinator.handle(
        new HttpErrorResponse({
          status: 403,
          error: { isSuccess: false, message: 'لم تعد لديك الصلاحية', data: null },
        }),
      ),
    ).resolves.toEqual({ kind: 'forbidden', message: 'لم تعد لديك الصلاحية' });

    expect(refresh).toHaveBeenCalledOnce();
    expect(authorize).not.toHaveBeenCalled();
  });

  it('does not coordinate normal public-request errors', async () => {
    const { coordinator, authorize, refresh } = setup();

    await expect(coordinator.handle(new HttpErrorResponse({ status: 500 }))).resolves.toBeNull();

    expect(authorize).not.toHaveBeenCalled();
    expect(refresh).not.toHaveBeenCalled();
  });
});
