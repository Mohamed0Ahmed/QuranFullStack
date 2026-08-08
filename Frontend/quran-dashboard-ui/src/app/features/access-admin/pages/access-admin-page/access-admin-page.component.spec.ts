import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { NEVER, of } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { environment } from '../../../../../environments/environment';
import { AccessAuditEventItem } from '../../../../core/api/generated/models/access-audit-event-item';
import { AccessUserDetail } from '../../../../core/api/generated/models/access-user-detail';
import { AccessUserPermissions } from '../../../../core/api/generated/models/access-user-permissions';
import { AccessUserSummary } from '../../../../core/api/generated/models/access-user-summary';
import { CurrentUserResponse } from '../../../../core/api/generated/models/current-user-response';
import { PermissionCatalogueItem } from '../../../../core/api/generated/models/permission-catalogue-item';
import { CurrentUserStore } from '../../../../core/auth/current-user.store';
import { AccessAdminApi } from '../../data-access/access-admin.api';
import { AccessAdminPageComponent } from './access-admin-page.component';

const ACCESS_BASE_URL = `${environment.apiBaseUrl}/api/access`;
const REASON = 'سبب إداري موثق';
const CATALOGUE_ERROR = 'تعذر تحميل كتالوج الصلاحيات.';

type CatalogueOutcome =
  | {
      readonly kind: 'served';
      readonly assignmentReady: boolean;
      readonly items?: readonly PermissionCatalogueItem[];
    }
  | { readonly kind: 'failure' };

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

const AUDIT_EVENTS: readonly AccessAuditEventItem[] = [
  {
    id: 1,
    occurredAtUtc: '2026-08-07T10:00:00Z',
    actionType: 'PermissionGranted',
    actorType: 'User',
    actorUserId: 9,
    targetUserId: 17,
    actorSnapshot: {},
    targetSnapshot: {},
    permissionCode: 'abwab.doors.edit',
    beforeState: {},
    afterState: {},
    reason: 'تكليف مراجعة',
    metadata: {},
  },
  {
    id: 2,
    occurredAtUtc: '2026-08-07T09:00:00Z',
    actionType: 'OwnerReconciled',
    actorType: 'System',
    actorUserId: null,
    targetUserId: 17,
    actorSnapshot: {},
    targetSnapshot: {},
    permissionCode: null,
    beforeState: {},
    afterState: {},
    reason: null,
    metadata: {},
  },
];

function user(
  status: 'pending' | 'active' | 'disabled',
  version = 4,
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
    status,
    isOwner: false,
    permissionCodes,
    createdAtUtc: '2026-01-01T00:00:00Z',
    updatedAtUtc: '2026-01-01T00:00:00Z',
    version,
    ...overrides,
  };
}

