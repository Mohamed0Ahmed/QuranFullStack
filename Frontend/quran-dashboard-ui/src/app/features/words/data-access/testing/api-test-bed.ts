import { Type } from '@angular/core';
import { TestBed, getTestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { ApiResponse } from '../../../../core/data-access/api-response.model';
import { PagedResultDto } from '../../../../core/data-access/paged-result.model';

/**
 * Shared `HttpTestingController` bootstrap for the Words data-access API specs (decision 5,
 * DRY). Every `*.api.spec.ts` under `words/data-access` was hand-rolling the same
 * `resetTestingModule` + `configureTestingModule` + inject pair per `describe` block (see the
 * two duplicated blocks this replaced in `unique-words.api.spec.ts`). This collapses it to one
 * call while keeping the exact same providers, injection, and verify/reset teardown shape the
 * specs already relied on — behavior-preserving, not a new test strategy.
 */
export interface ApiTestBed<T> {
  readonly api: T;
  readonly httpMock: HttpTestingController;
}

/** Bootstraps a fresh TestBed providing `apiClass` plus the real `HttpClient` test doubles. */
export function setupApiTestBed<T>(apiClass: Type<T>): ApiTestBed<T> {
  getTestBed().resetTestingModule();
  TestBed.configureTestingModule({
    providers: [apiClass, provideHttpClient(), provideHttpClientTesting()],
  });
  return {
    api: TestBed.inject(apiClass),
    httpMock: TestBed.inject(HttpTestingController),
  };
}

/** Mirrors the specs' existing `afterEach`: verify no outstanding requests, then reset the bed. */
export function teardownApiTestBed(httpMock: HttpTestingController): void {
  httpMock.verify();
  getTestBed().resetTestingModule();
}

/** The `{ isSuccess: true, message, data }` envelope every API client returns for one object. */
export function ok<T>(data: T, message = 'تم'): ApiResponse<T> {
  return { isSuccess: true, message, data };
}

/** The same envelope wrapping a `PagedResultDto<T>` page, matching what the paged endpoints flush. */
export function page<T>(
  items: T[],
  totalCount = items.length,
  pageNo = 1,
  pageSize = 50,
): ApiResponse<PagedResultDto<T>> {
  return ok<PagedResultDto<T>>({ page: pageNo, pageSize, totalCount, items });
}
