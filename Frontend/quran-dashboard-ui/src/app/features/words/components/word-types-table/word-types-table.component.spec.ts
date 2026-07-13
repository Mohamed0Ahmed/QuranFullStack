import { getTestBed, TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { WordTypeCountOpenedEvent, WordTypesTableComponent } from './word-types-table.component';
import {
  LemmaTableRowDto,
  PagedResultDto,
  RootTableRowDto,
  StemTableRowDto,
  WordTableRowDto,
  WordTypeTableRowDto,
  WordTypeTableView,
} from '../../models/word-types.models';

function word(overrides: Partial<WordTableRowDto> = {}): WordTableRowDto {
  return {
    kind: 'word',
    tashkeelWordId: 191001,
    contextCode: 'PN',
    case: 'all',
    tense: 'all',
    voice: 'all',
    displayText: 'SYNTH_WORD_TEXT',
    typeCode: 'PN',
    typeLabel: { ar: 'SYNTH_TYPE_LABEL' },
    broadLabel: { ar: 'SYNTH_BROAD_LABEL' },
    caseOrFeature: null,
    rootText: 'SYNTH_ROOT_TEXT',
    lemmaText: null,
    stemText: null,
    occurrencesCount: 2,
    ayahsCount: 2,
    surahsCount: 1,
    ...overrides,
  };
}

function page(items: WordTypeTableRowDto[]): PagedResultDto<WordTypeTableRowDto> {
  return {
    page: 1,
    pageSize: 25,
    totalCount: items.length,
    items,
  };
}

function pagedPage(items: WordTypeTableRowDto[], pageNumber: number, pageSize: number): PagedResultDto<WordTypeTableRowDto> {
  return {
    page: pageNumber,
    pageSize,
    totalCount: pageNumber * pageSize + items.length,
    items,
  };
}

const rootRow: RootTableRowDto = {
  kind: 'root',
  rootId: 190700,
  displayText: 'SYNTH_ROOT_TEXT',
  occurrencesCount: 3,
  ayahsCount: 2,
  surahsCount: 1,
};

const stemRow: StemTableRowDto = {
  kind: 'stem',
  stemId: 190701,
  displayText: 'SYNTH_STEM_TEXT',
  occurrencesCount: 4,
  ayahsCount: 3,
  surahsCount: 2,
};

const lemmaRow: LemmaTableRowDto = {
  kind: 'lemma',
  lemmaId: 190702,
  displayText: 'SYNTH_LEMMA_TEXT',
  occurrencesCount: 5,
  ayahsCount: 4,
  surahsCount: 2,
};

describe('WordTypesTableComponent', () => {
  beforeEach(() => {
    getTestBed().resetTestingModule();
    TestBed.configureTestingModule({ imports: [WordTypesTableComponent], teardown: { destroyAfterEach: true } });
  });

  afterEach(() => getTestBed().resetTestingModule());

  it('renders corrected word headers and makes only its statistics actionable', () => {
    const fixture = TestBed.createComponent(WordTypesTableComponent);
    const countEvents: WordTypeCountOpenedEvent[] = [];
    fixture.componentRef.setInput('rows', page([word()]));
    fixture.componentRef.setInput('selectedRow', word());
    fixture.componentInstance.countOpened.subscribe((event) => countEvents.push(event));
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    const headers = Array.from(root.querySelectorAll('[role="columnheader"]')).map((header) => header.textContent?.trim());
    const tableRow = root.querySelector('.word-types-table__row') as HTMLElement;
    const countButtons = root.querySelectorAll('[data-testid="word-count-chip"]') as NodeListOf<HTMLButtonElement>;

    expect(headers).toEqual(['م', 'الكلمة', 'النوع', 'الجذر', 'الأصل', 'الصيغة', 'المواضع', 'الآيات', 'السور']);
    expect(root.textContent).toContain('SYNTH_WORD_TEXT');
    expect(root.textContent).toContain('—');
    expect(root.textContent).not.toContain('191001');
    expect(root.querySelector('.word-types-table__header-gutter')).not.toBeNull();
    expect(tableRow.getAttribute('tabindex')).toBeNull();
    expect(tableRow.getAttribute('aria-current')).toBe('true');
    expect(tableRow.getAttribute('aria-selected')).toBe('true');
    expect(tableRow.classList.contains('qd-is-selected')).toBe(true);
    expect(countButtons).toHaveLength(3);

    tableRow.click();
    tableRow.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));
    tableRow.dispatchEvent(new KeyboardEvent('keydown', { key: ' ', bubbles: true }));
    expect(countEvents).toEqual([]);

    countButtons.forEach((button) => button.click());

    expect(countEvents).toEqual([
      { row: word(), column: 'occurrences', view: 'ayahs' },
      { row: word(), column: 'ayahs', view: 'ayahs' },
      { row: word(), column: 'surahs', view: 'surahs' },
    ]);
  });

  it.each([
    ['roots', rootRow, 'الجذر', 'جدول الجذور', 190700, 'root:190700'],
    ['stems', stemRow, 'الأصل', 'جدول الأصول', 190701, 'stem:190701'],
    ['lemmas', lemmaRow, 'الصيغة', 'جدول الصيغ', 190702, 'lemma:190702'],
  ] as const)(
    'renders %s grouped rows with three statistic actions and an inert row container',
    (tableView, groupedRow, dimensionHeader, tableLabel, numericId, rowDomId) => {
      const fixture = TestBed.createComponent(WordTypesTableComponent);
      const countEvents: WordTypeCountOpenedEvent[] = [];
      fixture.componentRef.setInput('rows', page([groupedRow]));
      fixture.componentRef.setInput('tableView', tableView as WordTypeTableView);
      fixture.componentRef.setInput('selectedRow', groupedRow);
      fixture.componentInstance.countOpened.subscribe((event) => countEvents.push(event));
      fixture.detectChanges();

      const root = fixture.nativeElement as HTMLElement;
      const headers = Array.from(root.querySelectorAll('[role="columnheader"]')).map((header) => header.textContent?.trim());
      const groupedTableRow = root.querySelector('.word-types-table__row') as HTMLElement;
      const countButtons = root.querySelectorAll('[data-testid="word-count-chip"]') as NodeListOf<HTMLButtonElement>;

      expect(root.querySelector('[role="table"]')?.getAttribute('aria-label')).toBe(tableLabel);
      expect(headers).toEqual(['م', dimensionHeader, 'المواضع', 'الآيات', 'السور']);
      expect(groupedTableRow).not.toBeNull();
      expect(groupedTableRow.getAttribute('tabindex')).toBeNull();
      expect(groupedTableRow.getAttribute('data-word-types-row')).toBe(rowDomId);
      expect(groupedTableRow.textContent).toContain(groupedRow.displayText);
      expect(groupedTableRow.textContent).toContain(String(groupedRow.occurrencesCount));
      expect(groupedTableRow.textContent).toContain(String(groupedRow.ayahsCount));
      expect(groupedTableRow.textContent).toContain(String(groupedRow.surahsCount));
      expect(root.textContent).not.toContain(String(numericId));
      expect(countButtons).toHaveLength(3);
      expect(Array.from(countButtons).map((button) => button.getAttribute('aria-label'))).toEqual([
        `المواضع: ${groupedRow.occurrencesCount}`,
        `الآيات: ${groupedRow.ayahsCount}`,
        `السور: ${groupedRow.surahsCount}`,
      ]);
      expect(groupedTableRow.classList.contains('qd-is-selected')).toBe(true);
      expect(groupedTableRow.getAttribute('aria-current')).toBe('true');
      expect(groupedTableRow.getAttribute('aria-selected')).toBe('true');

      groupedTableRow.click();
      groupedTableRow.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));
      groupedTableRow.dispatchEvent(new KeyboardEvent('keydown', { key: ' ', bubbles: true }));
      expect(countEvents).toEqual([]);

      countButtons.forEach((button) => button.click());
      expect(countEvents).toEqual([
        { row: groupedRow, column: 'occurrences', view: 'words' },
        { row: groupedRow, column: 'ayahs', view: 'ayahs' },
        { row: groupedRow, column: 'surahs', view: 'surahs' },
      ]);
    },
  );

  it('does not mark a grouped row selected when the active selection is a different kind', () => {
    const fixture = TestBed.createComponent(WordTypesTableComponent);
    fixture.componentRef.setInput('rows', page([rootRow]));
    fixture.componentRef.setInput('tableView', 'roots');
    fixture.componentRef.setInput('selectedRow', word());
    fixture.detectChanges();

    const groupedTableRow = (fixture.nativeElement as HTMLElement).querySelector('.word-types-table__row') as HTMLElement;
    expect(groupedTableRow.classList.contains('qd-is-selected')).toBe(false);
    expect(groupedTableRow.getAttribute('aria-selected')).toBe('false');
    expect(groupedTableRow.getAttribute('aria-current')).toBeNull();
  });

  it('skips rows whose discriminant does not match the active table view', () => {
    const fixture = TestBed.createComponent(WordTypesTableComponent);
    fixture.componentRef.setInput('rows', page([word()]));
    fixture.componentRef.setInput('tableView', 'roots');
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('.word-types-table__body [role="row"]')).toBeNull();
    expect(root.textContent).not.toContain('SYNTH_WORD_TEXT');
  });

  it.each([
    { label: 'stem under roots', tableView: 'roots', row: stemRow, hiddenText: stemRow.displayText },
    { label: 'lemma under roots', tableView: 'roots', row: lemmaRow, hiddenText: lemmaRow.displayText },
    { label: 'root under stems', tableView: 'stems', row: rootRow, hiddenText: rootRow.displayText },
    { label: 'lemma under stems', tableView: 'stems', row: lemmaRow, hiddenText: lemmaRow.displayText },
    { label: 'root under lemmas', tableView: 'lemmas', row: rootRow, hiddenText: rootRow.displayText },
    { label: 'stem under lemmas', tableView: 'lemmas', row: stemRow, hiddenText: stemRow.displayText },
  ] as const)(
    'skips a $label mismatch so a stale grouped response never paints under the wrong tab',
    ({ tableView, row, hiddenText }) => {
      const fixture = TestBed.createComponent(WordTypesTableComponent);
      fixture.componentRef.setInput('rows', page([row]));
      fixture.componentRef.setInput('tableView', tableView as WordTypeTableView);
      fixture.detectChanges();

      const root = fixture.nativeElement as HTMLElement;

      expect(root.querySelector('.word-types-table__body [role="row"]')).toBeNull();
      expect(root.textContent).not.toContain(hiddenText);
    },
  );

  it('renders a skeleton body while loading, even when prior rows exist', () => {
    const loadingFixture = TestBed.createComponent(WordTypesTableComponent);
    loadingFixture.componentRef.setInput('loading', true);
    loadingFixture.detectChanges();

    const loadingRoot = loadingFixture.nativeElement as HTMLElement;
    expect(loadingRoot.querySelector('[data-testid="word-types-table-loading"]')).not.toBeNull();
    expect(loadingRoot.querySelector('.word-types-table__row--loading')).not.toBeNull();

    const refreshFixture = TestBed.createComponent(WordTypesTableComponent);
    refreshFixture.componentRef.setInput('rows', page([word()]));
    refreshFixture.componentRef.setInput('loading', true);
    refreshFixture.detectChanges();

    const refreshRoot = refreshFixture.nativeElement as HTMLElement;
    expect(refreshRoot.querySelector('[data-testid="word-types-table-loading"]')).not.toBeNull();
    expect(refreshRoot.querySelector('[data-word-types-row]')).toBeNull();
  });

  it('renders the select prompt inside the table when no rows and status is selectPrompt', () => {
    const fixture = TestBed.createComponent(WordTypesTableComponent);
    fixture.componentRef.setInput('status', 'selectPrompt');
    fixture.componentRef.setInput('selectPromptLabel', 'اختر نوعًا فرعيًا');
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('[data-testid="word-types-select-subtype"]')?.textContent).toContain('اختر نوعًا فرعيًا');
    expect(root.querySelector('.word-types-table__header')).toBeNull();
  });

  it('renders the empty label inside the table when status is empty', () => {
    const fixture = TestBed.createComponent(WordTypesTableComponent);
    fixture.componentRef.setInput('rows', page([]));
    fixture.componentRef.setInput('status', 'empty');
    fixture.componentRef.setInput('emptyLabel', 'لا توجد نتائج');
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('[data-testid="word-types-table-empty"]')?.textContent).toContain('لا توجد نتائج');
  });

  it('renders an error message and emits retry from inside the table', () => {
    const fixture = TestBed.createComponent(WordTypesTableComponent);
    const retries: void[] = [];
    fixture.componentRef.setInput('status', 'error');
    fixture.componentRef.setInput('errorMessage', 'تعذّر التحميل');
    fixture.componentRef.setInput('retryLabel', 'إعادة المحاولة');
    fixture.componentInstance.retry.subscribe(() => retries.push(undefined));
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('[data-testid="word-types-table-error"]')?.textContent).toContain('تعذّر التحميل');
    const retryButton = root.querySelector('[data-testid="word-types-table-retry"]') as HTMLButtonElement;
    retryButton.click();
    expect(retries).toHaveLength(1);
  });

  it('renders page-relative row numbers without database ids across all four views', () => {
    const views: readonly { tableView: WordTypeTableView; rows: WordTypeTableRowDto[]; hiddenId: string }[] = [
      { tableView: 'words', rows: [word({ tashkeelWordId: 191001 }), word({ tashkeelWordId: 191002, contextCode: 'N' })], hiddenId: '191001' },
      { tableView: 'roots', rows: [rootRow, { ...rootRow, rootId: 190711 }], hiddenId: '190700' },
      { tableView: 'stems', rows: [stemRow, { ...stemRow, stemId: 190712 }], hiddenId: '190701' },
      { tableView: 'lemmas', rows: [lemmaRow, { ...lemmaRow, lemmaId: 190713 }], hiddenId: '190702' },
    ];

    for (const view of views) {
      const fixture = TestBed.createComponent(WordTypesTableComponent);
      fixture.componentRef.setInput('rows', pagedPage(view.rows, 2, 25));
      fixture.componentRef.setInput('tableView', view.tableView);
      fixture.detectChanges();

      const root = fixture.nativeElement as HTMLElement;
      const numberCells = Array.from(root.querySelectorAll('.word-types-table__cell--row-number')).map((cell) => cell.textContent?.trim());

      expect(numberCells).toEqual(['26', '27']);
      expect(root.textContent).not.toContain(view.hiddenId);

      fixture.destroy();
    }
  });

  it.each([
    ['words', [word()]],
    ['roots', [rootRow]],
  ] as const)('renders %s rows with the quiet explorer-row class and no interactive-surface', (tableView, rows) => {
    const fixture = TestBed.createComponent(WordTypesTableComponent);
    fixture.componentRef.setInput('rows', page([...rows]));
    fixture.componentRef.setInput('tableView', tableView as WordTypeTableView);
    fixture.detectChanges();

    const tableRow = (fixture.nativeElement as HTMLElement).querySelector('.word-types-table__row') as HTMLElement;
    expect(tableRow.classList.contains('qd-explorer-table__row')).toBe(true);
    expect(tableRow.classList.contains('qd-interactive-surface')).toBe(false);
  });

  it('keeps loading rows non-selectable with no interactive surface', () => {
    const fixture = TestBed.createComponent(WordTypesTableComponent);
    fixture.componentRef.setInput('loading', true);
    fixture.detectChanges();

    const loadingRow = (fixture.nativeElement as HTMLElement).querySelector('.word-types-table__row--loading') as HTMLElement;
    expect(loadingRow).not.toBeNull();
    expect(loadingRow.classList.contains('qd-interactive-surface')).toBe(false);
    expect(loadingRow.classList.contains('qd-explorer-table__row')).toBe(false);
    expect(loadingRow.getAttribute('data-word-types-row')).toBeNull();
    expect(loadingRow.getAttribute('aria-selected')).toBeNull();
  });

  it.each([
    ['words', word(), '191001:PN:all:all:all'],
    ['roots', rootRow, 'root:190700'],
    ['stems', stemRow, 'stem:190701'],
    ['lemmas', lemmaRow, 'lemma:190702'],
  ] as const)('renders an inert %s row container with the stable DOM identity', (tableView, row, domId) => {
    const fixture = TestBed.createComponent(WordTypesTableComponent);
    fixture.componentRef.setInput('rows', page([row]));
    fixture.componentRef.setInput('tableView', tableView as WordTypeTableView);
    fixture.detectChanges();

    const tableRow = (fixture.nativeElement as HTMLElement).querySelector('.word-types-table__row') as HTMLElement;
    expect(tableRow.getAttribute('tabindex')).toBeNull();
    expect(tableRow.getAttribute('data-word-types-row')).toBe(domId);
  });

  it.each([
    ['words', word(), 'ayahs', 'occurrences', 0],
    ['roots', rootRow, 'words', null, 0],
    ['stems', stemRow, 'ayahs', null, 1],
    ['lemmas', lemmaRow, 'surahs', null, 2],
  ] as const)('returns focus to the matching %s statistic button', (tableView, row, view, column, expectedIndex) => {
    const fixture = TestBed.createComponent(WordTypesTableComponent);
    fixture.componentRef.setInput('rows', page([row]));
    fixture.componentRef.setInput('tableView', tableView as WordTypeTableView);
    fixture.detectChanges();

    const focusStatistic = (fixture.componentInstance as unknown as {
      focusStatistic?: (target: WordTypeTableRowDto, targetView: string, targetColumn?: string | null) => void;
    }).focusStatistic;
    focusStatistic?.call(fixture.componentInstance, row, view, column);

    const buttons = (fixture.nativeElement as HTMLElement).querySelectorAll('[data-testid="word-count-chip"]');
    expect(document.activeElement).toBe(buttons[expectedIndex]);
  });

  it.each([
    ['words', word(), word({ tashkeelWordId: 191002 })],
    ['roots', rootRow, { ...rootRow, rootId: 190711 }],
    ['stems', stemRow, { ...stemRow, stemId: 190712 }],
    ['lemmas', lemmaRow, { ...lemmaRow, lemmaId: 190713 }],
  ] as const)('transfers and clears the shared selected style for %s rows', (tableView, firstRow, secondRow) => {
    const fixture = TestBed.createComponent(WordTypesTableComponent);
    fixture.componentRef.setInput('rows', page([firstRow, secondRow]));
    fixture.componentRef.setInput('tableView', tableView as WordTypeTableView);
    fixture.componentRef.setInput('selectedRow', firstRow);
    fixture.detectChanges();

    let rows = (fixture.nativeElement as HTMLElement).querySelectorAll('.word-types-table__row');
    expect(rows[0].classList.contains('qd-is-selected')).toBe(true);
    expect(rows[1].classList.contains('qd-is-selected')).toBe(false);

    fixture.componentRef.setInput('selectedRow', secondRow);
    fixture.detectChanges();
    rows = (fixture.nativeElement as HTMLElement).querySelectorAll('.word-types-table__row');
    expect(rows[0].classList.contains('qd-is-selected')).toBe(false);
    expect(rows[1].classList.contains('qd-is-selected')).toBe(true);

    fixture.componentRef.setInput('selectedRow', null);
    fixture.detectChanges();
    expect(Array.from(rows).every((item) => !item.classList.contains('qd-is-selected'))).toBe(true);
  });
});
