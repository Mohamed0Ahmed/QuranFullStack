import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Observable, firstValueFrom } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { AccessAdminApi } from './access-admin.api';

const BASE = `${environment.apiBaseUrl}/api/access`;

describe('AccessAdminApi', () => {
  let api: AccessAdminApi;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [AccessAdminApi, provideHttpClient(), provideHttpClientTesting()],
    });
    api = TestBed.inject(AccessAdminApi);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('sends the selected user-list filters and omits blank optional values', async () => {
    const request = firstValueFrom(
      api.listUsers({ status: 'active', isOwner: false, search: '  معلّم  ', page: 2, pageSize: 50 }),
    );

    const req = httpTesting.expectOne((candidate) => candidate.url === `${BASE}/users`);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('status')).toBe('active');
    expect(req.request.params.get('isOwner')).toBe('false');
    expect(req.request.params.get('search')).toBe('معلّم');
    expect(req.request.params.get('page')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('50');
    req.flush({ isSuccess: true, message: 'تم', data: null });

    await expect(request).resolves.toMatchObject({ isSuccess: true });
  });

  it('uses the individual-code permission replacement endpoint', async () => {
    const request = firstValueFrom(
      api.replacePermissions(17, {
        expectedVersion: 4,
        permissionCodes: ['abwab.doors.create', 'abwab.doors.edit'],
        reason: 'تكليف جديد',
      }),
    );

    const req = httpTesting.expectOne(`${BASE}/users/17/permissions`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({
      expectedVersion: 4,
      permissionCodes: ['abwab.doors.create', 'abwab.doors.edit'],
      reason: 'تكليف جديد',
    });
    req.flush({ isSuccess: true, message: 'تم', data: null });

    await expect(request).resolves.toMatchObject({ isSuccess: true });
  });

  it.each([
    ['user detail', (api: AccessAdminApi) => api.getUser(17), `${BASE}/users/17`],
    [
      'current direct permissions',
      (api: AccessAdminApi) => api.getUserPermissions(17),
      `${BASE}/users/17/permissions`,
    ],
    ['permission catalogue', (api: AccessAdminApi) => api.getPermissionCatalogue(), `${BASE}/permissions`],
    [
      'owner-reconciliation status',
      (api: AccessAdminApi) => api.getOwnerReconciliationStatus(),
      `${BASE}/owner-reconciliation/status`,
    ],
  ])('reads %s from its Phase 6 endpoint', async (_name, call, url) => {
    const request = firstValueFrom(call(api) as Observable<unknown>);

    const req = httpTesting.expectOne(url);
    expect(req.request.method).toBe('GET');
    req.flush({ isSuccess: true, message: 'تم', data: null });

    await expect(request).resolves.toMatchObject({ isSuccess: true });
  });

  it.each([
    ['accept', (api: AccessAdminApi) => api.acceptUser(17, { expectedVersion: 4, permissionCodes: [], reason: 'اعتماد' }), 'POST', `${BASE}/users/17/accept`],
    ['disable', (api: AccessAdminApi) => api.disableUser(17, { expectedVersion: 4, reason: 'إيقاف' }), 'POST', `${BASE}/users/17/disable`],
    ['reactivate', (api: AccessAdminApi) => api.reactivateUser(17, { expectedVersion: 4, reason: 'إعادة تفعيل' }), 'POST', `${BASE}/users/17/reactivate`],
  ])('sends the %s status transition to its Owner-only endpoint', async (_name, call, method, url) => {
    const request = firstValueFrom(call(api));

    const req = httpTesting.expectOne(url);
    expect(req.request.method).toBe(method);
    expect(req.request.body.expectedVersion).toBe(4);
    req.flush({ isSuccess: true, message: 'تم', data: null });

    await expect(request).resolves.toMatchObject({ isSuccess: true });
  });

  it('uses evidence plus a subject for relink preview and never sends an email field', async () => {
    const request = firstValueFrom(
      api.previewRelink(17, { newSub: 'new-subject', evidenceToken: 'verified-evidence' }),
    );

    const req = httpTesting.expectOne(`${BASE}/users/17/logto-sub/relink/preview`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ newSub: 'new-subject', evidenceToken: 'verified-evidence' });
    expect(req.request.body).not.toHaveProperty('email');
    req.flush({ isSuccess: true, message: 'تم', data: null });

    await expect(request).resolves.toMatchObject({ isSuccess: true });
  });

  it('keeps relink confirmation as a distinct request with version and explicit confirmation', async () => {
    const request = firstValueFrom(
      api.confirmRelink(17, {
        expectedVersion: 4,
        oldSub: 'old-subject',
        newSub: 'new-subject',
        evidenceToken: 'verified-evidence',
        reason: 'تغيير المعرّف',
        confirmed: true,
      }),
    );

    const req = httpTesting.expectOne(`${BASE}/users/17/logto-sub/relink/confirm`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({
      expectedVersion: 4,
      oldSub: 'old-subject',
      newSub: 'new-subject',
      evidenceToken: 'verified-evidence',
      reason: 'تغيير المعرّف',
      confirmed: true,
    });
    req.flush({ isSuccess: true, message: 'تم', data: null });

    await expect(request).resolves.toMatchObject({ isSuccess: true });
  });

  it('sends audit filters and cursor as query values', async () => {
    const request = firstValueFrom(
      api.listAuditEvents({
        targetUserId: 17,
        actorUserId: 9,
        actionType: 'PermissionGranted',
        cursor: 'next-page',
        pageSize: 10,
      }),
    );

    const req = httpTesting.expectOne((candidate) => candidate.url === `${BASE}/audit-events`);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('targetUserId')).toBe('17');
    expect(req.request.params.get('actorUserId')).toBe('9');
    expect(req.request.params.get('actionType')).toBe('PermissionGranted');
    expect(req.request.params.get('cursor')).toBe('next-page');
    expect(req.request.params.get('pageSize')).toBe('10');
    req.flush({ isSuccess: true, message: 'تم', data: null });

    await expect(request).resolves.toMatchObject({ isSuccess: true });
  });
});
