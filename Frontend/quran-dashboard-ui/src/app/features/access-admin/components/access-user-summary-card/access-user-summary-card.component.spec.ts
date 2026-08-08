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
  it.each([
    ['a whitespace-only stored name', '   '],
    ['no stored name', null],
  ])('heads the workspace with the email when the account has %s', (_scenario, displayName) => {
    const fixture = setup({ ...USER, displayName });

    expect(element(fixture, 'access-user-summary-name').textContent?.trim()).toBe(
      'member@example.test',
    );
  });

  it('titles the truncatable name with its full value', () => {
    const fixture = setup({ ...USER, displayName: 'اسم طويل جدًا لحساب عضو في المنصة' });
    const name = element(fixture, 'access-user-summary-name');

    expect(name.classList).toContain('qd-truncate');
    expect(name.getAttribute('title')).toBe('اسم طويل جدًا لحساب عضو في المنصة');
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

  it('names the workspace before a user is chosen, without inventing an account', () => {
    const fixture = setup(null);
    const root = fixture.nativeElement as HTMLElement;

    expect(root.textContent).toContain('مساحة عمل الحساب');
    expect(root.querySelector('[data-testid="access-user-summary-badges"]')).toBeNull();
    expect(root.querySelector('[dir="ltr"]')).toBeNull();
  });
});
