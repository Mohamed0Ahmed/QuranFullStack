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

  it('keeps identity recovery out of the permission workspace', () => {
    const fixture = setup();

    expect(fixture.nativeElement.querySelector('[data-testid="access-relink-new-sub"]')).toBeNull();
    expect(fixture.nativeElement.querySelector('[data-testid="access-relink-preview"]')).toBeNull();
    expect(fixture.nativeElement.textContent).not.toContain('إعادة ربط');
  });

  it('promises permissions on acceptance only once some are selected', () => {
    const fixture = setup({ ...USER, status: 'pending', permissionCodes: [] });

    expect(element(fixture, 'access-request-accept').textContent).toContain('قبول وتفعيل دون صلاحيات');
    expect(element(fixture, 'access-pending-permissions-note').textContent).toContain(
      'تُمنح الصلاحيات المحددة عند تفعيل الحساب',
    );
    expect(fixture.nativeElement.querySelector('[data-testid="access-request-disable"]')).toBeNull();
    expect(fixture.nativeElement.querySelector('[data-testid="access-request-reactivate"]')).toBeNull();

    fixture.componentRef.setInput('permissionDiff', CHANGED_DIFF);
    fixture.detectChanges();

    expect(element(fixture, 'access-request-accept').textContent).toContain(
      'قبول وتفعيل مع الصلاحيات المحددة',
    );
  });

  it('lists the grants an acceptance will assign before it is confirmed', () => {
    const fixture = setup({ ...USER, status: 'pending', permissionCodes: [] });
    fixture.componentRef.setInput('permissionDiff', CHANGED_DIFF);
    fixture.detectChanges();

    element(fixture, 'access-request-accept').click();
    fixture.detectChanges();

    const confirmation = element(fixture, 'access-action-confirmation');
    expect(element(fixture, 'access-accept-with-permissions').textContent).toContain(
      'تُمنح الصلاحيات التالية فور تفعيل الحساب',
    );
    expect(
      confirmation.querySelector('[data-testid="access-accept-without-permissions"]'),
    ).toBeNull();
    const diff = confirmation.querySelector('[aria-label="فرق الصلاحيات"]');
    expect(diff?.textContent).toContain('تعديل باب');
    expect(diff?.textContent).toContain('abwab.doors.edit');
  });

  it('explains that a disabled account holds nothing and that reactivation restores nothing', () => {
    const fixture = setup({ ...USER, status: 'disabled', permissionCodes: [] });

    expect(element(fixture, 'access-request-reactivate')).toBeTruthy();
    const region = element(fixture, 'access-disabled-permissions');
    expect(region.textContent).toContain('لا يحمل صلاحيات مباشرة، ولا يمكن إسنادها قبل إعادة التفعيل');
    expect(region.textContent).toContain('إعادة التفعيل تبدأ بلا صلاحيات مباشرة سابقة');
    expect(fixture.nativeElement.querySelector('qd-access-permission-editor')).toBeNull();
  });

  it('separates the permission save from account disabling and states what disabling costs', () => {
    const fixture = setup();

    const actions = element(fixture, 'access-account-actions');
    expect(actions.textContent).toContain('يوقف التعطيل وصول الحساب ويزيل جميع صلاحياته المباشرة نهائيًا');
    expect(actions.querySelector('[data-testid="access-request-disable"]')).toBeTruthy();

    fixture.componentRef.setInput('permissionDiff', CHANGED_DIFF);
    fixture.detectChanges();

    const draftBar = element(fixture, 'access-permission-draft-bar');
    expect(draftBar.querySelector('[data-testid="access-request-permissions"]')).toBeTruthy();
    expect(draftBar.querySelector('[data-testid="access-request-disable"]')).toBeNull();
  });

  it('states how an Active Owner receives access instead of rendering an editor', () => {
    const fixture = setup({ ...USER, isOwner: true, permissionCodes: [] });

    const region = element(fixture, 'access-owner-permissions');
    expect(region.textContent).toContain('كامل صلاحيات الإدارة عبر تجاوز المالك');
    expect(region.textContent).toContain('تُدار عضوية المالك عبر مطابقة المالكين');
    expect(fixture.nativeElement.querySelector('qd-access-permission-editor')).toBeNull();
    expect(fixture.nativeElement.querySelector('[data-testid="access-request-accept"]')).toBeNull();
    expect(fixture.nativeElement.querySelector('[data-testid="access-request-disable"]')).toBeNull();
    expect(fixture.nativeElement.querySelector('[data-testid="access-request-reactivate"]')).toBeNull();
    expect(fixture.nativeElement.querySelector('select')).toBeNull();
  });

  it('does not claim administrative access for an Owner account that is not active', () => {
    const fixture = setup({ ...USER, isOwner: true, status: 'pending', permissionCodes: [] });

    expect(element(fixture, 'access-owner-permissions').textContent).toContain(
      'لا يسري تجاوز المالك إلا على حساب مالك نشط',
    );
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

    expect(element(fixture, 'access-request-accept').textContent).toContain('قبول وتفعيل دون صلاحيات');
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