function permissions(detail: AccessUserDetail): AccessUserPermissions {
  return {
    userId: detail.id,
    status: detail.status,
    isOwner: detail.isOwner,
    version: detail.version,
    permissionCodes: detail.permissionCodes,
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

function element(
  fixture: ComponentFixture<AccessAdminPageComponent>,
  testId: string,
): HTMLElement {
  const found = fixture.nativeElement.querySelector(
    `[data-testid="${testId}"]`,
  ) as HTMLElement | null;
  if (!found) {
    throw new Error(`Missing ${testId}`);
  }
  return found;
}

describe('AccessAdminPageComponent', () => {
  let currentUserStore: CurrentUserStore;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [AccessAdminPageComponent],
      providers: [
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

    currentUserStore = TestBed.inject(CurrentUserStore);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  async function loadOwner(): Promise<void> {
    const load = currentUserStore.refresh();
    httpTesting.expectOne(`${ACCESS_BASE_URL}/me`).flush(success(OWNER));
    await load;
  }

  function flushCatalogue(outcome: CatalogueOutcome): void {
    const request = httpTesting.expectOne(`${ACCESS_BASE_URL}/permissions`);
    if (outcome.kind === 'failure') {
      request.flush(
        { isSuccess: false, message: CATALOGUE_ERROR, data: null },
        { status: 500, statusText: 'Server Error' },
      );
      return;
    }
    request.flush(
      success({ items: outcome.items ?? CATALOGUE, assignmentReady: outcome.assignmentReady }),
    );
  }

  function flushWorkspace(
    listedUsers: readonly AccessUserDetail[],
    catalogue: CatalogueOutcome,
  ): void {
    httpTesting
      .expectOne((request) => request.url === `${ACCESS_BASE_URL}/users`)
      .flush(
        success({
          items: listedUsers.map(summary),
          page: 1,
          pageSize: 25,
          totalCount: listedUsers.length,
        }),
      );
    flushCatalogue(catalogue);
    httpTesting
      .expectOne((request) => request.url === `${ACCESS_BASE_URL}/audit-events`)
      .flush(success({ items: AUDIT_EVENTS, nextCursor: null }));
    httpTesting
      .expectOne(`${ACCESS_BASE_URL}/owner-reconciliation/status`)
      .flush(
        success({
          canApply: false,
          candidates: [],
          configurationFingerprint: 'fingerprint',
          isReady: true,
          lastReconciliation: null,
        }),
      );
  }

  async function renderPage(
    listedUser: AccessUserDetail,
    catalogue: CatalogueOutcome = { kind: 'served', assignmentReady: true },
    alsoListed: readonly AccessUserDetail[] = [],
  ): Promise<ComponentFixture<AccessAdminPageComponent>> {
    await loadOwner();
    const fixture = TestBed.createComponent(AccessAdminPageComponent);
    fixture.detectChanges();

    flushWorkspace([listedUser, ...alsoListed], catalogue);
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
  }

  function togglePermission(
    fixture: ComponentFixture<AccessAdminPageComponent>,
    code: string,
    checked: boolean,
  ): void {
    const box = element(fixture, `access-permission-${code}`) as HTMLInputElement;
    box.checked = checked;
    box.dispatchEvent(new Event('change'));
    fixture.detectChanges();
  }

  async function selectUser(
    fixture: ComponentFixture<AccessAdminPageComponent>,
    detail: AccessUserDetail,
  ): Promise<void> {
    element(fixture, `access-user-${detail.id}`).click();
    httpTesting.expectOne(`${ACCESS_BASE_URL}/users/${detail.id}`).flush(success(detail));
    httpTesting
      .expectOne(`${ACCESS_BASE_URL}/users/${detail.id}/permissions`)
      .flush(success(permissions(detail)));
    await fixture.whenStable();
    fixture.detectChanges();
  }

  function confirmAction(
    fixture: ComponentFixture<AccessAdminPageComponent>,
    requestButtonTestId: string,
  ): void {
    element(fixture, requestButtonTestId).click();
    fixture.detectChanges();
    const reason = element(fixture, 'access-action-reason') as HTMLTextAreaElement;
    reason.value = REASON;
    reason.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    element(fixture, 'access-confirm-action').click();
  }

  async function flushMutationRefresh(
    fixture: ComponentFixture<AccessAdminPageComponent>,
    detail: AccessUserDetail,
  ): Promise<void> {
    await new Promise((resolve) => setTimeout(resolve, 0));
    httpTesting.expectOne(`${ACCESS_BASE_URL}/users/${detail.id}`).flush(success(detail));
    httpTesting
      .expectOne(`${ACCESS_BASE_URL}/users/${detail.id}/permissions`)
      .flush(success(permissions(detail)));
    httpTesting
      .expectOne((request) => request.url === `${ACCESS_BASE_URL}/users`)
      .flush(
        success({
          items: [summary(detail)],
          page: 1,
          pageSize: 25,
          totalCount: 1,
        }),
      );
    httpTesting
      .expectOne(`${ACCESS_BASE_URL}/permissions`)
      .flush(success({ items: CATALOGUE, assignmentReady: true }));
    httpTesting
      .expectOne((request) => request.url === `${ACCESS_BASE_URL}/audit-events`)
      .flush(success({ items: AUDIT_EVENTS, nextCursor: null }));
    await new Promise((resolve) => setTimeout(resolve, 0));
    await fixture.whenStable();
    fixture.detectChanges();
  }

  it.each([
    {
      name: 'accept',
      initialStatus: 'pending' as const,
      requestButton: 'access-request-accept',
      endpoint: 'accept',
      finalStatus: 'active' as const,
      expectedBody: {
        expectedVersion: 4,
        permissionCodes: [],
        reason: REASON,
      },
      expectedLabel: 'نشط',
    },
    {
      name: 'disable',
      initialStatus: 'active' as const,
      requestButton: 'access-request-disable',
      endpoint: 'disable',
      finalStatus: 'disabled' as const,
      expectedBody: {
        expectedVersion: 4,
        reason: REASON,
      },
      expectedLabel: 'معطّل',
    },
    {
      name: 'reactivate',
      initialStatus: 'disabled' as const,
      requestButton: 'access-request-reactivate',
      endpoint: 'reactivate',
      finalStatus: 'active' as const,
      expectedBody: {
        expectedVersion: 4,
        reason: REASON,
      },
      expectedLabel: 'نشط',
    },
  ])(
    'sends $name through its HTTP boundary and renders the refreshed lifecycle',
    async ({
      initialStatus,
      requestButton,
      endpoint,
      finalStatus,
      expectedBody,
      expectedLabel,
    }) => {
      const initialUser = user(initialStatus);
      const fixture = await renderPage(initialUser);
      await selectUser(fixture, initialUser);

      confirmAction(fixture, requestButton);

      const request = httpTesting.expectOne(
        `${ACCESS_BASE_URL}/users/17/${endpoint}`,
      );
      expect(request.request.method).toBe('POST');
      expect(request.request.body).toEqual(expectedBody);
      const refreshedUser = user(finalStatus, 5);
      request.flush(success(refreshedUser));
      await flushMutationRefresh(fixture, refreshedUser);

      const labels = Array.from(
        fixture.nativeElement.querySelectorAll(
          '.access-user-workflows__header .qd-badge',
        ) as NodeListOf<HTMLElement>,
        (badge) => badge.textContent?.trim(),
      );
      expect(labels).toEqual([expectedLabel]);
      expect(
        fixture.nativeElement.querySelector('[data-testid="access-action-confirmation"]'),
      ).toBeNull();
    },
  );

  it('submits the user-edited individual permissions and renders the refreshed selection', async () => {
    const initialUser = user('active', 4, ['abwab.doors.create']);
    const fixture = await renderPage(initialUser);
    await selectUser(fixture, initialUser);
    togglePermission(fixture, 'abwab.doors.edit', true);

    confirmAction(fixture, 'access-request-permissions');

    const request = httpTesting.expectOne(`${ACCESS_BASE_URL}/users/17/permissions`);
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({
      expectedVersion: 4,
      permissionCodes: ['abwab.doors.create', 'abwab.doors.edit'],
      reason: REASON,
    });
    const refreshedUser = user('active', 5, [
      'abwab.doors.create',
      'abwab.doors.edit',
    ]);
    request.flush(success(permissions(refreshedUser)));
    await flushMutationRefresh(fixture, refreshedUser);

    expect(
      (
        element(
          fixture,
          'access-permission-abwab.doors.edit',
        ) as HTMLInputElement
      ).checked,
    ).toBe(true);
    expect(
      fixture.nativeElement.querySelector('[data-testid="access-action-confirmation"]'),
    ).toBeNull();
  });

  it('renders relink preview and confirms it through distinct HTTP requests', async () => {
    const initialUser = user('active');
    const fixture = await renderPage(initialUser);
    await selectUser(fixture, initialUser);
    const newSub = element(fixture, 'access-relink-new-sub') as HTMLInputElement;
    newSub.value = 'replacement-subject';
    newSub.dispatchEvent(new Event('input'));
    const evidence = element(fixture, 'access-relink-evidence') as HTMLInputElement;
    evidence.value = 'verified-evidence';
    evidence.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    element(fixture, 'access-relink-preview').click();

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
    await fixture.whenStable();
    fixture.detectChanges();

    expect(evidence.value).toBe('');
    expect(element(fixture, 'access-relink-confirmation').textContent).toContain(
      'replacement-subject',
    );
    const reason = element(
      fixture,
      'access-relink-confirm-reason',
    ) as HTMLTextAreaElement;
    reason.value = 'تصحيح المعرّف';
    reason.dispatchEvent(new Event('input'));
    const confirmed = element(
      fixture,
      'access-relink-confirmed',
    ) as HTMLInputElement;
    confirmed.checked = true;
    confirmed.dispatchEvent(new Event('change'));
    fixture.detectChanges();
    element(fixture, 'access-relink-confirm').click();

    const confirmRequest = httpTesting.expectOne(
      `${ACCESS_BASE_URL}/users/17/logto-sub/relink/confirm`,
    );
    expect(confirmRequest.request.body).toEqual({
      expectedVersion: 4,
      oldSub: 'subject-17',
      newSub: 'replacement-subject',
      evidenceToken: 'verified-evidence',
      reason: 'تصحيح المعرّف',
      confirmed: true,
    });
    const refreshedUser = user('active', 5, [], {
      sub: 'replacement-subject',
    });
    confirmRequest.flush(success(refreshedUser));
    await flushMutationRefresh(fixture, refreshedUser);

    expect(
      fixture.nativeElement.querySelector('[data-testid="access-relink-confirmation"]'),
    ).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('الإصدار 5');
  });

  it('removes a canceled relink preview without issuing confirmation', async () => {
    const initialUser = user('active');
    const fixture = await renderPage(initialUser);
    await selectUser(fixture, initialUser);
    const newSub = element(fixture, 'access-relink-new-sub') as HTMLInputElement;
    newSub.value = 'replacement-subject';
    newSub.dispatchEvent(new Event('input'));
    const evidence = element(fixture, 'access-relink-evidence') as HTMLInputElement;
    evidence.value = 'verified-evidence';
    evidence.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    element(fixture, 'access-relink-preview').click();
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
    await fixture.whenStable();
    fixture.detectChanges();

    element(fixture, 'access-relink-cancel').click();
    fixture.detectChanges();

    expect(
      fixture.nativeElement.querySelector('[data-testid="access-relink-confirmation"]'),
    ).toBeNull();
    expect(newSub.value).toBe('');
    expect(evidence.value).toBe('');
    httpTesting.expectNone(`${ACCESS_BASE_URL}/users/17/logto-sub/relink/confirm`);
  });

  it('renders actor attribution and applies the actor filter through HTTP', async () => {
    const fixture = await renderPage(user('active'));
    const root = fixture.nativeElement as HTMLElement;

    expect(root.textContent).toContain('المنفّذ: مستخدم');
    expect(root.textContent).toContain('معرّف المستخدم المنفّذ: 9');
    expect(root.textContent).toContain('المنفّذ: النظام');
    expect(root.textContent).toMatch(/معرّف المستخدم المنفّذ:\s*غير متاح/);

    const actor = element(fixture, 'access-audit-actor') as HTMLInputElement;
    actor.value = '9';
    actor.dispatchEvent(new Event('input'));
    root
      .querySelector('.access-admin-page__audit-filters')
      ?.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));

    const request = httpTesting.expectOne(
      (candidate) => candidate.url === `${ACCESS_BASE_URL}/audit-events`,
    );
    expect(request.request.params.get('actorUserId')).toBe('9');
    expect(request.request.params.has('targetUserId')).toBe(false);
    request.flush(
      success({
        items: [{ ...AUDIT_EVENTS[0], id: 3, reason: 'نتيجة المرشح' }],
        nextCursor: null,
      }),
    );
    await fixture.whenStable();
    fixture.detectChanges();

    expect(root.textContent).toContain('نتيجة المرشح');
  });

  it('degrades only the permission region when the catalogue request fails, then recovers on retry', async () => {
    const activeUser = user('active', 4, ['abwab.doors.create']);
    const fixture = await renderPage(activeUser, { kind: 'failure' });
    await selectUser(fixture, activeUser);
    const root = fixture.nativeElement as HTMLElement;

    expect(root.textContent).toContain('عضو');
    expect(root.textContent).toContain('member@example.test');
    expect(
      Array.from(
        root.querySelectorAll('.access-user-workflows__header .qd-badge') as NodeListOf<HTMLElement>,
        (badge) => badge.textContent?.trim(),
      ),
    ).toEqual(['نشط']);
    expect(element(fixture, 'access-request-disable')).toBeTruthy();
    expect(element(fixture, 'access-relink-new-sub')).toBeTruthy();

    const region = element(fixture, 'access-permissions-section');
    expect(region.textContent).toContain(CATALOGUE_ERROR);
    expect(region.querySelector('qd-access-permission-editor')).toBeNull();
    expect(root.querySelector('[data-testid="access-request-permissions"]')).toBeNull();

    (region.querySelector('[data-testid="qd-state-action"]') as HTMLButtonElement).click();
    flushCatalogue({ kind: 'served', assignmentReady: true });
    await fixture.whenStable();
    fixture.detectChanges();

    expect(element(fixture, 'access-permissions-section').textContent).not.toContain(CATALOGUE_ERROR);
    expect(
      (element(fixture, 'access-permission-abwab.doors.create') as HTMLInputElement).disabled,
    ).toBe(false);

    togglePermission(fixture, 'abwab.doors.edit', true);

    expect(element(fixture, 'access-request-permissions')).toBeTruthy();
  });

  it.each([
    { name: 'a failed catalogue request', catalogue: { kind: 'failure' } as const },
    {
      name: 'an empty catalogue served as ready',
      catalogue: { kind: 'served', assignmentReady: true, items: [] } as const,
    },
  ])(
    'holds no unsaved permission draft over $name and keeps relink reachable',
    async ({ catalogue }) => {
      const activeUser = user('active', 4, ['abwab.doors.create']);
      const fixture = await renderPage(activeUser, catalogue);
      await selectUser(fixture, activeUser);
      const root = fixture.nativeElement as HTMLElement;

      expect(fixture.componentInstance.hasUnsavedChanges()).toBe(false);
      expect(root.querySelector('[data-testid="access-permission-diff-summary"]')).toBeNull();
      expect(root.querySelector('[data-testid="access-permission-draft-bar"]')).toBeNull();
      expect(root.querySelector('[data-testid="access-relink-blocked"]')).toBeNull();

      const newSub = element(fixture, 'access-relink-new-sub') as HTMLInputElement;
      newSub.value = 'replacement-subject';
      newSub.dispatchEvent(new Event('input'));
      const evidence = element(fixture, 'access-relink-evidence') as HTMLInputElement;
      evidence.value = 'verified-evidence';
      evidence.dispatchEvent(new Event('input'));
      fixture.detectChanges();

      expect((element(fixture, 'access-relink-preview') as HTMLButtonElement).disabled).toBe(false);
      expect(httpTesting.match((request) => request.method === 'PUT')).toEqual([]);
    },
  );

  it.each([
    { name: 'an unready catalogue', catalogue: { kind: 'served', assignmentReady: false } as const },
    {
      name: 'an empty catalogue reported as ready',
      catalogue: { kind: 'served', assignmentReady: true, items: [] } as const,
    },
  ])('fails closed over $name and offers no permission write path', async ({ catalogue }) => {
    const activeUser = user('active', 4, ['abwab.doors.create']);
    const fixture = await renderPage(activeUser, catalogue);
    await selectUser(fixture, activeUser);
    const root = fixture.nativeElement as HTMLElement;

    expect(element(fixture, 'access-permissions-unavailable').textContent).toContain(
      'إسناد الصلاحيات غير متاح مؤقتًا',
    );
    expect(root.querySelector('[data-testid="access-request-permissions"]')).toBeNull();
    expect(element(fixture, 'access-request-disable')).toBeTruthy();
    expect(httpTesting.match((request) => request.method === 'PUT')).toEqual([]);
  });

  it('keeps the editor readable but read-only while assignment is unavailable', async () => {
    const activeUser = user('active', 4, ['abwab.doors.create']);
    const fixture = await renderPage(activeUser, { kind: 'served', assignmentReady: false });
    await selectUser(fixture, activeUser);

    const granted = element(fixture, 'access-permission-abwab.doors.create') as HTMLInputElement;
    expect(granted.checked).toBe(true);
    expect(granted.disabled).toBe(true);
    const ungranted = element(fixture, 'access-permission-abwab.doors.edit') as HTMLInputElement;
    expect(ungranted.checked).toBe(false);
    expect(ungranted.disabled).toBe(true);

    ungranted.checked = true;
    ungranted.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(httpTesting.match((request) => request.method === 'PUT')).toEqual([]);
  });

  it('accepts a pending user without a permission payload while assignment is unavailable', async () => {
    const pendingUser = user('pending');
    const fixture = await renderPage(pendingUser, { kind: 'served', assignmentReady: false });
    await selectUser(fixture, pendingUser);

    expect(element(fixture, 'access-permissions-unavailable')).toBeTruthy();
    confirmAction(fixture, 'access-request-accept');

    const request = httpTesting.expectOne(`${ACCESS_BASE_URL}/users/17/accept`);
    expect(request.request.body).toEqual({
      expectedVersion: 4,
      permissionCodes: [],
      reason: REASON,
    });
    const activeUser = user('active', 5);
    request.flush(success(activeUser));
    await flushMutationRefresh(fixture, activeUser);

    expect(httpTesting.match((candidate) => candidate.method === 'PUT')).toEqual([]);
  });

  it('reports a completed change without an error state', async () => {
    const initialUser = user('active');
    const fixture = await renderPage(initialUser);
    await selectUser(fixture, initialUser);

    confirmAction(fixture, 'access-request-disable');
    const disabledUser = user('disabled', 5);
    httpTesting.expectOne(`${ACCESS_BASE_URL}/users/17/disable`).flush(success(disabledUser));
    await flushMutationRefresh(fixture, disabledUser);

    expect(element(fixture, 'access-mutation-message-success').textContent).toContain('تم حفظ التغيير');
    expect(
      (fixture.nativeElement as HTMLElement).querySelector('[data-testid="qd-state-error"]'),
    ).toBeNull();
  });

  it('reports a version conflict as a recovery notice rather than an error', async () => {
    const initialUser = user('active', 4, ['abwab.doors.create']);
    const fixture = await renderPage(initialUser);
    await selectUser(fixture, initialUser);
    togglePermission(fixture, 'abwab.doors.edit', true);

    confirmAction(fixture, 'access-request-permissions');
    httpTesting
      .expectOne(`${ACCESS_BASE_URL}/users/17/permissions`)
      .flush(
        { isSuccess: false, message: 'تغيرت بيانات المستخدم', data: null },
        { status: 409, statusText: 'Conflict' },
      );
    await new Promise((resolve) => setTimeout(resolve, 0));
    const refreshedUser = user('active', 5, ['abwab.doors.edit']);
    httpTesting.expectOne(`${ACCESS_BASE_URL}/users/17`).flush(success(refreshedUser));
    httpTesting
      .expectOne(`${ACCESS_BASE_URL}/users/17/permissions`)
      .flush(success(permissions(refreshedUser)));
    await new Promise((resolve) => setTimeout(resolve, 0));
    await fixture.whenStable();
    fixture.detectChanges();

    const notice = element(fixture, 'access-mutation-message-notice');
    expect(notice.textContent).toContain('تغيرت بيانات المستخدم');
    expect(notice.querySelector('[data-testid="qd-state-error"]')).toBeNull();
  });

  it('loads the workspace when Owner access resolves after the page is mounted', async () => {
    const listedUser = user('active');
    const load = currentUserStore.refresh();
    const identity = httpTesting.expectOne(`${ACCESS_BASE_URL}/me`);
    const fixture = TestBed.createComponent(AccessAdminPageComponent);
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('[data-testid="qd-state-loading"]')).toBeTruthy();
    expect(root.textContent).not.toContain('لا تملك صلاحية إدارة الوصول');
    httpTesting.expectNone((request) => request.url === `${ACCESS_BASE_URL}/users`);

    identity.flush(success(OWNER));
    await load;
    fixture.detectChanges();

    flushWorkspace([listedUser], { kind: 'served', assignmentReady: true });
    await fixture.whenStable();
    fixture.detectChanges();

    expect(root.textContent).not.toContain('لا تملك صلاحية إدارة الوصول');
    expect(element(fixture, `access-user-${listedUser.id}`)).toBeTruthy();
  });

  it('renders the permission-denied error once the current-user state is known', async () => {
    const load = currentUserStore.refresh();
    httpTesting.expectOne(`${ACCESS_BASE_URL}/me`).flush(success({ ...OWNER, isOwner: false }));
    await load;

    const fixture = TestBed.createComponent(AccessAdminPageComponent);
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain(
      'لا تملك صلاحية إدارة الوصول',
    );
  });

  it('summarises a pending permission change and reverts it without issuing a request', async () => {
    const activeUser = user('active', 4, ['abwab.doors.create']);
    const fixture = await renderPage(activeUser);
    await selectUser(fixture, activeUser);
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('[data-testid="access-permission-draft-bar"]')).toBeNull();
    expect(root.querySelector('[data-testid="access-request-permissions"]')).toBeNull();
    expect(fixture.componentInstance.hasUnsavedChanges()).toBe(false);

    togglePermission(fixture, 'abwab.doors.edit', true);
    togglePermission(fixture, 'abwab.doors.create', false);

    expect(element(fixture, 'access-permission-diff-summary-text').textContent).toBe(
      'صلاحيات مضافة: 1، صلاحيات ملغاة: 1',
    );
    expect(element(fixture, 'access-request-permissions')).toBeTruthy();
    expect(fixture.componentInstance.hasUnsavedChanges()).toBe(true);

    element(fixture, 'access-discard-draft').click();
    fixture.detectChanges();

    expect(
      (element(fixture, 'access-permission-abwab.doors.create') as HTMLInputElement).checked,
    ).toBe(true);
    expect(
      (element(fixture, 'access-permission-abwab.doors.edit') as HTMLInputElement).checked,
    ).toBe(false);
    expect(root.querySelector('[data-testid="access-permission-draft-bar"]')).toBeNull();
    expect(fixture.componentInstance.hasUnsavedChanges()).toBe(false);
    expect(httpTesting.match(() => true)).toEqual([]);
  });

  it('refuses to confirm a permission save that would carry no change', async () => {
    const activeUser = user('active', 4, ['abwab.doors.create']);
    const fixture = await renderPage(activeUser);
    await selectUser(fixture, activeUser);

    togglePermission(fixture, 'abwab.doors.edit', true);
    element(fixture, 'access-request-permissions').click();
    fixture.detectChanges();
    const reason = element(fixture, 'access-action-reason') as HTMLTextAreaElement;
    reason.value = REASON;
    reason.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    expect((element(fixture, 'access-confirm-action') as HTMLButtonElement).disabled).toBe(false);

    element(fixture, 'access-discard-draft').click();
    fixture.detectChanges();

    const confirm = element(fixture, 'access-confirm-action') as HTMLButtonElement;
    expect(confirm.disabled).toBe(true);
    confirm.click();

    httpTesting.expectNone(`${ACCESS_BASE_URL}/users/17/permissions`);
  });

  it('confirms before a user switch discards an unsaved draft, and keeps the draft when declined', async () => {
    const activeUser = user('active', 4, ['abwab.doors.create']);
    const otherUser = user('active', 2, [], {
      id: 18,
      sub: 'subject-18',
      email: 'other@example.test',
      normalizedEmail: 'other@example.test',
      displayName: 'زميل',
    });
    const fixture = await renderPage(activeUser, { kind: 'served', assignmentReady: true }, [
      otherUser,
    ]);
    await selectUser(fixture, activeUser);
    togglePermission(fixture, 'abwab.doors.edit', true);

    element(fixture, `access-user-${otherUser.id}`).click();
    fixture.detectChanges();

    expect(element(fixture, 'access-switch-user-confirm')).toBeTruthy();
    httpTesting.expectNone(`${ACCESS_BASE_URL}/users/${otherUser.id}`);

    element(fixture, 'access-switch-user-confirm-cancel').click();
    fixture.detectChanges();

    expect(
      (fixture.nativeElement as HTMLElement).querySelector(
        '[data-testid="access-switch-user-confirm"]',
      ),
    ).toBeNull();
    expect(
      (element(fixture, 'access-permission-abwab.doors.edit') as HTMLInputElement).checked,
    ).toBe(true);

    element(fixture, `access-user-${otherUser.id}`).click();
    fixture.detectChanges();
    element(fixture, 'access-switch-user-confirm-confirm').click();
    fixture.detectChanges();

    httpTesting.expectOne(`${ACCESS_BASE_URL}/users/${otherUser.id}`).flush(success(otherUser));
    httpTesting
      .expectOne(`${ACCESS_BASE_URL}/users/${otherUser.id}/permissions`)
      .flush(success(permissions(otherUser)));
    await fixture.whenStable();
    fixture.detectChanges();

    expect(
      (element(fixture, 'access-permission-abwab.doors.edit') as HTMLInputElement).checked,
    ).toBe(false);
    expect(fixture.componentInstance.hasUnsavedChanges()).toBe(false);
  });

  it('switches users without a prompt while the draft matches the stored grants', async () => {
    const activeUser = user('active', 4, ['abwab.doors.create']);
    const otherUser = user('active', 2, [], {
      id: 18,
      sub: 'subject-18',
      email: 'other@example.test',
      normalizedEmail: 'other@example.test',
      displayName: 'زميل',
    });
    const fixture = await renderPage(activeUser, { kind: 'served', assignmentReady: true }, [
      otherUser,
    ]);
    await selectUser(fixture, activeUser);

    await selectUser(fixture, otherUser);

    expect(
      (fixture.nativeElement as HTMLElement).querySelector(
        '[data-testid="access-switch-user-confirm"]',
      ),
    ).toBeNull();
  });

  it('uses a labelled section instead of a nested main landmark', async () => {
    const fixture = await renderPage(user('active'));
    const root = fixture.nativeElement as HTMLElement;

    expect(root.firstElementChild?.tagName).toBe('SECTION');
    expect(root.firstElementChild?.getAttribute('aria-labelledby')).toBe(
      'access-admin-page-title',
    );
    expect(root.querySelector('main')).toBeNull();
  });
});
