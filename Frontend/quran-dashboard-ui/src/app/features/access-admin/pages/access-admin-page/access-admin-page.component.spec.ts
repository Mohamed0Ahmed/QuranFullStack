import { signal } from '@angular/core';
import { ComponentFixture, TestBed, getTestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { AccessAuditEventItem } from '../../../../core/api/generated/models/access-audit-event-item';
import { AccessUserDetail } from '../../../../core/api/generated/models/access-user-detail';
import { LogtoSubjectRelinkPreview } from '../../../../core/api/generated/models/logto-subject-relink-preview';
import { PermissionCode } from '../../../../core/auth/permission-code';
import { AccessPermissionDiff } from '../../models/access-admin.models';
import { AccessPermissionGroup } from '../../models/access-admin-permissions';
import { AccessAdminFacade } from '../../state/access-admin.facade';
import { AccessAdminPageComponent } from './access-admin-page.component';

const GROUPS: readonly AccessPermissionGroup[] = [
  {
    key: 'doors',
    label: 'الأبواب',
    codes: ['abwab.doors.create', 'abwab.doors.edit'],
    labels: new Map([
      ['abwab.doors.create', 'إضافة باب'],
      ['abwab.doors.edit', 'تعديل باب'],
    ]),
  },
];

const PERMISSION_DIFF: AccessPermissionDiff = {
  granted: ['abwab.doors.edit'],
  revoked: ['abwab.doors.create'],
};

const RELINK_PREVIEW: LogtoSubjectRelinkPreview = {
  userId: 17,
  oldSub: 'subject-17',
  newSub: 'replacement-subject',
  version: 4,
  isOwner: false,
};

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

function user(status: 'pending' | 'active' | 'disabled'): AccessUserDetail {
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
    permissionCodes: ['abwab.doors.create'],
    createdAtUtc: '2026-01-01T00:00:00Z',
    updatedAtUtc: '2026-01-01T00:00:00Z',
    version: 4,
  };
}

function createFacade(status: 'pending' | 'active' | 'disabled') {
  return {
    canAccess: signal(true),
    users: signal([]),
    selectedUser: signal<AccessUserDetail | null>(user(status)),
    userQuery: signal({ page: 1, pageSize: 25 }),
    userPage: signal(1),
    userPageSize: signal(25),
    userTotalCount: signal(1),
    usersLoading: signal(false),
    usersError: signal<string | null>(null),
    permissionGroups: signal(GROUPS),
    catalogueLoading: signal(false),
    catalogueError: signal<string | null>(null),
    selectedUserLoading: signal(false),
    selectedUserError: signal<string | null>(null),
    selectedPermissionCodes: signal<ReadonlySet<PermissionCode>>(new Set(['abwab.doors.create'])),
    permissionDiff: signal(PERMISSION_DIFF),
    busyAction: signal<string | null>(null),
    relinkPreview: signal<LogtoSubjectRelinkPreview | null>(null),
    mutationMessage: signal<string | null>(null),
    auditEvents: signal(AUDIT_EVENTS),
    auditNextCursor: signal<string | null>(null),
    auditLoading: signal(false),
    auditError: signal<string | null>(null),
    reconciliationStatus: signal(null),
    reconciliationError: signal<string | null>(null),
    load: vi.fn().mockResolvedValue(undefined),
    selectUser: vi.fn().mockResolvedValue(undefined),
    updateUserQuery: vi.fn().mockResolvedValue(undefined),
    setSelectedPermissionCodes: vi.fn(),
    acceptSelectedUser: vi.fn().mockResolvedValue('success'),
    disableSelectedUser: vi.fn().mockResolvedValue('success'),
    reactivateSelectedUser: vi.fn().mockResolvedValue('success'),
    replaceSelectedPermissions: vi.fn().mockResolvedValue('success'),
    previewSelectedUserRelink: vi.fn().mockResolvedValue('success'),
    confirmSelectedUserRelink: vi.fn().mockResolvedValue('success'),
    cancelSelectedUserRelink: vi.fn(),
    updateAuditQuery: vi.fn().mockResolvedValue(undefined),
    loadNextAuditPage: vi.fn().mockResolvedValue(undefined),
  };
}

function render(status: 'pending' | 'active' | 'disabled' = 'active') {
  getTestBed().resetTestingModule();
  const facade = createFacade(status);
  TestBed.configureTestingModule({ imports: [AccessAdminPageComponent] });
  TestBed.overrideComponent(AccessAdminPageComponent, {
    set: { providers: [{ provide: AccessAdminFacade, useValue: facade }] },
  });
  const fixture = TestBed.createComponent(AccessAdminPageComponent);
  fixture.detectChanges();
  return { fixture, facade };
}

function element(fixture: ComponentFixture<AccessAdminPageComponent>, testId: string): HTMLElement {
  const found = fixture.nativeElement.querySelector(`[data-testid="${testId}"]`) as HTMLElement | null;
  if (!found) {
    throw new Error(`Missing ${testId}`);
  }
  return found;
}

