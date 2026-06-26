import { describe, expect, it, beforeEach, afterEach, vi } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';

import { UniqueWordsTableComponent } from './unique-words-table.component';
import { UniqueWordListItemViewModel } from '../../models/unique-words.models';

function row(id: number, overrides: Partial<UniqueWordListItemViewModel> = {}): UniqueWordListItemViewModel {
  return {
    id,
    kind: 'tashkeel',
    displayText: `كلمة-تجريبية-${id}`,
    occurrencesCount: id,
    ayahsCount: id,
    surahsCount: id,
    missingSurahsCount: 114 - id,
    primaryWordTypeCode: null,
    primaryWordTypeBroadArabicLabel: null,
    rootId: null,
    rootText: null,
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

  it('renders the type and root column headers', () => {
    const fixture = setup([row(1)]);
    const root = fixture.nativeElement as HTMLElement;
    const headers = Array.from(root.querySelectorAll('[role="columnheader"]')).map((h) => h.textContent?.trim());

    expect(headers).toContain('نوع الكلمة');
    expect(headers).toContain('الجذر');
  });

  it('renders the primary type label and the root deep link when morphology is present', () => {
    const fixture = setup([
      row(1, {
        primaryWordTypeCode: 'PN',
        primaryWordTypeBroadArabicLabel: 'اسم',
        rootId: 5001,
        rootText: 'أ ل ه',
      }),
    ]);
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('[data-word-type-code="PN"]')?.textContent).toContain('اسم');
    const link = root.querySelector('[data-testid="unique-words-table-root-link"]') as HTMLAnchorElement | null;
    expect(link).toBeTruthy();
    expect(link?.textContent).toContain('أ ل ه');
    expect(link?.getAttribute('target')).toBe('_blank');
    expect(link?.getAttribute('rel')).toBe('noopener noreferrer');
    expect(link?.getAttribute('href')).toContain('/dashboard/words/roots');
  });

  it('renders broad labels for verb, particle, and initials rows', () => {
    const fixture = setup([
      row(1, { primaryWordTypeCode: 'V', primaryWordTypeBroadArabicLabel: 'فعل' }),
      row(2, { primaryWordTypeCode: 'P', primaryWordTypeBroadArabicLabel: 'حرف' }),
      row(3, { primaryWordTypeCode: 'INL', primaryWordTypeBroadArabicLabel: 'حروف مقطّعة' }),
    ]);
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('[data-word-type-code="V"]')?.textContent).toContain('فعل');
    expect(root.querySelector('[data-word-type-code="P"]')?.textContent).toContain('حرف');
    expect(root.querySelector('[data-word-type-code="INL"]')?.textContent).toContain('حروف مقطّعة');
  });

  it('renders the placeholder and no root link when type and root are absent', () => {
    const fixture = setup([row(1)]);
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('[data-testid="unique-words-table-root-link"]')).toBeNull();
    // Two placeholder em-dashes: type cell + root cell.
    expect(root.querySelectorAll('.unique-words-table__text')).toHaveLength(2);
  });

  it('does not render a +N counter for the word type', () => {
    const fixture = setup([row(1, { primaryWordTypeBroadArabicLabel: 'اسم' })]);
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('.unique-words-table__type-more')).toBeNull();
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

  it('renders a content-shaped loading skeleton when loading', () => {
    const fixture = setup([row(1)], { loading: true });
    const root = fixture.nativeElement as HTMLElement;
    const loading = root.querySelector('[data-testid="unique-words-loading"]');

    expect(loading).toBeTruthy();
    expect(loading?.getAttribute('aria-busy')).toBe('true');
    expect(loading?.querySelectorAll('.unique-words-table__row')).toHaveLength(12);
    expect(loading?.querySelectorAll('.qd-skeleton--text')).toHaveLength(48);
    expect(root.querySelector('[data-testid="unique-words-table-word-button"]')).toBeNull();
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
