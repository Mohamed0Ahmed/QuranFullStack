import { describe, expect, it, beforeEach } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';

import { PaginationComponent } from './pagination.component';

describe('PaginationComponent', () => {
  beforeEach(() => {
    getTestBed().resetTestingModule();
    TestBed.configureTestingModule({
      imports: [PaginationComponent],
      teardown: { destroyAfterEach: true },
    });
  });

  function setup(overrides: Record<string, unknown> = {}) {
    const fixture = TestBed.createComponent(PaginationComponent);
    fixture.componentRef.setInput('currentPage', 1);
    fixture.componentRef.setInput('pageSize', 50);
    fixture.componentRef.setInput('totalCount', 120);
    fixture.componentRef.setInput('ariaLabel', 'تصفّح الكلمات');
    for (const [key, value] of Object.entries(overrides)) {
      (fixture.componentRef as { setInput: (name: string, value: unknown) => void }).setInput(key, value);
    }
    fixture.detectChanges();
    return fixture;
  }

  it('hides pagination when totalCount fits in one page', () => {
    const fixture = setup({ totalCount: 40 });
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('[data-testid="qd-pagination-prev"]')).toBeNull();
  });

  it('renders a five-page window centered on the current page', () => {
    const fixture = setup({ currentPage: 5, pageSize: 1000, totalCount: 22000 });
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('[data-testid="qd-pagination-page-3"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="qd-pagination-page-7"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="qd-pagination-page-5"]')?.getAttribute('aria-current')).toBe('page');
  });

  it('renders an empty jump input beside the page buttons', () => {
    const fixture = setup({ currentPage: 2, totalCount: 120 });
    const root = fixture.nativeElement as HTMLElement;
    const input = root.querySelector('[data-testid="qd-pagination-jump-input"]') as HTMLInputElement;

    expect(input).toBeTruthy();
    expect(input.value).toBe('');
    expect(root.querySelector('[data-testid="qd-pagination-range-label"]')).toBeNull();
  });

  it('disables previous on the first page', () => {
    const fixture = setup({ currentPage: 1 });
    const root = fixture.nativeElement as HTMLElement;

    expect((root.querySelector('[data-testid="qd-pagination-prev"]') as HTMLButtonElement).disabled).toBe(true);
  });

  it('disables next on the last page', () => {
    const fixture = setup({ currentPage: 3, totalCount: 120 });
    const root = fixture.nativeElement as HTMLElement;

    expect((root.querySelector('[data-testid="qd-pagination-next"]') as HTMLButtonElement).disabled).toBe(true);
  });

  it('emits pageChange when next is clicked', () => {
    const fixture = setup({ currentPage: 1 });
    const emitted: number[] = [];
    fixture.componentInstance.pageChange.subscribe((page) => emitted.push(page));

    (fixture.nativeElement as HTMLElement)
      .querySelector('[data-testid="qd-pagination-next"]')
      ?.dispatchEvent(new Event('click'));

    expect(emitted).toEqual([2]);
  });

  it('shows jump submit on focus and emits a valid page via mouse click', () => {
    const fixture = setup({ currentPage: 1, totalCount: 500, pageSize: 50 });
    const emitted: number[] = [];
    fixture.componentInstance.pageChange.subscribe((page) => emitted.push(page));
    const root = fixture.nativeElement as HTMLElement;

    const input = root.querySelector('[data-testid="qd-pagination-jump-input"]') as HTMLInputElement;
    input.dispatchEvent(new Event('focus'));
    fixture.detectChanges();

    const submit = root.querySelector('[data-testid="qd-pagination-jump-submit"]') as HTMLButtonElement;
    expect(submit).toBeTruthy();

    input.value = '4';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    submit.dispatchEvent(new MouseEvent('mousedown', { bubbles: true, cancelable: true }));
    submit.click();
    fixture.detectChanges();

    expect(emitted).toEqual([4]);
    expect((root.querySelector('[data-testid="qd-pagination-jump-input"]') as HTMLInputElement).value).toBe('');
  });

  it('shows an error for an invalid jump without emitting', () => {
    const fixture = setup({ currentPage: 1, totalCount: 500, pageSize: 50 });
    const emitted: number[] = [];
    fixture.componentInstance.pageChange.subscribe((page) => emitted.push(page));
    const root = fixture.nativeElement as HTMLElement;

    const input = root.querySelector('[data-testid="qd-pagination-jump-input"]') as HTMLInputElement;
    input.dispatchEvent(new Event('focus'));
    fixture.detectChanges();

    input.value = '99';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    (root.querySelector('[data-testid="qd-pagination-jump-submit"]') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(emitted).toEqual([]);
    expect(root.querySelector('[data-testid="qd-pagination-jump-error"]')?.textContent).toContain('صفحة صالح');
  });
});
