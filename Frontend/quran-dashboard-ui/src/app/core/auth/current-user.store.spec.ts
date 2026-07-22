import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { TestBed } from '@angular/core/testing';
import {
  HttpTestingController,
  TestRequest,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';

import { environment } from '../../../environments/environment';
import { ApiResponse } from '../data-access/api-response.model';
import { CurrentUser } from './current-user.model';
import { CurrentUserStore } from './current-user.store';

// `CurrentUserStore.load()` (Feature 033, Phase 1) must NEVER throw — an envelope- or HTTP-level
// failure resolves to a calm Arabic message with `currentUser` left null, so it can't crash the
// post-login callback. Wired with the real `AccessApi` + a real HTTP backend so the store's mapping
// is exercised end to end.
const ME_URL = `${environment.apiBaseUrl}/api/access/me`;
const FALLBACK_MESSAGE = 'تعذر تحميل بيانات المستخدم الحالي.';

const CURRENT_USER: CurrentUser = {
  sub: 'logto-subject-1',
  email: 'teacher@example.test',
  displayName: 'معلّم',
  status: 'pending',
  roleId: null,
  roleName: null,
  permissions: [],
};

const OWNER_USER: CurrentUser = {
  sub: 'logto-subject-owner',
  email: 'owner@example.test',
  displayName: 'المالك',
  status: 'active',
  roleId: 1,
  roleName: 'Owner',
  permissions: [],
};

describe('CurrentUserStore.load', () => {
  let store: CurrentUserStore;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    store = TestBed.inject(CurrentUserStore);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('populates currentUser from a successful envelope and clears any error', () => {
    store.load();

    const response: ApiResponse<CurrentUser> = { isSuccess: true, message: 'تم', data: CURRENT_USER };
    httpTesting.expectOne(ME_URL).flush(response);

    // CURRENT_USER carries `roleName: null` — the pending, role-less default.
    expect(store.currentUser()).toEqual(CURRENT_USER);
    expect(store.currentUser()?.roleName).toBeNull();
    expect(store.errorMessage()).toBeNull();
  });

  it('maps a non-null roleName through from the envelope (the bootstrapped Owner)', () => {
    store.load();

    httpTesting
      .expectOne(ME_URL)
      .flush({ isSuccess: true, message: 'تم', data: OWNER_USER } satisfies ApiResponse<CurrentUser>);

    expect(store.currentUser()).toEqual(OWNER_USER);
    expect(store.currentUser()?.roleName).toBe('Owner');
  });

  const failureCases: { name: string; flush: (req: TestRequest) => void; expected: string }[] = [
    {
      name: 'a failure envelope surfaces its own Arabic message',
      flush: (req) => req.flush({ isSuccess: false, message: 'حساب غير مُفعَّل', data: null }),
      expected: 'حساب غير مُفعَّل',
    },
    {
      name: 'a failure envelope without a message falls back to the default',
      flush: (req) => req.flush({ isSuccess: false, message: null, data: null }),
      expected: FALLBACK_MESSAGE,
    },
    {
      name: 'an HTTP error body message is surfaced',
      flush: (req) =>
        req.flush(
          { isSuccess: false, message: 'غير مصرح لك', data: null },
          { status: 401, statusText: 'Unauthorized' },
        ),
      expected: 'غير مصرح لك',
    },
    {
      name: 'an HTTP error without a usable body falls back to the default',
      flush: (req) => req.flush('gateway boom', { status: 502, statusText: 'Bad Gateway' }),
      expected: FALLBACK_MESSAGE,
    },
  ];

  it.each(failureCases)(
    'leaves currentUser null and records a calm Arabic message — $name',
    ({ flush, expected }) => {
      store.load();

      flush(httpTesting.expectOne(ME_URL));

      expect(store.currentUser()).toBeNull();
      expect(store.errorMessage()).toBe(expected);
    },
  );

  it('clears a previously loaded user when a later load fails', () => {
    store.load();
    httpTesting.expectOne(ME_URL).flush({ isSuccess: true, message: 'تم', data: CURRENT_USER });
    expect(store.currentUser()).toEqual(CURRENT_USER);

    store.load();
    httpTesting.expectOne(ME_URL).flush({ isSuccess: false, message: 'انتهت الجلسة', data: null });

    expect(store.currentUser()).toBeNull();
    expect(store.errorMessage()).toBe('انتهت الجلسة');
  });

  describe('ensureLoaded', () => {
    it('resolves after a single request and populates currentUser (incl. roleName)', async () => {
      const settled = store.ensureLoaded();
      httpTesting
        .expectOne(ME_URL)
        .flush({ isSuccess: true, message: 'تم', data: OWNER_USER } satisfies ApiResponse<CurrentUser>);

      await expect(settled).resolves.toBeUndefined();
      expect(store.currentUser()).toEqual(OWNER_USER);
      expect(store.currentUser()?.roleName).toBe('Owner');
    });

    it('loads once and caches — a second call issues no further request', async () => {
      const first = store.ensureLoaded();
      httpTesting.expectOne(ME_URL).flush({ isSuccess: true, message: 'تم', data: CURRENT_USER });
      await first;

      await store.ensureLoaded();

      httpTesting.expectNone(ME_URL);
      expect(store.currentUser()).toEqual(CURRENT_USER);
    });

    it('never rejects on failure — it resolves with a calm Arabic message and a null user', async () => {
      const settled = store.ensureLoaded();
      httpTesting
        .expectOne(ME_URL)
        .flush({ isSuccess: false, message: 'حساب غير مُفعَّل', data: null });

      await expect(settled).resolves.toBeUndefined();
      expect(store.currentUser()).toBeNull();
      expect(store.errorMessage()).toBe('حساب غير مُفعَّل');
    });

    // A failed load is NOT cached (cache-success-only), so a guard that fired during a
    // transient /api/access/me outage can retry on the next evaluation instead of being
    // pinned to the failure until a full page reload.
    const retryFirstFailures: { name: string; fail: (req: TestRequest) => void }[] = [
      {
        name: 'an envelope failure',
        fail: (req) => req.flush({ isSuccess: false, message: 'انتهت الجلسة', data: null }),
      },
      {
        name: 'an HTTP error',
        fail: (req) => req.flush('gateway boom', { status: 502, statusText: 'Bad Gateway' }),
      },
    ];

    it.each(retryFirstFailures)(
      'does not cache a failed load ($name) — a second ensureLoaded() re-requests and can succeed',
      async ({ fail }) => {
        const first = store.ensureLoaded();
        fail(httpTesting.expectOne(ME_URL));
        await first;
        expect(store.currentUser()).toBeNull();
        expect(store.errorMessage()).not.toBeNull();

        const second = store.ensureLoaded();
        httpTesting
          .expectOne(ME_URL)
          .flush({ isSuccess: true, message: 'تم', data: OWNER_USER } satisfies ApiResponse<CurrentUser>);
        await second;

        expect(store.currentUser()).toEqual(OWNER_USER);
        expect(store.errorMessage()).toBeNull();
      },
    );

    it('load() seeds the cache — an ensureLoaded() while it is in flight issues exactly one request', async () => {
      store.load();
      const guarded = store.ensureLoaded();

      // expectOne asserts a single in-flight request; it throws if load() and ensureLoaded()
      // each fired their own GET.
      httpTesting.expectOne(ME_URL).flush({ isSuccess: true, message: 'تم', data: OWNER_USER });
      await guarded;

      expect(store.currentUser()).toEqual(OWNER_USER);
    });
  });
});
