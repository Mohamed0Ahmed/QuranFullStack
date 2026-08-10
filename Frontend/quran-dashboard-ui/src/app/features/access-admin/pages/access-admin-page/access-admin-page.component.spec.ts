import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, ParamMap, Router, convertToParamMap, provideRouter } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { BehaviorSubject, NEVER, of } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { environment } from '../../../../../environments/environment';
import { AccessAuditEventItem } from '../../../../core/api/generated/models/access-audit-event-item';
import { AccessUserDetail } from '../../../../core/api/generated/models/access-user-detail';
import { AccessUserPermissions } from '../../../../core/api/generated/models/access-user-permissions';
import { AccessUserSummary } from '../../../../core/api/generated/models/access-user-summary';
import { CurrentUserResponse } from '../../../../core/api/generated/models/current-user-response';
import { PermissionCatalogueItem } from '../../../../core/api/generated/models/permission-catalogue-item';
import { CurrentUserStore } from '../../../../core/auth/current-user.store';
import { QD_BP_WIDE_QUERY } from '../../../../shared/layout/breakpoints';
import { AccessAdminApi } from '../../data-access/access-admin.api';
import { AccessAdminTab } from '../../models/access-admin-tabs';
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
    actorDisplayName: 'المالك',
    actorEmail: 'owner@example.test',
    targetDisplayName: 'عضو',
    targetEmail: 'member@example.test',
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
    actionType: 'OwnerGrantedByReconciliation',
    actorType: 'System',
    actorUserId: null,
    targetUserId: 17,
    actorDisplayName: null,
    actorEmail: null,
    targetDisplayName: 'عضو',
    targetEmail: 'member@example.test',
    actorSnapshot: {},
    targetSnapshot: {},
    permissionCode: null,
    beforeState: {},
    afterState: {},
    reason: null,
    metadata: {},
  },
];

const OWNER_SUMMARY: AccessUserSummary = {
  id: 9,
  email: 'owner@example.test',
  displayName: 'المالك',
  status: 'active',
  isOwner: true,
  permissionCount: 0,
  createdAtUtc: '2026-01-01T00:00:00Z',
  updatedAtUtc: '2026-01-01T00:00:00Z',
  version: 2,
};

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

function absent(fixture: ComponentFixture<AccessAdminPageComponent>, testId: string): boolean {
  return fixture.nativeElement.querySelector(`[data-testid="${testId}"]`) === null;
}

function stubViewport(wide: boolean): void {
  vi.stubGlobal('matchMedia', (query: string) => ({
    matches: query === QD_BP_WIDE_QUERY ? wide : !wide,
    media: query,
    onchange: null,
    addEventListener: () => undefined,
    removeEventListener: () => undefined,
    addListener: () => undefined,
    removeListener: () => undefined,
    dispatchEvent: () => false,
  }));
}

