import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { getTestBed, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { By } from '@angular/platform-browser';
import { BehaviorSubject, Subject, of, throwError } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { ApiResponse } from '../../../../core/data-access/api-response.model';
import { WordTypeGroupedMemberWordDto, WordTypeGroupedSummaryDto, WordTypesApi } from '../../data-access/word-types.api';
import { ADDITIONAL_ACTIVE_HUB_SECTIONS } from '../../models/unique-words.labels';
import { WORD_TYPE_SORT_OPTIONS, WORD_TYPES_SELECT_SUBTYPE_LABEL, WORD_TYPES_SORT_LABEL } from '../../models/word-types.labels';
import { LemmaTableRowDto, PagedResultDto, RootTableRowDto, StemTableRowDto, WordTableRowDto, WordTypeAyahMatchDto, WordTypeTableRowDto, WordTypeTreeDto } from '../../models/word-types.models';
import { WordTypesTableComponent } from '../../components/word-types-table/word-types-table.component';
import { WordTypesDetailFacade } from '../../state/word-types-detail.facade';
import { WordTypesExplorerFacade } from '../../state/word-types-explorer.facade';
import { WordTypesExplorerPageComponent } from './word-types-explorer-page.component';

const queryParamMap$ = new BehaviorSubject(convertToParamMap({}));

const tree: WordTypeTreeDto = {
  mainTypes: [
    {
      code: 'noun', label: { ar: 'اسم' }, count: 1,
      secondaryFilter: { kind: 'case', options: [], voiceOptions: [] },
      children: [{ code: 'N', childCode: 'N', label: { ar: 'اسم' }, count: 1 }],
    },
    {
      code: 'verb', label: { ar: 'فعل' }, count: 1,
      secondaryFilter: { kind: 'tense+voice', options: [], voiceOptions: [] },
      children: [{ code: 'past', childCode: 'past', label: { ar: 'ماض' }, count: 1 }],
    },
    {
      code: 'particle', label: { ar: 'حرف وأداة' }, count: 1,
      secondaryFilter: { kind: 'none', options: [], voiceOptions: [] },
      children: [{ code: 'PRO', childCode: 'PRO', label: { ar: 'حرف نهي' }, count: 1 }],
    },
    { code: 'inl', label: { ar: 'حروف مقطّعة' }, count: 1, secondaryFilter: { kind: 'none', options: [], voiceOptions: [] }, children: [] },
  ],
};

const row: WordTableRowDto = {
  kind: 'word',
  tashkeelWordId: 191001,
  contextCode: 'N',
  case: null,
  tense: null,
  voice: null,
  displayText: 'كَلِمَة',
  typeCode: 'N',
  typeLabel: { ar: 'اسم' },
  broadLabel: { ar: 'اسم' },
  caseOrFeature: null,
  rootText: 'ك ل م',
  lemmaText: null,
  stemText: null,
  occurrencesCount: 1,
  ayahsCount: 1,
  surahsCount: 1,
};

const properRow: WordTableRowDto = {
  ...row,
  contextCode: 'PN',
  typeCode: 'PN',
  typeLabel: { ar: 'اسم علم' },
  broadLabel: { ar: 'اسم' },
};

const verbRow: WordTableRowDto = {
  ...row,
  contextCode: 'present',
  typeCode: 'present',
  typeLabel: { ar: 'مضارع' },
  broadLabel: { ar: 'فعل' },
};

const nullableInlRow: WordTableRowDto = {
  kind: 'word',
  tashkeelWordId: 191001,
  contextCode: 'INL',
  case: null,
  tense: null,
  voice: null,
  displayText: 'الٓمٓ',
  typeCode: 'INL',
  typeLabel: { ar: 'حروف مقطّعة' },
  broadLabel: { ar: 'حروف مقطّعة' },
  caseOrFeature: null,
  rootText: null,
  lemmaText: null,
  stemText: null,
  occurrencesCount: 1,
  ayahsCount: 1,
  surahsCount: 1,
};

const groupedRootRow: RootTableRowDto = {
  kind: 'root',
  rootId: 190700,
  displayText: 'ك ل م',
  occurrencesCount: 3,
  ayahsCount: 2,
  surahsCount: 1,
};

const secondGroupedRootRow: RootTableRowDto = {
  ...groupedRootRow,
  rootId: 190701,
  displayText: 'ك ت ب',
};

const groupedStemRow: StemTableRowDto = {
  kind: 'stem',
  stemId: 190600,
  displayText: 'مَكْتُوب',
  occurrencesCount: 4,
  ayahsCount: 3,
  surahsCount: 2,
};

const groupedLemmaRow: LemmaTableRowDto = {
  kind: 'lemma',
  lemmaId: 190500,
  displayText: 'كِتَاب',
  occurrencesCount: 5,
  ayahsCount: 4,
  surahsCount: 2,
};

const groupedSummaryDto: WordTypeGroupedSummaryDto = {
  kind: 'root',
  dimensionId: 190700,
  displayText: 'ك ل م',
  occurrencesCount: 3,
  ayahsCount: 2,
  surahsCount: 1,
};

const groupedMemberWord: WordTypeGroupedMemberWordDto = {
  tashkeelWordId: 191001,
  contextCode: 'N',
  case: 'all',
  tense: 'all',
  voice: 'all',
  displayText: 'كَلِمَة',
  typeCode: 'N',
  typeLabel: { ar: 'اسم' },
  broadLabel: { ar: 'اسم' },
  caseOrFeature: null,
  rootText: 'ك ل م',
  lemmaText: null,
  stemText: null,
  occurrencesCount: 1,
  ayahsCount: 1,
  surahsCount: 1,
};

const groupedMemberWordsPage: PagedResultDto<WordTypeGroupedMemberWordDto> = {
  page: 1,
  pageSize: 25,
  totalCount: 60,
  items: [groupedMemberWord],
};

const ayahMatch: WordTypeAyahMatchDto = {
  verseKey: '1:1',
  surahNumber: 1,
  ayahNumber: 1,
  pageNumber: 1,
  matchedWordPositions: [2],
  matchedWordIds: [1903002],
  words: [
    { quranWordId: 1903001, textUthmani: 'SYNTH_WORD_1', isAyahMarker: false },
    { quranWordId: 1903002, textUthmani: 'SYNTH_WORD_2', isAyahMarker: false },
  ],
};

function ok<T>(data: T): ApiResponse<T> {
  return { isSuccess: true, data, message: null, errors: null };
}

function withDetailScope(
  query: Record<string, string>,
  scope: Partial<Record<'type' | 'childCode' | 'case' | 'tense' | 'voice', string>> = {},
): Record<string, string> {
  const type = scope.type ?? query['type'] ?? 'noun';
  const childCode = scope.childCode ?? query['childCode'] ?? (type === 'inl' ? null : type === 'verb' ? 'present' : 'N');
  const result: Record<string, string> = {
    ...query,
    detailType: type,
    detailCase: scope.case ?? query['case'] ?? 'all',
    detailTense: scope.tense ?? query['tense'] ?? 'all',
    detailVoice: scope.voice ?? query['voice'] ?? 'all',
  };
  if (childCode !== null) {
    result['detailChildCode'] = childCode;
  }
  return result;
}

describe('WordTypesExplorerPageComponent', () => {
  let api: {
    getTree: ReturnType<typeof vi.fn>;
    getTableRows: ReturnType<typeof vi.fn>;
    getSummary: ReturnType<typeof vi.fn>;
    getAyahMatches: ReturnType<typeof vi.fn>;
    getSurahs: ReturnType<typeof vi.fn>;
    getGroupedSummary: ReturnType<typeof vi.fn>;
    getGroupedMemberWords: ReturnType<typeof vi.fn>;
    getGroupedAyahMatches: ReturnType<typeof vi.fn>;
    getGroupedSurahs: ReturnType<typeof vi.fn>;
  };
  let router: Router;

  beforeEach(async () => {
    getTestBed().resetTestingModule();
    api = {
      getTree: vi.fn().mockReturnValue(of(ok(tree))),
      getTableRows: vi.fn().mockReturnValue(of(ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [row] }))),
      getSummary: vi.fn().mockReturnValue(of(ok(properRow))),
      getAyahMatches: vi.fn().mockReturnValue(of(ok<PagedResultDto<WordTypeAyahMatchDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [ayahMatch] }))),
      getSurahs: vi.fn().mockReturnValue(of(ok({ surahs: [{ surahNumber: 1, nameArabic: 'الفاتحة', occurrencesCount: 1 }], missingSurahs: [] }))),
      getGroupedSummary: vi.fn().mockReturnValue(of(ok(groupedSummaryDto))),
      getGroupedMemberWords: vi.fn().mockReturnValue(of(ok(groupedMemberWordsPage))),
      getGroupedAyahMatches: vi.fn().mockReturnValue(of(ok<PagedResultDto<WordTypeAyahMatchDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [ayahMatch] }))),
      getGroupedSurahs: vi.fn().mockReturnValue(of(ok({ surahs: [{ surahNumber: 1, nameArabic: 'الفاتحة', occurrencesCount: 1 }], missingSurahs: [] }))),
    };

    await TestBed.configureTestingModule({
      imports: [WordTypesExplorerPageComponent],
      providers: [
        provideRouter([{ path: 'types', component: WordTypesExplorerPageComponent }]),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: WordTypesApi, useValue: api },
        {
          provide: ActivatedRoute,
          useValue: { queryParamMap: queryParamMap$.asObservable() },
        },
      ],
      teardown: { destroyAfterEach: true },
    }).compileComponents();

    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);
    queryParamMap$.next(convertToParamMap({}));
  });

  afterEach(() => getTestBed().resetTestingModule());

  async function createPage() {
    const fixture = TestBed.createComponent(WordTypesExplorerPageComponent);
    fixture.componentInstance.ngOnInit();
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
  }

  it('defaults route state to type=noun, loads tree only, and shows the select prompt', async () => {
    const fixture = await createPage();
    const root = fixture.nativeElement as HTMLElement;

    expect(api.getTableRows).not.toHaveBeenCalled();
    expect(TestBed.inject(WordTypesExplorerFacade).listState().status).toBe('selectPrompt');
    expect(TestBed.inject(WordTypesDetailFacade).panelState().summary).toBeNull();
    expect(root.textContent).toContain(WORD_TYPES_SELECT_SUBTYPE_LABEL);
    expect(root.querySelector('[data-testid="word-types-select-subtype"]')).not.toBeNull();
    expect(root.querySelector('qd-word-types-table')).not.toBeNull();
    expect(root.querySelector('qd-word-type-table-view-tabs')).not.toBeNull();
  });

  it('renders sort label and options from centralized labels', async () => {
    const fixture = await createPage();
    const root = fixture.nativeElement as HTMLElement;
    const sortSelect = root.querySelector('.word-types-page__sort select') as HTMLSelectElement;
    const optionLabels = Array.from(sortSelect.querySelectorAll('option')).map((option) => option.textContent?.trim());

    expect(root.textContent).toContain(WORD_TYPES_SORT_LABEL);
    expect(optionLabels).toEqual(WORD_TYPE_SORT_OPTIONS.map((option) => option.label));
  });

  it('clears prior leaf rows for the in-table subtype prompt when returning to a parent, keeping the shells mounted', async () => {
    api.getTableRows.mockReturnValue(of(ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [groupedRootRow] })));
    queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'PN', tableView: 'roots' }));
    const fixture = await createPage();

    const tableHost = () => fixture.nativeElement.querySelector('qd-word-types-table');
    const detailsHost = () => fixture.nativeElement.querySelector('qd-word-type-details-panel');
    const initialTable = tableHost();
    const initialDetails = detailsHost();
    expect(fixture.nativeElement.querySelector('[data-word-types-row="root:190700"]')).not.toBeNull();

    // Return to a parent scope (no leaf). The previous leaf's grouped row must disappear and the
    // in-table subtype prompt must appear, while the strip/table/details hosts keep their identity.
    queryParamMap$.next(convertToParamMap({ type: 'verb', tableView: 'roots' }));
    await fixture.whenStable();
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('[data-word-types-row="root:190700"]')).toBeNull();
    expect(root.querySelector('[data-testid="word-types-select-subtype"]')).not.toBeNull();
    expect(root.querySelector('qd-word-type-table-view-tabs')).not.toBeNull();
    expect(tableHost()).toBe(initialTable);
    expect(detailsHost()).toBe(initialDetails);
  });

  it('loads rows when a subtype is selected', async () => {
    const fixture = await createPage();
    const childButton = fixture.nativeElement.querySelector('.word-type-filter__child-button') as HTMLButtonElement;
    childButton.click();
    fixture.detectChanges();

    expect(router.navigate).toHaveBeenCalledWith([], expect.objectContaining({
      queryParams: expect.objectContaining({ type: 'noun', childCode: 'N', page: '1' }),
      queryParamsHandling: 'merge',
    }));

    queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'N' }));
    await fixture.whenStable();
    fixture.detectChanges();

    expect(api.getTableRows).toHaveBeenCalledWith(expect.objectContaining({ type: 'noun', childCode: 'N', pageSize: 25 }));
    expect((fixture.nativeElement as HTMLElement).querySelector('qd-word-types-table')).not.toBeNull();
  });

  it('renders scoped tabs and passes the complete grouped payload to the table while keeping the details host mounted', async () => {
    api.getTableRows.mockReturnValueOnce(of(ok<PagedResultDto<WordTypeTableRowDto>>({
      page: 1,
      pageSize: 25,
      totalCount: 1,
      items: [groupedRootRow],
    })));
    queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'PN', tableView: 'roots' }));

    const fixture = await createPage();
    const root = fixture.nativeElement as HTMLElement;
    const table = fixture.debugElement.query(By.directive(WordTypesTableComponent)).componentInstance as unknown as {
      rows: () => PagedResultDto<WordTypeTableRowDto> | null;
      tableView: () => string;
    };

    expect(root.querySelector('qd-word-type-table-view-tabs')).not.toBeNull();
    expect(table.rows()?.items).toEqual([groupedRootRow]);
    expect(table.tableView()).toBe('roots');
    expect(root.querySelector('.word-types-page__layout')?.classList.contains('word-types-page__layout--grouped')).toBe(false);
    expect(root.querySelector('qd-word-type-details-panel')).not.toBeNull();
  });

  it('delegates a scoped table-view tab selection to the explorer facade', async () => {
    queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'PN' }));
    const fixture = await createPage();
    const rootsTab = fixture.nativeElement.querySelector(
      '[data-testid="word-type-table-view-tab--roots"]',
    ) as HTMLButtonElement;

    rootsTab.click();

    expect(router.navigate).toHaveBeenLastCalledWith([], expect.objectContaining({
      queryParams: expect.objectContaining({ tableView: 'roots', page: '1' }),
      queryParamsHandling: 'merge',
    }));
  });

  it('keeps the details host mounted across a grouped-to-words switch without the grouped modifier', async () => {
    api.getTableRows.mockReturnValueOnce(of(ok<PagedResultDto<WordTypeTableRowDto>>({
      page: 1,
      pageSize: 25,
      totalCount: 1,
      items: [groupedRootRow],
    })));
    queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'PN', tableView: 'roots' }));
    const fixture = await createPage();

    expect((fixture.nativeElement as HTMLElement).querySelector('qd-word-type-details-panel')).not.toBeNull();

    queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'PN', tableView: 'words' }));
    await fixture.whenStable();
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).querySelector('qd-word-type-details-panel')).not.toBeNull();
    expect((fixture.nativeElement as HTMLElement).querySelector('.word-types-page__layout')?.classList.contains('word-types-page__layout--grouped')).toBe(false);
  });

  it('uses the active grouped-view empty label', async () => {
    api.getTableRows.mockReturnValueOnce(of(ok<PagedResultDto<WordTypeTableRowDto>>({
      page: 1,
      pageSize: 25,
      totalCount: 0,
      items: [],
    })));
    queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'PN', tableView: 'roots' }));

    const fixture = await createPage();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('لا توجد جذور لهذا النطاق');
  });

  it('keeps the table-view strip visible after the tree loads for parent and leaf scopes', async () => {
    queryParamMap$.next(convertToParamMap({ type: 'noun' }));
    const fixture = await createPage();
    expect((fixture.nativeElement as HTMLElement).querySelector('qd-word-type-table-view-tabs')).not.toBeNull();

    queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'N' }));
    await fixture.whenStable();
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).querySelector('qd-word-type-table-view-tabs')).not.toBeNull();
  });

  it('keeps the active grouped view highlighted across scope filter changes', async () => {
    api.getTableRows.mockReturnValue(of(ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [groupedRootRow] })));
    queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'N', tableView: 'roots' }));
    const fixture = await createPage();

    const rootsTab = () => fixture.nativeElement.querySelector('[data-testid="word-type-table-view-tab--roots"]') as HTMLButtonElement;
    expect(rootsTab().getAttribute('aria-selected')).toBe('true');

    queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'N', tableView: 'roots', case: 'genitive' }));
    await fixture.whenStable();
    fixture.detectChanges();
    expect(rootsTab().getAttribute('aria-selected')).toBe('true');
  });

  it('keeps the qd-word-types-table host as the same node across view, filter, empty, and error transitions', async () => {
    queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'N' }));
    const fixture = await createPage();
    const host = () => fixture.nativeElement.querySelector('qd-word-types-table');
    const initial = host();
    expect(initial).not.toBeNull();

    api.getTableRows.mockReturnValue(of(ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [groupedRootRow] })));
    queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'N', tableView: 'roots' }));
    await fixture.whenStable();
    fixture.detectChanges();
    expect(host()).toBe(initial);

    api.getTableRows.mockReturnValue(of(ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 0, items: [] })));
    queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'N', tableView: 'roots', case: 'genitive' }));
    await fixture.whenStable();
    fixture.detectChanges();
    expect(host()).toBe(initial);

    api.getTableRows.mockReturnValue(of({ isSuccess: false, data: null, message: 'خطأ', errors: null }));
    queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'N', tableView: 'roots', case: 'accusative' }));
    await fixture.whenStable();
    fixture.detectChanges();
    expect(host()).toBe(initial);
  });

  it('keeps the qd-word-type-details-panel host as the same node across words, roots, stems, lemmas, and empty selection', async () => {
    api.getTableRows.mockReturnValue(of(ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [groupedRootRow] })));
    queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'N' }));
    const fixture = await createPage();
    const host = () => fixture.nativeElement.querySelector('qd-word-type-details-panel');
    const initial = host();
    expect(initial).not.toBeNull();

    for (const tableView of ['roots', 'stems', 'lemmas', 'words']) {
      queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'N', tableView }));
      await fixture.whenStable();
      fixture.detectChanges();
      expect(host()).toBe(initial);
    }
  });

  it('renders select prompt, empty, error, and loading states inside qd-word-types-table', async () => {
    queryParamMap$.next(convertToParamMap({ type: 'noun' }));
    const fixture = await createPage();
    const tableHost = () => fixture.nativeElement.querySelector('qd-word-types-table') as HTMLElement;
    expect(tableHost().querySelector('[data-testid="word-types-select-subtype"]')).not.toBeNull();

    api.getTableRows.mockReturnValue(of(ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 0, items: [] })));
    queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'N' }));
    await fixture.whenStable();
    fixture.detectChanges();
    expect(tableHost().querySelector('[data-testid="word-types-table-empty"]')).not.toBeNull();

    api.getTableRows.mockReturnValue(of({ isSuccess: false, data: null, message: 'خطأ', errors: null }));
    queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'PN' }));
    await fixture.whenStable();
    fixture.detectChanges();
    expect(tableHost().querySelector('[data-testid="word-types-table-error"]')).not.toBeNull();

    const pending = new Subject<ApiResponse<PagedResultDto<WordTypeTableRowDto>>>();
    api.getTableRows.mockReturnValue(pending.asObservable());
    queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'N', tableView: 'stems' }));
    await fixture.whenStable();
    fixture.detectChanges();
    expect(tableHost().querySelector('[data-testid="word-types-table-loading"]')).not.toBeNull();
    pending.complete();
  });

  it('never shows a frame without the table shell or skeleton during a table-view switch', async () => {
    queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'N' }));
    const fixture = await createPage();

    const pending = new Subject<ApiResponse<PagedResultDto<WordTypeTableRowDto>>>();
    api.getTableRows.mockReturnValue(pending.asObservable());
    queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'N', tableView: 'roots' }));
    await fixture.whenStable();
    fixture.detectChanges();

    const tableHost = fixture.nativeElement.querySelector('qd-word-types-table') as HTMLElement;
    expect(tableHost).not.toBeNull();
    expect(tableHost.querySelector('[data-testid="word-types-table-loading"]')).not.toBeNull();

    pending.next(ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [groupedRootRow] }));
    pending.complete();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('qd-word-types-table')).not.toBeNull();
  });

  it('delegates the in-table retry to the explorer facade and keeps the shell mounted', async () => {
    api.getTableRows.mockReturnValue(of({ isSuccess: false, data: null, message: 'خطأ', errors: null }));
    queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'N' }));
    const fixture = await createPage();

    const facade = TestBed.inject(WordTypesExplorerFacade);
    const retrySpy = vi.spyOn(facade, 'retryList');

    const retryButton = fixture.nativeElement.querySelector('[data-testid="word-types-table-retry"]') as HTMLButtonElement;
    expect(retryButton).not.toBeNull();
    retryButton.click();

    expect(retrySpy).toHaveBeenCalled();
    expect(fixture.nativeElement.querySelector('qd-word-types-table')).not.toBeNull();
  });

  it('loads rows directly for inl', async () => {
    queryParamMap$.next(convertToParamMap({ type: 'inl' }));
    api.getTableRows.mockReturnValueOnce(of(ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [verbRow] })));
    const fixture = await createPage();

    expect(api.getTableRows).toHaveBeenCalledWith(expect.objectContaining({ type: 'inl', childCode: null, pageSize: 25 }));
    expect(TestBed.inject(WordTypesExplorerFacade).listState().status).toBe('success');
    expect((fixture.nativeElement as HTMLElement).querySelector('qd-word-types-table')).not.toBeNull();
  });

  it('restores a backend-shaped nullable identity row against the default URL filters', async () => {
    api.getTableRows.mockReturnValueOnce(of(ok<PagedResultDto<WordTypeTableRowDto>>({
      page: 1,
      pageSize: 25,
      totalCount: 1,
      items: [nullableInlRow],
    })));
    queryParamMap$.next(convertToParamMap(withDetailScope({
      type: 'inl',
      page: '1',
      word: '191001',
      contextCode: 'INL',
    })));

    const fixture = await createPage();
    fixture.detectChanges();
    const renderedRow = (fixture.nativeElement as HTMLElement).querySelector('[data-word-types-row]');
    expect(renderedRow?.getAttribute('aria-current')).toBe('true');
    expect(renderedRow?.getAttribute('data-word-types-row')).toBe('191001:INL:all:all:all');
  });

  it('renders independent table and details scroll containers for restored row views', async () => {
    queryParamMap$.next(convertToParamMap(withDetailScope({
      type: 'noun',
      childCode: 'PN',
      page: '1',
      word: '191001',
      contextCode: 'PN',
      view: 'surahs',
    })));

    const fixture = await createPage();
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('.word-types-table__body')).not.toBeNull();
    expect(root.querySelector('.word-types-details__scroll')).not.toBeNull();
  });

  it('browses a main parent without route navigation', async () => {
    const fixture = await createPage();
    const buttons = fixture.nativeElement.querySelectorAll('qd-word-type-filter .word-type-filter__button') as NodeListOf<HTMLButtonElement>;

    buttons[1].click();
    fixture.detectChanges();

    expect(router.navigate).not.toHaveBeenCalled();
    expect((buttons[1] as HTMLButtonElement).getAttribute('aria-expanded')).toBe('true');
  });

  it('keeps the committed verb table and its details unchanged while browsing the noun parent', async () => {
    api.getTableRows.mockReturnValue(of(ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [verbRow] })));
    queryParamMap$.next(convertToParamMap(withDetailScope({
      type: 'verb',
      childCode: 'past',
      tableView: 'words',
      word: '191001',
      contextCode: 'present',
      view: 'ayahs',
    }, { type: 'verb', childCode: 'past' })));
    const fixture = await createPage();
    const before = TestBed.inject(WordTypesDetailFacade).panelState();
    const nounButton = fixture.nativeElement.querySelectorAll(
      'qd-word-type-filter .word-type-filter__button',
    )[0] as HTMLButtonElement;

    nounButton.click();
    fixture.detectChanges();

    expect(router.navigate).not.toHaveBeenCalled();
    expect(TestBed.inject(WordTypesExplorerFacade).listState().query).toEqual(expect.objectContaining({ type: 'verb', childCode: 'past' }));
    expect(TestBed.inject(WordTypesDetailFacade).panelState().selection).toEqual(before.selection);
    expect(fixture.nativeElement.querySelector('[data-word-types-row="191001:present:all:all:all"]')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('[data-testid="word-type-details-panel-entity"]')?.textContent).toContain('كَلِمَة');
  });

  it('commits a child under the browsed parent', async () => {
    const fixture = await createPage();
    const root = fixture.nativeElement as HTMLElement;
    const buttons = root.querySelectorAll('qd-word-type-filter .word-type-filter__button') as NodeListOf<HTMLButtonElement>;

    buttons[1].click();
    fixture.detectChanges();
    const child = root.querySelector('qd-word-type-filter .word-type-filter__child-button') as HTMLButtonElement | null;
    child?.click();

    expect(router.navigate).toHaveBeenCalledWith([], expect.objectContaining({
      queryParams: expect.objectContaining({ type: 'verb', childCode: 'past', page: '1' }),
      queryParamsHandling: 'merge',
    }));
  });

  it('routes case filter selection and resets the page while clearing the selected row', async () => {
    const fixture = await createPage();
    const caseSelect = fixture.nativeElement.querySelector(
      'qd-word-type-filter [data-testid="word-type-case-filter"] select',
    ) as HTMLSelectElement;

    caseSelect.value = 'genitive';
    caseSelect.dispatchEvent(new Event('change'));

    expect(router.navigate).toHaveBeenCalledWith([], expect.objectContaining({
      queryParams: expect.objectContaining({ case: 'genitive', page: '1', word: null, contextCode: null }),
      queryParamsHandling: 'merge',
    }));
  });

  it('renders the noun case controls but not the verb controls by default', async () => {
    const fixture = await createPage();
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('qd-word-type-filter [data-testid="word-type-case-filter"]')).not.toBeNull();
    expect(root.querySelector('qd-word-type-filter [data-testid="word-type-verb-filter"]')).toBeNull();
  });

  it('renders empty and error states without adding a simple-text toggle', async () => {
    api.getTableRows.mockReturnValueOnce(of(ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 0, items: [] })));
    queryParamMap$.next(convertToParamMap({ type: 'particle', childCode: 'P', page: '1' }));
    const emptyFixture = await createPage();
    expect((emptyFixture.nativeElement as HTMLElement).textContent).toContain('لا توجد نتائج لهذا النوع');
    expect((emptyFixture.nativeElement as HTMLElement).textContent).not.toContain('بدون تشكيل');

    getTestBed().resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [WordTypesExplorerPageComponent],
      providers: [
        provideRouter([{ path: 'types', component: WordTypesExplorerPageComponent }]),
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: WordTypesApi,
          useValue: {
            getTree: vi.fn().mockReturnValue(of(ok(tree))),
            getTableRows: vi.fn().mockReturnValue(of({ isSuccess: false, data: null, message: 'تعذّر تحميل أنواع الكلمات', errors: null })),
          },
        },
        { provide: ActivatedRoute, useValue: { queryParamMap: queryParamMap$.asObservable() } },
      ],
      teardown: { destroyAfterEach: true },
    }).compileComponents();
    vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    const errorFixture = TestBed.createComponent(WordTypesExplorerPageComponent);
    errorFixture.componentInstance.ngOnInit();
    await errorFixture.whenStable();
    errorFixture.detectChanges();

    expect((errorFixture.nativeElement as HTMLElement).textContent).toContain('تعذّر تحميل أنواع الكلمات');
  });

  it('does not call detail APIs until a row is selected', async () => {
    const detailApi = {
      getSummary: vi.fn(),
      getAyahMatches: vi.fn(),
      getSurahs: vi.fn(),
    };

    getTestBed().resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [WordTypesExplorerPageComponent],
      providers: [
        provideRouter([{ path: 'types', component: WordTypesExplorerPageComponent }]),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: WordTypesApi, useValue: { ...api, ...detailApi } },
        { provide: ActivatedRoute, useValue: { queryParamMap: queryParamMap$.asObservable() } },
      ],
      teardown: { destroyAfterEach: true },
    }).compileComponents();

    const fixture = TestBed.createComponent(WordTypesExplorerPageComponent);
    fixture.componentInstance.ngOnInit();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(detailApi.getSummary).not.toHaveBeenCalled();
    expect(detailApi.getAyahMatches).not.toHaveBeenCalled();
    expect(detailApi.getSurahs).not.toHaveBeenCalled();
  });

  it('restores exact row context and view from route state, then clears on back navigation', async () => {
    api.getTableRows.mockReturnValueOnce(of(ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [properRow] })));
    queryParamMap$.next(convertToParamMap(withDetailScope({
      type: 'noun',
      childCode: 'PN',
      page: '1',
      word: '191001',
      contextCode: 'PN',
      view: 'surahs',
      detailPage: '1',
      column: 'analysis',
    })));

    const fixture = await createPage();
    const detailFacade = TestBed.inject(WordTypesDetailFacade);
    const explorerFacade = TestBed.inject(WordTypesExplorerFacade);

    expect(api.getSummary).toHaveBeenCalledWith(expect.objectContaining({
      tashkeelWordId: 191001,
      contextCode: 'PN',
      case: 'all',
      tense: 'all',
      voice: 'all',
    }));
    expect(detailFacade.panelState().selectedRow?.contextCode).toBe('PN');
    expect(detailFacade.panelState().view).toBe('surahs');
    expect(detailFacade.panelState().surahs?.surahs).toHaveLength(1);
    expect(explorerFacade.listState().query.word).toBe(191001);
    expect(explorerFacade.listState().query.contextCode).toBe('PN');

    queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'PN', page: '1' }));
    await fixture.whenStable();
    fixture.detectChanges();

    expect(detailFacade.panelState().selectedRow).toBeNull();
    expect(detailFacade.panelState().status).toBe('idle');
    expect(explorerFacade.listState().status).toBe('success');
  });

  it('reloads summary when active feature changes for the same restored word context', async () => {
    api.getTableRows.mockReturnValue(of(ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [properRow] })));
    queryParamMap$.next(convertToParamMap(withDetailScope({
      type: 'noun',
      case: 'genitive',
      childCode: 'PN',
      page: '1',
      word: '191001',
      contextCode: 'PN',
      view: 'surahs',
    })));

    const fixture = await createPage();

    expect(api.getSummary).toHaveBeenLastCalledWith(expect.objectContaining({ case: 'genitive' }));

    queryParamMap$.next(convertToParamMap(withDetailScope({
      type: 'noun',
      case: 'nominative',
      childCode: 'PN',
      page: '1',
      word: '191001',
      contextCode: 'PN',
      view: 'surahs',
    })));
    await fixture.whenStable();
    fixture.detectChanges();

    expect(api.getSummary).toHaveBeenCalledTimes(2);
    expect(api.getSummary).toHaveBeenLastCalledWith(expect.objectContaining({ case: 'nominative' }));
  });

  it('falls back stale analysis deep-links to ayahs and removes the analysis action from the DOM', async () => {
    api.getTableRows.mockReturnValueOnce(of(ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [properRow] })));
    api.getAyahMatches.mockReturnValueOnce(of(ok<PagedResultDto<WordTypeAyahMatchDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [ayahMatch] })));
    queryParamMap$.next(convertToParamMap(withDetailScope({
      type: 'noun',
      childCode: 'PN',
      page: '1',
      word: '191001',
      contextCode: 'PN',
      view: 'analysis',
      detailPage: '1',
      location: '1:1:2',
      column: 'analysis',
    })));

    const fixture = await createPage();
    const root = fixture.nativeElement as HTMLElement;

    expect(TestBed.inject(WordTypesDetailFacade).panelState().view).toBe('ayahs');
    expect(root.querySelector('[data-testid="word-type-details-tab-analysis"]')).toBeNull();
    expect(root.querySelector('[data-testid="ayah-match-analysis"]')).toBeNull();
  });

  it('returns focus to the statistic that opened details after selection clears', async () => {
    queryParamMap$.next(convertToParamMap(withDetailScope({
      type: 'noun',
      childCode: 'N',
      page: '1',
      word: '191001',
      contextCode: 'N',
      view: 'ayahs',
      column: 'occurrences',
    })));

    const fixture = await createPage();
    const table = fixture.debugElement.query(By.directive(WordTypesTableComponent)).componentInstance as WordTypesTableComponent;
    const focusSpy = vi.spyOn(table, 'focusStatistic');

    const closeButton = fixture.nativeElement.querySelector('[data-testid="word-type-details-panel-close"]') as HTMLButtonElement;

    closeButton.click();

    expect(focusSpy).toHaveBeenCalledWith(expect.objectContaining({
      kind: 'word',
      tashkeelWordId: 191001,
      contextCode: 'N',
      case: null,
      tense: null,
      voice: null,
    }), 'ayahs', 'occurrences');
  });

  it('renders a controlled not-found panel for missing restored rows while leaving the table active', async () => {
    api.getSummary.mockReturnValueOnce(of({ isSuccess: false, data: null, message: 'غير موجود', errors: null }));
    queryParamMap$.next(convertToParamMap(withDetailScope({
      type: 'noun',
      childCode: 'PN',
      page: '1',
      word: '999999',
      contextCode: 'PN',
      view: 'ayahs',
      detailPage: '1',
    })));

    const fixture = await createPage();
    const detailFacade = TestBed.inject(WordTypesDetailFacade);
    const explorerFacade = TestBed.inject(WordTypesExplorerFacade);

    expect(detailFacade.panelState().status).toBe('notFound');
    expect(detailFacade.panelState().selectedRow?.tashkeelWordId).toBe(999999);
    expect(explorerFacade.listState().status).toBe('success');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('كَلِمَة');
  });

  const groupedSelectionCases = [
    { kind: 'root', tableView: 'roots', groupedRow: groupedRootRow, urlKey: 'root', domId: 'root:190700', id: 190700 },
    { kind: 'stem', tableView: 'stems', groupedRow: groupedStemRow, urlKey: 'stem', domId: 'stem:190600', id: 190600 },
    { kind: 'lemma', tableView: 'lemmas', groupedRow: groupedLemmaRow, urlKey: 'lemma', domId: 'lemma:190500', id: 190500 },
  ] as const;

  it.each(groupedSelectionCases)(
    'selecting a $kind occurrence statistic writes its identity, full detail scope, and view=words',
    async ({ tableView, groupedRow, urlKey, domId, id }) => {
      api.getTableRows.mockReturnValue(of(ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [groupedRow] })));
      queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'PN', tableView }));
      const fixture = await createPage();

      const occurrenceButton = fixture.nativeElement.querySelector(
        `[data-word-types-row="${domId}"] [data-word-count-column="occurrences"] button`,
      ) as HTMLButtonElement;
      expect(occurrenceButton).not.toBeNull();
      occurrenceButton.click();

      expect(router.navigate).toHaveBeenLastCalledWith([], expect.objectContaining({
        queryParams: {
          word: null,
          contextCode: null,
          root: urlKey === 'root' ? String(id) : null,
          stem: urlKey === 'stem' ? String(id) : null,
          lemma: urlKey === 'lemma' ? String(id) : null,
          detailType: 'noun',
          detailChildCode: 'PN',
          detailCase: 'all',
          detailTense: 'all',
          detailVoice: 'all',
          view: 'words',
          detailPage: null,
          location: null,
          column: null,
        },
        queryParamsHandling: 'merge',
      }));
    },
  );

  it('opens word details only from statistics, writes the full detail scope, and keeps the active row through tab changes', async () => {
    api.getTableRows.mockReturnValue(of(ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [properRow] })));
    queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'PN', tableView: 'words' }));
    const fixture = await createPage();
    const rowContainer = fixture.nativeElement.querySelector('[data-word-types-row="191001:PN:all:all:all"]') as HTMLElement;

    (rowContainer.querySelector('[data-word-count-column="occurrences"] button') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(router.navigate).toHaveBeenLastCalledWith([], expect.objectContaining({
      queryParams: {
        word: '191001',
        contextCode: 'PN',
        root: null,
        stem: null,
        lemma: null,
        detailType: 'noun',
        detailChildCode: 'PN',
        detailCase: 'all',
        detailTense: 'all',
        detailVoice: 'all',
        view: 'ayahs',
        detailPage: null,
        location: null,
        column: 'occurrences',
      },
      queryParamsHandling: 'merge',
    }));
    expect(rowContainer.classList.contains('qd-is-selected')).toBe(true);

    (fixture.nativeElement.querySelector('[data-word-type-tab="surahs"]') as HTMLButtonElement).click();
    fixture.detectChanges();
    expect(rowContainer.classList.contains('qd-is-selected')).toBe(true);
  });

  it('transfers the active grouped row on another statistic and clears it when details close', async () => {
    api.getTableRows.mockReturnValue(of(ok<PagedResultDto<WordTypeTableRowDto>>({
      page: 1,
      pageSize: 25,
      totalCount: 2,
      items: [groupedRootRow, secondGroupedRootRow],
    })));
    queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'PN', tableView: 'roots' }));
    const fixture = await createPage();
    const first = fixture.nativeElement.querySelector('[data-word-types-row="root:190700"]') as HTMLElement;
    const second = fixture.nativeElement.querySelector('[data-word-types-row="root:190701"]') as HTMLElement;

    (first.querySelector('[data-word-count-column="ayahs"] button') as HTMLButtonElement).click();
    fixture.detectChanges();
    expect(first.classList.contains('qd-is-selected')).toBe(true);
    expect(second.classList.contains('qd-is-selected')).toBe(false);

    (second.querySelector('[data-word-count-column="surahs"] button') as HTMLButtonElement).click();
    fixture.detectChanges();
    expect(first.classList.contains('qd-is-selected')).toBe(false);
    expect(second.classList.contains('qd-is-selected')).toBe(true);

    (fixture.nativeElement.querySelector('[data-testid="word-type-details-panel-close"]') as HTMLButtonElement).click();
    fixture.detectChanges();
    expect(first.classList.contains('qd-is-selected')).toBe(false);
    expect(second.classList.contains('qd-is-selected')).toBe(false);
  });

  it('restores list and detail scopes independently through history without cross-scope row highlighting', async () => {
    api.getTableRows.mockReturnValue(of(ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [groupedRootRow] })));
    const nounListWithVerbDetail = withDetailScope(
      { type: 'noun', childCode: 'PN', tableView: 'roots', root: '190700', view: 'ayahs' },
      { type: 'verb', childCode: 'present', tense: 'present', voice: 'active' },
    );
    queryParamMap$.next(convertToParamMap(nounListWithVerbDetail));
    const fixture = await createPage();
    const rowContainer = fixture.nativeElement.querySelector('[data-word-types-row="root:190700"]') as HTMLElement;

    expect(TestBed.inject(WordTypesExplorerFacade).listState().query).toEqual(expect.objectContaining({ type: 'noun', childCode: 'PN' }));
    expect(TestBed.inject(WordTypesDetailFacade).panelState().selection).toEqual(expect.objectContaining({
      kind: 'root',
      rootId: 190700,
      scope: expect.objectContaining({ type: 'verb', childCode: 'present', tense: 'present', voice: 'active' }),
    }));
    expect(rowContainer.classList.contains('qd-is-selected')).toBe(false);

    queryParamMap$.next(convertToParamMap(withDetailScope({ type: 'noun', childCode: 'PN', tableView: 'roots', root: '190700', view: 'ayahs' })));
    await fixture.whenStable();
    fixture.detectChanges();
    expect(rowContainer.classList.contains('qd-is-selected')).toBe(true);

    queryParamMap$.next(convertToParamMap(nounListWithVerbDetail));
    await fixture.whenStable();
    fixture.detectChanges();
    expect(rowContainer.classList.contains('qd-is-selected')).toBe(false);
  });

  it('keeps mismatched stem details populated across table views and restores the exact active row on return', async () => {
    api.getTableRows.mockImplementation((request: { tableView: string }) => of(ok<PagedResultDto<WordTypeTableRowDto>>({
      page: 1,
      pageSize: 25,
      totalCount: 1,
      items: [request.tableView === 'stems' ? groupedStemRow : groupedRootRow],
    })));
    api.getGroupedSummary.mockReturnValue(of(ok({
      ...groupedSummaryDto,
      kind: 'stem',
      dimensionId: 190600,
      displayText: 'مَكْتُوب',
    })));
    const preserved = withDetailScope({
      type: 'noun',
      childCode: 'PN',
      tableView: 'roots',
      stem: '190600',
      view: 'ayahs',
      detailPage: '2',
    });
    queryParamMap$.next(convertToParamMap(preserved));
    const fixture = await createPage();
    const detailFacade = TestBed.inject(WordTypesDetailFacade);
    const panelBefore = detailFacade.panelState();
    const summaryCalls = api.getGroupedSummary.mock.calls.length;
    const detailCalls = api.getGroupedAyahMatches.mock.calls.length;

    expect(fixture.nativeElement.querySelector('[data-word-types-row="root:190700"].qd-is-selected')).toBeNull();
    expect(panelBefore).toMatchObject({
      selection: { kind: 'stem', stemId: 190600 },
      view: 'ayahs',
      detailPage: 2,
      groupedSummary: { displayText: 'مَكْتُوب' },
    });

    for (const tableView of ['words', 'lemmas', 'roots']) {
      queryParamMap$.next(convertToParamMap({ ...preserved, tableView }));
      await fixture.whenStable();
      fixture.detectChanges();
      expect(detailFacade.panelState()).toEqual(panelBefore);
    }
    expect(api.getGroupedSummary).toHaveBeenCalledTimes(summaryCalls);
    expect(api.getGroupedAyahMatches).toHaveBeenCalledTimes(detailCalls);

    queryParamMap$.next(convertToParamMap({ ...preserved, tableView: 'stems' }));
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[data-word-types-row="stem:190600"].qd-is-selected')).not.toBeNull();
  });

  it('atomically replaces a preserved mismatched identity when a new statistic opens details', async () => {
    api.getTableRows.mockReturnValue(of(ok<PagedResultDto<WordTypeTableRowDto>>({
      page: 1,
      pageSize: 25,
      totalCount: 1,
      items: [groupedRootRow],
    })));
    queryParamMap$.next(convertToParamMap(withDetailScope({
      type: 'noun',
      childCode: 'PN',
      tableView: 'roots',
      stem: '190600',
      view: 'ayahs',
      detailPage: '2',
    })));
    const fixture = await createPage();

    (fixture.nativeElement.querySelector(
      '[data-word-types-row="root:190700"] [data-word-count-column="occurrences"] button',
    ) as HTMLButtonElement).click();

    expect(router.navigate).toHaveBeenLastCalledWith([], expect.objectContaining({
      queryParams: expect.objectContaining({
        word: null,
        contextCode: null,
        root: '190700',
        stem: null,
        lemma: null,
        detailType: 'noun',
        detailChildCode: 'PN',
        detailCase: 'all',
        detailTense: 'all',
        detailVoice: 'all',
        view: 'words',
        detailPage: null,
      }),
      queryParamsHandling: 'merge',
    }));
    expect(TestBed.inject(WordTypesDetailFacade).panelState().selection).toMatchObject({
      kind: 'root',
      rootId: 190700,
    });
  });

  it.each([
    { tableView: 'words', tableRow: properRow, domId: '191001:PN:all:all:all' },
    ...groupedSelectionCases.map(({ tableView, groupedRow, domId }) => ({ tableView, tableRow: groupedRow, domId })),
  ] as const)('keeps the $tableView row container inert for pointer and keyboard interaction', async ({ tableView, tableRow, domId }) => {
    api.getTableRows.mockReturnValue(of(ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [tableRow] })));
    queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'PN', tableView }));
    const fixture = await createPage();
    const rowContainer = fixture.nativeElement.querySelector(`[data-word-types-row="${domId}"]`) as HTMLElement;

    rowContainer.click();
    rowContainer.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));
    rowContainer.dispatchEvent(new KeyboardEvent('keydown', { key: ' ', bubbles: true }));

    expect(router.navigate).not.toHaveBeenCalled();
    expect(api.getSummary).not.toHaveBeenCalled();
    expect(api.getGroupedSummary).not.toHaveBeenCalled();
  });

  it('gives the restored grouped row aria-selected and a distinct selected state', async () => {
    api.getTableRows.mockReturnValue(of(ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [groupedRootRow] })));
    queryParamMap$.next(convertToParamMap(withDetailScope({ type: 'noun', childCode: 'PN', tableView: 'roots', root: '190700' })));
    const fixture = await createPage();

    const rowContainer = fixture.nativeElement.querySelector('[data-word-types-row="root:190700"]') as HTMLElement;
    expect(rowContainer.getAttribute('aria-selected')).toBe('true');
    expect(rowContainer.getAttribute('aria-current')).toBe('true');
    expect(rowContainer.classList.contains('qd-is-selected')).toBe(true);
  });

  it('passes the selection kind and full grammatical scope to the detail facade', async () => {
    api.getTableRows.mockReturnValue(of(ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [groupedRootRow] })));
    queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'PN', tableView: 'roots' }));
    const fixture = await createPage();
    const selectSpy = vi.spyOn(TestBed.inject(WordTypesDetailFacade), 'select');

    (fixture.nativeElement.querySelector('[data-word-types-row="root:190700"] [data-word-count-column="occurrences"] button') as HTMLButtonElement).click();

    expect(selectSpy).toHaveBeenCalledWith(
      { kind: 'root', rootId: 190700, scope: { type: 'noun', childCode: 'PN', case: 'all', tense: 'all', voice: 'all' } },
      'words',
    );
  });

  it('defaults a new grouped selection to the words tab and renders its member words', async () => {
    api.getTableRows.mockReturnValue(of(ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [groupedRootRow] })));
    queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'PN', tableView: 'roots' }));
    const fixture = await createPage();

    (fixture.nativeElement.querySelector('[data-word-types-row="root:190700"] [data-word-count-column="occurrences"] button') as HTMLButtonElement).click();
    await fixture.whenStable();
    fixture.detectChanges();

    const detailFacade = TestBed.inject(WordTypesDetailFacade);
    expect(detailFacade.panelState().kind).toBe('root');
    expect(detailFacade.panelState().view).toBe('words');
    expect(api.getGroupedMemberWords).toHaveBeenCalled();
    expect(fixture.nativeElement.querySelector('[data-word-type-tab="words"]')?.getAttribute('aria-selected')).toBe('true');
    expect(fixture.nativeElement.querySelector('[data-testid="word-type-grouped-word-row"]')).not.toBeNull();
  });

  it('omits the grouped summary card while retaining the details header, tabs, and content', async () => {
    api.getTableRows.mockReturnValue(of(ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [groupedRootRow] })));
    queryParamMap$.next(convertToParamMap(withDetailScope({ type: 'noun', childCode: 'PN', tableView: 'roots', root: '190700' })));
    const fixture = await createPage();

    expect(api.getGroupedSummary).toHaveBeenCalledWith(expect.objectContaining({ kind: 'root', dimensionId: 190700, type: 'noun', childCode: 'PN' }));
    const detailScroll = fixture.nativeElement.querySelector('.word-types-details__scroll') as HTMLElement;
    expect(detailScroll.firstElementChild?.tagName).toBe('QD-WORD-TYPE-GROUPED-WORDS-LIST');
    expect(fixture.nativeElement.querySelector('[data-testid="word-type-details-panel-entity"]')?.textContent).toContain('ك ل م');
    expect(fixture.nativeElement.querySelector('[data-word-type-tab="words"]')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('[data-testid="word-type-grouped-word-row"]')).not.toBeNull();
  });

  it('keeps grouped words/ayahs at internal page one, omits page one from the URL, and writes only pages above one', async () => {
    api.getTableRows.mockReturnValue(of(ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [groupedRootRow] })));
    queryParamMap$.next(convertToParamMap(withDetailScope({ type: 'noun', childCode: 'PN', tableView: 'roots', root: '190700' })));
    const fixture = await createPage();
    const detailFacade = TestBed.inject(WordTypesDetailFacade);

    expect(detailFacade.panelState().view).toBe('words');
    expect(detailFacade.panelState().detailPage).toBe(1);

    const page2 = fixture.nativeElement.querySelector('qd-word-type-grouped-words-list [data-testid="qd-pagination-page-2"]') as HTMLButtonElement;
    expect(page2).not.toBeNull();
    page2.click();
    expect(router.navigate).toHaveBeenLastCalledWith([], expect.objectContaining({
      queryParams: { detailPage: '2' },
      queryParamsHandling: 'merge',
    }));

    const ayahsTab = fixture.nativeElement.querySelector('[data-word-type-tab="ayahs"]') as HTMLButtonElement;
    ayahsTab.click();
    expect(router.navigate).toHaveBeenLastCalledWith([], expect.objectContaining({
      queryParams: { view: 'ayahs', detailPage: null },
      queryParamsHandling: 'merge',
    }));
    expect(detailFacade.panelState().detailPage).toBe(1);
  });

  it('always removes detailPage for the surahs view while staying at internal page one', async () => {
    api.getTableRows.mockReturnValue(of(ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [groupedRootRow] })));
    queryParamMap$.next(convertToParamMap(withDetailScope({ type: 'noun', childCode: 'PN', tableView: 'roots', root: '190700', view: 'ayahs', detailPage: '3' })));
    const fixture = await createPage();
    const detailFacade = TestBed.inject(WordTypesDetailFacade);

    const surahsTab = fixture.nativeElement.querySelector('[data-word-type-tab="surahs"]') as HTMLButtonElement;
    surahsTab.click();

    expect(router.navigate).toHaveBeenLastCalledWith([], expect.objectContaining({
      queryParams: { view: 'surahs', detailPage: null },
      queryParamsHandling: 'merge',
    }));
    expect(detailFacade.panelState().view).toBe('surahs');
    expect(detailFacade.panelState().detailPage).toBe(1);
  });

  it('renders grouped error with retry inside the mounted details region and retries the failed view', async () => {
    api.getTableRows.mockReturnValue(of(ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [groupedRootRow] })));
    api.getGroupedMemberWords.mockReturnValue(of({ isSuccess: false, data: null, message: 'تعذّر تحميل الكلمات', errors: null }));
    queryParamMap$.next(convertToParamMap(withDetailScope({ type: 'noun', childCode: 'PN', tableView: 'roots', root: '190700' })));
    const fixture = await createPage();

    expect(fixture.nativeElement.querySelector('qd-word-type-details-panel')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('[data-testid="word-type-details-error"]')?.textContent).toContain('تعذّر تحميل الكلمات');

    api.getGroupedMemberWords.mockReturnValue(of(ok(groupedMemberWordsPage)));
    (fixture.nativeElement.querySelector('[data-testid="word-type-details-retry"]') as HTMLButtonElement).click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(api.getGroupedMemberWords).toHaveBeenCalledTimes(2);
    expect(fixture.nativeElement.querySelector('qd-word-type-details-panel')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('[data-testid="word-type-grouped-word-row"]')).not.toBeNull();
  });

  it('renders a loading state in the details panel while the grouped summary is still in flight', async () => {
    api.getTableRows.mockReturnValue(of(ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [groupedRootRow] })));
    const pendingSummary = new Subject<ApiResponse<WordTypeGroupedSummaryDto>>();
    api.getGroupedSummary.mockReturnValue(pendingSummary.asObservable());
    queryParamMap$.next(convertToParamMap(withDetailScope({ type: 'noun', childCode: 'PN', tableView: 'roots', root: '190700' })));

    const fixture = await createPage();
    const detailFacade = TestBed.inject(WordTypesDetailFacade);

    // The grouped summary has not arrived, so activeSummary() is null; the panel must still show a
    // visible loading state (not a blank surface) rather than gating everything behind the summary.
    expect(detailFacade.panelState().status).toBe('loading');
    expect(detailFacade.panelState().groupedSummary).toBeNull();
    const panel = fixture.nativeElement.querySelector('qd-word-type-details-panel') as HTMLElement;
    expect(panel.querySelector('[data-testid="word-type-grouped-words-loading"]')).not.toBeNull();

    pendingSummary.next(ok(groupedSummaryDto));
    pendingSummary.complete();
  });

  it('renders an error with retry in the details panel when the grouped summary transport fails', async () => {
    api.getTableRows.mockReturnValue(of(ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [groupedRootRow] })));
    api.getGroupedSummary.mockReturnValue(throwError(() => new Error('network')));
    queryParamMap$.next(convertToParamMap(withDetailScope({ type: 'noun', childCode: 'PN', tableView: 'roots', root: '190700' })));

    const fixture = await createPage();
    const detailFacade = TestBed.inject(WordTypesDetailFacade);

    // A summary transport failure leaves no summary; the panel must surface a retryable error rather
    // than a blank surface with no way forward.
    expect(detailFacade.panelState().status).toBe('error');
    expect(detailFacade.panelState().groupedSummary).toBeNull();
    const panel = fixture.nativeElement.querySelector('qd-word-type-details-panel') as HTMLElement;
    expect(panel.querySelector('[data-testid="word-type-details-error"]')).not.toBeNull();
    expect(panel.querySelector('[data-testid="word-type-details-retry"]')).not.toBeNull();
  });

  it('renders a grouped not-found state inside the mounted details region', async () => {
    api.getTableRows.mockReturnValue(of(ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [groupedRootRow] })));
    api.getGroupedSummary.mockReturnValue(of({ isSuccess: false, data: null, message: 'المجموعة غير موجودة', errors: null }));
    queryParamMap$.next(convertToParamMap(withDetailScope({ type: 'noun', childCode: 'PN', tableView: 'roots', root: '190700' })));
    const fixture = await createPage();

    expect(TestBed.inject(WordTypesDetailFacade).panelState().status).toBe('notFound');
    expect(fixture.nativeElement.querySelector('qd-word-type-details-panel')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('[data-testid="word-type-details-not-found"]')).not.toBeNull();
  });

  it('exposes the Words hub access route for Word Types', () => {
    expect(ADDITIONAL_ACTIVE_HUB_SECTIONS).toContainEqual(expect.objectContaining({
      labelAr: 'أنواع الكلمات',
      route: '/dashboard/words/types',
    }));
  });
});
