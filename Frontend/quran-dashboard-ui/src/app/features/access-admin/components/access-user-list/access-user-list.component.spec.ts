import { describe, expect, it } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AccessUserSummary } from '../../../../core/api/generated/models/access-user-summary';
import { AccessUserListQuery } from '../../models/access-admin.models';
import { AccessUserListComponent } from './access-user-list.component';

const USER: AccessUserSummary = {
  id: 17,
  email: 'member@example.test',
  displayName: 'عضو',
  status: 'pending',
  isOwner: false,
  permissionCount: 0,
  createdAtUtc: '2026-01-01T00:00:00Z',
  updatedAtUtc: '2026-01-01T00:00:00Z',
  version: 4,
};

const QUERY: AccessUserListQuery = { page: 1, pageSize: 25 };

function setup(users: readonly AccessUserSummary[] = [USER]): ComponentFixture<AccessUserListComponent> {
  TestBed.configureTestingModule({ imports: [AccessUserListComponent] });
  const fixture = TestBed.createComponent(AccessUserListComponent);
  fixture.componentRef.setInput('users', users);
  fixture.componentRef.setInput('selectedUserId', null);
  fixture.componentRef.setInput('query', QUERY);
  fixture.componentRef.setInput('page', 1);
  fixture.componentRef.setInput('pageSize', 25);
  fixture.componentRef.setInput('totalCount', 1);
  fixture.componentRef.setInput('loading', false);
  fixture.componentRef.setInput('error', null);
  fixture.detectChanges();
  return fixture;
}

function element(fixture: ComponentFixture<AccessUserListComponent>, testId: string): HTMLElement {
  const found = fixture.nativeElement.querySelector(`[data-testid="${testId}"]`) as HTMLElement | null;
  if (!found) {
    throw new Error(`Missing ${testId}`);
  }
  return found;
}