const ACTIONS = [
  ['accept', 'pending', 'access-request-accept', 'acceptSelectedUser'],
  ['disable', 'active', 'access-request-disable', 'disableSelectedUser'],
  ['reactivate', 'disabled', 'access-request-reactivate', 'reactivateSelectedUser'],
  ['permissions', 'active', 'access-request-permissions', 'replaceSelectedPermissions'],
] as const;

const MUTATION_METHODS = [
  'acceptSelectedUser',
  'disableSelectedUser',
  'reactivateSelectedUser',
  'replaceSelectedPermissions',
  'previewSelectedUserRelink',
  'confirmSelectedUserRelink',
  'cancelSelectedUserRelink',
] as const;

describe('AccessAdminPageComponent', () => {
  it.each(ACTIONS)('sends %s only to %s', async (_action, status, testId, method) => {
    const { fixture, facade } = render(status);

    element(fixture, testId).click();
    fixture.detectChanges();
    const reason = element(fixture, 'access-action-reason') as HTMLTextAreaElement;
    reason.value = 'سبب إداري موثق';
    reason.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    element(fixture, 'access-confirm-action').click();
    await fixture.whenStable();

    expect(facade[method]).toHaveBeenCalledWith('سبب إداري موثق');
    for (const neighboringMethod of MUTATION_METHODS) {
      if (neighboringMethod !== method) {
        expect(facade[neighboringMethod]).not.toHaveBeenCalled();
      }
    }
  });

  it('passes the edited individual permission codes to the facade', () => {
    const { fixture, facade } = render();
    const editPermission = element(fixture, 'access-permission-abwab.doors.edit') as HTMLInputElement;
    editPermission.checked = true;
    editPermission.dispatchEvent(new Event('change'));

    expect(facade.setSelectedPermissionCodes).toHaveBeenCalledWith(
      new Set(['abwab.doors.create', 'abwab.doors.edit']),
    );
  });

  it('routes relink preview and explicit confirmation to their distinct facade methods', async () => {
    const { fixture, facade } = render();
    const newSub = element(fixture, 'access-relink-new-sub') as HTMLInputElement;
    newSub.value = RELINK_PREVIEW.newSub;
    newSub.dispatchEvent(new Event('input'));
    const evidence = element(fixture, 'access-relink-evidence') as HTMLInputElement;
    evidence.value = 'verified-evidence';
    evidence.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    element(fixture, 'access-relink-preview').click();
    await fixture.whenStable();

    expect(facade.previewSelectedUserRelink).toHaveBeenCalledWith({
      newSub: RELINK_PREVIEW.newSub,
      evidenceToken: 'verified-evidence',
    });
    for (const method of MUTATION_METHODS) {
      if (method !== 'previewSelectedUserRelink') {
        expect(facade[method]).not.toHaveBeenCalled();
      }
    }

    facade.relinkPreview.set(RELINK_PREVIEW);
    fixture.detectChanges();
    const reason = element(fixture, 'access-relink-confirm-reason') as HTMLTextAreaElement;
    reason.value = 'تصحيح المعرّف';
    reason.dispatchEvent(new Event('input'));
    const confirmed = element(fixture, 'access-relink-confirmed') as HTMLInputElement;
    confirmed.checked = true;
    confirmed.dispatchEvent(new Event('change'));
    fixture.detectChanges();
    element(fixture, 'access-relink-confirm').click();
    await fixture.whenStable();

    expect(facade.confirmSelectedUserRelink).toHaveBeenCalledWith('تصحيح المعرّف');
    expect(facade.confirmSelectedUserRelink).toHaveBeenCalledTimes(1);
    expect(facade.previewSelectedUserRelink).toHaveBeenCalledTimes(1);
    for (const method of MUTATION_METHODS) {
      if (method !== 'previewSelectedUserRelink' && method !== 'confirmSelectedUserRelink') {
        expect(facade[method]).not.toHaveBeenCalled();
      }
    }
  });

  it('routes relink cancellation only to its facade method', () => {
    const { fixture, facade } = render();
    facade.relinkPreview.set(RELINK_PREVIEW);
    fixture.detectChanges();

    element(fixture, 'access-relink-cancel').click();

    expect(facade.cancelSelectedUserRelink).toHaveBeenCalledTimes(1);
    for (const method of MUTATION_METHODS) {
      if (method !== 'cancelSelectedUserRelink') {
        expect(facade[method]).not.toHaveBeenCalled();
      }
    }
  });

  it('renders actor attribution and passes the actor filter to audit retrieval', async () => {
    const { fixture, facade } = render();
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
    await fixture.whenStable();

    expect(facade.updateAuditQuery).toHaveBeenCalledWith({
      targetUserId: undefined,
      actorUserId: 9,
      actionType: undefined,
      permissionCode: undefined,
    });
  });

  it('uses a labelled section instead of a nested main landmark', () => {
    const { fixture } = render();
    const root = fixture.nativeElement as HTMLElement;

    expect(root.firstElementChild?.tagName).toBe('SECTION');
    expect(root.firstElementChild?.getAttribute('aria-labelledby')).toBe('access-admin-page-title');
    expect(root.querySelector('main')).toBeNull();
  });
});
