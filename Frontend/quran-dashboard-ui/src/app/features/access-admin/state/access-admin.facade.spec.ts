import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { NEVER, of } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { environment } from '../../../../environments/environment';
import { AccessUserDetail } from '../../../core/api/generated/models/access-user-detail';
import { AccessUserPermissions } from '../../../core/api/generated/models/access-user-permissions';
import { AccessUserSummary } from '../../../core/api/generated/models/access-user-summary';
import { CurrentUserResponse } from '../../../core/api/generated/models/current-user-response';
import { PermissionCatalogueItem } from '../../../core/api/generated/models/permission-catalogue-item';
import { CurrentUserStore } from '../../../core/auth/current-user.store';
import { AccessAdminApi } from '../data-access/access-admin.api';
import { AccessAdminFacade } from './access-admin.facade';

const ACCESS_BASE_URL = `${environment.apiBaseUrl}/api/access`;

const OWNER: CurrentUserResponse = {
  sub: 'owner-subject',
  email: 'owner@example.test',
  displayName: 'المالك',
  status: 'active',
  isOwner: true,
  permissions: [],
};

const CATALOGUE: PermissionCatalogueItem[] = [
  {
    code: 'abwab.doors.create',
    arabicLabel: 'إضافة باب',
    englishDescription: 'Create a door.',
    groupKey: 'doors',
    groupLabel: 'الأبواب',
    groupDisplayOrder: 1,
    displayOrder: 1,
  },
  {
    code: 'abwab.doors.edit',
    arabicLabel: 'تعديل باب',
    englishDescription: 'Edit a door.',
    groupKey: 'doors',
    groupLabel: 'الأبواب',
    groupDisplayOrder: 1,
    displayOrder: 2,
  },
];

function user(
  version: number,
  permissionCodes: string[] = [],
  overrides: Partial<AccessUserDetail> = {},
): AccessUserDetail {
  return {
    id: 17,
    sub: 'subject-17',
    email: 'member@example.test',
    normalizedEmail: 'member@example.test',
    userName: null,
    displayName: 'عضو',
    title: null,
    status: 'active',
    isOwner: false,
    permissionCodes,
    createdAtUtc: '2026-01-01T00:00:00Z',
    updatedAtUtc: '2026-01-01T00:00:00Z',
    version,
    ...overrides,
  };
}

function permissions(
  version: number,
  permissionCodes: string[] = [],
  overrides: Partial<AccessUserPermissions> = {},
): AccessUserPermissions {
  return {
    userId: 17,
    status: 'active',
    isOwner: false,
    version,
    permissionCodes,
    ...overrides,
  };
}

function summary(detail: AccessUserDetail): AccessUserSummary {
  return {
    id: detail.id,
    email: detail.email,
    displayName: detail.displayName,
    status: detail.status,
    isOwner: detail.isOwner,
    permissionCount: detail.permissionCodes.length,
    createdAtUtc: detail.createdAtUtc,
    updatedAtUtc: detail.updatedAtUtc,
    version: detail.version,
  };
}

function success<T>(data: T) {
  return { isSuccess: true, message: 'تم', data };
}

