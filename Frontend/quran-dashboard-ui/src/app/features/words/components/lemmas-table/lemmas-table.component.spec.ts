import { describe, expect, it, beforeEach, afterEach } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';

import { LemmasTableComponent } from './lemmas-table.component';
import { LemmaListItemViewModel } from '../../models/lemmas.models';
import { LEMMAS_NO_RESULTS_LABEL } from '../../models/lemmas.labels';

function row(id: number, overrides: Partial<LemmaListItemViewModel> = {}): LemmaListItemViewModel {
  return {
    id,
    lemmaText: `صيغة-${id}`,
    displayText: `صيغة-${id}`,
    rootId: id === 501 ? null : 700,
    rootText: id === 501 ? null : 'ك ل م',
    occurrencesCount: id * 10,
    ayahsCount: id * 2,
    surahsCount: id,
    simpleWordsCount: id + 1,
    tashkeelWordsCount: id + 2,
    stemsCount: id + 3,
    ...overrides,
  };
}

describe('LemmasTableComponent', () => {
  beforeEach(() => {
    getTestBed().resetTestingModule();
    TestBed.configureTestingModule({
      imports: [LemmasTableComponent],
      teardown: { destroyAfterEach: true },
    });
  });

  afterEach(() => {
    getTestBed().resetTestingModule();
  });

  function setup(
    rows: readonly LemmaListItemViewModel[],
    overrides: Record<string, unknown> = {},
  ) {
    const fixture = TestBed.createComponent(LemmasTableComponent);
    fixture.componentRef.setInput('rows', rows);
    for (const [key, value] of Object.entries(overrides)) {
      (fixture.componentRef as unknown as { setInput: (k: string, v: unknown) => void }).setInput(key, value);
    }
    fixture.detectChanges();
    return fixture;
  }

  it('renders the lemma headers without type column', () => {
    const fixture = setup([], { loading: false });
    const root = fixture.nativeElement as HTMLElement;

    const headers = Array.from(root.querySelectorAll('[role="columnheader"]')).map((h) =>
      h.textContent?.trim() ?? '',
    );
    expect(headers).toContain('م');
    expect(headers).toContain('الصيغة المعجمية');
    expect(headers).toContain('الجذر');
    expect(headers).toContain('المواضع');
    expect(headers).toContain('الآيات');
    expect(headers).toContain('السور');
    expect(headers).toContain('بدون تشكيل');
    expect(headers).toContain('بالتشكيل');
    expect(headers).toContain('الأصول');
  });

  it('renders counts and UI row numbers, never backend ids', () => {
    const fixture = setup([row(1), row(2)], { currentPage: 1 });
    const root = fixture.nativeElement as HTMLElement;

    const dataRows = root.querySelectorAll('.lemmas-table__row:not(.lemmas-table__header-row)');
    expect(dataRows.length).toBeGreaterThanOrEqual(2);

    const rowNumbers = Array.from(
      root.querySelectorAll('.lemmas-table__cell--row-number'),
    ).map((c) => c.textContent?.trim() ?? '');
    expect(rowNumbers).toContain('1');
    expect(rowNumbers).toContain('2');

    expect(root.textContent).not.toContain('id-');
    expect(root.querySelectorAll('qd-word-count-chip')).toHaveLength(12);
  });

  it('shows a dash for null owned root and a safe new-tab root anchor when present', () => {
    const fixture = setup([row(501), row(502)]);
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('[data-testid="lemmas-table-root-missing"]')).toBeTruthy();

    const link = root.querySelector('[data-testid="lemmas-table-root-link"]') as HTMLAnchorElement | null;
    expect(link).toBeTruthy();
    expect(link?.getAttribute('target')).toBe('_blank');
    expect(link?.getAttribute('rel')).toBe('noopener noreferrer');
    expect(link?.getAttribute('href')).toContain('/dashboard/words/roots');
    expect(link?.getAttribute('href')).toContain('root=700');
  });

  it('does not render a type column', () => {
    const fixture = setup([row(500)]);
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('[data-testid="lemmas-table-type"]')).toBeNull();
  });

  it('emits rowSelected when the lemma cell is clicked', () => {
    const fixture = setup([row(1), row(2)]);
    const emitted: LemmaListItemViewModel[] = [];
    fixture.componentInstance.rowSelected.subscribe((lemma) => emitted.push(lemma));

    const buttons = fixture.nativeElement.querySelectorAll(
      '[data-testid="lemmas-table-lemma-button"]',
    ) as NodeListOf<HTMLElement>;
    buttons[1].click();
    fixture.detectChanges();

    expect(emitted).toHaveLength(1);
    expect(emitted[0].id).toBe(2);
  });

  it('marks the selected row with aria-selected', () => {
    const fixture = setup([row(1), row(2)], { selectedLemmaId: 2 });
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('[aria-selected="true"]')).toBeTruthy();
  });

  it('emits countOpened with the mapped view and sub-view', () => {
    const fixture = setup([row(1)]);
    const emitted: {
      lemma: LemmaListItemViewModel;
      view: string;
      wordView?: string;
      surahView?: string;
    }[] = [];
    fixture.componentInstance.countOpened.subscribe((e) => emitted.push(e));

    const chips = fixture.nativeElement.querySelectorAll(
      'qd-word-count-chip',
    ) as NodeListOf<HTMLElement>;

    const expected = [
      { view: 'ayahs' },
      { view: 'ayahs' },
      { view: 'surahs', surahView: 'mentioned' },
      { view: 'words', wordView: 'simple' },
      { view: 'words', wordView: 'tashkeel' },
      { view: 'stems' },
    ];

    chips.forEach((chip) => {
      const btn = chip.querySelector('button');
      btn?.click();
      fixture.detectChanges();
    });

    expect(emitted.map((e) => e.view)).toEqual(expected.map((e) => e.view));
    expect(emitted.map((e) => e.wordView ?? null)).toEqual(
      expected.map((e) => e.wordView ?? null),
    );
    expect(emitted.map((e) => e.surahView ?? null)).toEqual(
      expected.map((e) => e.surahView ?? null),
    );
  });

  it('disables zero-count chips', () => {
    const fixture = setup([row(1, { ayahsCount: 0, simpleWordsCount: 0, stemsCount: 0 })]);
    const buttons = Array.from(
      fixture.nativeElement.querySelectorAll('qd-word-count-chip button'),
    ) as HTMLButtonElement[];

    expect(buttons[1]?.hasAttribute('disabled')).toBe(true);
    expect(buttons[3]?.hasAttribute('disabled')).toBe(true);
    expect(buttons[5]?.hasAttribute('disabled')).toBe(true);
  });

  describe('column-header sorting (Feature 030, N8)', () => {
    function headerCellFor(root: HTMLElement, key: string): HTMLElement {
      const button = root.querySelector(`[data-testid="lemmas-table-sort-${key}"]`) as HTMLElement;
      return button.closest('[role="columnheader"]') as HTMLElement;
    }

    it('renders a sort button on every allowlisted column and none anywhere else', () => {
      const fixture = setup([]);
      const root = fixture.nativeElement as HTMLElement;

      const ids = Array.from(root.querySelectorAll('[data-testid^="lemmas-table-sort-"]')).map(
        (button) => button.getAttribute('data-testid'),
      );
      expect(ids).toEqual([
        'lemmas-table-sort-alpha',
        'lemmas-table-sort-occurrences',
        'lemmas-table-sort-ayahs',
        'lemmas-table-sort-surahs',
        'lemmas-table-sort-simple',
        'lemmas-table-sort-tashkeel',
        'lemmas-table-sort-stems',
      ]);
    });

    it('leaves the row-number and related-entity الجذر headers plain — no button, no aria-sort', () => {
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
      ).toEqual(['م', 'الجذر']);
    });

    it('cycles a count column: natural desc → asc → release', () => {
      const emitted: (string | null)[] = [];
      const fixture = setup([], { sort: 'mushaf-order' });
      fixture.componentInstance.sortChange.subscribe((sort) => emitted.push(sort));
      const root = fixture.nativeElement as HTMLElement;
      const button = () =>
        root.querySelector('[data-testid="lemmas-table-sort-occurrences"]') as HTMLButtonElement;

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
        root.querySelector('[data-testid="lemmas-table-sort-alpha"]') as HTMLButtonElement;

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
        '[data-testid="lemmas-table-sort-occurrences"]',
      ) as HTMLElement;
      const glyph = button.querySelector('.qd-explorer-table__sort-glyph') as HTMLElement;

      expect(glyph.getAttribute('aria-hidden')).toBe('true');
      expect(glyph.textContent?.trim()).toBe('▼');
      expect(
        root
          .querySelector('[data-testid="lemmas-table-sort-alpha"]')
          ?.querySelector('.qd-explorer-table__sort-glyph'),
      ).toBeNull();
    });

    it('names the column and the next cycle state in the Arabic aria-label', () => {
      const fixture = setup([], { sort: 'mushaf-order' });
      const root = fixture.nativeElement as HTMLElement;
      const button = () =>
        root.querySelector('[data-testid="lemmas-table-sort-alpha"]') as HTMLElement;

      expect(button().getAttribute('aria-label')).toBe('ترتيب حسب الصيغة المعجمية تصاعديًا');

      fixture.componentRef.setInput('sort', 'alpha');
      fixture.detectChanges();
      expect(button().getAttribute('aria-label')).toBe('ترتيب حسب الصيغة المعجمية تنازليًا');

      fixture.componentRef.setInput('sort', 'alpha-desc');
      fixture.detectChanges();
      expect(button().getAttribute('aria-label')).toBe('إلغاء الترتيب حسب الصيغة المعجمية');
    });
  });

  // Feature 030, N3 row 5: these states used to be page-level banners that inserted above the fixed
  // table+panel grid and pushed it down ~4.5rem. They now stand in for the body INSIDE the mounted
  // shell. jsdom does no layout, so the geometry itself is pinned in SCSS (`.lemmas-table__state`
  // matches the body it replaces in every band) — these assert the structure that SCSS keys off.
  describe('in-shell list states (Feature 030, N3 row 5)', () => {
    it('renders the error state inside the table shell, replacing the body', () => {
      const fixture = setup([], { status: 'error', errorMessage: 'تعذر تحميل الصيغ' });
      const root = fixture.nativeElement as HTMLElement;

      const state = root.querySelector('[data-testid="lemmas-list-error"]');
      expect(state).toBeTruthy();
      expect(state?.getAttribute('role')).toBe('alert');
      expect(state?.textContent?.trim()).toBe('تعذر تحميل الصيغ');
      expect(state?.closest('.qd-explorer-table')).toBeTruthy();
      expect(state?.classList.contains('lemmas-table__state')).toBe(true);
      // it REPLACES the body (rather than stacking above it) and the shell stays mounted
      expect(root.querySelector('.qd-explorer-table__body')).toBeNull();
      expect(root.querySelector('.qd-explorer-table__header')).toBeTruthy();
    });

    it('renders the no-results state inside the table shell, replacing the body', () => {
      const fixture = setup([], { status: 'empty' });
      const root = fixture.nativeElement as HTMLElement;

      const state = root.querySelector('[data-testid="lemmas-list-no-results"]');
      expect(state).toBeTruthy();
      expect(state?.textContent?.trim()).toBe(LEMMAS_NO_RESULTS_LABEL);
      expect(state?.closest('.qd-explorer-table')).toBeTruthy();
      expect(state?.classList.contains('lemmas-table__state')).toBe(true);
      expect(root.querySelector('.qd-explorer-table__body')).toBeNull();
      expect(root.querySelector('.qd-explorer-table__header')).toBeTruthy();
    });

    it('shows the skeleton body and no state box while loading', () => {
      const fixture = setup([], { loading: true, status: 'loading' });
      const root = fixture.nativeElement as HTMLElement;

      expect(root.querySelector('[data-testid="lemmas-table-loading"]')).toBeTruthy();
      expect(root.querySelector('.lemmas-table__state')).toBeNull();
    });

    it('renders the body and no state box on success', () => {
      const fixture = setup([row(1)], { status: 'success' });
      const root = fixture.nativeElement as HTMLElement;

      expect(root.querySelector('.lemmas-table__state')).toBeNull();
      expect(root.querySelector('.qd-explorer-table__body')).toBeTruthy();
    });
  });

});
