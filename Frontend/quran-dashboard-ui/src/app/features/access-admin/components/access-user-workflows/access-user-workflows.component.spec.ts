import { describe, expect, it } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AccessUserDetail } from '../../../../core/api/generated/models/access-user-detail';
import { AccessPermissionDiff } from '../../models/access-admin.models';
import { AccessPermissionGroup } from '../../models/access-admin-permissions';
import { AccessUserWorkflowsComponent } from './access-user-workflows.component';

const USER: AccessUserDetail = {
  id: 17,
  sub: 'subject-17',
  email: 'member@example.test',
  normalizedEmail: 'member@example.test',
  userName: null,
  displayName: 'عضو',
  title: null,
  status: 'active',
  isOwner: false,
  permissionCodes: ['abwab.doors.create'],
  createdAtUtc: '2026-01-01T00:00:00Z',
  updatedAtUtc: '2026-01-01T00:00:00Z',
  version: 4,
};

const GROUPS: AccessPermissionGroup[] = [
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
const EMPTY_DIFF: AccessPermissionDiff = { granted: [], revoked: [] };
const CHANGED_DIFF: AccessPermissionDiff = {
  granted: ['abwab.doors.edit'],
  revoked: ['abwab.doors.create'],
};

function setup(
  user: AccessUserDetail = USER,
  canAssignPermissions = true,
): ComponentFixture<AccessUserWorkflowsComponent> {
  TestBed.configureTestingModule({ imports: [AccessUserWorkflowsComponent] });
  const fixture = TestBed.createComponent(AccessUserWorkflowsComponent);
  fixture.componentRef.setInput('user', user);
  fixture.componentRef.setInput('groups', GROUPS);
  fixture.componentRef.setInput('selectedCodes', new Set(['abwab.doors.create']));
  fixture.componentRef.setInput('permissionDiff', EMPTY_DIFF);
  fixture.componentRef.setInput('busyAction', null);
  fixture.componentRef.setInput('relinkPreview', null);
  fixture.componentRef.setInput('canAssignPermissions', canAssignPermissions);
  fixture.detectChanges();
  return fixture;
}

function element(fixture: ComponentFixture<AccessUserWorkflowsComponent>, testId: string): HTMLElement {
  const found = fixture.nativeElement.querySelector(`[data-testid="${testId}"]`) as HTMLElement | null;
  if (!found) {
    throw new Error(`Missing ${testId}`);
  }
  return found;
}

describe('AccessUserWorkflowsComponent', () => {
  it('shows the grant-removal warning before disabling and requires a reason before confirmation', () => {
    const fixture = setup();
    const actions: { kind: string; reason: string }[] = [];
    fixture.componentInstance.actionConfirmed.subscribe((action) => actions.push(action));

    element(fixture, 'access-request-disable').click();
    fixture.detectChanges();

    expect(element(fixture, 'access-action-confirmation').textContent).toContain(
      'سيؤدي التعطيل إلى إزالة جميع الصلاحيات المباشرة',
    );
    const reason = element(fixture, 'access-action-reason') as HTMLTextAreaElement;
    reason.value = 'مراجعة الوصول';
    reason.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    (element(fixture, 'access-confirm-action') as HTMLButtonElement).click();

    expect(actions).toEqual([{ kind: 'disable', reason: 'مراجعة الوصول' }]);
  });

  it('lists every permission change by Arabic label and stable code before confirmation', () => {
    const fixture = setup();
    fixture.componentRef.setInput('permissionDiff', CHANGED_DIFF);
    fixture.detectChanges();

    element(fixture, 'access-request-permissions').click();
    fixture.detectChanges();

    const confirmation = element(fixture, 'access-action-confirmation');
    expect(confirmation.textContent).toContain('الصلاحيات المضافة');
    expect(confirmation.textContent).toContain('تعديل باب');
    expect(confirmation.textContent).toContain('abwab.doors.edit');
    expect(confirmation.textContent).toContain('الصلاحيات الملغاة');
    expect(confirmation.textContent).toContain('إضافة باب');
    expect(confirmation.textContent).toContain('abwab.doors.create');
    expect((element(fixture, 'access-confirm-action') as HTMLButtonElement).disabled).toBe(true);
  });

  it('requires a preview before relink confirmation and never renders an email-only control', () => {
    const fixture = setup();
    const previews: { newSub: string; evidenceToken: string }[] = [];
    const confirmations: string[] = [];
    fixture.componentInstance.relinkPreviewRequested.subscribe((request) => previews.push(request));
    fixture.componentInstance.relinkConfirmed.subscribe((reason) => confirmations.push(reason));

    expect(fixture.nativeElement.querySelector('[data-testid="access-relink-confirmation"]')).toBeNull();
    expect(fixture.nativeElement.querySelector('input[type="email"]')).toBeNull();

    const newSub = element(fixture, 'access-relink-new-sub') as HTMLInputElement;
    newSub.value = 'new-subject';
    newSub.dispatchEvent(new Event('input'));
    const evidence = element(fixture, 'access-relink-evidence') as HTMLInputElement;
    expect(evidence.type).toBe('password');
    evidence.value = 'verified-evidence';
    evidence.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    element(fixture, 'access-relink-preview').click();
    fixture.detectChanges();

    expect(previews).toEqual([{ newSub: 'new-subject', evidenceToken: 'verified-evidence' }]);
    expect(evidence.value).toBe('');

    fixture.componentRef.setInput('relinkPreview', {
      userId: 17,
      oldSub: 'subject-17',
      newSub: 'new-subject',
      version: 4,
      isOwner: false,
    });
    fixture.detectChanges();

    const confirmation = element(fixture, 'access-relink-confirmation');
    expect(confirmation.textContent).toContain('المعرّف الحالي');
    expect(confirmation.textContent).toContain('subject-17');
    expect(confirmation.textContent).toContain('المعرّف الجديد');
    expect(confirmation.textContent).toContain('new-subject');
    expect(confirmation.textContent).not.toContain('←');

    const reason = element(fixture, 'access-relink-confirm-reason') as HTMLTextAreaElement;
    reason.value = 'تحديث المعرّف';
    reason.dispatchEvent(new Event('input'));
    const confirmed = element(fixture, 'access-relink-confirmed') as HTMLInputElement;
    confirmed.checked = true;
    confirmed.dispatchEvent(new Event('change'));
    fixture.detectChanges();
    element(fixture, 'access-relink-confirm').click();

    expect(confirmations).toEqual(['تحديث المعرّف']);
  });

  it('clears relink values and emits cancellation when the preview is abandoned', () => {
    const fixture = setup();
    let cancellations = 0;
    fixture.componentInstance.relinkCancelled.subscribe(() => cancellations++);

    const newSub = element(fixture, 'access-relink-new-sub') as HTMLInputElement;
    newSub.value = 'new-subject';
    newSub.dispatchEvent(new Event('input'));
    const evidence = element(fixture, 'access-relink-evidence') as HTMLInputElement;
    evidence.value = 'verified-evidence';
    evidence.dispatchEvent(new Event('input'));
    fixture.componentRef.setInput('relinkPreview', {
      userId: 17,
      oldSub: 'subject-17',
      newSub: 'new-subject',
      version: 4,
      isOwner: false,
    });
    fixture.detectChanges();

    element(fixture, 'access-relink-cancel').click();
    fixture.detectChanges();

    expect(cancellations).toBe(1);
    expect(newSub.value).toBe('');
    expect(evidence.value).toBe('');
  });

  it('shows the pending acceptance path without a disable path', () => {
    const fixture = setup({ ...USER, status: 'pending', permissionCodes: [] });

    expect(element(fixture, 'access-request-accept')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('[data-testid="access-request-disable"]')).toBeNull();
    expect(fixture.nativeElement.querySelector('[data-testid="access-request-reactivate"]')).toBeNull();
  });

  it('shows reactivation as an empty-grant restart for a disabled account', () => {
    const fixture = setup({ ...USER, status: 'disabled', permissionCodes: [] });

    expect(element(fixture, 'access-request-reactivate')).toBeTruthy();
    expect(fixture.nativeElement.textContent).toContain('إعادة التفعيل تبدأ بلا صلاحيات مباشرة سابقة');
    expect(fixture.nativeElement.querySelector('qd-access-permission-editor')).toBeNull();
  });

  it('keeps an Owner role and grants read-only without a role selector or status mutation', () => {
    const fixture = setup({ ...USER, isOwner: true, permissionCodes: [] });

    expect(fixture.nativeElement.textContent).toContain('لا يمكن تعديلها من هذه الواجهة');
    expect(fixture.nativeElement.querySelector('qd-access-permission-editor')).toBeNull();
    expect(fixture.nativeElement.querySelector('[data-testid="access-request-accept"]')).toBeNull();
    expect(fixture.nativeElement.querySelector('[data-testid="access-request-disable"]')).toBeNull();
    expect(fixture.nativeElement.querySelector('[data-testid="access-request-reactivate"]')).toBeNull();
    expect(fixture.nativeElement.querySelector('select')).toBeNull();
  });

  it.each([
    ['active', 'نشط'],
    ['pending', 'معلّق'],
    ['disabled', 'معطّل'],
  ] as const)('shows Owner membership alongside the %s lifecycle status', (status, statusLabel) => {
    const fixture = setup({ ...USER, isOwner: true, status, permissionCodes: [] });
    const badges = fixture.nativeElement.querySelectorAll(
      '.access-user-workflows__header .qd-badge',
    ) as NodeListOf<HTMLElement>;
    const labels = Array.from(badges, (badge) => badge.textContent?.trim());

    expect(labels).toEqual(['مالك', statusLabel]);
  });

  it('keeps the disable path and hides the permission-save path when assignment is unavailable', () => {
    const fixture = setup(USER, false);

    expect(element(fixture, 'access-request-disable')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('[data-testid="access-request-permissions"]')).toBeNull();
    expect(element(fixture, 'access-permissions-unavailable').textContent).toContain(
      'لم يطرأ أي تغيير على الوصول الحالي',
    );
    expect(
      (element(fixture, 'access-permission-abwab.doors.create') as HTMLInputElement).disabled,
    ).toBe(true);
  });

  it('states that acceptance grants no permissions while assignment is unavailable', () => {
    const fixture = setup({ ...USER, status: 'pending', permissionCodes: [] }, false);
    fixture.componentRef.setInput('permissionDiff', CHANGED_DIFF);
    fixture.detectChanges();

    element(fixture, 'access-request-accept').click();
    fixture.detectChanges();

    const confirmation = element(fixture, 'access-action-confirmation');
    expect(confirmation.textContent).toContain('سيُفعَّل الحساب دون إسناد صلاحيات مباشرة');
    expect(confirmation.querySelector('[aria-label="فرق الصلاحيات"]')).toBeNull();
  });

  it('replaces the permission editor with a recoverable error when the catalogue request fails', () => {
    const fixture = setup(USER, false);
    let retries = 0;
    fixture.componentInstance.catalogueRetryRequested.subscribe(() => retries++);
    fixture.componentRef.setInput('catalogueError', 'تعذر تحميل كتالوج الصلاحيات.');
    fixture.detectChanges();

    const region = element(fixture, 'access-permissions-section');
    expect(region.textContent).toContain('تعذر تحميل كتالوج الصلاحيات.');
    expect(region.querySelector('qd-access-permission-editor')).toBeNull();
    (region.querySelector('[data-testid="qd-state-action"]') as HTMLButtonElement).click();

    expect(retries).toBe(1);
  });

  it('offers no permission save path while the draft matches the stored grants', () => {
    const fixture = setup();

    expect(fixture.nativeElement.querySelector('[data-testid="access-permission-draft-bar"]')).toBeNull();
    expect(fixture.nativeElement.querySelector('[data-testid="access-request-permissions"]')).toBeNull();
    expect(
      fixture.nativeElement.querySelector('[data-testid="access-permission-diff-summary"]'),
    ).toBeNull();
  });

  it('summarises a dirty draft and emits a discard request from the same bar', () => {
    const fixture = setup();
    let discards = 0;
    fixture.componentInstance.draftDiscarded.subscribe(() => discards++);
    fixture.componentRef.setInput('permissionDiff', CHANGED_DIFF);
    fixture.detectChanges();

    expect(element(fixture, 'access-permission-diff-summary-text').textContent).toBe(
      'صلاحيات مضافة: 1، صلاحيات ملغاة: 1',
    );
    const glyphs = element(fixture, 'access-permission-diff-summary');
    expect(glyphs.getAttribute('aria-hidden')).toBe('true');
    expect(glyphs.hasAttribute('aria-label')).toBe(false);
    element(fixture, 'access-discard-draft').click();

    expect(discards).toBe(1);
  });

  it('offers only a discard path for a pending account whose commit is acceptance', () => {
    const fixture = setup({ ...USER, status: 'pending', permissionCodes: [] });
    fixture.componentRef.setInput('permissionDiff', CHANGED_DIFF);
    fixture.detectChanges();

    expect(element(fixture, 'access-discard-draft')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('[data-testid="access-request-permissions"]')).toBeNull();
    expect(element(fixture, 'access-request-accept')).toBeTruthy();
  });

  it('holds back relink while permission edits are unsaved', () => {
    const fixture = setup();
    const previews: unknown[] = [];
    fixture.componentInstance.relinkPreviewRequested.subscribe((request) => previews.push(request));
    fixture.componentRef.setInput('permissionDiff', CHANGED_DIFF);
    fixture.detectChanges();

    const newSub = element(fixture, 'access-relink-new-sub') as HTMLInputElement;
    newSub.value = 'new-subject';
    newSub.dispatchEvent(new Event('input'));
    const evidence = element(fixture, 'access-relink-evidence') as HTMLInputElement;
    evidence.value = 'verified-evidence';
    evidence.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    expect(element(fixture, 'access-relink-blocked')).toBeTruthy();
    expect((element(fixture, 'access-relink-preview') as HTMLButtonElement).disabled).toBe(true);
    element(fixture, 'access-relink-preview').click();

    expect(previews).toEqual([]);
  });

  it('cancels an inline confirmation when the selected target changes', () => {
    const fixture = setup();

    element(fixture, 'access-request-disable').click();
    fixture.detectChanges();
    expect(element(fixture, 'access-action-confirmation')).toBeTruthy();

    fixture.componentRef.setInput('user', { ...USER, id: 18, email: 'other@example.test' });
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[data-testid="access-action-confirmation"]')).toBeNull();
  });
});
