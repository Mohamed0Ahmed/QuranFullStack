import { describe, expect, it, beforeEach, afterEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { HttpClient, provideHttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../environments/environment';
import { ApiResponse } from './api-response.model';
import { AppInfo } from './system.models';
import { SystemApi } from './system.api';

function appInfoResponse(): ApiResponse<AppInfo> {
  return {
    isSuccess: true,
    message: 'تم',
    data: { appName: 'test-app', environment: 'test', version: '1.0.0' },
  };
}

describe('SystemApi.getDashboardInfo (app-lifetime cache, perf finding F2)', () => {
  let httpTesting: HttpTestingController;
  let systemApi: SystemApi;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), SystemApi],
    });

    httpTesting = TestBed.inject(HttpTestingController);
    systemApi = TestBed.inject(SystemApi);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('issues GET /api/dashboard/info once for repeated calls and replays the same success to every caller', async () => {
    const firstCall = firstValueFrom(systemApi.getDashboardInfo());
    httpTesting.expectOne(`${environment.apiBaseUrl}/api/dashboard/info`).flush(appInfoResponse());
    const firstResult = await firstCall;

    const secondResult = await firstValueFrom(systemApi.getDashboardInfo());
    const thirdResult = await firstValueFrom(systemApi.getDashboardInfo());

    httpTesting.expectNone(`${environment.apiBaseUrl}/api/dashboard/info`);
    expect(firstResult).toEqual(appInfoResponse().data);
    expect(secondResult).toEqual(appInfoResponse().data);
    expect(thirdResult).toEqual(appInfoResponse().data);
  });

  it('shares one in-flight request across concurrent callers before it resolves', async () => {
    const first = firstValueFrom(systemApi.getDashboardInfo());
    const second = firstValueFrom(systemApi.getDashboardInfo());

    httpTesting.expectOne(`${environment.apiBaseUrl}/api/dashboard/info`).flush(appInfoResponse());

    expect(await first).toEqual(appInfoResponse().data);
    expect(await second).toEqual(appInfoResponse().data);
  });

  it('keeps the error path retryable instead of caching a failed response', async () => {
    const failingCall = firstValueFrom(systemApi.getDashboardInfo());
    httpTesting
      .expectOne(`${environment.apiBaseUrl}/api/dashboard/info`)
      .flush({ isSuccess: false, message: 'خطأ في الخادم', data: null }, { status: 500, statusText: 'Server Error' });

    await expect(failingCall).rejects.toThrow();

    const retryCall = firstValueFrom(systemApi.getDashboardInfo());
    httpTesting.expectOne(`${environment.apiBaseUrl}/api/dashboard/info`).flush(appInfoResponse());

    expect(await retryCall).toEqual(appInfoResponse().data);
  });
});
