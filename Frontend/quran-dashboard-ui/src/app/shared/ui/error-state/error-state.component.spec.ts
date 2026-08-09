import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';

import { QdErrorStateComponent } from './error-state.component';

describe('QdErrorStateComponent', () => {
  beforeEach(() => {
    getTestBed().resetTestingModule();
    TestBed.configureTestingModule({
      imports: [QdErrorStateComponent],
      teardown: { destroyAfterEach: true },
    });
  });

  afterEach(() => getTestBed().resetTestingModule());

  function render(overrides: Record<string, unknown> = {}) {
    const fixture = TestBed.createComponent(QdErrorStateComponent);
    fixture.componentRef.setInput('message', 'تعذّر تحميل البيانات');
    for (const [key, value] of Object.entries(overrides)) {
      fixture.componentRef.setInput(key, value);
    }
    fixture.detectChanges();
    return { fixture, root: fixture.nativeElement as HTMLElement };
  }

  // The read/write split is the point of the owner: a failed read is announced by the
  // workspace's own polite region, so making it an alert interrupts the operator twice.
  it('leaves a read failure to the workspace announcer instead of raising an alert', () => {
    const { root } = render();
    const box = root.querySelector('[data-testid="qd-error-state"]');

    expect(box?.getAttribute('role')).toBeNull();
    expect(box?.textContent?.trim()).toBe('تعذّر تحميل البيانات');
  });

  it('raises a write failure as an alert', () => {
    const { root } = render({ severity: 'write' });

    expect(root.querySelector('[data-testid="qd-error-state"]')?.getAttribute('role')).toBe(
      'alert',
    );
  });

  it('offers a scoped retry only when the caller supplies one', () => {
    const plain = render();
    expect(plain.root.querySelectorAll('button')).toHaveLength(0);

    const withRetry = render({
      actionLabel: 'إعادة المحاولة',
      actionAriaLabel: 'إعادة المحاولة: قائمة الجذور',
    });
    const button = withRetry.root.querySelector('button') as HTMLButtonElement;

    expect(button.textContent?.trim()).toBe('إعادة المحاولة');
    expect(button.getAttribute('aria-label')).toBe('إعادة المحاولة: قائمة الجذور');
    expect(button.classList.contains('qd-action')).toBe(true);
  });

  it('emits action when retry is activated', () => {
    const { fixture, root } = render({ actionLabel: 'إعادة المحاولة' });
    const emitted: number[] = [];
    fixture.componentInstance.action.subscribe(() => emitted.push(1));

    root.querySelector<HTMLButtonElement>('[data-testid="qd-error-state-action"]')?.click();

    expect(emitted).toHaveLength(1);
  });

  // A write failure can only be announced if its alert element already exists when the text
  // lands, so a permanently mounted region must stay visually quiet rather than unmounted.
  it('keeps a reserved alert region mounted, quiet, and identical when the failure lands', () => {
    const { fixture, root } = render({ message: '', severity: 'write', reserve: true });
    const before = root.querySelector('[data-testid="qd-error-state"]');

    expect(before?.getAttribute('role')).toBe('alert');
    expect(before?.classList.contains('qd-state--reserve')).toBe(true);
    expect(before?.classList.contains('qd-state--reserve-empty')).toBe(true);
    expect(before?.textContent?.trim()).toBe('');

    fixture.componentRef.setInput('message', 'تعارض: تم تحديث السجل من جهة أخرى');
    fixture.detectChanges();
    const after = root.querySelector('[data-testid="qd-error-state"]');

    expect(after).toBe(before);
    expect(after?.classList.contains('qd-state--reserve-empty')).toBe(false);
    expect(after?.textContent?.trim()).toBe('تعارض: تم تحديث السجل من جهة أخرى');
  });

  it('never marks a non-reserved region quiet, even with an empty message', () => {
    const { root } = render({ message: '' });

    expect(
      root.querySelector('[data-testid="qd-error-state"]')?.classList.contains(
        'qd-state--reserve-empty',
      ),
    ).toBe(false);
  });
});
