import { afterEach, beforeEach, describe, expect, it } from 'vitest';
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
import {
  AbstractSecurityStorage,
  OidcSecurityService,
  authInterceptor,
  provideAuth,
} from 'angular-auth-oidc-client';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  SecureUrlBlockedError,
  secureUrlInterceptor,
} from '../data-access/secure-url.interceptor';
import { devLatencyInterceptor } from '../data-access/dev-latency.interceptor';

const TEST_CONFIG_ID = 'bearer-token-spec-config';
const TEST_ACCESS_TOKEN = 'test-access-token-value';
const IN_SCOPE_URL = `${environment.apiBaseUrl}/api/access/me`;

class InMemorySecurityStorage implements AbstractSecurityStorage {
  private readonly store = new Map<string, string>();

  read(key: string): string | null {
    return this.store.get(key) ?? null;
  }

  write(key: string, value: string): void {
    this.store.set(key, value);
  }

  remove(key: string): void {
    this.store.delete(key);
  }

  clear(): void {
    this.store.clear();
  }
}

function seededStorage(accessToken: string | null): InMemorySecurityStorage {
  const storage = new InMemorySecurityStorage();
  if (accessToken !== null) {
    storage.write(
      TEST_CONFIG_ID,
      JSON.stringify({ authzData: accessToken, authnResult: { id_token: 'test-id-token' } }),
    );
  }
  return storage;
}

function provideTestAuth(secureRoutes: string[], storage: AbstractSecurityStorage) {
  return [
    provideAuth({
      config: {
        configId: TEST_CONFIG_ID,
        authority: 'https://auth.test',
        redirectUrl: 'https://app.test/callback',
        clientId: 'test-client',
        scope: 'openid',
        responseType: 'code',
        secureRoutes,
      },
    }),
    { provide: AbstractSecurityStorage, useValue: storage },
  ];
}

async function activateConfig(): Promise<void> {
  await firstValueFrom(TestBed.inject(OidcSecurityService).getConfiguration());
}

describe('Bearer-token attach — full interceptor chain', () => {
  let httpClient: HttpClient;
  let httpTesting: HttpTestingController;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(
          withInterceptors([secureUrlInterceptor, authInterceptor(), devLatencyInterceptor]),
        ),
        provideHttpClientTesting(),
        ...provideTestAuth([environment.apiBaseUrl], seededStorage(TEST_ACCESS_TOKEN)),
      ],
    });

    httpClient = TestBed.inject(HttpClient);
    httpTesting = TestBed.inject(HttpTestingController);
    await activateConfig();
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('attaches Authorization: Bearer <token> to an API-base request when the library holds a token', async () => {
    const response = firstValueFrom(httpClient.get(IN_SCOPE_URL));

    const req = httpTesting.expectOne(IN_SCOPE_URL);
    expect(req.request.headers.get('Authorization')).toBe(`Bearer ${TEST_ACCESS_TOKEN}`);

    req.flush({ ok: true });
    await response;
  });

  it('never sends a request — or a token — to a foreign origin (secureUrlInterceptor blocks it)', () => {
    const foreignUrl = 'https://evil.example/api/access/me';
    let caughtError: unknown;

    httpClient.get(foreignUrl).subscribe({
      error: (error) => {
        caughtError = error;
      },
    });

    expect(caughtError).toBeInstanceOf(SecureUrlBlockedError);
    httpTesting.expectNone(foreignUrl);
  });
});

describe('Bearer-token attach scope — secureRoutes gate (authInterceptor in isolation)', () => {
  let httpClient: HttpClient;
  let httpTesting: HttpTestingController;

  const cases = [
    {
      name: 'a URL inside secureRoutes receives the token',
      url: IN_SCOPE_URL,
      expectsHeader: true,
    },
    {
      name: 'a URL outside secureRoutes does not, even though a token is held',
      url: 'https://other-service.test/api/data',
      expectsHeader: false,
    },
  ];

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor()])),
        provideHttpClientTesting(),
        ...provideTestAuth([environment.apiBaseUrl], seededStorage(TEST_ACCESS_TOKEN)),
      ],
    });

    httpClient = TestBed.inject(HttpClient);
    httpTesting = TestBed.inject(HttpTestingController);
    await activateConfig();
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it.each(cases)('$name', async ({ url, expectsHeader }) => {
    const response = firstValueFrom(httpClient.get(url));

    const req = httpTesting.expectOne(url);
    expect(req.request.headers.has('Authorization')).toBe(expectsHeader);
    if (expectsHeader) {
      expect(req.request.headers.get('Authorization')).toBe(`Bearer ${TEST_ACCESS_TOKEN}`);
    }

    req.flush({ ok: true });
    await response;
  });
});
