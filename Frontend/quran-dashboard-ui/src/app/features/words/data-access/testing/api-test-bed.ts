import { Type } from '@angular/core';
import { TestBed, getTestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { ApiResponse } from '../../../../core/data-access/api-response.model';
import { PagedResultDto } from '../../../../core/data-access/paged-result.model';

export interface ApiTestBed<T> {
  readonly api: T;
  readonly httpMock: HttpTestingController;
}

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

export function teardownApiTestBed(httpMock: HttpTestingController): void {
  httpMock.verify();
  getTestBed().resetTestingModule();
}

export function ok<T>(data: T, message = 'تم'): ApiResponse<T> {
  return { isSuccess: true, message, data };
}

export function page<T>(
  items: T[],
  totalCount = items.length,
  pageNo = 1,
  pageSize = 50,
): ApiResponse<PagedResultDto<T>> {
  return ok<PagedResultDto<T>>({ page: pageNo, pageSize, totalCount, items });
}
