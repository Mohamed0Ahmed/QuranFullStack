import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';

import { QdEmptyStateComponent } from './empty-state.component';

describe('QdEmptyStateComponent', () => {
  beforeEach(() => {
    getTestBed().resetTestingModule();
    TestBed.configureTestingModule({
      imports: [QdEmptyStateComponent],
      teardown: { destroyAfterEach: true },
    });
  });

  afterEach(() => getTestBed().resetTestingModule());

  function render(overrides: Record<string, unknown> = {}) {
    const fixture = TestBed.createComponent(QdEmptyStateComponent);
    fixture.componentRef.setInput('message', 'لا توجد بيانات بعد');
    for (const [key, value] of Object.entries(overrides)) {
      fixture.componentRef.setInput(key, value);
    }
    fixture.detectChanges();
    return { fixture, root: fixture.nativeElement as HTMLElement };
  }

  it('announces emptiness politely as a status, never as an alert', () => {
    const { root } = render();
    const box = root.querySelector('[data-testid="qd-empty-state"]');

    expect(box?.getAttribute('role')).toBe('status');
    expect(root.querySelector('[role="alert"]')).toBeNull();
    expect(box?.textContent?.trim()).toBe('لا توجد بيانات بعد');
  });

  it('offers at most one action, and only when the caller names it', () => {
    const withoutAction = render();
    expect(withoutAction.root.querySelectorAll('button')).toHaveLength(0);

    const withAction = render({ actionLabel: 'مسح المرشحات' });
    const buttons = withAction.root.querySelectorAll('button');
    expect(buttons).toHaveLength(1);
    expect(buttons[0].textContent?.trim()).toBe('مسح المرشحات');
    expect(buttons[0].classList.contains('qd-action')).toBe(true);
  });

  it('emits action when the single action is activated', () => {
    const { fixture, root } = render({ actionLabel: 'مسح المرشحات' });
    const emitted: number[] = [];
    fixture.componentInstance.action.subscribe(() => emitted.push(1));

    root.querySelector<HTMLButtonElement>('[data-testid="qd-empty-state-action"]')?.click();

    expect(emitted).toHaveLength(1);
  });

  it('keeps a reserved region mounted and quiet until its message lands', () => {
    const { fixture, root } = render({ message: '', reserve: true });
    const box = root.querySelector('[data-testid="qd-empty-state"]');

    expect(box?.classList.contains('qd-state--reserve')).toBe(true);
    expect(box?.querySelector('.qd-state__message--visible')).toBeNull();

    fixture.componentRef.setInput('message', 'لا نتائج مطابقة للمرشحات');
    fixture.detectChanges();

    expect(root.querySelector('[data-testid="qd-empty-state"]')).toBe(box);
    expect(box?.querySelector('.qd-state__message--visible')?.textContent).toBe(
      'لا نتائج مطابقة للمرشحات',
    );
  });

  it('lets a caller keep its own test id when the owner stands in for a legacy surface', () => {
    const { root } = render({ testId: 'qd-state-empty', actionTestId: 'qd-state-action' });

    expect(root.querySelector('[data-testid="qd-state-empty"]')).toBeTruthy();
  });
});
