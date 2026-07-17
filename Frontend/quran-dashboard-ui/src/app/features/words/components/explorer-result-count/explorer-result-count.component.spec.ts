import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';

import { ExplorerResultCountComponent } from './explorer-result-count.component';

describe('ExplorerResultCountComponent', () => {
  beforeEach(() => {
    getTestBed().resetTestingModule();
    TestBed.configureTestingModule({
      imports: [ExplorerResultCountComponent],
      teardown: { destroyAfterEach: true },
    });
  });

  afterEach(() => {
    getTestBed().resetTestingModule();
  });

  function render(inputs: { count: number; labelPrefix: string; loading?: boolean; hasError?: boolean }) {
    const fixture = TestBed.createComponent(ExplorerResultCountComponent);
    fixture.componentRef.setInput('count', inputs.count);
    fixture.componentRef.setInput('labelPrefix', inputs.labelPrefix);
    fixture.componentRef.setInput('loading', inputs.loading ?? false);
    fixture.componentRef.setInput('hasError', inputs.hasError ?? false);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  it('renders the label-prefix phrasing with the count', () => {
    const root = render({ count: 1642, labelPrefix: 'عدد الجذور' });

    const stat = root.querySelector('[data-testid="explorer-result-count"]');
    expect(stat).toBeTruthy();
    expect(stat?.getAttribute('aria-label')).toBe('عدد الجذور: 1642');
    expect(root.querySelector('[data-testid="explorer-result-count-value"]')?.textContent?.trim()).toBe('1642');
    expect(root.textContent).toContain('عدد الجذور');
  });

  it('renders a non-interactive skeleton while loading, announced via sr-only text', () => {
    const root = render({ count: 0, labelPrefix: 'عدد الكلمات', loading: true });

    const skeleton = root.querySelector('[data-testid="explorer-result-count-skeleton"]');
    expect(skeleton).toBeTruthy();
    // The role="status" container must NOT be aria-hidden (that would nullify the announcement); the
    // visual skeleton bar is hidden while the sr-only loading text carries the announcement.
    expect(skeleton?.getAttribute('role')).toBe('status');
    expect(skeleton?.getAttribute('aria-hidden')).toBeNull();
    expect(skeleton?.querySelector('.qd-sr-only')?.textContent?.trim()).toBe('جارٍ التحميل…');
    expect(skeleton?.querySelector('.qd-skeleton')?.getAttribute('aria-hidden')).toBe('true');
    expect(root.querySelector('[data-testid="explorer-result-count"]')).toBeNull();
    expect(root.querySelector('button')).toBeNull();
    expect(root.querySelector('a')).toBeNull();
  });

  // N3 row 6: the stat used to render nothing on error and the toolbar collapsed by a full line.
  it('holds its line with a muted placeholder when the list errored, without announcing it', () => {
    const root = render({ count: 42, labelPrefix: 'عدد الكلمات', hasError: true });

    const error = root.querySelector('[data-testid="explorer-result-count-error"]');
    expect(error).toBeTruthy();
    // Same line box as the loaded stat: the label prefix plus a value slot, not a bare message.
    expect(error?.querySelector('.explorer-result-count__label')?.textContent).toContain('عدد الكلمات');
    expect(error?.querySelector('.explorer-result-count__value--unavailable')?.textContent?.trim()).toBe('—');
    // The stale count must never leak, and the page's own role="alert" stays the only announcement.
    expect(error?.textContent).not.toContain('42');
    expect(error?.getAttribute('aria-hidden')).toBe('true');
    expect(error?.getAttribute('role')).toBeNull();
    expect(root.querySelector('[data-testid="explorer-result-count"]')).toBeNull();
    expect(root.querySelector('[data-testid="explorer-result-count-skeleton"]')).toBeNull();
  });

  it('renders every state as the same single line box', () => {
    const lineBox = (root: HTMLElement) => {
      const line = root.querySelector('.explorer-result-count');
      expect(line).toBeTruthy();
      // One structural line per state — never a taller flex row wrapping a short bar.
      return line!.tagName;
    };

    const loaded = render({ count: 1642, labelPrefix: 'عدد الجذور' });
    const loading = render({ count: 0, labelPrefix: 'عدد الجذور', loading: true });
    const errored = render({ count: 0, labelPrefix: 'عدد الجذور', hasError: true });

    expect([lineBox(loaded), lineBox(loading), lineBox(errored)]).toEqual(['P', 'P', 'P']);
    // The loading bar is the line itself, not a fixed-height chip inside a padded row.
    expect(loading.querySelector('.explorer-result-count__skeleton')?.classList.contains('qd-skeleton')).toBe(true);
  });

  it('renders 0 for an empty scope', () => {
    const root = render({ count: 0, labelPrefix: 'عدد الأصول الصرفية' });

    expect(root.querySelector('[data-testid="explorer-result-count-value"]')?.textContent?.trim()).toBe('0');
  });
});
