import { TestBed } from '@angular/core/testing';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import {
  HttpClient,
  provideHttpClient,
  withInterceptors,
} from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

import { devLatencyInterceptor } from './dev-latency.interceptor';

describe('devLatencyInterceptor', () => {
  let httpClient: HttpClient;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([devLatencyInterceptor])),
        provideHttpClientTesting(),
      ],
    });

    httpClient = TestBed.inject(HttpClient);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('forwards successful responses after the configured dev latency', async () => {
    const startedAt = performance.now();
    const responsePromise = firstValueFrom(httpClient.get<{ ok: true }>('/api/test'));

    httpTesting.expectOne('/api/test').flush({ ok: true });

    const response = await responsePromise;
    const elapsedMs = performance.now() - startedAt;

    expect(response).toEqual({ ok: true });
    expect(elapsedMs).toBeGreaterThanOrEqual(350);
  });
});
