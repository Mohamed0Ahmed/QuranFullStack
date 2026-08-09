import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';

import { QdNoticeComponent } from './notice.component';

describe('QdNoticeComponent', () => {
  beforeEach(() => {
    getTestBed().resetTestingModule();
    TestBed.configureTestingModule({
      imports: [QdNoticeComponent],
      teardown: { destroyAfterEach: true },
    });
  });

  afterEach(() => getTestBed().resetTestingModule());

  function render(overrides: Record<string, unknown> = {}) {
    const fixture = TestBed.createComponent(QdNoticeComponent);
    for (const [key, value] of Object.entries(overrides)) {
      fixture.componentRef.setInput(key, value);
    }
    fixture.detectChanges();
    return { fixture, root: fixture.nativeElement as HTMLElement };
  }

  // D41: the mutation slot used to hold a permanent blank band. The announcer must exist
  // before the mutation (or nothing is announced) while contributing no idle geometry.
  it('mounts its polite announcer while idle and renders nothing inside it', () => {
    const { root } = render();
    const live = root.querySelector('[data-testid="qd-notice"]');

    expect(live?.getAttribute('role')).toBe('status');
    expect(live?.getAttribute('aria-live')).toBe('polite');
    expect(live?.children).toHaveLength(0);
    expect(live?.textContent?.trim()).toBe('');
  });

  it('grows into the same announcer when an outcome arrives', () => {
    const { fixture, root } = render();
    const live = root.querySelector('[data-testid="qd-notice"]');

    fixture.componentRef.setInput('message', 'تم منح 3 صلاحيات وسحب 1.');
    fixture.detectChanges();

    expect(root.querySelector('[data-testid="qd-notice"]')).toBe(live);
    expect(live?.querySelector('.qd-notice__body')?.textContent?.trim()).toBe(
      'تم منح 3 صلاحيات وسحب 1.',
    );
  });

  it.each([
    ['success', false],
    ['info', true],
  ] as const)('renders the %s tone without changing the announcement role', (tone, isInfo) => {
    const { root } = render({ message: 'تم حفظ التغيير', tone });
    const body = root.querySelector('.qd-notice__body');

    expect(body?.classList.contains('qd-notice__body--info')).toBe(isInfo);
    expect(root.querySelector('[data-testid="qd-notice"]')?.getAttribute('role')).toBe('status');
  });

  it('offers dismissal only when the caller names it, and emits on activation', () => {
    const withoutDismiss = render({ message: 'تم حفظ التغيير' });
    expect(withoutDismiss.root.querySelectorAll('button')).toHaveLength(0);

    const { fixture, root } = render({ message: 'تم حفظ التغيير', dismissLabel: 'إخفاء' });
    const emitted: number[] = [];
    fixture.componentInstance.dismiss.subscribe(() => emitted.push(1));

    root.querySelector<HTMLButtonElement>('[data-testid="qd-notice-dismiss"]')?.click();

    expect(emitted).toHaveLength(1);
  });
});
