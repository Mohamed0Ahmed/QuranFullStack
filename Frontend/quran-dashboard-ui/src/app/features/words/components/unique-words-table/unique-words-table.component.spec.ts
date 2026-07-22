import { describe, expect, it, beforeEach, afterEach, vi } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';

import { UniqueWordsTableComponent } from './unique-words-table.component';
import { UniqueWordListItemViewModel } from '../../models/unique-words.models';
import { EMPTY_LIST_LABEL } from '../../models/unique-words.labels';

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

  it('marks the active drilldown chip for the selected row', () => {
    const fixture = setup([row(1)], { selectedWordId: 1, drilldownIsOpen: true, activeColumn: 'surahs' });
    const buttons = Array.from(fixture.nativeElement.querySelectorAll('qd-word-count-chip button')) as HTMLButtonElement[];

    expect(buttons[2]?.classList.contains('qd-is-selected')).toBe(true);
    expect(buttons[1]?.classList.contains('qd-is-selected')).toBe(false);
  });

  it('moves between drilldown columns and rows with keyboard navigation events', () => {
    const fixture = setup([row(1), row(2, { surahsCount: 0 })], {
      selectedWordId: 1,
      drilldownIsOpen: true,
      activeColumn: 'surahs',
    });
    const emitted: { word: UniqueWordListItemViewModel; column: string; view: string; source?: string }[] = [];
    fixture.componentInstance.drilldownOpen.subscribe((event) => emitted.push(event));

    const table = fixture.nativeElement.querySelector('.unique-words-table') as HTMLElement;
    table.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowLeft', bubbles: true }));
    fixture.detectChanges();

    fixture.componentRef.setInput('activeColumn', 'missing');
    fixture.detectChanges();

    table.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true }));
    fixture.detectChanges();

    expect(emitted[0]).toMatchObject({ word: row(1), column: 'missing', view: 'missing', source: 'keyboard' });
    expect(emitted[1]).toMatchObject({ word: row(2, { surahsCount: 0 }), column: 'missing', view: 'missing', source: 'keyboard' });
  });

  describe('column-header sorting (Feature 030, N8)', () => {
    function headerCellFor(root: HTMLElement, key: string): HTMLElement {
      const button = root.querySelector(`[data-testid="unique-words-table-sort-${key}"]`) as HTMLElement;
      return button.closest('[role="columnheader"]') as HTMLElement;
    }

    it('renders a sort button on every allowlisted column and none anywhere else', () => {
      const fixture = setup([]);
      const root = fixture.nativeElement as HTMLElement;

      const ids = Array.from(root.querySelectorAll('[data-testid^="unique-words-table-sort-"]')).map(
        (button) => button.getAttribute('data-testid'),
      );
      expect(ids).toEqual([
        'unique-words-table-sort-alpha',
        'unique-words-table-sort-occurrences',
        'unique-words-table-sort-ayahs',
        'unique-words-table-sort-surahs',
      ]);
    });

    it('leaves the row-number, type/root and missing-surahs headers plain — no button, no aria-sort', () => {
      const fixture = setup([]);
      const root = fixture.nativeElement as HTMLElement;
      const plainHeaders = Array.from(
        root.querySelectorAll('[role="columnheader"]'),
      ).filter((header) => header.querySelector('.qd-explorer-table__sort-button') === null);

      expect(plainHeaders.length).toBeGreaterThan(0);
      for (const header of plainHeaders) {
        expect(header.querySelector('button')).toBeNull();
        expect(header.hasAttribute('aria-sort')).toBe(false);
      }
      expect(
        plainHeaders.map((header) => header.textContent?.trim()),
      ).toEqual(['م', 'نوع الكلمة', 'الجذر', 'لم يذكر فيها']);
    });

    it('cycles a count column: natural desc → asc → release', () => {
      const emitted: (string | null)[] = [];
      const fixture = setup([], { sort: 'mushaf-order' });
      fixture.componentInstance.sortChange.subscribe((sort) => emitted.push(sort));
      const root = fixture.nativeElement as HTMLElement;
      const button = () =>
        root.querySelector('[data-testid="unique-words-table-sort-occurrences"]') as HTMLButtonElement;

      button().click();
      fixture.componentRef.setInput('sort', 'occurrences');
      fixture.detectChanges();
      button().click();
      fixture.componentRef.setInput('sort', 'occurrences-asc');
      fixture.detectChanges();
      button().click();

      expect(emitted).toEqual(['occurrences', 'occurrences-asc', null]);
    });

    it('cycles the text column alpha the other way: natural asc → desc → release', () => {
      const emitted: (string | null)[] = [];
      const fixture = setup([], { sort: 'mushaf-order' });
      fixture.componentInstance.sortChange.subscribe((sort) => emitted.push(sort));
      const root = fixture.nativeElement as HTMLElement;
      const button = () =>
        root.querySelector('[data-testid="unique-words-table-sort-alpha"]') as HTMLButtonElement;

      button().click();
      fixture.componentRef.setInput('sort', 'alpha');
      fixture.detectChanges();
      button().click();
      fixture.componentRef.setInput('sort', 'alpha-desc');
      fixture.detectChanges();
      button().click();

      expect(emitted).toEqual(['alpha', 'alpha-desc', null]);
    });

    it('carries aria-sort only on the active column, and drops it on release', () => {
      const fixture = setup([], { sort: 'occurrences' });
      const root = fixture.nativeElement as HTMLElement;

      expect(headerCellFor(root, 'occurrences').getAttribute('aria-sort')).toBe('descending');
      expect(headerCellFor(root, 'alpha').hasAttribute('aria-sort')).toBe(false);

      fixture.componentRef.setInput('sort', 'occurrences-asc');
      fixture.detectChanges();
      expect(headerCellFor(root, 'occurrences').getAttribute('aria-sort')).toBe('ascending');

      fixture.componentRef.setInput('sort', 'mushaf-order');
      fixture.detectChanges();
      expect(headerCellFor(root, 'occurrences').hasAttribute('aria-sort')).toBe(false);
    });

    it('renders the direction glyph as an aria-hidden span, and none when inactive', () => {
      const fixture = setup([], { sort: 'occurrences' });
      const root = fixture.nativeElement as HTMLElement;
      const button = root.querySelector(
        '[data-testid="unique-words-table-sort-occurrences"]',
      ) as HTMLElement;
      const glyph = button.querySelector('.qd-explorer-table__sort-glyph') as HTMLElement;

      expect(glyph.getAttribute('aria-hidden')).toBe('true');
      expect(glyph.textContent?.trim()).toBe('▼');
      expect(
        root
          .querySelector('[data-testid="unique-words-table-sort-alpha"]')
          ?.querySelector('.qd-explorer-table__sort-glyph'),
      ).toBeNull();
    });

    it('names the column and the next cycle state in the Arabic aria-label', () => {
      const fixture = setup([], { sort: 'mushaf-order' });
      const root = fixture.nativeElement as HTMLElement;
      const button = () =>
        root.querySelector('[data-testid="unique-words-table-sort-alpha"]') as HTMLElement;

      expect(button().getAttribute('aria-label')).toBe('ترتيب حسب الكلمة تصاعديًا');

      fixture.componentRef.setInput('sort', 'alpha');
      fixture.detectChanges();
      expect(button().getAttribute('aria-label')).toBe('ترتيب حسب الكلمة تنازليًا');

      fixture.componentRef.setInput('sort', 'alpha-desc');
      fixture.detectChanges();
      expect(button().getAttribute('aria-label')).toBe('إلغاء الترتيب حسب الكلمة');
    });
  });

  describe('in-shell list states (Feature 030, N3 row 5)', () => {
    it('renders the error state inside the table shell, replacing the body', () => {
      const fixture = setup([], { status: 'error', errorMessage: 'تعذر تحميل الكلمات' });
      const root = fixture.nativeElement as HTMLElement;

      const state = root.querySelector('[data-testid="unique-words-error"]');
      expect(state).toBeTruthy();
      expect(state?.getAttribute('role')).toBe('alert');
      expect(state?.textContent?.trim()).toBe('تعذر تحميل الكلمات');
      expect(state?.closest('.qd-explorer-table')).toBeTruthy();
      expect(state?.classList.contains('unique-words-table__state')).toBe(true);
      expect(root.querySelector('.qd-explorer-table__body')).toBeNull();
      expect(root.querySelector('.qd-explorer-table__header')).toBeTruthy();
    });

    it('renders the no-results state inside the table shell, replacing the body', () => {
      const fixture = setup([], { status: 'empty' });
      const root = fixture.nativeElement as HTMLElement;

      const state = root.querySelector('[data-testid="unique-words-empty"]');
      expect(state).toBeTruthy();
      expect(state?.textContent?.trim()).toBe(EMPTY_LIST_LABEL);
      expect(state?.closest('.qd-explorer-table')).toBeTruthy();
      expect(state?.classList.contains('unique-words-table__state')).toBe(true);
      expect(root.querySelector('.qd-explorer-table__body')).toBeNull();
      expect(root.querySelector('.qd-explorer-table__header')).toBeTruthy();
    });

    it('shows the skeleton body and no state box while loading', () => {
      const fixture = setup([], { loading: true, status: 'loading' });
      const root = fixture.nativeElement as HTMLElement;

      expect(root.querySelector('[data-testid="unique-words-loading"]')).toBeTruthy();
      expect(root.querySelector('.unique-words-table__state')).toBeNull();
    });

    it('renders the body and no state box on success', () => {
      const fixture = setup([row(1)], { status: 'success' });
      const root = fixture.nativeElement as HTMLElement;

      expect(root.querySelector('.unique-words-table__state')).toBeNull();
      expect(root.querySelector('.qd-explorer-table__body')).toBeTruthy();
    });
  });

});
