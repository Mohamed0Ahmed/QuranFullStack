import { ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { AccessUserDetail } from '../../../../core/api/generated/models/access-user-detail';
import { AccessUserLifecycleAction } from '../../models/access-admin.models';
import { AccessLifecycleActionsComponent } from './access-lifecycle-actions.component';

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

function setup(
  user: AccessUserDetail = USER,
  options: { acceptGrantsPermissions?: boolean; busyAction?: string | null } = {},
): {
  fixture: ComponentFixture<AccessLifecycleActionsComponent>;
  requested: AccessUserLifecycleAction[];
} {
  TestBed.configureTestingModule({ imports: [AccessLifecycleActionsComponent] });
  const fixture = TestBed.createComponent(AccessLifecycleActionsComponent);
  fixture.componentRef.setInput('user', user);
  fixture.componentRef.setInput('busyAction', options.busyAction ?? null);
  fixture.componentRef.setInput('acceptGrantsPermissions', options.acceptGrantsPermissions ?? false);
  const requested: AccessUserLifecycleAction[] = [];
  fixture.componentInstance.actionRequested.subscribe((action) => requested.push(action));
  fixture.detectChanges();
  return { fixture, requested };
}

function element(
  fixture: ComponentFixture<AccessLifecycleActionsComponent>,
  testId: string,
): HTMLElement {
  const found = fixture.nativeElement.querySelector(`[data-testid="${testId}"]`) as HTMLElement | null;
  if (!found) {
    throw new Error(`Missing ${testId}`);
  }
  return found;
}

function absent(
  fixture: ComponentFixture<AccessLifecycleActionsComponent>,
  testId: string,
): boolean {
  return fixture.nativeElement.querySelector(`[data-testid="${testId}"]`) === null;
}

describe('AccessLifecycleActionsComponent', () => {
  it.each([
    ['pending', 'access-request-accept', 'accept'],
    ['active', 'access-request-disable', 'disable'],
    ['disabled', 'access-request-reactivate', 'reactivate'],
  ] as const)('offers only the %s account its own commit', (status, testId, action) => {
    const { fixture, requested } = setup({ ...USER, status });

    element(fixture, testId).click();

    expect(requested).toEqual([action]);
    const offered = ['access-request-accept', 'access-request-disable', 'access-request-reactivate']
      .filter((candidate) => !absent(fixture, candidate));
    expect(offered).toEqual([testId]);
  });

  it('states what disabling costs, in a calm tone, before anything is chosen', () => {
    const { fixture } = setup();
    const cost = element(fixture, 'access-disable-cost');

    expect(cost.textContent).toContain(
      'يوقف التعطيل وصول الحساب ويزيل جميع صلاحياته المباشرة نهائيًا',
    );
    expect(cost.className).toContain('qd-text-muted');
    expect(cost.className).not.toContain('warning');
  });

  it('keeps the account actions separate from any permission-save control', () => {
    const { fixture } = setup();
    const region = element(fixture, 'access-account-actions');

    expect(region.querySelector('[data-testid="access-request-disable"]')).toBeTruthy();
    expect(region.querySelector('[data-testid="access-request-permissions"]')).toBeNull();
    expect(region.querySelector('[data-testid="access-discard-draft"]')).toBeNull();
  });

  it('promises permissions on acceptance only once some are actually granted', () => {
    const { fixture } = setup({ ...USER, status: 'pending', permissionCodes: [] });

    expect(element(fixture, 'access-request-accept').textContent).toContain(
      'قبول وتفعيل دون صلاحيات',
    );

    fixture.componentRef.setInput('acceptGrantsPermissions', true);
    fixture.detectChanges();

    expect(element(fixture, 'access-request-accept').textContent).toContain(
      'قبول وتفعيل مع الصلاحيات المحددة',
    );
  });

  it('offers an Owner account no lifecycle commit at all', () => {
    const { fixture } = setup({ ...USER, isOwner: true, permissionCodes: [] });

    expect(absent(fixture, 'access-account-actions')).toBe(true);
    expect(absent(fixture, 'access-request-disable')).toBe(true);
  });

  it('emits nothing while a write is in flight', () => {
    const { fixture, requested } = setup(USER, { busyAction: 'permissions' });

    const disable = element(fixture, 'access-request-disable') as HTMLButtonElement;
    expect(disable.disabled).toBe(true);
    disable.click();

    expect(requested).toEqual([]);
  });
});