describe('AccessAdminFacade', () => {
  let facade: AccessAdminFacade;
  let currentUserStore: CurrentUserStore;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        AccessAdminFacade,
        AccessAdminApi,
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: OidcSecurityService,
          useValue: {
            isAuthenticated$: NEVER,
            getIdToken: () => of('signed.id.token'),
            authorize: vi.fn(),
          },
        },
      ],
    });

    facade = TestBed.inject(AccessAdminFacade);
    currentUserStore = TestBed.inject(CurrentUserStore);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  async function loadCurrentUser(currentUser: CurrentUserResponse): Promise<void> {
    const load = currentUserStore.refresh();
    httpTesting.expectOne(`${ACCESS_BASE_URL}/me`).flush(success(currentUser));
    await load;
  }

  async function loadCatalogue(): Promise<void> {
    const load = facade.loadPermissionCatalogue();
    httpTesting
      .expectOne(`${ACCESS_BASE_URL}/permissions`)
      .flush(success({ items: CATALOGUE, assignmentReady: true }));
    await load;
  }

  async function selectTarget(
    detail: AccessUserDetail,
    permissionSnapshot: AccessUserPermissions,
  ): Promise<void> {
    const selection = facade.selectUser(detail.id);
    httpTesting.expectOne(`${ACCESS_BASE_URL}/users/${detail.id}`).flush(success(detail));
    httpTesting
      .expectOne(`${ACCESS_BASE_URL}/users/${detail.id}/permissions`)
      .flush(success(permissionSnapshot));
    await selection;
  }

  async function flushMutationRefresh(
    detail: AccessUserDetail,
    permissionSnapshot: AccessUserPermissions,
    assignmentReady = true,
  ): Promise<void> {
    await new Promise((resolve) => setTimeout(resolve, 0));
    httpTesting.expectOne(`${ACCESS_BASE_URL}/users/${detail.id}`).flush(success(detail));
    httpTesting
      .expectOne(`${ACCESS_BASE_URL}/users/${detail.id}/permissions`)
      .flush(success(permissionSnapshot));
    httpTesting
      .expectOne((request) => request.url === `${ACCESS_BASE_URL}/users`)
      .flush(success({ items: [summary(detail)], page: 1, pageSize: 25, totalCount: 1 }));
    httpTesting
      .expectOne(`${ACCESS_BASE_URL}/permissions`)
      .flush(success({ items: CATALOGUE, assignmentReady }));
    httpTesting
      .expectOne((request) => request.url === `${ACCESS_BASE_URL}/audit-events`)
      .flush(success({ items: [], nextCursor: null }));
  }

  it.each([
    [
      'a pending account',
      { ...OWNER, status: 'pending', isOwner: false } satisfies CurrentUserResponse,
    ],
    [
      'a disabled Owner',
      { ...OWNER, status: 'disabled' } satisfies CurrentUserResponse,
    ],
    [
      'an active non-owner',
      { ...OWNER, isOwner: false } satisfies CurrentUserResponse,
    ],
  ])('does not request security-admin resources for %s', async (_scenario, currentUser) => {
    await loadCurrentUser(currentUser);

    await facade.load();

    expect(facade.canAccess()).toBe(false);
    httpTesting.expectNone((request) => request.url === `${ACCESS_BASE_URL}/users`);
    httpTesting.expectNone(`${ACCESS_BASE_URL}/permissions`);
    httpTesting.expectNone((request) => request.url === `${ACCESS_BASE_URL}/audit-events`);
    httpTesting.expectNone(`${ACCESS_BASE_URL}/owner-reconciliation/status`);
  });

  it.each([[true], [false]])(
    'reads the catalogue items and the assignmentReady flag (%s) off the response envelope',
    async (assignmentReady) => {
      await loadCurrentUser(OWNER);

      const load = facade.loadPermissionCatalogue();
      httpTesting
        .expectOne(`${ACCESS_BASE_URL}/permissions`)
        .flush(success({ items: CATALOGUE, assignmentReady }));
      await load;

      expect(facade.assignmentReady()).toBe(assignmentReady);
      expect(facade.permissionGroups().flatMap((group) => [...group.codes])).toEqual(
        CATALOGUE.map((item) => item.code),
      );
    },
  );

  it('looks accounts up by free text for the audit pickers without touching the listed page', async () => {
    await loadCurrentUser(OWNER);
    const listed = user(4);
    const before = facade.users();

    const lookup = facade.findUsers('عضو');
    const request = httpTesting.expectOne(
      (candidate) =>
        candidate.url === `${ACCESS_BASE_URL}/users` && candidate.params.get('search') === 'عضو',
    );
    expect(request.request.params.get('pageSize')).toBe('10');
    request.flush(success({ items: [summary(listed)], page: 1, pageSize: 10, totalCount: 1 }));

    expect(await lookup).toEqual({ users: [summary(listed)], error: null, loading: false });
    expect(facade.users()).toBe(before);
  });

  it('reports a failed account lookup rather than reading as no matches', async () => {
    await loadCurrentUser(OWNER);

    const lookup = facade.findUsers('عضو');
    httpTesting
      .expectOne((candidate) => candidate.url === `${ACCESS_BASE_URL}/users`)
      .flush(
        { isSuccess: false, message: 'تعذر البحث عن الحسابات.', data: null },
        { status: 500, statusText: 'Server Error' },
      );

    expect(await lookup).toEqual({
      users: [],
      error: 'تعذر البحث عن الحسابات.',
      loading: false,
    });
  });

  it('refreshes the target state after a version conflict without retrying or retaining attempted grants', async () => {
    await loadCurrentUser(OWNER);
    await loadCatalogue();
    await selectTarget(
      user(1, ['abwab.doors.create']),
      permissions(1, ['abwab.doors.create']),
    );
    facade.setSelectedPermissionCodes(new Set(['abwab.doors.create', 'abwab.doors.edit']));

    const replacement = facade.replaceSelectedPermissions('تحديث الصلاحيات');
    const request = httpTesting.expectOne(`${ACCESS_BASE_URL}/users/17/permissions`);
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({
      expectedVersion: 1,
      permissionCodes: ['abwab.doors.create', 'abwab.doors.edit'],
      reason: 'تحديث الصلاحيات',
    });
    request.flush(
      { isSuccess: false, message: 'تغيرت بيانات المستخدم', data: null },
      { status: 409, statusText: 'Conflict' },
    );
    await new Promise((resolve) => setTimeout(resolve, 0));
    httpTesting
      .expectOne(`${ACCESS_BASE_URL}/users/17`)
      .flush(success(user(2, ['abwab.doors.edit'])));
    httpTesting
      .expectOne(`${ACCESS_BASE_URL}/users/17/permissions`)
      .flush(success(permissions(2, ['abwab.doors.edit'])));

    await expect(replacement).resolves.toBe('conflict');
    expect(facade.selectedUser()?.version).toBe(2);
    expect([...facade.selectedPermissionCodes()]).toEqual(['abwab.doors.edit']);
    httpTesting.expectNone(`${ACCESS_BASE_URL}/users/17/permissions`);
  });

  it('keeps fetched individual grants while the permission catalogue is still loading', async () => {
    await loadCurrentUser(OWNER);
    await selectTarget(
      user(1, ['abwab.doors.create']),
      permissions(1, ['abwab.doors.create']),
    );

    expect([...facade.selectedPermissionCodes()]).toEqual(['abwab.doors.create']);

    await loadCatalogue();

    expect([...facade.selectedPermissionCodes()]).toEqual(['abwab.doors.create']);
  });

  it('submits only current individual permission codes when accepting a pending user', async () => {
    await loadCurrentUser(OWNER);
    await loadCatalogue();
    const pendingUser = user(1, [], { status: 'pending' });
    await selectTarget(
      pendingUser,
      permissions(1, [], { status: 'pending' }),
    );
    facade.setSelectedPermissionCodes(
      new Set([
        'abwab.doors.create',
        'abwab.doors.edit',
        'abwab.doors.manage-all',
      ]),
    );

    const acceptance = facade.acceptSelectedUser('قبول الحساب');
    const request = httpTesting.expectOne(`${ACCESS_BASE_URL}/users/17/accept`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      expectedVersion: 1,
      permissionCodes: ['abwab.doors.create', 'abwab.doors.edit'],
      reason: 'قبول الحساب',
    });
    const activeUser = user(2, ['abwab.doors.create', 'abwab.doors.edit']);
    const activePermissions = permissions(2, ['abwab.doors.create', 'abwab.doors.edit']);
    request.flush(success(activeUser));
    await flushMutationRefresh(activeUser, activePermissions);

    await expect(acceptance).resolves.toBe('success');
    expect(facade.selectedUser()?.status).toBe('active');
    expect([...facade.selectedPermissionCodes()]).toEqual([
      'abwab.doors.create',
      'abwab.doors.edit',
    ]);
  });

  it('keeps preview evidence for the separate relink confirmation HTTP request', async () => {
    await loadCurrentUser(OWNER);
    await selectTarget(user(1), permissions(1));

    const preview = facade.previewSelectedUserRelink({
      newSub: 'replacement-subject',
      evidenceToken: 'verified-evidence',
    });
    const previewRequest = httpTesting.expectOne(
      `${ACCESS_BASE_URL}/users/17/logto-sub/relink/preview`,
    );
    expect(previewRequest.request.body).toEqual({
      newSub: 'replacement-subject',
      evidenceToken: 'verified-evidence',
    });
    previewRequest.flush(
      success({
        userId: 17,
        oldSub: 'subject-17',
        newSub: 'replacement-subject',
        version: 4,
        isOwner: false,
      }),
    );
    await expect(preview).resolves.toBe('success');

    const confirmation = facade.confirmSelectedUserRelink('تصحيح معرّف الدخول');
    const confirmRequest = httpTesting.expectOne(
      `${ACCESS_BASE_URL}/users/17/logto-sub/relink/confirm`,
    );
    expect(confirmRequest.request.body).toEqual({
      expectedVersion: 4,
      oldSub: 'subject-17',
      newSub: 'replacement-subject',
      evidenceToken: 'verified-evidence',
      reason: 'تصحيح معرّف الدخول',
      confirmed: true,
    });
    const relinkedUser = user(5, [], { sub: 'replacement-subject' });
    confirmRequest.flush(success(relinkedUser));
    await flushMutationRefresh(relinkedUser, permissions(5));

    await expect(confirmation).resolves.toBe('success');
    expect(facade.relinkPreview()).toBeNull();
    expect(facade.selectedUser()?.sub).toBe('replacement-subject');
  });

  it('does not publish a relink preview after a different target is selected', async () => {
    await loadCurrentUser(OWNER);
    await selectTarget(user(1), permissions(1));

    const preview = facade.previewSelectedUserRelink({
      newSub: 'replacement-subject',
      evidenceToken: 'verified-evidence',
    });
    const deferredPreview = httpTesting.expectOne(
      `${ACCESS_BASE_URL}/users/17/logto-sub/relink/preview`,
    );

    const secondUser = user(2, [], {
      id: 18,
      sub: 'subject-18',
      email: 'second@example.test',
      normalizedEmail: 'second@example.test',
    });
    await selectTarget(secondUser, permissions(2, [], { userId: 18 }));

    deferredPreview.flush(
      success({
        userId: 17,
        oldSub: 'subject-17',
        newSub: 'replacement-subject',
        version: 4,
        isOwner: false,
      }),
    );
    await preview;

    expect(facade.selectedUser()?.id).toBe(18);
    expect(facade.relinkPreview()).toBeNull();
    await expect(facade.confirmSelectedUserRelink('تصحيح معرّف الدخول')).resolves.toBe('invalid');
    httpTesting.expectNone(`${ACCESS_BASE_URL}/users/18/logto-sub/relink/confirm`);
  });

  it('clears retained relink evidence when the preview is canceled', async () => {
    await loadCurrentUser(OWNER);
    await selectTarget(user(1), permissions(1));

    const preview = facade.previewSelectedUserRelink({
      newSub: 'replacement-subject',
      evidenceToken: 'verified-evidence',
    });
    httpTesting
      .expectOne(`${ACCESS_BASE_URL}/users/17/logto-sub/relink/preview`)
      .flush(
        success({
          userId: 17,
          oldSub: 'subject-17',
          newSub: 'replacement-subject',
          version: 4,
          isOwner: false,
        }),
      );
    await preview;

    facade.cancelSelectedUserRelink();

    expect(facade.relinkPreview()).toBeNull();
    await expect(facade.confirmSelectedUserRelink('إلغاء المعاينة')).resolves.toBe('invalid');
    httpTesting.expectNone(`${ACCESS_BASE_URL}/users/17/logto-sub/relink/confirm`);
  });

  it('does not preview a relink after the Owner access snapshot becomes stale', async () => {
    await loadCurrentUser(OWNER);
    await selectTarget(user(1), permissions(1));
    await loadCurrentUser({ ...OWNER, isOwner: false });

    await expect(
      facade.previewSelectedUserRelink({
        newSub: 'replacement-subject',
        evidenceToken: 'evidence',
      }),
    ).resolves.toBe('invalid');

    expect(facade.canAccess()).toBe(false);
    httpTesting.expectNone(`${ACCESS_BASE_URL}/users/17/logto-sub/relink/preview`);
  });

  it.each([
    ['an unready catalogue', { items: CATALOGUE, assignmentReady: false }],
    ['an empty catalogue reported as ready', { items: [], assignmentReady: true }],
  ])('withholds permission assignment over %s', async (_scenario, payload) => {
    await loadCurrentUser(OWNER);
    const load = facade.loadPermissionCatalogue();
    httpTesting.expectOne(`${ACCESS_BASE_URL}/permissions`).flush(success(payload));
    await load;
    await selectTarget(
      user(1, ['abwab.doors.create']),
      permissions(1, ['abwab.doors.create']),
    );

    expect(facade.canAssignPermissions()).toBe(false);
    expect(facade.permissionDiff()).toEqual({ granted: [], revoked: [] });
    expect(facade.isDirty()).toBe(false);
    facade.setSelectedPermissionCodes(new Set(['abwab.doors.edit']));
    expect([...facade.selectedPermissionCodes()]).toEqual(['abwab.doors.create']);
    await expect(facade.replaceSelectedPermissions('تحديث الصلاحيات')).resolves.toBe('invalid');

    httpTesting.expectNone(`${ACCESS_BASE_URL}/users/17/permissions`);
  });

  it('withholds permission assignment from a failed catalogue refresh until a reload succeeds', async () => {
    await loadCurrentUser(OWNER);
    await loadCatalogue();
    await selectTarget(
      user(1, ['abwab.doors.create']),
      permissions(1, ['abwab.doors.create']),
    );
    expect(facade.canAssignPermissions()).toBe(true);

    const retry = facade.loadPermissionCatalogue();
    httpTesting
      .expectOne(`${ACCESS_BASE_URL}/permissions`)
      .flush(
        { isSuccess: false, message: 'تعذر تحميل كتالوج الصلاحيات.', data: null },
        { status: 500, statusText: 'Server Error' },
      );
    await retry;

    expect(facade.assignmentReady()).toBe(false);
    expect(facade.catalogueError()).not.toBeNull();
    expect(facade.canAssignPermissions()).toBe(false);
    expect(facade.isDirty()).toBe(false);
    expect([...facade.selectedPermissionCodes()]).toEqual(['abwab.doors.create']);
    await expect(facade.replaceSelectedPermissions('تحديث الصلاحيات')).resolves.toBe('invalid');

    httpTesting.expectNone(`${ACCESS_BASE_URL}/users/17/permissions`);

    const secondRetry = facade.loadPermissionCatalogue();
    const inFlight = httpTesting.expectOne(`${ACCESS_BASE_URL}/permissions`);

    expect(facade.catalogueError()).toBeNull();
    expect(facade.canAssignPermissions()).toBe(false);
    await expect(facade.replaceSelectedPermissions('تحديث الصلاحيات')).resolves.toBe('invalid');

    inFlight.flush(success({ items: CATALOGUE, assignmentReady: true }));
    await secondRetry;

    expect(facade.canAssignPermissions()).toBe(true);
  });

  it('re-evaluates catalogue readiness after every mutation', async () => {
    await loadCurrentUser(OWNER);
    const load = facade.loadPermissionCatalogue();
    httpTesting
      .expectOne(`${ACCESS_BASE_URL}/permissions`)
      .flush(success({ items: CATALOGUE, assignmentReady: false }));
    await load;
    await selectTarget(user(1), permissions(1));
    expect(facade.canAssignPermissions()).toBe(false);

    const disabling = facade.disableSelectedUser('مراجعة الوصول');
    httpTesting
      .expectOne(`${ACCESS_BASE_URL}/users/17/disable`)
      .flush(success(user(2, [], { status: 'disabled' })));
    await flushMutationRefresh(user(2, [], { status: 'disabled' }), permissions(2));

    await expect(disabling).resolves.toBe('success');
    expect(facade.canAssignPermissions()).toBe(true);
  });

  it('drops a retained draft from the accept payload once assignment becomes unavailable', async () => {
    await loadCurrentUser(OWNER);
    await loadCatalogue();
    const pendingUser = user(1, [], { status: 'pending' });
    await selectTarget(pendingUser, permissions(1, [], { status: 'pending' }));
    facade.setSelectedPermissionCodes(new Set(['abwab.doors.create', 'abwab.doors.edit']));

    const unreadyReload = facade.loadPermissionCatalogue();
    httpTesting
      .expectOne(`${ACCESS_BASE_URL}/permissions`)
      .flush(success({ items: CATALOGUE, assignmentReady: false }));
    await unreadyReload;

    expect([...facade.selectedPermissionCodes()]).toEqual([
      'abwab.doors.create',
      'abwab.doors.edit',
    ]);

    const acceptance = facade.acceptSelectedUser('قبول الحساب');
    const request = httpTesting.expectOne(`${ACCESS_BASE_URL}/users/17/accept`);
    expect(request.request.body).toEqual({
      expectedVersion: 1,
      permissionCodes: [],
      reason: 'قبول الحساب',
    });
    const activeUser = user(2);
    request.flush(success(activeUser));
    await flushMutationRefresh(activeUser, permissions(2), false);

    await expect(acceptance).resolves.toBe('success');
  });

  it('carries a draft code the catalogue no longer offers into the diff and into the saved set', async () => {
    await loadCurrentUser(OWNER);
    await loadCatalogue();
    await selectTarget(
      user(1, ['abwab.doors.create']),
      permissions(1, ['abwab.doors.create']),
    );

    facade.setSelectedPermissionCodes(new Set(['abwab.doors.create', 'abwab.sections.create']));

    expect(CATALOGUE.map((item) => item.code)).not.toContain('abwab.sections.create');
    expect([...facade.selectedPermissionCodes()]).toEqual([
      'abwab.doors.create',
      'abwab.sections.create',
    ]);
    expect(facade.permissionDiff()).toEqual({
      granted: ['abwab.sections.create'],
      revoked: [],
    });
    expect(facade.isDirty()).toBe(true);

    const saving = facade.replaceSelectedPermissions('تحديث الصلاحيات');
    const request = httpTesting.expectOne(`${ACCESS_BASE_URL}/users/17/permissions`);
    expect(request.request.body).toEqual({
      expectedVersion: 1,
      permissionCodes: ['abwab.doors.create', 'abwab.sections.create'],
      reason: 'تحديث الصلاحيات',
    });

    const saved = user(2, ['abwab.doors.create', 'abwab.sections.create']);
    request.flush(success(permissions(2, ['abwab.doors.create', 'abwab.sections.create'])));
    await flushMutationRefresh(saved, permissions(2, ['abwab.doors.create', 'abwab.sections.create']));

    await expect(saving).resolves.toBe('success');
  });

  it('keeps a granted code this build does not model out of the diff but inside the saved set', async () => {
    await loadCurrentUser(OWNER);
    await loadCatalogue();
    const granted = ['abwab.doors.create', 'abwab.doors.publish'];
    await selectTarget(user(1, granted), permissions(1, granted));

    expect(facade.permissionDiff()).toEqual({ granted: [], revoked: [] });
    expect(facade.isDirty()).toBe(false);

    facade.setSelectedPermissionCodes(new Set(['abwab.doors.create', 'abwab.doors.edit']));

    expect(facade.permissionDiff()).toEqual({ granted: ['abwab.doors.edit'], revoked: [] });

    const saving = facade.replaceSelectedPermissions('تحديث الصلاحيات');
    const request = httpTesting.expectOne(`${ACCESS_BASE_URL}/users/17/permissions`);
    expect(request.request.body).toEqual({
      expectedVersion: 1,
      permissionCodes: ['abwab.doors.create', 'abwab.doors.edit', 'abwab.doors.publish'],
      reason: 'تحديث الصلاحيات',
    });

    const saved = user(2, [...granted, 'abwab.doors.edit']);
    request.flush(success(permissions(2, [...granted, 'abwab.doors.edit'])));
    await flushMutationRefresh(saved, permissions(2, [...granted, 'abwab.doors.edit']));

    await expect(saving).resolves.toBe('success');
  });

  it('stops reading a draft as unsaved while a failed refresh withholds assignment, and resumes on recovery', async () => {
    await loadCurrentUser(OWNER);
    await loadCatalogue();
    await selectTarget(
      user(1, ['abwab.doors.create']),
      permissions(1, ['abwab.doors.create']),
    );
    facade.setSelectedPermissionCodes(new Set(['abwab.doors.edit']));
    expect(facade.isDirty()).toBe(true);

    const failedRetry = facade.loadPermissionCatalogue();
    httpTesting
      .expectOne(`${ACCESS_BASE_URL}/permissions`)
      .flush(
        { isSuccess: false, message: 'تعذر تحميل كتالوج الصلاحيات.', data: null },
        { status: 500, statusText: 'Server Error' },
      );
    await failedRetry;

    expect(facade.isDirty()).toBe(false);
    expect([...facade.selectedPermissionCodes()]).toEqual(['abwab.doors.edit']);

    const recovery = facade.loadPermissionCatalogue();
    httpTesting
      .expectOne(`${ACCESS_BASE_URL}/permissions`)
      .flush(success({ items: CATALOGUE, assignmentReady: true }));
    await recovery;

    expect(facade.isDirty()).toBe(true);
  });

  it('keeps a granted code that the catalogue no longer offers out of the pending revocations', async () => {
    await loadCurrentUser(OWNER);
    await loadCatalogue();
    await selectTarget(
      user(1, ['abwab.doors.create', 'abwab.sections.create']),
      permissions(1, ['abwab.doors.create', 'abwab.sections.create']),
    );

    expect(facade.permissionDiff()).toEqual({ granted: [], revoked: [] });
    expect(facade.isDirty()).toBe(false);
  });

  it('derives dirty state from the request body it would send, not from the raw selection', async () => {
    await loadCurrentUser(OWNER);
    await loadCatalogue();
    await selectTarget(
      user(1, ['abwab.doors.create']),
      permissions(1, ['abwab.doors.create']),
    );

    expect(facade.isDirty()).toBe(false);

    facade.setSelectedPermissionCodes(new Set(['abwab.doors.create', 'doors.manage-all']));

    expect(facade.isDirty()).toBe(false);

    facade.setSelectedPermissionCodes(new Set(['abwab.doors.edit']));

    expect(facade.isDirty()).toBe(true);
    expect(facade.permissionDiff()).toEqual({
      granted: ['abwab.doors.edit'],
      revoked: ['abwab.doors.create'],
    });
  });

  it('restores the stored grants when a draft is discarded', async () => {
    await loadCurrentUser(OWNER);
    await loadCatalogue();
    await selectTarget(
      user(1, ['abwab.doors.create']),
      permissions(1, ['abwab.doors.create']),
    );
    facade.setSelectedPermissionCodes(new Set(['abwab.doors.edit']));

    facade.discardDraft();

    expect([...facade.selectedPermissionCodes()]).toEqual(['abwab.doors.create']);
    expect(facade.isDirty()).toBe(false);
    httpTesting.expectNone((request) => request.url.startsWith(ACCESS_BASE_URL));
  });

  it('does not submit a permission replacement without a confirmation reason', async () => {
    await loadCurrentUser(OWNER);
    await loadCatalogue();
    await selectTarget(user(1), permissions(1));

    await expect(facade.replaceSelectedPermissions('   ')).resolves.toBe('invalid');

    httpTesting.expectNone(`${ACCESS_BASE_URL}/users/17/permissions`);
  });
});