describe('AccessUserListComponent', () => {
  it('emits the selected filter values and a trimmed search term', () => {
    const fixture = setup();
    const filters: unknown[] = [];
    fixture.componentInstance.filtersChange.subscribe((value) => filters.push(value));

    const search = element(fixture, 'access-users-search') as HTMLInputElement;
    search.value = '  معلّم  ';
    search.dispatchEvent(new Event('input'));
    const status = element(fixture, 'access-users-status') as HTMLSelectElement;
    status.value = 'pending';
    status.dispatchEvent(new Event('change'));
    const owner = element(fixture, 'access-users-owner') as HTMLSelectElement;
    owner.value = 'non-owner';
    owner.dispatchEvent(new Event('change'));
    element(fixture, 'access-users-filter-form').dispatchEvent(new Event('submit'));

    expect(filters).toEqual([{ status: 'pending', isOwner: false, search: 'معلّم' }]);
  });

  it('emits the chosen user id from the rendered list', () => {
    const fixture = setup();
    const selected: number[] = [];
    fixture.componentInstance.userSelected.subscribe((userId) => selected.push(userId));

    element(fixture, 'access-user-17').click();

    expect(selected).toEqual([17]);
  });

  it.each([
    ['a whitespace-only stored name', '   '],
    ['no stored name', null],
  ])('labels the row with the email when the account has %s', (_scenario, displayName) => {
    const fixture = setup([{ ...USER, displayName }]);

    const name = element(fixture, 'access-user-17').querySelector('.access-user-list__name');

    expect(name?.textContent?.trim()).toBe('member@example.test');
  });

  it.each([
    ['an active Owner', { ...USER, status: 'active' as const, isOwner: true }, ['مالك', 'نشط']],
    ['a pending Owner', { ...USER, isOwner: true }, ['مالك', 'معلّق']],
    [
      'a disabled Owner',
      {
        ...USER,
        status: 'disabled' as const,
        isOwner: true,
        id: 18,
        email: 'disabled@example.test',
      },
      ['مالك', 'معطّل'],
    ],
  ])('renders membership and lifecycle independently for %s', (_scenario, user, expectedLabels) => {
    const fixture = setup([user]);
    const labels = Array.from(element(fixture, `access-user-${user.id}`).querySelectorAll('.qd-badge')).map(
      (badge) => badge.textContent?.trim(),
    );

    expect(labels).toEqual(expectedLabels);
  });

  it('reads a status outside the known set as unknown rather than as disabled', () => {
    const fixture = setup([{ ...USER, status: 'archived' }]);

    const labels = Array.from(
      element(fixture, 'access-user-17').querySelectorAll('.qd-badge'),
      (badge) => badge.textContent?.trim(),
    );

    expect(labels).toEqual(['حالة غير معروفة']);
  });

  it('gives every row button its own list item inside the list', () => {
    const fixture = setup([USER, { ...USER, id: 18, email: 'second@example.test' }]);
    const list = (fixture.nativeElement as HTMLElement).querySelector('[role="list"]');

    const items = Array.from(list?.querySelectorAll(':scope > [role="listitem"]') ?? []);

    expect(
      items.map((item) => item.querySelector('button')?.getAttribute('data-testid')),
    ).toEqual(['access-user-17', 'access-user-18']);
  });

  it.each([
    ['pending', 'qd-badge--lifecycle-pending'],
    ['active', 'qd-badge--lifecycle-active'],
    ['disabled', 'qd-badge--lifecycle-disabled'],
    ['archived', 'qd-badge--lifecycle-unknown'],
  ])('carries the %s lifecycle semantics on the row badge', (status, expectedClass) => {
    const fixture = setup([{ ...USER, status }]);
    const badge = element(fixture, 'access-user-lifecycle-17');

    expect(badge.classList).toContain('qd-badge--status');
    expect(badge.classList).toContain(expectedClass);
    expect(badge.className).not.toContain('qd-badge--membership-owner');
  });

  it('marks the selected row through the shared logical selection state', () => {
    const fixture = setup([USER, { ...USER, id: 18, email: 'second@example.test' }]);
    fixture.componentRef.setInput('selectedUserId', 18);
    fixture.detectChanges();
    const items = Array.from(
      (fixture.nativeElement as HTMLElement).querySelectorAll('[role="listitem"]'),
    );

    expect(items[0].classList).not.toContain('qd-is-selected');
    expect(items[1].classList).toContain('qd-is-selected');
    expect(items[1].getAttribute('aria-current')).toBe('true');
    expect(items[1].getAttribute('aria-posinset')).toBe('2');
    expect(items[1].getAttribute('aria-setsize')).toBe('2');
  });

  it('renders a content-shaped skeleton rather than a text loader while the page loads', () => {
    const fixture = setup();
    fixture.componentRef.setInput('loading', true);
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('[data-testid="qd-skeleton-rows"]')).toBeTruthy();
    expect(root.querySelector('[role="list"]')).toBeNull();
  });

  it('scopes a read failure to the list without an alert role', () => {
    const fixture = setup();
    fixture.componentRef.setInput('error', 'تعذر تحميل بيانات إدارة الوصول.');
    fixture.detectChanges();
    const failure = element(fixture, 'access-users-error');

    expect(failure.textContent).toContain('تعذر تحميل بيانات إدارة الوصول.');
    expect(failure.getAttribute('role')).toBeNull();
  });

  it('titles the truncatable name and email with their full values', () => {
    const fullName = 'اسم طويل جدًا لحساب عضو في المنصة';
    const fixture = setup([{ ...USER, displayName: fullName }]);
    const row = element(fixture, 'access-user-17');
    const name = row.querySelector('.access-user-list__name') as HTMLElement;
    const email = row.querySelector('.access-user-list__email') as HTMLElement;

    expect(name.classList).toContain('qd-truncate');
    expect(name.getAttribute('title')).toBe(fullName);
    expect(email.classList).toContain('qd-truncate');
    expect(email.getAttribute('title')).toBe('member@example.test');
  });
});
