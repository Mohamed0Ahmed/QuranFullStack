import { describe, expect, it, beforeEach, afterEach, vi } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';

import { UniqueWordsTableComponent } from './unique-words-table.component';
import { UniqueWordListItemViewModel } from '../../models/unique-words.models';

function row(id: number, overrides: Partial<UniqueWordListItemViewModel> = {}): UniqueWordListItemViewModel {
  return {
    id,
    kind: 'tashkeel',
    displayTextUthmani: `كلمة-تجريبية-${id}`,
    displayText: `كلمة-تجريبية-${id}`,
    occurrencesCount: id,
    ayahsCount: id,
    surahsCount: id,
    missingSurahsCount: 114 - id,
    firstVerseKey: '1:1',
    firstLocation: '1:1:1',
    ...overrides,
  };
}

describe('UniqueWordsTableComponent', () => {
  beforeEach(() => {
    getTestBed().resetTestingModule();
    TestBed.configureTestingModule({
      imports: [UniqueWordsTableComponent],
      teardown: { destroyAfterEach: true },
    });
  });

  afterEach(() => {
    getTestBed().resetTestingModule();
  });

  function setup(rows: readonly UniqueWordListItemViewModel[], overrides: Record<string, unknown> = {}) {
    const fixture = TestBed.createComponent(UniqueWordsTableComponent);
    fixture.componentRef.setInput('rows', rows);
    for (const [key, value] of Object.entries(overrides)) {
      (fixture.componentRef as any).setInput(key, value);
    }
    fixture.detectChanges();
    return fixture;
  }

  it('renders rows with the mode-aware display text', () => {
    const fixture = setup([row(1), row(2)], { selectedWordId: 2 });
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('[role="table"]')).toBeTruthy();
    expect(root.querySelectorAll('[data-testid="unique-words-table-word-button"]')).toHaveLength(2);
    expect(root.querySelector('[data-testid="unique-words-table-word-button"]')?.textContent).toContain('كلمة-تجريبية-1');
    expect(root.querySelector('[aria-selected="true"]')).toBeTruthy();
  });

  it('emits row selection when the word button is clicked', () => {
    const fixture = setup([row(1)]);
    const selected = vi.fn();
    fixture.componentInstance.rowSelected.subscribe(selected);

    const button = (fixture.nativeElement as HTMLElement).querySelector(
      '[data-testid="unique-words-table-word-button"]',
    ) as HTMLButtonElement | null;
    button?.click();

    expect(selected).toHaveBeenCalledTimes(1);
  });

  it('renders count-only chips without repeating column labels in each row', () => {
    const fixture = setup([row(1)]);
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelectorAll('.word-count-chip__label')).toHaveLength(0);
    expect(root.querySelectorAll('.word-count-chip__count').length).toBeGreaterThan(0);
  });

  it('scrolls the fallback body back to the top', () => {
    const fixture = setup(Array.from({ length: 60 }, (_, index) => row(index + 1)));
    const root = fixture.nativeElement as HTMLElement;
    const body = root.querySelector('.unique-words-table__body') as HTMLElement;
    body.scrollTop = 120;

    fixture.componentInstance.scrollToTop();

    expect(body.scrollTop).toBe(0);
  });
});
