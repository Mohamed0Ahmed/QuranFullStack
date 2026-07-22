import { describe, expect, it, beforeEach, afterEach } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';

import { RootsTableComponent } from './roots-table.component';
import { RootListItemViewModel } from '../../models/roots.models';
import { ROOTS_NO_RESULTS_LABEL } from '../../models/roots.labels';

function row(id: number, overrides: Partial<RootListItemViewModel> = {}): RootListItemViewModel {
  return {
    id,
    rootText: `جذر-${id}`,
    displayText: `جذر-${id}`,
    occurrencesCount: id * 10,
    ayahsCount: id * 2,
    surahsCount: id,
    simpleWordsCount: id + 1,
    tashkeelWordsCount: id + 2,
    lemmasCount: id + 3,
    stemsCount: id + 4,
    ...overrides,
  };
}

describe('RootsTableComponent', () => {
  beforeEach(() => {
    getTestBed().resetTestingModule();
    TestBed.configureTestingModule({
      imports: [RootsTableComponent],
      teardown: { destroyAfterEach: true },
    });
  });

  afterEach(() => {
    getTestBed().resetTestingModule();
  });

  function setup(
    rows: readonly RootListItemViewModel[],
    overrides: Record<string, unknown> = {},
  ) {
    const fixture = TestBed.createComponent(RootsTableComponent);
    fixture.componentRef.setInput('rows', rows);
    for (const [key, value] of Object.entries(overrides)) {
      (fixture.componentRef as unknown as { setInput: (k: string, v: unknown) => void }).setInput(key, value);
    }
    fixture.detectChanges();
    return fixture;
  }

  it('renders the column headers', () => {
    const fixture = setup([], { loading: false });
    const root = fixture.nativeElement as HTMLElement;

    const headers = Array.from(root.querySelectorAll('[role="columnheader"]')).map((h) =>
      h.textContent?.trim() ?? '',
    );
    expect(headers).toContain('م');
    expect(headers).toContain('الجذر');
    expect(headers).toContain('المواضع');
    expect(headers).toContain('الآيات');
    expect(headers).toContain('السور');
    expect(headers).toContain('بدون تشكيل');
    expect(headers).toContain('بالتشكيل');
    expect(headers).toContain('الصيغ');
    expect(headers).toContain('الأصول');
  });

  it('renders the eight counts and UI row numbers, never backend ids', () => {
    const fixture = setup([row(1), row(2)], { currentPage: 1 });
    const root = fixture.nativeElement as HTMLElement;

    const rows = root.querySelectorAll('[role="row"]:not(.roots-table__header-row)');
    expect(rows).toHaveLength(2);

    const rowNumbers = Array.from(
      root.querySelectorAll('.roots-table__cell--row-number'),
    ).map((c) => c.textContent?.trim() ?? '');
    expect(rowNumbers).toEqual(['1', '2']);

    expect(root.textContent).not.toContain('id-');
    expect(root.querySelectorAll('qd-word-count-chip')).toHaveLength(14);
  });

  it('keeps count chip aria names semantic while grid headers stay compact', () => {
    const fixture = setup([row(1)]);
    const root = fixture.nativeElement as HTMLElement;

    const chipLabels = Array.from(root.querySelectorAll('qd-word-count-chip button')).map(
      (btn) => btn.getAttribute('aria-label') ?? '',
    );
    expect(chipLabels[3]).toContain('كلمات بدون تشكيل');
    expect(chipLabels[4]).toContain('كلمات بالتشكيل');
    expect(chipLabels[5]).toContain('الصيغ المعجمية');
    expect(chipLabels[6]).toContain('الأصول الصرفية');

    const headers = Array.from(root.querySelectorAll('[role="columnheader"]')).map(
      (h) => h.textContent?.trim() ?? '',
    );
    expect(headers).toContain('الصيغ');
    expect(headers).not.toContain('الصيغ المعجمية');
  });

  it('emits rowSelected with the row when the root cell is clicked', () => {
    const fixture = setup([row(1), row(2)]);
    const emitted: RootListItemViewModel[] = [];
    fixture.componentInstance.rowSelected.subscribe((r) => emitted.push(r));

    const buttons = fixture.nativeElement.querySelectorAll(
      '[data-testid="roots-table-root-button"]',
    ) as NodeListOf<HTMLElement>;
    buttons[1].click();
    fixture.detectChanges();

    expect(emitted).toHaveLength(1);
    expect(emitted[0].id).toBe(2);
  });

  it('marks the selected row with aria-selected', () => {
    const fixture = setup([row(1), row(2)], { selectedRootId: 2 });
    const root = fixture.nativeElement as HTMLElement;

    const selected = root.querySelector('[aria-selected="true"]');
    expect(selected).toBeTruthy();
  });

  it('emits countOpened with the matching view and sub-view per the count-click mapping', () => {
    const fixture = setup([row(1)]);
    const emitted: {
      root: RootListItemViewModel;
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
      { view: 'lemmas' },
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

  it('marks the active chip for the selected row', () => {
    const fixture = setup([row(1)], {
      selectedRootId: 1,
      activeView: 'surahs',
      activeSurahView: 'mentioned',
      activeColumn: 'surahs',
    });

    const buttons = Array.from(
      fixture.nativeElement.querySelectorAll('qd-word-count-chip button'),
    ) as HTMLButtonElement[];

    expect(buttons[2]?.classList.contains('qd-is-selected')).toBe(true);
    expect(buttons[1]?.classList.contains('qd-is-selected')).toBe(false);
  });

  it('moves between columns and rows with keyboard navigation events', () => {
    const fixture = setup([row(1), row(2)], {
      selectedRootId: 1,
      activeView: 'surahs',
      activeSurahView: 'mentioned',
      activeColumn: 'surahs',
    });
    const emitted: {
      root: RootListItemViewModel;
      column?: string;
      view: string;
      source?: string;
    }[] = [];
    fixture.componentInstance.countOpened.subscribe((event) => emitted.push(event));

    const table = fixture.nativeElement.querySelector('.roots-table') as HTMLElement;

    table.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowRight', bubbles: true }));
    fixture.detectChanges();

    fixture.componentRef.setInput('activeView', 'ayahs');
    fixture.componentRef.setInput('activeColumn', 'ayahs');
    fixture.detectChanges();

    table.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true }));
    fixture.detectChanges();

    expect(emitted[0]).toMatchObject({
      root: row(1),
      column: 'ayahs',
      view: 'ayahs',
      source: 'keyboard',
    });
    expect(emitted[1]).toMatchObject({
      root: row(2),
      column: 'ayahs',
      view: 'ayahs',
      source: 'keyboard',
    });
  });

  it('leaves arrow keys to a focused sort header instead of moving the selected row', () => {
    const fixture = setup([row(1), row(2)], {
      selectedRootId: 1,
      activeView: 'ayahs',
      activeColumn: 'ayahs',
    });
    const emitted: unknown[] = [];
    fixture.componentInstance.countOpened.subscribe((event) => emitted.push(event));

    const sortButton = fixture.nativeElement.querySelector(
      '[data-testid="roots-table-sort-surahs"]',
    ) as HTMLElement;
    const event = new KeyboardEvent('keydown', {
      key: 'ArrowDown',
      bubbles: true,
      cancelable: true,
    });
    sortButton.dispatchEvent(event);
    fixture.detectChanges();

    expect(event.defaultPrevented).toBe(false);
    expect(emitted).toEqual([]);
  });

  it('still navigates on arrow keys pressed from a row count chip', () => {
    const fixture = setup([row(1), row(2)], {
      selectedRootId: 1,
      activeView: 'ayahs',
      activeColumn: 'ayahs',
    });
    const emitted: { root: RootListItemViewModel; column?: string; source?: string }[] = [];
    fixture.componentInstance.countOpened.subscribe((event) => emitted.push(event));

    const chipButton = fixture.nativeElement.querySelector(
      'qd-word-count-chip button',
    ) as HTMLElement;
    const event = new KeyboardEvent('keydown', {
      key: 'ArrowDown',
      bubbles: true,
      cancelable: true,
    });
    chipButton.dispatchEvent(event);
    fixture.detectChanges();

    expect(event.defaultPrevented).toBe(true);
    expect(emitted).toHaveLength(1);
    expect(emitted[0]).toMatchObject({ root: row(2), column: 'ayahs', source: 'keyboard' });
  });

  describe('column-header sorting (Feature 030, N8)', () => {
    function headerCellFor(root: HTMLElement, key: string): HTMLElement {
      const button = root.querySelector(`[data-testid="roots-table-sort-${key}"]`) as HTMLElement;
      return button.closest('[role="columnheader"]') as HTMLElement;
    }

    it('renders a sort button on every allowlisted column and none anywhere else', () => {
      const fixture = setup([]);
      const root = fixture.nativeElement as HTMLElement;

      const keys = Array.from(root.querySelectorAll('[data-testid^="roots-table-sort-"]')).map(
        (button) => button.getAttribute('data-testid'),
      );
      expect(keys).toEqual([
        'roots-table-sort-alpha',
        'roots-table-sort-occurrences',
        'roots-table-sort-ayahs',
        'roots-table-sort-surahs',
        'roots-table-sort-simple',
        'roots-table-sort-tashkeel',
        'roots-table-sort-lemmas',
        'roots-table-sort-stems',
      ]);
    });

    it('leaves the row-number header plain — no button, no aria-sort', () => {
      const fixture = setup([]);
      const root = fixture.nativeElement as HTMLElement;
      const rowNumber = root.querySelector(
        '.qd-explorer-table__header-cell--row-number',
      ) as HTMLElement;

      expect(rowNumber.querySelector('button')).toBeNull();
      expect(rowNumber.hasAttribute('aria-sort')).toBe(false);
    });

    it.each([
      ['occurrences', 'occurrences', 'occurrences-asc'],
      ['ayahs', 'ayahs', 'ayahs-asc'],
      ['stems', 'stems', 'stems-asc'],
    ])('cycles the count column %s: natural desc → asc → release', (key, natural, opposite) => {
      const emitted: (string | null)[] = [];
      const fixture = setup([], { sort: 'mushaf-order' });
      fixture.componentInstance.sortChange.subscribe((sort) => emitted.push(sort));
      const root = fixture.nativeElement as HTMLElement;
      const button = () =>
        root.querySelector(`[data-testid="roots-table-sort-${key}"]`) as HTMLButtonElement;

      button().click();
      fixture.componentRef.setInput('sort', natural);
      fixture.detectChanges();
      button().click();
      fixture.componentRef.setInput('sort', opposite);
      fixture.detectChanges();
      button().click();

      expect(emitted).toEqual([natural, opposite, null]);
    });

    it('cycles the text column alpha the other way: natural asc → desc → release', () => {
      const emitted: (string | null)[] = [];
      const fixture = setup([], { sort: 'mushaf-order' });
      fixture.componentInstance.sortChange.subscribe((sort) => emitted.push(sort));
      const root = fixture.nativeElement as HTMLElement;
      const button = () =>
        root.querySelector('[data-testid="roots-table-sort-alpha"]') as HTMLButtonElement;

      button().click();
      fixture.componentRef.setInput('sort', 'alpha');
      fixture.detectChanges();
      button().click();
      fixture.componentRef.setInput('sort', 'alpha-desc');
      fixture.detectChanges();
      button().click();

      expect(emitted).toEqual(['alpha', 'alpha-desc', null]);
    });

    it('starts a fresh cycle at the natural direction when another column was active', () => {
      const emitted: (string | null)[] = [];
      const fixture = setup([], { sort: 'alpha-desc' });
      fixture.componentInstance.sortChange.subscribe((sort) => emitted.push(sort));
      const root = fixture.nativeElement as HTMLElement;

      (root.querySelector('[data-testid="roots-table-sort-surahs"]') as HTMLButtonElement).click();

      expect(emitted).toEqual(['surahs']);
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

    it('renders the direction glyph as an aria-hidden span beside the label', () => {
      const fixture = setup([], { sort: 'occurrences' });
      const root = fixture.nativeElement as HTMLElement;
      const button = root.querySelector(
        '[data-testid="roots-table-sort-occurrences"]',
      ) as HTMLElement;
      const glyph = button.querySelector('.qd-explorer-table__sort-glyph') as HTMLElement;

      expect(glyph.getAttribute('aria-hidden')).toBe('true');
      expect(glyph.textContent?.trim()).toBe('▼');

      fixture.componentRef.setInput('sort', 'occurrences-asc');
      fixture.detectChanges();
      expect(
        button.querySelector('.qd-explorer-table__sort-glyph')?.textContent?.trim(),
      ).toBe('▲');

      expect(
        root
          .querySelector('[data-testid="roots-table-sort-alpha"]')
          ?.querySelector('.qd-explorer-table__sort-glyph'),
      ).toBeNull();
    });

    it('names the column and the next cycle state in the Arabic aria-label', () => {
      const fixture = setup([], { sort: 'mushaf-order' });
      const root = fixture.nativeElement as HTMLElement;
      const button = () =>
        root.querySelector('[data-testid="roots-table-sort-occurrences"]') as HTMLElement;

      expect(button().getAttribute('aria-label')).toBe('ترتيب حسب المواضع تنازليًا');

      fixture.componentRef.setInput('sort', 'occurrences');
      fixture.detectChanges();
      expect(button().getAttribute('aria-label')).toBe('ترتيب حسب المواضع تصاعديًا');

      fixture.componentRef.setInput('sort', 'occurrences-asc');
      fixture.detectChanges();
      expect(button().getAttribute('aria-label')).toBe('إلغاء الترتيب حسب المواضع');
    });
  });

  describe('in-shell list states (Feature 030, N3 row 5)', () => {
    it('renders the error state inside the table shell, replacing the body', () => {
      const fixture = setup([], { status: 'error', errorMessage: 'تعذر تحميل الجذور' });
      const root = fixture.nativeElement as HTMLElement;

      const state = root.querySelector('[data-testid="roots-list-error"]');
      expect(state).toBeTruthy();
      expect(state?.getAttribute('role')).toBe('alert');
      expect(state?.textContent?.trim()).toBe('تعذر تحميل الجذور');
      expect(state?.closest('.qd-explorer-table')).toBeTruthy();
      expect(state?.classList.contains('roots-table__state')).toBe(true);
      expect(root.querySelector('.qd-explorer-table__body')).toBeNull();
      expect(root.querySelector('.qd-explorer-table__header')).toBeTruthy();
    });

    it('renders the no-results state inside the table shell, replacing the body', () => {
      const fixture = setup([], { status: 'empty' });
      const root = fixture.nativeElement as HTMLElement;

      const state = root.querySelector('[data-testid="roots-list-no-results"]');
      expect(state).toBeTruthy();
      expect(state?.textContent?.trim()).toBe(ROOTS_NO_RESULTS_LABEL);
      expect(state?.closest('.qd-explorer-table')).toBeTruthy();
      expect(state?.classList.contains('roots-table__state')).toBe(true);
      expect(root.querySelector('.qd-explorer-table__body')).toBeNull();
      expect(root.querySelector('.qd-explorer-table__header')).toBeTruthy();
    });

    it('shows the skeleton body and no state box while loading', () => {
      const fixture = setup([], { loading: true, status: 'loading' });
      const root = fixture.nativeElement as HTMLElement;

      expect(root.querySelector('[data-testid="roots-table-loading"]')).toBeTruthy();
      expect(root.querySelector('.roots-table__state')).toBeNull();
    });

    it('renders the body and no state box on success', () => {
      const fixture = setup([row(1)], { status: 'success' });
      const root = fixture.nativeElement as HTMLElement;

      expect(root.querySelector('.roots-table__state')).toBeNull();
      expect(root.querySelector('.qd-explorer-table__body')).toBeTruthy();
    });
  });

});
