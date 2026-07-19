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
  SecureUrlBlockedError,
  isUrlUnderApiBase,
  secureUrlInterceptor,
} from './secure-url.interceptor';
import { environment } from '../../../environments/environment';

const devApiBaseUrl = 'https://localhost:5015';

describe('isUrlUnderApiBase', () => {
  it('fails closed (blocks every URL) when apiBaseUrl is empty, a misconfiguration', () => {
    expect(isUrlUnderApiBase('http://any-host.example/api/foo', '')).toBe(false);
    expect(isUrlUnderApiBase('https://evil.example/', '')).toBe(false);
  });

  it('allows URLs on the exact API origin', () => {
    expect(isUrlUnderApiBase('https://localhost:5015/api/mushaf/pages/1', devApiBaseUrl)).toBe(
      true
    );
    expect(isUrlUnderApiBase('https://localhost:5015', devApiBaseUrl)).toBe(true);
  });

  it('rejects a host that only prefix-matches the base URL', () => {
    expect(
      isUrlUnderApiBase('https://localhost:5015.evil.example/api/x', devApiBaseUrl)
    ).toBe(false);
  });

  it('rejects a different HTTPS host', () => {
    expect(isUrlUnderApiBase('https://evil.example/api/foo', devApiBaseUrl)).toBe(false);
  });

  it('rejects HTTP on the API port', () => {
    expect(isUrlUnderApiBase('http://localhost:5014/api/mushaf/pages/1', devApiBaseUrl)).toBe(
      false
    );
  });

  it('rejects invalid URLs', () => {
    expect(isUrlUnderApiBase('not-a-url', devApiBaseUrl)).toBe(false);
  });
});

describe('secureUrlInterceptor', () => {
  let httpClient: HttpClient;
  let httpTestingController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([secureUrlInterceptor])),
        provideHttpClientTesting(),
      ],
    });

    httpClient = TestBed.inject(HttpClient);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('blocks an http:// URL with a controlled error', () => {
    const blockedUrl = 'http://localhost:5014/api/mushaf/pages/1';
    let caughtError: unknown;

    httpClient.get(blockedUrl).subscribe({
      error: (error) => {
        caughtError = error;
      },
    });

    expect(caughtError).toBeInstanceOf(SecureUrlBlockedError);
    expect((caughtError as SecureUrlBlockedError).message).toContain(blockedUrl);
  });

  it('blocks a prefix-matching evil host with a controlled error', () => {
    const blockedUrl = 'https://localhost:5015.evil.example/api/mushaf/pages/1';
    let caughtError: unknown;

    httpClient.get(blockedUrl).subscribe({
      error: (error) => {
        caughtError = error;
      },
    });

    expect(caughtError).toBeInstanceOf(SecureUrlBlockedError);
    expect((caughtError as SecureUrlBlockedError).message).toContain(blockedUrl);
  });

  it('allows an https://localhost:5015 URL to proceed', () => {
    const allowedUrl = 'https://localhost:5015/api/mushaf/pages/1';
    let responseBody: unknown;

    httpClient.get(allowedUrl).subscribe({
      next: (body) => {
        responseBody = body;
      },
    });

    const req = httpTestingController.expectOne(allowedUrl);
    expect(req.request.method).toBe('GET');
    req.flush({ ok: true });
    expect(responseBody).toEqual({ ok: true });
  });

  it('lets the identity-provider origin pass through untouched (no block, no Authorization)', () => {
    const discoveryUrl = `${environment.logto.endpoint}/oidc/.well-known/openid-configuration`;
    let responseBody: unknown;

    httpClient.get(discoveryUrl).subscribe({
      next: (body) => {
        responseBody = body;
      },
    });

    const req = httpTestingController.expectOne(discoveryUrl);
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush({ issuer: `${environment.logto.endpoint}/oidc` });
    expect(responseBody).toEqual({ issuer: `${environment.logto.endpoint}/oidc` });
  });

  it('still blocks a foreign origin that is neither the API nor the identity provider', () => {
    const blockedUrl = 'https://evil.example/oidc/.well-known/openid-configuration';
    let caughtError: unknown;

    httpClient.get(blockedUrl).subscribe({
      error: (error) => {
        caughtError = error;
      },
    });

    expect(caughtError).toBeInstanceOf(SecureUrlBlockedError);
    expect((caughtError as SecureUrlBlockedError).message).toContain(blockedUrl);
  });
});
