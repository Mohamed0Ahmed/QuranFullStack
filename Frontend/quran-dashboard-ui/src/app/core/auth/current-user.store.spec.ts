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

/**
 * `CurrentUserStore.load()` (Feature 033, Phase 1). It unwraps the `GET /api/access/me`
 * envelope into the `currentUser` / `errorMessage` signals and must NEVER throw — a failure
 * (envelope-level or HTTP-level) resolves to a calm Arabic message with `currentUser` left
 * null, so it can never crash the post-login callback flow. The real `AccessApi` and a real
 * HTTP backend are wired so the store's mapping is exercised end to end.
 */
const ME_URL = `${environment.apiBaseUrl}/api/access/me`;
const FALLBACK_MESSAGE = 'تعذر تحميل بيانات المستخدم الحالي.';

const CURRENT_USER: CurrentUser = {
  sub: 'logto-subject-1',
  email: 'teacher@example.test',
  displayName: 'معلّم',
  status: 'pending',
  roleId: null,
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

    expect(store.currentUser()).toEqual(CURRENT_USER);
    expect(store.errorMessage()).toBeNull();
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
});
