import { ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { OwnerReconciliationStatus } from '../../../../core/api/generated/models/owner-reconciliation-status';
import { AccessOwnerReconciliationComponent } from './access-owner-reconciliation.component';

const STATUS: OwnerReconciliationStatus = {
  canApply: true,
  candidates: [
    { normalizedEmail: 'owner@example.test', state: 'Unchanged', userId: 4 },
    { normalizedEmail: 'candidate@example.test', state: 'AwaitingVerifiedSignIn', userId: null },
  ],
  configurationFingerprint: 'fingerprint-9f2c',
  isReady: true,
  lastReconciliation: null,
};

function setup(
  inputs: {
    loading?: boolean;
    error?: string | null;
    status?: OwnerReconciliationStatus | null;
  } = {},
): ComponentFixture<AccessOwnerReconciliationComponent> {
  TestBed.configureTestingModule({ imports: [AccessOwnerReconciliationComponent] });
  const fixture = TestBed.createComponent(AccessOwnerReconciliationComponent);
  fixture.componentRef.setInput('loading', inputs.loading ?? false);
  fixture.componentRef.setInput('error', inputs.error ?? null);
  fixture.componentRef.setInput('status', inputs.status ?? null);
  fixture.detectChanges();
  return fixture;
}

function element(
  fixture: ComponentFixture<AccessOwnerReconciliationComponent>,
  testId: string,
): HTMLElement {
  const found = fixture.nativeElement.querySelector(`[data-testid="${testId}"]`) as HTMLElement | null;
  if (!found) {
    throw new Error(`Missing ${testId}`);
  }
  return found;
}

function absent(
  fixture: ComponentFixture<AccessOwnerReconciliationComponent>,
  testId: string,
): boolean {
  return fixture.nativeElement.querySelector(`[data-testid="${testId}"]`) === null;
}

describe('AccessOwnerReconciliationComponent', () => {
  it.each([
    [{ loading: true }, 'access-reconciliation-loading'],
    [{ error: 'تعذّر تحميل حالة المطابقة.' }, 'access-reconciliation-error'],
    [{}, 'access-reconciliation-empty'],
    [{ status: STATUS }, 'access-reconciliation-diagnostic-note'],
  ])('shows one read state at a time (%o)', (inputs, expected) => {
    const fixture = setup(inputs);

    const shown = [
      'access-reconciliation-loading',
      'access-reconciliation-error',
      'access-reconciliation-empty',
      'access-reconciliation-diagnostic-note',
    ].filter((testId) => !absent(fixture, testId));
    expect(shown).toEqual([expected]);
  });

  it('says the reconciliation is a diagnostic read that this page never applies', () => {
    const fixture = setup({ status: STATUS });

    expect(element(fixture, 'access-reconciliation-diagnostic-note').textContent).toContain(
      'قراءة تشخيصية لا تُطبَّق من هذه الصفحة',
    );
    expect(fixture.nativeElement.querySelector('button[qdAction="primary"]')).toBeNull();
  });

  it('names every candidate state in Arabic instead of its wire value', () => {
    const fixture = setup({ status: STATUS });

    const rows = fixture.nativeElement.querySelectorAll('li');
    expect(rows).toHaveLength(2);
    expect(rows[0].textContent).toContain('دون تغيير');
    expect(rows[1].textContent).toContain('بانتظار تسجيل دخول موثّق');
    expect(rows[1].textContent).not.toContain('AwaitingVerifiedSignIn');
  });

  it('keeps the technical fingerprint behind a disclosure that reports its own state', () => {
    const fixture = setup({ status: STATUS });
    const toggle = element(fixture, 'access-reconciliation-fingerprint-toggle');

    expect(absent(fixture, 'access-reconciliation-fingerprint')).toBe(true);
    expect(toggle.getAttribute('aria-expanded')).toBe('false');

    toggle.click();
    fixture.detectChanges();

    expect(element(fixture, 'access-reconciliation-fingerprint').textContent).toContain(
      'fingerprint-9f2c',
    );
    expect(toggle.getAttribute('aria-expanded')).toBe('true');

    toggle.click();
    fixture.detectChanges();

    expect(absent(fixture, 'access-reconciliation-fingerprint')).toBe(true);
  });

  it('discloses each candidate email without a pointer-only title (D35)', () => {
    const fixture = setup({ status: STATUS });
    const root = fixture.nativeElement as HTMLElement;

    const rows = Array.from(root.querySelectorAll('[role="listitem"]'));
    expect(rows).toHaveLength(STATUS.candidates.length);
    for (const row of rows) {
      // The list is display-only: the row owns no control and takes no tab stop, so the address
      // wraps in full rather than eliding behind a `title` no keyboard or touch user can reach.
      expect(row.getAttribute('tabindex')).toBeNull();
      const email = row.querySelector('.access-owner-reconciliation__candidate-email') as HTMLElement;
      expect(email).not.toBeNull();
      expect(email.getAttribute('title')).toBeNull();
      expect(email.classList.contains('qd-truncate')).toBe(false);
      expect(email.getAttribute('tabindex')).toBeNull();
    }
    expect(root.querySelectorAll('[title]')).toHaveLength(0);
  });

  it('reads the configured and executable state out of the status it is given', () => {
    const fixture = setup({ status: { ...STATUS, isReady: false, canApply: false } });

    const list = fixture.nativeElement.querySelector('dl') as HTMLElement;
    expect(list.textContent).toContain('غير مطابِقة للإعداد');
    expect(list.textContent).toContain('يوجد ما يمنع التنفيذ');
  });
});
