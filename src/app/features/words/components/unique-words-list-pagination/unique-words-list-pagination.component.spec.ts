import { describe, expect, it, beforeEach } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';

import { UniqueWordsListPaginationComponent } from './unique-words-list-pagination.component';

describe('UniqueWordsListPaginationComponent', () => {
  beforeEach(() => {
    getTestBed().resetTestingModule();
    TestBed.configureTestingModule({
      imports: [UniqueWordsListPaginationComponent],
      teardown: { destroyAfterEach: true },
    });
  });

  function setup(overrides: Record<string, unknown> = {}) {
    const fixture = TestBed.createComponent(UniqueWordsListPaginationComponent);
    fixture.componentRef.setInput('currentPage', 1);
    fixture.componentRef.setInput('pageSize', 50);
    fixture.componentRef.setInput('totalCount', 120);
    for (const [key, value] of Object.entries(overrides)) {
      (fixture.componentRef as { setInput: (name: string, value: unknown) => void }).setInput(key, value);
    }
    fixture.detectChanges();
    return fixture;
  }

  it('hides pagination when totalCount fits in one page', () => {
    const fixture = setup({ totalCount: 40 });
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('[data-testid="unique-words-pagination-label"]')).toBeNull();
  });

  it('shows the current and last page labels when pagination is needed', () => {
    const fixture = setup({ currentPage: 2, totalCount: 120 });
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('[data-testid="unique-words-pagination-label"]')?.textContent).toContain('2 / 3');
  });

  it('disables previous on the first page', () => {
    const fixture = setup({ currentPage: 1 });
    const root = fixture.nativeElement as HTMLElement;

    expect((root.querySelector('[data-testid="unique-words-pagination-prev"]') as HTMLButtonElement).disabled).toBe(
      true,
    );
  });

  it('disables next on the last page', () => {
    const fixture = setup({ currentPage: 3, totalCount: 120 });
    const root = fixture.nativeElement as HTMLElement;

    expect((root.querySelector('[data-testid="unique-words-pagination-next"]') as HTMLButtonElement).disabled).toBe(
      true,
    );
  });

  it('emits pageChange when next is clicked', () => {
    const fixture = setup({ currentPage: 1 });
    const emitted: number[] = [];
    fixture.componentInstance.pageChange.subscribe((page) => emitted.push(page));

    (fixture.nativeElement as HTMLElement)
      .querySelector('[data-testid="unique-words-pagination-next"]')
      ?.dispatchEvent(new Event('click'));

    expect(emitted).toEqual([2]);
  });
});
