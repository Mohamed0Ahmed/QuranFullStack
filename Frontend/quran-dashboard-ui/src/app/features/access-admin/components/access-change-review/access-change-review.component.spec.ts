import { ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import {
  AccessPermissionDiff,
  AccessUserWorkflowAction,
} from '../../models/access-admin.models';
import { AccessPermissionGroup } from '../../models/access-admin-permissions';
import { AccessChangeReviewComponent } from './access-change-review.component';

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
  action: AccessUserWorkflowAction | null,
  options: {
    permissionDiff?: AccessPermissionDiff;
    canAssignPermissions?: boolean;
    busyAction?: string | null;
  } = {},
): { fixture: ComponentFixture<AccessChangeReviewComponent>; confirmed: string[]; cancelled: number } {
  TestBed.configureTestingModule({ imports: [AccessChangeReviewComponent] });
  const fixture = TestBed.createComponent(AccessChangeReviewComponent);
  fixture.componentRef.setInput('action', action);
  fixture.componentRef.setInput('groups', GROUPS);
  fixture.componentRef.setInput('permissionDiff', options.permissionDiff ?? EMPTY_DIFF);
  fixture.componentRef.setInput('canAssignPermissions', options.canAssignPermissions ?? true);
  fixture.componentRef.setInput('busyAction', options.busyAction ?? null);
  const confirmed: string[] = [];
  const state = { fixture, confirmed, cancelled: 0 };
  fixture.componentInstance.confirmed.subscribe((reason) => confirmed.push(reason));
  fixture.componentInstance.cancelled.subscribe(() => (state.cancelled += 1));
  fixture.detectChanges();
  return state;
}

function element(
  fixture: ComponentFixture<AccessChangeReviewComponent>,
  testId: string,
): HTMLElement {
  const found = fixture.nativeElement.querySelector(`[data-testid="${testId}"]`) as HTMLElement | null;
  if (!found) {
    throw new Error(`Missing ${testId}`);
  }
  return found;
}

function fillReason(
  fixture: ComponentFixture<AccessChangeReviewComponent>,
  value = 'مراجعة الوصول',
): void {
  const reason = element(fixture, 'access-action-reason') as HTMLTextAreaElement;
  reason.value = value;
  reason.dispatchEvent(new Event('input'));
  fixture.detectChanges();
}

describe('AccessChangeReviewComponent', () => {
  it('renders nothing until an action is pending', () => {
    const { fixture } = setup(null);

    expect(
      (fixture.nativeElement as HTMLElement).querySelector(
        '[data-testid="access-action-confirmation"]',
      ),
    ).toBeNull();
  });

  it('requires a reason before it will confirm anything', () => {
    const { fixture, confirmed } = setup('disable');

    const confirm = element(fixture, 'access-confirm-action') as HTMLButtonElement;
    expect(confirm.disabled).toBe(true);
    confirm.click();
    expect(confirmed).toEqual([]);

    fillReason(fixture);
    element(fixture, 'access-confirm-action').click();

    expect(confirmed).toEqual(['مراجعة الوصول']);
  });

  it('trims the captured reason before emitting it', () => {
    const { fixture, confirmed } = setup('disable');

    fillReason(fixture, '  تصحيح إداري  ');
    element(fixture, 'access-confirm-action').click();

    expect(confirmed).toEqual(['تصحيح إداري']);
  });

  it('states the cost of disabling in the danger tone the at-rest copy does not use', () => {
    const { fixture } = setup('disable');
    const warning = element(fixture, 'access-disable-warning');

    expect(warning.textContent).toContain('سيؤدي التعطيل إلى إزالة جميع الصلاحيات المباشرة');
    expect(warning.className).toContain('access-change-review__warning');
  });

  it('lists every permission change by Arabic label and stable code before confirmation', () => {
    const { fixture } = setup('permissions', { permissionDiff: CHANGED_DIFF });
    const confirmation = element(fixture, 'access-action-confirmation');

    expect(confirmation.textContent).toContain('الصلاحيات المضافة');
    expect(confirmation.textContent).toContain('تعديل باب');
    expect(confirmation.textContent).toContain('abwab.doors.edit');
    expect(confirmation.textContent).toContain('الصلاحيات الملغاة');
    expect(confirmation.textContent).toContain('إضافة باب');
    expect(confirmation.textContent).toContain('abwab.doors.create');
  });

  it('blocks a permission save whose diff is empty even with a reason typed', () => {
    const { fixture, confirmed } = setup('permissions', { permissionDiff: CHANGED_DIFF });
    fillReason(fixture);
    expect((element(fixture, 'access-confirm-action') as HTMLButtonElement).disabled).toBe(false);

    fixture.componentRef.setInput('permissionDiff', EMPTY_DIFF);
    fixture.detectChanges();

    const confirm = element(fixture, 'access-confirm-action') as HTMLButtonElement;
    expect(confirm.disabled).toBe(true);
    confirm.click();

    expect(confirmed).toEqual([]);
  });

  it('lists the grants an acceptance will assign before it is confirmed', () => {
    const { fixture } = setup('accept', { permissionDiff: CHANGED_DIFF });
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

  it('states that acceptance grants no permissions while assignment is unavailable', () => {
    const { fixture } = setup('accept', {
      permissionDiff: CHANGED_DIFF,
      canAssignPermissions: false,
    });
    const confirmation = element(fixture, 'access-action-confirmation');

    expect(confirmation.textContent).toContain('سيُفعَّل الحساب دون إسناد صلاحيات مباشرة');
    expect(confirmation.querySelector('[aria-label="فرق الصلاحيات"]')).toBeNull();
  });

  it('promises no permission payload for a reactivation and says nothing is restored', () => {
    const { fixture } = setup('reactivate', { permissionDiff: CHANGED_DIFF });
    const confirmation = element(fixture, 'access-action-confirmation');

    expect(confirmation.textContent).toContain('لن تُستعاد الصلاحيات السابقة عند إعادة التفعيل');
    expect(confirmation.querySelector('[aria-label="فرق الصلاحيات"]')).toBeNull();
  });

  it('drops a half-typed reason when the pending action changes', () => {
    const { fixture } = setup('disable');
    fillReason(fixture);

    fixture.componentRef.setInput('action', 'permissions');
    fixture.detectChanges();

    expect((element(fixture, 'access-action-reason') as HTMLTextAreaElement).value).toBe('');
  });

  it('emits a cancellation instead of a confirmation when the review is abandoned', () => {
    const state = setup('disable');
    fillReason(state.fixture);

    element(state.fixture, 'access-cancel-action').click();

    expect(state.cancelled).toBe(1);
    expect(state.confirmed).toEqual([]);
  });

  it('freezes both decisions while a write is in flight', () => {
    const { fixture, confirmed } = setup('disable', { busyAction: 'disable' });

    expect((element(fixture, 'access-action-reason') as HTMLTextAreaElement).disabled).toBe(true);
    expect((element(fixture, 'access-cancel-action') as HTMLButtonElement).disabled).toBe(true);
    const confirm = element(fixture, 'access-confirm-action') as HTMLButtonElement;
    expect(confirm.disabled).toBe(true);
    confirm.click();

    expect(confirmed).toEqual([]);
  });
});