describe('AccessAdminPageComponent', () => {
  let currentUserStore: CurrentUserStore;
  let httpTesting: HttpTestingController;
  let queryParamMap$: BehaviorSubject<ParamMap>;

  beforeEach(() => {
    queryParamMap$ = new BehaviorSubject(convertToParamMap({}));
    stubViewport(true);

    TestBed.configureTestingModule({
      imports: [AccessAdminPageComponent],
      providers: [
        AccessAdminApi,
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            queryParamMap: queryParamMap$,
            snapshot: { queryParamMap: convertToParamMap({}) },
          },
        },
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
          candidates: [{ normalizedEmail: 'OWNER@EXAMPLE.TEST', state: 'Unchanged', userId: 9 }],
          configurationFingerprint: 'a1b2c3d4e5f6',
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

  function showTab(fixture: ComponentFixture<AccessAdminPageComponent>, tab: AccessAdminTab): void {
    queryParamMap$.next(convertToParamMap(tab === 'workspace' ? {} : { tab }));
    fixture.detectChanges();
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

  describe('sections and their URL state', () => {
    it('opens on the workspace section and shows neither the audit nor the security section', async () => {
      const fixture = await renderPage(user('active'));

      expect(element(fixture, 'access-admin-panel-workspace')).toBeTruthy();
      expect(absent(fixture, 'access-admin-panel-audit')).toBe(true);
      expect(absent(fixture, 'access-admin-panel-security')).toBe(true);
      expect(element(fixture, 'access-admin-tab-workspace').getAttribute('aria-selected')).toBe(
        'true',
      );
    });

    it.each([
      { tab: 'audit' as const, panel: 'access-admin-panel-audit' },
      { tab: 'security' as const, panel: 'access-admin-panel-security' },
    ])('renders the $tab section from ?tab=$tab alone', async ({ tab, panel }) => {
      const fixture = await renderPage(user('active'));

      showTab(fixture, tab);

      expect(element(fixture, panel)).toBeTruthy();
      expect(absent(fixture, 'access-admin-panel-workspace')).toBe(true);
      expect(element(fixture, `access-admin-tab-${tab}`).getAttribute('aria-selected')).toBe('true');
    });

    it('writes the chosen section to ?tab= and drops the parameter for the default section', async () => {
      const fixture = await renderPage(user('active'));
      const navigate = vi
        .spyOn(TestBed.inject(Router), 'navigate')
        .mockResolvedValue(true);

      element(fixture, 'access-admin-tab-security').click();

      expect(navigate).toHaveBeenCalledTimes(1);
      expect(navigate.mock.calls[0][0]).toEqual([]);
      expect(navigate.mock.calls[0][1]).toMatchObject({
        queryParams: { tab: 'security' },
        queryParamsHandling: 'merge',
      });

      showTab(fixture, 'security');
      element(fixture, 'access-admin-tab-workspace').click();

      expect(navigate).toHaveBeenCalledTimes(2);
      expect(navigate.mock.calls[1][1]).toMatchObject({
        queryParams: { tab: null },
        queryParamsHandling: 'merge',
      });
    });

    it('falls back to the workspace section for a value outside the enum', async () => {
      const fixture = await renderPage(user('active'));

      queryParamMap$.next(convertToParamMap({ tab: 'audit-log' }));
      fixture.detectChanges();

      expect(element(fixture, 'access-admin-panel-workspace')).toBeTruthy();
      expect(absent(fixture, 'access-admin-panel-audit')).toBe(true);
    });

    it('carries no user identifier in the URL it writes', async () => {
      const activeUser = user('active');
      const fixture = await renderPage(activeUser);
      const navigate = vi
        .spyOn(TestBed.inject(Router), 'navigate')
        .mockResolvedValue(true);
      await selectUser(fixture, activeUser);

      element(fixture, 'access-admin-tab-audit').click();

      expect(navigate.mock.calls[0][1]).toMatchObject({ queryParams: { tab: 'audit' } });
      expect(Object.keys(navigate.mock.calls[0][1]?.queryParams ?? {})).toEqual(['tab']);
    });
  });

  describe('lifecycle and permission writes', () => {
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
      async ({ initialStatus, requestButton, endpoint, finalStatus, expectedBody, expectedLabel }) => {
        const initialUser = user(initialStatus);
        const fixture = await renderPage(initialUser);
        await selectUser(fixture, initialUser);

        confirmAction(fixture, requestButton);

        const request = httpTesting.expectOne(`${ACCESS_BASE_URL}/users/17/${endpoint}`);
        expect(request.request.method).toBe('POST');
        expect(request.request.body).toEqual(expectedBody);
        const refreshedUser = user(finalStatus, 5);
        request.flush(success(refreshedUser));
        await flushMutationRefresh(fixture, refreshedUser);

        const labels = Array.from(
          element(fixture, 'access-user-summary-badges').querySelectorAll(
            '.qd-badge',
          ) as NodeListOf<HTMLElement>,
          (badge) => badge.textContent?.trim(),
        );
        expect(labels).toEqual([expectedLabel]);
        expect(absent(fixture, 'access-action-confirmation')).toBe(true);
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
      const refreshedUser = user('active', 5, ['abwab.doors.create', 'abwab.doors.edit']);
      request.flush(success(permissions(refreshedUser)));
      await flushMutationRefresh(fixture, refreshedUser);

      expect(
        (element(fixture, 'access-permission-abwab.doors.edit') as HTMLInputElement).checked,
      ).toBe(true);
      expect(absent(fixture, 'access-action-confirmation')).toBe(true);
    });

    it('grants the selected permissions through acceptance for a pending user', async () => {
      const pendingUser = user('pending');
      const fixture = await renderPage(pendingUser);
      await selectUser(fixture, pendingUser);
      togglePermission(fixture, 'abwab.doors.edit', true);

      expect(element(fixture, 'access-request-accept').textContent).toContain(
        'قبول وتفعيل مع الصلاحيات المحددة',
      );
      confirmAction(fixture, 'access-request-accept');

      const request = httpTesting.expectOne(`${ACCESS_BASE_URL}/users/17/accept`);
      expect(request.request.body).toEqual({
        expectedVersion: 4,
        permissionCodes: ['abwab.doors.edit'],
        reason: REASON,
      });
      const activeUser = user('active', 5, ['abwab.doors.edit']);
      request.flush(success(activeUser));
      await flushMutationRefresh(fixture, activeUser);

      expect(httpTesting.match((candidate) => candidate.method === 'PUT')).toEqual([]);
      expect(
        (element(fixture, 'access-permission-abwab.doors.edit') as HTMLInputElement).checked,
      ).toBe(true);
    });

    it('reports a completed change without an error state', async () => {
      const initialUser = user('active');
      const fixture = await renderPage(initialUser);
      await selectUser(fixture, initialUser);

      confirmAction(fixture, 'access-request-disable');
      const disabledUser = user('disabled', 5);
      httpTesting.expectOne(`${ACCESS_BASE_URL}/users/17/disable`).flush(success(disabledUser));
      await flushMutationRefresh(fixture, disabledUser);

      expect(element(fixture, 'access-mutation-message-success').textContent).toContain(
        'تم حفظ التغيير',
      );
      expect(absent(fixture, 'access-mutation-message-error')).toBe(true);
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
      expect(absent(fixture, 'access-mutation-message-error')).toBe(true);
    });
  });

  describe('the mutation message region', () => {
    it('mounts a permanently empty polite announcer in the workspace before any write runs', async () => {
      const fixture = await renderPage(user('active'));

      const announcer = element(fixture, 'access-details-status');

      expect(element(fixture, 'access-admin-panel-workspace').contains(announcer)).toBe(true);
      expect(announcer.getAttribute('role')).toBe('status');
      expect(announcer.getAttribute('aria-live')).toBe('polite');
      expect(announcer.textContent?.trim()).toBe('');
      expect(announcer.getBoundingClientRect().height).toBe(0);
      expect(absent(fixture, 'access-mutation-message-success')).toBe(true);
    });

    it('mounts a permanently empty polite announcer in the security section too', async () => {
      const fixture = await renderPage(user('active'));

      showTab(fixture, 'security');
      const announcer = element(fixture, 'access-security-announcer');

      expect(element(fixture, 'access-admin-panel-security').contains(announcer)).toBe(true);
      expect(announcer.getAttribute('role')).toBe('status');
      expect(announcer.textContent?.trim()).toBe('');
    });

    it('announces a write failure once by shielding its alert from the polite details region', async () => {
      const activeUser = user('active', 4, ['abwab.doors.create']);
      const fixture = await renderPage(activeUser);
      await selectUser(fixture, activeUser);
      togglePermission(fixture, 'abwab.doors.edit', true);

      confirmAction(fixture, 'access-request-permissions');
      httpTesting
        .expectOne(`${ACCESS_BASE_URL}/users/17/permissions`)
        .flush(
          { isSuccess: false, message: 'تعذر حفظ الصلاحيات.', data: null },
          { status: 500, statusText: 'Server Error' },
        );
      await new Promise((resolve) => setTimeout(resolve, 0));
      await fixture.whenStable();
      fixture.detectChanges();

      const alert = element(fixture, 'access-mutation-message-error');
      const status = element(fixture, 'access-details-status');

      expect(alert.getAttribute('role')).toBe('alert');
      expect(status.contains(alert)).toBe(true);
      expect(alert.closest('[aria-live]')?.getAttribute('aria-live')).toBe('off');
      expect(
        (element(fixture, 'access-permission-abwab.doors.edit') as HTMLInputElement).checked,
      ).toBe(true);
    });

    it('clears a settled message when the operator moves to another section', async () => {
      const activeUser = user('active');
      const fixture = await renderPage(activeUser);
      await selectUser(fixture, activeUser);

      confirmAction(fixture, 'access-request-disable');
      const disabledUser = user('disabled', 5);
      httpTesting.expectOne(`${ACCESS_BASE_URL}/users/17/disable`).flush(success(disabledUser));
      await flushMutationRefresh(fixture, disabledUser);
      expect(element(fixture, 'access-mutation-message-success').textContent).toContain(
        'تم حفظ التغيير',
      );

      showTab(fixture, 'security');

      expect(absent(fixture, 'access-mutation-message-success')).toBe(true);
      expect(element(fixture, 'access-security-announcer').textContent?.trim()).toBe('');
    });
  });

  describe('catalogue failure and write readiness', () => {
    it('degrades only the permission region when the catalogue request fails, then recovers on retry', async () => {
      const activeUser = user('active', 4, ['abwab.doors.create']);
      const fixture = await renderPage(activeUser, { kind: 'failure' });
      await selectUser(fixture, activeUser);
      const root = fixture.nativeElement as HTMLElement;

      expect(root.textContent).toContain('عضو');
      expect(root.textContent).toContain('member@example.test');
      expect(
        Array.from(
          element(fixture, 'access-user-summary-badges').querySelectorAll(
            '.qd-badge',
          ) as NodeListOf<HTMLElement>,
          (badge) => badge.textContent?.trim(),
        ),
      ).toEqual(['نشط']);
      expect(element(fixture, 'access-request-disable')).toBeTruthy();

      const region = element(fixture, 'access-permissions-section');
      expect(region.textContent).toContain(CATALOGUE_ERROR);
      expect(region.querySelector('qd-access-permission-editor')).toBeNull();
      expect(absent(fixture, 'access-request-permissions')).toBe(true);

      showTab(fixture, 'security');
      expect(element(fixture, 'access-relink-new-sub')).toBeTruthy();
      showTab(fixture, 'workspace');

      (element(fixture, 'access-catalogue-retry') as HTMLButtonElement).click();
      flushCatalogue({ kind: 'served', assignmentReady: true });
      await fixture.whenStable();
      fixture.detectChanges();

      expect(element(fixture, 'access-permissions-section').textContent).not.toContain(
        CATALOGUE_ERROR,
      );
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

        expect(fixture.componentInstance.hasUnsavedChanges()).toBe(false);
        expect(absent(fixture, 'access-permission-diff-summary')).toBe(true);
        expect(absent(fixture, 'access-permission-draft-bar')).toBe(true);

        showTab(fixture, 'security');
        expect(absent(fixture, 'access-relink-blocked')).toBe(true);

        const newSub = element(fixture, 'access-relink-new-sub') as HTMLInputElement;
        newSub.value = 'replacement-subject';
        newSub.dispatchEvent(new Event('input'));
        const evidence = element(fixture, 'access-relink-evidence') as HTMLInputElement;
        evidence.value = 'verified-evidence';
        evidence.dispatchEvent(new Event('input'));
        fixture.detectChanges();

        expect((element(fixture, 'access-relink-preview') as HTMLButtonElement).disabled).toBe(
          false,
        );
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

      expect(element(fixture, 'access-permissions-unavailable').textContent).toContain(
        'إسناد الصلاحيات غير متاح مؤقتًا',
      );
      expect(absent(fixture, 'access-request-permissions')).toBe(true);
      expect(element(fixture, 'access-request-disable')).toBeTruthy();
      httpTesting.expectNone((request) => request.url.endsWith('/users/17/permissions'));
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

      expect(absent(fixture, 'access-permission-draft-bar')).toBe(true);
      expect(httpTesting.match((request) => request.method === 'PUT')).toEqual([]);
    });

    it('accepts a pending user without a permission payload while assignment is unavailable', async () => {
      const pendingUser = user('pending');
      const fixture = await renderPage(pendingUser, { kind: 'served', assignmentReady: false });
      await selectUser(fixture, pendingUser);

      expect(element(fixture, 'access-permissions-unavailable')).toBeTruthy();
      expect(element(fixture, 'access-request-accept').textContent).toContain(
        'قبول وتفعيل دون صلاحيات',
      );
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
  });

  describe('per-status semantics in the workspace', () => {
    it('states how an Active Owner receives access instead of rendering an editor', async () => {
      const ownerUser = user('active', 4, [], { isOwner: true });
      const fixture = await renderPage(ownerUser);
      await selectUser(fixture, ownerUser);

      const region = element(fixture, 'access-owner-permissions');
      expect(region.textContent).toContain('كامل صلاحيات الإدارة عبر تجاوز المالك');
      expect(region.textContent).toContain('تُدار عضوية المالك عبر مطابقة المالكين');
      expect(
        (fixture.nativeElement as HTMLElement).querySelector('qd-access-permission-editor'),
      ).toBeNull();
      expect(absent(fixture, 'access-request-accept')).toBe(true);
      expect(absent(fixture, 'access-request-disable')).toBe(true);
      expect(absent(fixture, 'access-request-reactivate')).toBe(true);
      expect(absent(fixture, 'access-account-actions')).toBe(true);
    });

    it('does not claim administrative access for an Owner account that is not active', async () => {
      const ownerUser = user('pending', 4, [], { isOwner: true });
      const fixture = await renderPage(ownerUser);
      await selectUser(fixture, ownerUser);

      expect(element(fixture, 'access-owner-permissions').textContent).toContain(
        'لا يسري تجاوز المالك إلا على حساب مالك نشط',
      );
    });

    it('explains that a disabled account holds nothing and that reactivation restores nothing', async () => {
      const disabledUser = user('disabled');
      const fixture = await renderPage(disabledUser);
      await selectUser(fixture, disabledUser);

      const region = element(fixture, 'access-disabled-permissions');
      expect(region.textContent).toContain(
        'لا يحمل صلاحيات مباشرة، ولا يمكن إسنادها قبل إعادة التفعيل',
      );
      expect(region.textContent).toContain('إعادة التفعيل تبدأ بلا صلاحيات مباشرة سابقة');
      expect(
        (fixture.nativeElement as HTMLElement).querySelector('qd-access-permission-editor'),
      ).toBeNull();
      expect(element(fixture, 'access-request-reactivate')).toBeTruthy();
    });

    it('offers a pending account only a discard path beside its acceptance', async () => {
      const pendingUser = user('pending');
      const fixture = await renderPage(pendingUser);
      await selectUser(fixture, pendingUser);
      togglePermission(fixture, 'abwab.doors.edit', true);

      expect(element(fixture, 'access-discard-draft')).toBeTruthy();
      expect(absent(fixture, 'access-request-permissions')).toBe(true);
      expect(element(fixture, 'access-request-accept')).toBeTruthy();
      expect(element(fixture, 'access-pending-permissions-note').textContent).toContain(
        'تُمنح الصلاحيات المحددة عند تفعيل الحساب',
      );
    });

    it('keeps identity recovery out of the permission workspace', async () => {
      const activeUser = user('active');
      const fixture = await renderPage(activeUser);
      await selectUser(fixture, activeUser);

      expect(absent(fixture, 'access-relink-new-sub')).toBe(true);
      expect(absent(fixture, 'access-relink-preview')).toBe(true);
      expect(element(fixture, 'access-admin-panel-workspace').textContent).not.toContain(
        'إعادة ربط',
      );
    });
  });

  describe('permission draft protection', () => {
    it('summarises a pending permission change and reverts it without issuing a request', async () => {
      const activeUser = user('active', 4, ['abwab.doors.create']);
      const fixture = await renderPage(activeUser);
      await selectUser(fixture, activeUser);

      expect(absent(fixture, 'access-permission-draft-bar')).toBe(true);
      expect(absent(fixture, 'access-request-permissions')).toBe(true);
      expect(fixture.componentInstance.hasUnsavedChanges()).toBe(false);

      togglePermission(fixture, 'abwab.doors.edit', true);
      togglePermission(fixture, 'abwab.doors.create', false);

      expect(element(fixture, 'access-permission-diff-summary-text').textContent).toBe(
        'صلاحيات مضافة: 1، صلاحيات ملغاة: 1',
      );
      expect(element(fixture, 'access-permission-diff-summary').getAttribute('aria-hidden')).toBe(
        'true',
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
      expect(absent(fixture, 'access-permission-draft-bar')).toBe(true);
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

      expect(absent(fixture, 'access-switch-user-confirm')).toBe(true);
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

    it('closes an open change review when the selected account changes', async () => {
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

      element(fixture, 'access-request-disable').click();
      fixture.detectChanges();
      expect(element(fixture, 'access-action-confirmation')).toBeTruthy();

      await selectUser(fixture, otherUser);

      expect(absent(fixture, 'access-action-confirmation')).toBe(true);
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

      expect(absent(fixture, 'access-switch-user-confirm')).toBe(true);
    });

    it('holds relink back on both steps while the draft is dirty', async () => {
      const activeUser = user('active', 4, ['abwab.doors.create']);
      const fixture = await renderPage(activeUser);
      await selectUser(fixture, activeUser);
      togglePermission(fixture, 'abwab.doors.edit', true);

      showTab(fixture, 'security');

      expect(element(fixture, 'access-relink-blocked')).toBeTruthy();
      const newSub = element(fixture, 'access-relink-new-sub') as HTMLInputElement;
      newSub.value = 'replacement-subject';
      newSub.dispatchEvent(new Event('input'));
      const evidence = element(fixture, 'access-relink-evidence') as HTMLInputElement;
      evidence.value = 'verified-evidence';
      evidence.dispatchEvent(new Event('input'));
      fixture.detectChanges();

      expect((element(fixture, 'access-relink-preview') as HTMLButtonElement).disabled).toBe(true);
      element(fixture, 'access-relink-preview').click();

      httpTesting.expectNone(`${ACCESS_BASE_URL}/users/17/logto-sub/relink/preview`);
    });
  });

  describe('advanced security', () => {
    it('renders relink preview and confirms it through distinct HTTP requests', async () => {
      const initialUser = user('active');
      const fixture = await renderPage(initialUser);
      await selectUser(fixture, initialUser);
      showTab(fixture, 'security');

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
      const reason = element(fixture, 'access-relink-confirm-reason') as HTMLTextAreaElement;
      reason.value = 'تصحيح المعرّف';
      reason.dispatchEvent(new Event('input'));
      const confirmed = element(fixture, 'access-relink-confirmed') as HTMLInputElement;
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
      const refreshedUser = user('active', 5, [], { sub: 'replacement-subject' });
      confirmRequest.flush(success(refreshedUser));
      await flushMutationRefresh(fixture, refreshedUser);

      expect(absent(fixture, 'access-relink-confirmation')).toBe(true);
      expect(element(fixture, 'access-mutation-message-success').textContent).toContain(
        'تم حفظ التغيير',
      );
    });

    it('removes a canceled relink preview without issuing confirmation', async () => {
      const initialUser = user('active');
      const fixture = await renderPage(initialUser);
      await selectUser(fixture, initialUser);
      showTab(fixture, 'security');

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

      expect(absent(fixture, 'access-relink-confirmation')).toBe(true);
      expect(newSub.value).toBe('');
      expect(evidence.value).toBe('');
      httpTesting.expectNone(`${ACCESS_BASE_URL}/users/17/logto-sub/relink/confirm`);
    });

    it('keeps owner reconciliation beside identity recovery as read-only status', async () => {
      const fixture = await renderPage(user('active'));

      showTab(fixture, 'security');

      const panel = element(fixture, 'access-admin-panel-security');
      expect(panel.textContent).toContain('حالة مطابقة المالكين');
      expect(panel.querySelector('form')).toBeNull();
      expect(element(fixture, 'access-admin-reconciliation').textContent).toContain('دون تغيير');
    });

    it('states that reconciliation is diagnostic and offers no way to apply it', async () => {
      const fixture = await renderPage(user('active'));

      showTab(fixture, 'security');
      const reconciliation = element(fixture, 'access-admin-reconciliation');

      expect(element(fixture, 'access-reconciliation-diagnostic-note').textContent).toContain(
        'لا تُطبَّق من هذه الصفحة',
      );
      expect(
        Array.from(reconciliation.querySelectorAll('button'), (button) =>
          button.getAttribute('data-testid'),
        ),
      ).toEqual(['access-reconciliation-fingerprint-toggle']);
    });

    it('keeps the configuration fingerprint behind a diagnostic disclosure', async () => {
      const fixture = await renderPage(user('active'));

      showTab(fixture, 'security');

      expect(absent(fixture, 'access-reconciliation-fingerprint')).toBe(true);
      expect(element(fixture, 'access-admin-reconciliation').textContent).not.toContain(
        'a1b2c3d4e5f6',
      );

      const toggle = element(fixture, 'access-reconciliation-fingerprint-toggle');
      expect(toggle.getAttribute('aria-expanded')).toBe('false');
      toggle.click();
      fixture.detectChanges();

      expect(element(fixture, 'access-reconciliation-fingerprint').textContent).toContain(
        'a1b2c3d4e5f6',
      );
      expect(
        element(fixture, 'access-reconciliation-fingerprint-toggle').getAttribute('aria-expanded'),
      ).toBe('true');
    });
  });

  describe('audit section', () => {
    it('attributes every row to a human identity and prints no numeric identifier', async () => {
      const fixture = await renderPage(user('active'));
      showTab(fixture, 'audit');
      const panel = element(fixture, 'access-admin-panel-audit');

      expect(panel.textContent).toContain('المنفّذ: المالك');
      expect(panel.textContent).toContain('المنفّذ: النظام');
      expect(panel.textContent).toContain('الحساب: عضو');
      expect(panel.textContent).toContain('منح صلاحية');
      expect(panel.textContent).not.toContain('معرّف المستخدم');
      expect(panel.querySelector('input[type="number"]')).toBeNull();
      expect(
        Array.from(panel.querySelectorAll('.access-audit-log__row'), (row) => {
          const withoutTime = row.cloneNode(true) as HTMLElement;
          withoutTime.querySelectorAll('time').forEach((stamp) => stamp.remove());
          return withoutTime.textContent ?? '';
        }).join(' '),
      ).not.toMatch(/\d/);
    });

    it('resolves an account by name and filters the log by the id it never rendered', async () => {
      const fixture = await renderPage(user('active'));
      showTab(fixture, 'audit');

      const search = element(fixture, 'access-audit-actor-search') as HTMLInputElement;
      search.value = 'المالك';
      search.dispatchEvent(new Event('input'));
      fixture.detectChanges();
      element(fixture, 'access-audit-actor-find').click();

      const lookup = httpTesting.expectOne(
        (candidate) =>
          candidate.url === `${ACCESS_BASE_URL}/users` && candidate.params.get('search') === 'المالك',
      );
      expect(lookup.request.params.get('pageSize')).toBe('10');
      lookup.flush(
        success({
          items: [summary(user('active')), OWNER_SUMMARY],
          page: 1,
          pageSize: 10,
          totalCount: 2,
        }),
      );
      await fixture.whenStable();
      fixture.detectChanges();

      element(fixture, 'access-audit-actor-candidate-9').click();
      fixture.detectChanges();
      expect(element(fixture, 'access-audit-actor-selected').textContent).toContain('المالك');

      element(fixture, 'access-audit-filters').dispatchEvent(
        new Event('submit', { bubbles: true, cancelable: true }),
      );

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

      expect(element(fixture, 'access-admin-panel-audit').textContent).toContain('نتيجة المرشح');
    });
  });

  describe('the exhaustive lifecycle and membership matrix', () => {
    it.each([
      {
        variant: 'pending-non-owner',
        detail: user('pending'),
        body: 'access-permissions-section',
        absentBodies: ['access-owner-permissions', 'access-disabled-permissions', 'access-unknown-status'],
        lifecycle: 'access-request-accept',
      },
      {
        variant: 'active-non-owner',
        detail: user('active'),
        body: 'access-permissions-section',
        absentBodies: ['access-owner-permissions', 'access-disabled-permissions', 'access-unknown-status'],
        lifecycle: 'access-request-disable',
      },
      {
        variant: 'disabled-non-owner',
        detail: user('disabled'),
        body: 'access-disabled-permissions',
        absentBodies: ['access-permissions-section', 'access-owner-permissions', 'access-unknown-status'],
        lifecycle: 'access-request-reactivate',
      },
      {
        variant: 'active-owner',
        detail: user('active', 4, [], { isOwner: true }),
        body: 'access-owner-permissions',
        absentBodies: ['access-permissions-section', 'access-disabled-permissions', 'access-unknown-status'],
        lifecycle: null,
      },
      {
        variant: 'pending-owner',
        detail: user('pending', 4, [], { isOwner: true }),
        body: 'access-owner-permissions',
        absentBodies: ['access-permissions-section', 'access-disabled-permissions', 'access-unknown-status'],
        lifecycle: null,
      },
      {
        variant: 'disabled-owner',
        detail: user('disabled', 4, [], { isOwner: true }),
        body: 'access-owner-permissions',
        absentBodies: ['access-permissions-section', 'access-disabled-permissions', 'access-unknown-status'],
        lifecycle: null,
      },
      {
        variant: 'unknown-status',
        detail: user('active', 4, [], { status: 'archived' }),
        body: 'access-unknown-status',
        absentBodies: ['access-permissions-section', 'access-owner-permissions', 'access-disabled-permissions'],
        lifecycle: null,
      },
    ])('renders exactly one body for $variant', async ({ detail, body, absentBodies, lifecycle }) => {
      const fixture = await renderPage(detail);
      await selectUser(fixture, detail);

      expect(element(fixture, body)).toBeTruthy();
      for (const other of absentBodies) {
        expect(absent(fixture, other)).toBe(true);
      }
      if (lifecycle === null) {
        expect(absent(fixture, 'access-account-actions')).toBe(true);
        expect(absent(fixture, 'access-request-accept')).toBe(true);
        expect(absent(fixture, 'access-request-disable')).toBe(true);
        expect(absent(fixture, 'access-request-reactivate')).toBe(true);
      } else {
        expect(element(fixture, lifecycle)).toBeTruthy();
      }
    });

    it('offers an unknown status neither an editor nor a mutation control', async () => {
      const unknown = user('active', 4, ['abwab.doors.create'], { status: 'archived' });
      const fixture = await renderPage(unknown);
      await selectUser(fixture, unknown);

      expect(
        (fixture.nativeElement as HTMLElement).querySelector('qd-access-permission-editor'),
      ).toBeNull();
      expect(absent(fixture, 'access-account-actions')).toBe(true);
      expect(element(fixture, 'access-user-summary-lifecycle').textContent?.trim()).toBe(
        'حالة غير معروفة',
      );
      expect(
        element(fixture, 'access-user-summary-lifecycle').classList.contains(
          'qd-badge--lifecycle-disabled',
        ),
      ).toBe(false);
    });
  });

  describe('the responsive composition', () => {
    it('shows the rail beside the details at Wide and mounts no list sheet', async () => {
      const fixture = await renderPage(user('active'));

      expect(element(fixture, 'access-admin-user-rail').classList).toContain('qd-page-rail--l');
      expect(element(fixture, 'access-admin-user-rail').querySelector('qd-access-user-list')).toBeTruthy();
      expect(absent(fixture, 'access-selected-context-bar')).toBe(true);
      expect(absent(fixture, 'access-user-list-sheet')).toBe(true);
      expect((fixture.nativeElement as HTMLElement).querySelectorAll('.qd-page-shell')).toHaveLength(1);
    });

    it('replaces the rail with a pinned context bar and a focus-trapped list sheet below Wide', async () => {
      stubViewport(false);
      const activeUser = user('active');
      const fixture = await renderPage(activeUser);
      element(fixture, 'access-open-user-list').click();
      fixture.detectChanges();
      await selectUser(fixture, activeUser);
      element(fixture, 'access-open-user-list').click();
      fixture.detectChanges();
      element(fixture, 'access-user-list-sheet-close').click();
      fixture.detectChanges();

      expect(absent(fixture, 'access-admin-user-rail')).toBe(true);
      const bar = element(fixture, 'access-selected-context-bar');
      expect(bar.textContent).toContain('عضو');
      expect(bar.querySelector('[data-testid="access-user-summary-email"]')?.textContent).toContain(
        'member@example.test',
      );
      expect(bar.querySelector('[data-testid="access-user-summary-lifecycle"]')).toBeTruthy();
      expect(absent(fixture, 'access-user-list-sheet')).toBe(true);

      element(fixture, 'access-open-user-list').click();
      fixture.detectChanges();

      const sheet = element(fixture, 'access-user-list-sheet');
      expect(sheet.getAttribute('aria-modal')).toBe('true');
      expect(sheet.querySelector('qd-access-user-list')).toBeTruthy();
    });

    it('carries the account search in the pinned context bar and opens the filtered sheet', async () => {
      stubViewport(false);
      const activeUser = user('active');
      const fixture = await renderPage(activeUser);
      element(fixture, 'access-open-user-list').click();
      fixture.detectChanges();
      await selectUser(fixture, activeUser);

      const bar = element(fixture, 'access-selected-context-bar');
      const search = bar.querySelector(
        '[data-testid="access-context-search"]',
      ) as HTMLInputElement;
      search.value = 'عضو';
      search.dispatchEvent(new Event('input'));
      fixture.detectChanges();

      element(fixture, 'access-context-search-form').dispatchEvent(
        new Event('submit', { cancelable: true }),
      );
      const request = httpTesting.expectOne(
        (candidate) =>
          candidate.url === `${ACCESS_BASE_URL}/users` && candidate.params.get('search') === 'عضو',
      );
      request.flush(
        success({ items: [summary(activeUser)], page: 1, pageSize: 25, totalCount: 1 }),
      );
      await fixture.whenStable();
      fixture.detectChanges();

      const sheet = element(fixture, 'access-user-list-sheet');
      expect(sheet.querySelector('qd-access-user-list')).toBeTruthy();
      expect(
        (sheet.querySelector('[data-testid="access-users-search"]') as HTMLInputElement).value,
      ).toBe('عضو');
    });

    it('renders the identity summary once below Wide, in the pinned context bar only', async () => {
      stubViewport(false);
      const activeUser = user('active');
      const fixture = await renderPage(activeUser);
      element(fixture, 'access-open-user-list').click();
      fixture.detectChanges();
      await selectUser(fixture, activeUser);
      const root = fixture.nativeElement as HTMLElement;

      for (const testId of [
        'access-user-summary-email',
        'access-user-summary-lifecycle',
        'access-user-summary-badges',
      ]) {
        expect(root.querySelectorAll(`[data-testid="${testId}"]`)).toHaveLength(1);
      }
      expect(
        element(fixture, 'access-selected-context-bar').querySelector(
          '[data-testid="access-user-summary-email"]',
        ),
      ).toBeTruthy();
    });

    it('keeps the single identity summary in the details header at Wide', async () => {
      const activeUser = user('active');
      const fixture = await renderPage(activeUser);
      await selectUser(fixture, activeUser);

      expect(
        (fixture.nativeElement as HTMLElement).querySelectorAll(
          '[data-testid="access-user-summary-email"]',
        ),
      ).toHaveLength(1);
      expect(
        element(fixture, 'access-details-identity').parentElement?.querySelector(
          '[data-testid="access-user-summary-email"]',
        ),
      ).toBeTruthy();
    });

    it('closes the list sheet once an account is chosen from it', async () => {
      stubViewport(false);
      const activeUser = user('active');
      const fixture = await renderPage(activeUser);
      element(fixture, 'access-open-user-list').click();
      fixture.detectChanges();

      await selectUser(fixture, activeUser);

      expect(absent(fixture, 'access-user-list-sheet')).toBe(true);
    });
  });

  describe('the dirty review dock', () => {
    it('exists only while the draft is dirty and keeps lifecycle actions outside it', async () => {
      const activeUser = user('active', 4, ['abwab.doors.create']);
      const fixture = await renderPage(activeUser);
      await selectUser(fixture, activeUser);

      expect(absent(fixture, 'access-permission-draft-bar')).toBe(true);

      togglePermission(fixture, 'abwab.doors.edit', true);

      const dock = element(fixture, 'access-permission-draft-bar');
      expect(dock.querySelector('[data-testid="access-request-permissions"]')).toBeTruthy();
      expect(dock.querySelector('[data-testid="access-discard-draft"]')).toBeTruthy();
      expect(dock.querySelector('[data-testid="access-request-disable"]')).toBeNull();
      expect(element(fixture, 'access-request-disable')).toBeTruthy();

      element(fixture, 'access-discard-draft').click();
      fixture.detectChanges();

      expect(absent(fixture, 'access-permission-draft-bar')).toBe(true);
    });

    it('rides the details footer at Wide, where the panel scroller pins it', async () => {
      const activeUser = user('active', 4, ['abwab.doors.create']);
      const fixture = await renderPage(activeUser);
      await selectUser(fixture, activeUser);
      togglePermission(fixture, 'abwab.doors.edit', true);

      const docks = (fixture.nativeElement as HTMLElement).querySelectorAll(
        '[data-testid="access-permission-draft-bar"]',
      );

      expect(docks).toHaveLength(1);
      expect(element(fixture, 'access-details-footer').contains(docks[0])).toBe(true);
      expect(docks[0].classList.contains('access-admin-page__review-dock--pinned')).toBe(false);
    });

    it('leaves the details shell below Wide and becomes the page-pinned dock instead', async () => {
      stubViewport(false);
      const activeUser = user('active', 4, ['abwab.doors.create']);
      const fixture = await renderPage(activeUser);
      element(fixture, 'access-open-user-list').click();
      fixture.detectChanges();
      await selectUser(fixture, activeUser);
      togglePermission(fixture, 'abwab.doors.edit', true);

      const root = fixture.nativeElement as HTMLElement;
      const docks = root.querySelectorAll('[data-testid="access-permission-draft-bar"]');

      expect(docks).toHaveLength(1);
      const dock = docks[0] as HTMLElement;
      expect(dock.classList.contains('access-admin-page__review-dock--pinned')).toBe(true);
      expect(absent(fixture, 'access-details-footer')).toBe(true);
      expect(root.querySelector('[data-testid="access-details"]')?.contains(dock)).toBe(false);
      expect(element(fixture, 'access-admin-panel-workspace').contains(dock)).toBe(true);
      expect(dock.querySelector('[data-testid="access-request-permissions"]')).toBeTruthy();
      expect(dock.querySelector('[data-testid="access-discard-draft"]')).toBeTruthy();

      element(fixture, 'access-discard-draft').click();
      fixture.detectChanges();

      expect(absent(fixture, 'access-permission-draft-bar')).toBe(true);
    });
  });

  describe('route-leave protection', () => {
    it('resolves immediately without a dialog while the draft is clean', async () => {
      const activeUser = user('active');
      const fixture = await renderPage(activeUser);
      await selectUser(fixture, activeUser);

      await expect(fixture.componentInstance.confirmRouteLeave()).resolves.toBe(true);
      expect(absent(fixture, 'access-leave-page-confirm')).toBe(true);
    });

    it('asks once, keeps the draft and the selection when declined, and lets a later leave through', async () => {
      const activeUser = user('active', 4, ['abwab.doors.create']);
      const fixture = await renderPage(activeUser);
      await selectUser(fixture, activeUser);
      togglePermission(fixture, 'abwab.doors.edit', true);

      const first = fixture.componentInstance.confirmRouteLeave();
      const second = fixture.componentInstance.confirmRouteLeave();
      fixture.detectChanges();

      expect(element(fixture, 'access-leave-page-confirm')).toBeTruthy();
      expect(first).toBe(second);

      element(fixture, 'access-leave-page-confirm-cancel').click();
      fixture.detectChanges();

      await expect(first).resolves.toBe(false);
      expect(absent(fixture, 'access-leave-page-confirm')).toBe(true);
      expect(
        (element(fixture, 'access-permission-abwab.doors.edit') as HTMLInputElement).checked,
      ).toBe(true);
      expect(element(fixture, 'access-user-summary-email').textContent).toContain(
        'member@example.test',
      );
      expect(fixture.componentInstance.hasUnsavedChanges()).toBe(true);

      const third = fixture.componentInstance.confirmRouteLeave();
      fixture.detectChanges();
      element(fixture, 'access-leave-page-confirm-confirm').click();
      fixture.detectChanges();

      await expect(third).resolves.toBe(true);
      expect(fixture.componentInstance.hasUnsavedChanges()).toBe(true);
      expect(httpTesting.match(() => true)).toEqual([]);
    });
  });

  describe('the audit append capability', () => {
    it('appends the next cursor page in server order without replacing the mounted events', async () => {
      const fixture = await renderPage(user('active'));
      showTab(fixture, 'audit');

      expect(absent(fixture, 'access-audit-next-page')).toBe(true);

      element(fixture, 'access-audit-filters').dispatchEvent(
        new Event('submit', { bubbles: true, cancelable: true }),
      );
      const filtered = httpTesting.expectOne(
        (request) => request.url === `${ACCESS_BASE_URL}/audit-events`,
      );
      filtered.flush(success({ items: AUDIT_EVENTS, nextCursor: 'cursor-2' }));
      await fixture.whenStable();
      fixture.detectChanges();

      element(fixture, 'access-audit-next-page').click();
      const appended = httpTesting.expectOne(
        (request) =>
          request.url === `${ACCESS_BASE_URL}/audit-events` &&
          request.params.get('cursor') === 'cursor-2',
      );
      appended.flush(
        success({ items: [{ ...AUDIT_EVENTS[0], id: 5, reason: 'حدث لاحق' }], nextCursor: null }),
      );
      await fixture.whenStable();
      fixture.detectChanges();

      const rows = Array.from(
        (fixture.nativeElement as HTMLElement).querySelectorAll('.access-audit-log__row'),
      );
      expect(rows).toHaveLength(3);
      expect(rows[2].textContent).toContain('حدث لاحق');
      expect(element(fixture, 'access-audit-append-announcement').textContent).toContain('1');
      expect(absent(fixture, 'access-audit-next-page')).toBe(true);
    });
  });

  describe('access, structure and direction', () => {
    it('loads the workspace when Owner access resolves after the page is mounted', async () => {
      const listedUser = user('active');
      const load = currentUserStore.refresh();
      const identity = httpTesting.expectOne(`${ACCESS_BASE_URL}/me`);
      const fixture = TestBed.createComponent(AccessAdminPageComponent);
      fixture.detectChanges();
      const root = fixture.nativeElement as HTMLElement;

      expect(root.querySelector('[data-testid="access-admin-checking"]')).toBeTruthy();
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
      expect(absent(fixture, 'access-admin-tab-workspace')).toBe(true);
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

    it('marks Latin identifiers left-to-right inside the right-to-left page', async () => {
      const activeUser = user('active');
      const fixture = await renderPage(activeUser);
      await selectUser(fixture, activeUser);
      const root = fixture.nativeElement as HTMLElement;

      const emails = Array.from(
        root.querySelectorAll('[dir="ltr"]') as NodeListOf<HTMLElement>,
        (node) => node.textContent?.trim(),
      );
      expect(emails).toContain('member@example.test');
      expect(root.querySelector('[dir="rtl"]')).toBeNull();
    });

    it('never renders the optimistic-concurrency version or a database user id in the workspace', async () => {
      const activeUser = user('active', 5, ['abwab.doors.create']);
      const fixture = await renderPage(activeUser);
      await selectUser(fixture, activeUser);

      const workspace = element(fixture, 'access-admin-panel-workspace');
      expect(workspace.textContent).not.toContain('الإصدار');
      expect(workspace.textContent).not.toContain('17');
    });
  });
});
