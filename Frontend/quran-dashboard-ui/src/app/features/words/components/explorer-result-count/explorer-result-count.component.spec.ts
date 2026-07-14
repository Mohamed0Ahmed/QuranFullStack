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

  it('renders a non-interactive skeleton while loading', () => {
    const root = render({ count: 0, labelPrefix: 'عدد الكلمات', loading: true });

    expect(root.querySelector('[data-testid="explorer-result-count-skeleton"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="explorer-result-count"]')).toBeNull();
    expect(root.querySelector('button')).toBeNull();
    expect(root.querySelector('a')).toBeNull();
  });

  it('renders nothing when the list errored', () => {
    const root = render({ count: 42, labelPrefix: 'عدد الكلمات', hasError: true });

    expect(root.querySelector('[data-testid="explorer-result-count"]')).toBeNull();
    expect(root.querySelector('[data-testid="explorer-result-count-skeleton"]')).toBeNull();
    expect(root.textContent?.trim()).toBe('');
  });

  it('renders 0 for an empty scope', () => {
    const root = render({ count: 0, labelPrefix: 'عدد الأصول الصرفية' });

    expect(root.querySelector('[data-testid="explorer-result-count-value"]')?.textContent?.trim()).toBe('0');
  });
});
