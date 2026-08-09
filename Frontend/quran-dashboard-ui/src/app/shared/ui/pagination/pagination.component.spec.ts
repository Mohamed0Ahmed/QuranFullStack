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

  // D43: Go is mounted in every input state and only ever changes its disabled flag, so no control
  // appears or disappears while the operator is typing. Out-of-range stays submittable on purpose —
  // that is what surfaces the reserved-line error instead of silently doing nothing.
  it('keeps Go mounted, disabled while the jump input is empty and enabled once a number is typed', () => {
    const fixture = setup({ currentPage: 1, totalCount: 500, pageSize: 50 });
    const root = fixture.nativeElement as HTMLElement;
    const submit = () => root.querySelector('[data-testid="qd-pagination-jump-submit"]') as HTMLButtonElement;
    const input = root.querySelector('[data-testid="qd-pagination-jump-input"]') as HTMLInputElement;

    expect(submit()).toBeTruthy();
    expect(submit().disabled).toBe(true);

    input.value = '4';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    expect(submit().disabled).toBe(false);

    input.value = '';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    expect(submit()).toBeTruthy();
    expect(submit().disabled).toBe(true);
  });

  // D44: two pagers on one page (an explorer table and a nested detail list) must not share the ids
  // that wire a label, an error and a live region to their own input.
  it('gives every instance its own jump input, error and live-region ids', () => {
    const first = setup({ currentPage: 1, totalCount: 500, pageSize: 50 });
    const second = setup({ currentPage: 1, totalCount: 500, pageSize: 50 });

    const idsOf = (fixture: ReturnType<typeof setup>) => {
      const root = fixture.nativeElement as HTMLElement;
      return {
        input: root.querySelector('[data-testid="qd-pagination-jump-input"]')!.id,
        error: root.querySelector('[data-testid="qd-pagination-jump-error"]')!.id,
        live: root.querySelector('[data-testid="qd-pagination-live"]')!.id,
        labelFor: root.querySelector('label')!.getAttribute('for'),
      };
    };

    const a = idsOf(first);
    const b = idsOf(second);

    expect(a.input).toBeTruthy();
    expect(a.labelFor).toBe(a.input);
    expect(b.labelFor).toBe(b.input);
    expect(new Set([a.input, a.error, a.live, b.input, b.error, b.live]).size).toBe(6);
  });

  // F13: a page change is announced as the new result range, not just as a new page number.
  it('announces the new result range through its own polite region on every page change', () => {
    const fixture = setup({ currentPage: 1, totalCount: 120, pageSize: 50 });
    const root = fixture.nativeElement as HTMLElement;
    const live = () => root.querySelector('[data-testid="qd-pagination-live"]')!;

    expect(live().getAttribute('aria-live')).toBe('polite');
    expect(live().textContent?.trim()).toBe('');

    (root.querySelector('[data-testid="qd-pagination-next"]') as HTMLButtonElement).click();
    fixture.detectChanges();
    expect(live().textContent).toContain('51');
    expect(live().textContent).toContain('100');
    expect(live().textContent).toContain('120');

    fixture.componentRef.setInput('currentPage', 2);
    fixture.detectChanges();
    (root.querySelector('[data-testid="qd-pagination-page-1"]') as HTMLButtonElement).click();
    fixture.detectChanges();
    expect(live().textContent).toContain('1–50');
  });

  // D42: the error line is reserved rather than mounted on demand, so a failed jump does not push
  // the surrounding layout.
  it('reserves the jump error line in every state and only toggles its visibility', () => {
    const fixture = setup({ currentPage: 1, totalCount: 500, pageSize: 50 });
    const root = fixture.nativeElement as HTMLElement;
    const error = () => root.querySelector('[data-testid="qd-pagination-jump-error"]')!;

    expect(error()).toBeTruthy();
    expect(error().classList).not.toContain('qd-pagination__jump-error--visible');

    const input = root.querySelector('[data-testid="qd-pagination-jump-input"]') as HTMLInputElement;
    input.value = '99';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    (root.querySelector('[data-testid="qd-pagination-jump-submit"]') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(error().classList).toContain('qd-pagination__jump-error--visible');
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
