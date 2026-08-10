import { ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { AccessUserDetail } from '../../../../core/api/generated/models/access-user-detail';
import { AccessUserSummaryCardComponent } from './access-user-summary-card.component';

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
  user: AccessUserDetail | null = USER,
): ComponentFixture<AccessUserSummaryCardComponent> {
  TestBed.configureTestingModule({ imports: [AccessUserSummaryCardComponent] });
  const fixture = TestBed.createComponent(AccessUserSummaryCardComponent);
  fixture.componentRef.setInput('user', user);
  fixture.detectChanges();
  return fixture;
}

function element(
  fixture: ComponentFixture<AccessUserSummaryCardComponent>,
  testId: string,
): HTMLElement {
  const found = fixture.nativeElement.querySelector(`[data-testid="${testId}"]`) as HTMLElement | null;
  if (!found) {
    throw new Error(`Missing ${testId}`);
  }
  return found;
}

describe('AccessUserSummaryCardComponent', () => {
  it('exposes the whole login email rather than truncating the safety target', () => {
    const fixture = setup({ ...USER, email: 'a-very-long-account-address@example.test' });
    const email = element(fixture, 'access-user-summary-email');

    expect(email.textContent?.trim()).toBe('a-very-long-account-address@example.test');
    expect(email.classList).not.toContain('qd-truncate');
    expect(email.classList).toContain('qd-ltr-isolate');
  });

  it.each([
    ['pending', 'qd-badge--lifecycle-pending'],
    ['active', 'qd-badge--lifecycle-active'],
    ['disabled', 'qd-badge--lifecycle-disabled'],
    ['archived', 'qd-badge--lifecycle-unknown'],
  ])('carries the %s lifecycle semantics on its own badge', (status, expectedClass) => {
    const fixture = setup({ ...USER, status });
    const badge = element(fixture, 'access-user-summary-lifecycle');

    expect(badge.classList).toContain('qd-badge--status');
    expect(badge.classList).toContain(expectedClass);
    expect(badge.classList).not.toContain('qd-badge--membership-owner');
  });

  it('keeps Owner membership on a badge that carries no lifecycle class', () => {
    const fixture = setup({ ...USER, isOwner: true });
    const membership = element(fixture, 'access-user-summary-membership');

    expect(membership.classList).toContain('qd-badge--membership-owner');
    expect(membership.className).not.toContain('qd-badge--lifecycle');
  });

  it.each([
    ['active', 'نشط'],
    ['pending', 'معلّق'],
    ['disabled', 'معطّل'],
  ] as const)('shows Owner membership alongside the %s lifecycle status', (status, statusLabel) => {
    const fixture = setup({ ...USER, isOwner: true, status, permissionCodes: [] });

    const labels = Array.from(
      element(fixture, 'access-user-summary-badges').querySelectorAll(
        '.qd-badge',
      ) as NodeListOf<HTMLElement>,
      (badge) => badge.textContent?.trim(),
    );

    expect(labels).toEqual(['مالك', statusLabel]);
  });

  it('shows the lifecycle status alone for a non-Owner account', () => {
    const fixture = setup();

    const labels = Array.from(
      element(fixture, 'access-user-summary-badges').querySelectorAll(
        '.qd-badge',
      ) as NodeListOf<HTMLElement>,
      (badge) => badge.textContent?.trim(),
    );

    expect(labels).toEqual(['نشط']);
  });

  it('renders neither the optimistic-concurrency version nor the database identifier', () => {
    const fixture = setup({ ...USER, version: 42 });
    const text = fixture.nativeElement.textContent as string;

    expect(text).not.toContain('الإصدار');
    expect(text).not.toContain('42');
    expect(text).not.toContain('17');
  });

  it('marks the email left-to-right inside the right-to-left page', () => {
    const fixture = setup();

    expect(
      (fixture.nativeElement as HTMLElement).querySelector('[dir="ltr"]')?.textContent?.trim(),
    ).toBe('member@example.test');
  });

  it('invents no account before a user is chosen', () => {
    const fixture = setup(null);
    const root = fixture.nativeElement as HTMLElement;

    expect(root.textContent?.trim()).toBe('');
    expect(root.querySelector('[data-testid="access-user-summary-badges"]')).toBeNull();
    expect(root.querySelector('[dir="ltr"]')).toBeNull();
  });
});
