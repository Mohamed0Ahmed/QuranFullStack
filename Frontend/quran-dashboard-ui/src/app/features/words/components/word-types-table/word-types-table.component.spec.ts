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
    displayText: 'كَلِمَة',
    typeCode: 'PN',
    typeLabel: { ar: 'اسم علم' },
    broadLabel: { ar: 'اسم' },
    caseOrFeature: null,
    rootText: 'ك ل م',
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
  displayText: 'ك ل م',
  occurrencesCount: 3,
  ayahsCount: 2,
  surahsCount: 1,
};

const stemRow: StemTableRowDto = {
  kind: 'stem',
  stemId: 190701,
  displayText: 'مَكْتُوب',
  occurrencesCount: 4,
  ayahsCount: 3,
  surahsCount: 2,
};

const lemmaRow: LemmaTableRowDto = {
  kind: 'lemma',
  lemmaId: 190702,
  displayText: 'كِتَاب',
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

  it('renders corrected word headers, Uthmani display, and interactive count actions', () => {
    const fixture = TestBed.createComponent(WordTypesTableComponent);
    const selected: WordTypeTableRowDto[] = [];
    const countEvents: WordTypeCountOpenedEvent[] = [];
    fixture.componentRef.setInput('rows', page([word()]));
    fixture.componentRef.setInput('selectedRow', word());
    fixture.componentInstance.rowSelected.subscribe((row) => selected.push(row));
    fixture.componentInstance.countOpened.subscribe((event) => countEvents.push(event));
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    const headers = Array.from(root.querySelectorAll('[role="columnheader"]')).map((header) => header.textContent?.trim());
    const rowButton = root.querySelector('.word-types-table__row') as HTMLButtonElement;
    const countButton = root.querySelector('[data-testid="word-count-chip"]') as HTMLButtonElement;

    expect(headers).toEqual(['م', 'الكلمة', 'النوع', 'الجذر', 'الأصل', 'الصيغة', 'المواضع', 'الآيات', 'السور']);
    expect(root.textContent).toContain('كَلِمَة');
    expect(root.textContent).toContain('—');
    expect(root.textContent).not.toContain('191001');
    expect(root.querySelector('.word-types-table__header-gutter')).not.toBeNull();
    expect(rowButton.getAttribute('aria-current')).toBe('true');
    expect(rowButton.getAttribute('aria-selected')).toBe('true');
    expect(rowButton.classList.contains('qd-is-selected')).toBe(true);

    rowButton.click();
    countButton.click();

    expect(selected).toEqual([word()]);
    expect(countEvents).toEqual([{ row: word(), column: 'occurrences', view: 'ayahs' }]);
  });

  it.each([
    ['roots', rootRow, 'الجذر', 'جدول الجذور', 190700, 'root:190700'],
    ['stems', stemRow, 'الأصل', 'جدول الأصول', 190701, 'stem:190701'],
    ['lemmas', lemmaRow, 'الصيغة', 'جدول الصيغ', 190702, 'lemma:190702'],
  ] as const)(
    'renders %s grouped rows as selectable four-column row buttons that emit the grouped row',
    (tableView, groupedRow, dimensionHeader, tableLabel, numericId, rowDomId) => {
      const fixture = TestBed.createComponent(WordTypesTableComponent);
      const selected: WordTypeTableRowDto[] = [];
      fixture.componentRef.setInput('rows', page([groupedRow]));
      fixture.componentRef.setInput('tableView', tableView as WordTypeTableView);
      fixture.componentRef.setInput('selectedRow', groupedRow);
      fixture.componentInstance.rowSelected.subscribe((row) => selected.push(row));
      fixture.detectChanges();

      const root = fixture.nativeElement as HTMLElement;
      const headers = Array.from(root.querySelectorAll('[role="columnheader"]')).map((header) => header.textContent?.trim());
      const groupedTableRow = root.querySelector('button.word-types-table__row') as HTMLButtonElement;

      expect(root.querySelector('[role="table"]')?.getAttribute('aria-label')).toBe(tableLabel);
      expect(headers).toEqual(['م', dimensionHeader, 'المواضع', 'الآيات', 'السور']);
      expect(groupedTableRow).not.toBeNull();
      expect(groupedTableRow.getAttribute('data-word-types-row')).toBe(rowDomId);
      expect(groupedTableRow.textContent).toContain(groupedRow.displayText);
      expect(groupedTableRow.textContent).toContain(String(groupedRow.occurrencesCount));
      expect(groupedTableRow.textContent).toContain(String(groupedRow.ayahsCount));
      expect(groupedTableRow.textContent).toContain(String(groupedRow.surahsCount));
      expect(root.textContent).not.toContain(String(numericId));
      expect(root.querySelector('qd-word-count-chip')).toBeNull();
      expect(groupedTableRow.classList.contains('qd-is-selected')).toBe(true);
      expect(groupedTableRow.getAttribute('aria-current')).toBe('true');
      expect(groupedTableRow.getAttribute('aria-selected')).toBe('true');

      groupedTableRow.click();
      expect(selected).toEqual([groupedRow]);
    },
  );

  it('does not mark a grouped row selected when the active selection is a different kind', () => {
    const fixture = TestBed.createComponent(WordTypesTableComponent);
    fixture.componentRef.setInput('rows', page([rootRow]));
    fixture.componentRef.setInput('tableView', 'roots');
    fixture.componentRef.setInput('selectedRow', word());
    fixture.detectChanges();

    const groupedTableRow = (fixture.nativeElement as HTMLElement).querySelector('button.word-types-table__row') as HTMLButtonElement;
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
    expect(root.textContent).not.toContain('كَلِمَة');
  });

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

    const rowButton = (fixture.nativeElement as HTMLElement).querySelector('button.word-types-table__row') as HTMLButtonElement;
    expect(rowButton.classList.contains('qd-explorer-table__row')).toBe(true);
    expect(rowButton.classList.contains('qd-interactive-surface')).toBe(false);
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
  ] as const)('exposes keyboard-operable %s row buttons that focusRow can target', (tableView, row, domId) => {
    const fixture = TestBed.createComponent(WordTypesTableComponent);
    fixture.componentRef.setInput('rows', page([row]));
    fixture.componentRef.setInput('tableView', tableView as WordTypeTableView);
    fixture.detectChanges();

    const rowButton = (fixture.nativeElement as HTMLElement).querySelector('button.word-types-table__row') as HTMLButtonElement;
    expect(rowButton.tagName).toBe('BUTTON');

    fixture.componentInstance.focusRow(row);

    expect(document.activeElement).toBe(rowButton);
    expect((document.activeElement as HTMLElement).getAttribute('data-word-types-row')).toBe(domId);
  });

  it('focuses a word row by its canonicalized nullable identity', () => {
    const fixture = TestBed.createComponent(WordTypesTableComponent);
    const nullableWord = word({ case: null, tense: null, voice: null });
    fixture.componentRef.setInput('rows', page([nullableWord]));
    fixture.detectChanges();

    fixture.componentInstance.focusRow(nullableWord);

    expect(document.activeElement).toBe(fixture.nativeElement.querySelector('.word-types-table__row'));
    expect((document.activeElement as HTMLElement).getAttribute('data-word-types-row')).toBe('191001:PN:all:all:all');
  });
});
