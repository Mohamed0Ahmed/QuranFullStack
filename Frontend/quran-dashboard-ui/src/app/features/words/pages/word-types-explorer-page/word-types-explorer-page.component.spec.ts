import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, TestRequest, provideHttpClientTesting } from '@angular/common/http/testing';
import { getTestBed, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { By } from '@angular/platform-browser';
import { BehaviorSubject } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { ApiResponse } from '../../../../core/data-access/api-response.model';
import { WordTypeGroupedMemberWordDto, WordTypeGroupedSummaryDto } from '../../models/word-types-detail.models';
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
  displayText: 'SYNTH_WORD_TEXT',
  typeCode: 'N',
  typeLabel: { ar: 'SYNTH_TYPE_LABEL' },
  broadLabel: { ar: 'SYNTH_BROAD_LABEL' },
  caseOrFeature: null,
  rootText: 'SYNTH_ROOT_TEXT',
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
  typeLabel: { ar: 'SYNTH_PROPER_TYPE_LABEL' },
  broadLabel: { ar: 'SYNTH_BROAD_LABEL' },
};

const verbRow: WordTableRowDto = {
  ...row,
  contextCode: 'present',
  typeCode: 'present',
  typeLabel: { ar: 'SYNTH_VERB_TYPE_LABEL' },
  broadLabel: { ar: 'SYNTH_VERB_BROAD_LABEL' },
};

const nullableInlRow: WordTableRowDto = {
  kind: 'word',
  tashkeelWordId: 191001,
  contextCode: 'INL',
  case: null,
  tense: null,
  voice: null,
  displayText: 'SYNTH_INL_WORD_TEXT',
  typeCode: 'INL',
  typeLabel: { ar: 'SYNTH_INL_TYPE_LABEL' },
  broadLabel: { ar: 'SYNTH_INL_BROAD_LABEL' },
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
  displayText: 'SYNTH_ROOT_TEXT',
  occurrencesCount: 3,
  ayahsCount: 2,
  surahsCount: 1,
};

const secondGroupedRootRow: RootTableRowDto = {
  ...groupedRootRow,
  rootId: 190701,
  displayText: 'SYNTH_SECOND_ROOT_TEXT',
};

const groupedStemRow: StemTableRowDto = {
  kind: 'stem',
  stemId: 190600,
  displayText: 'SYNTH_STEM_TEXT',
  occurrencesCount: 4,
  ayahsCount: 3,
  surahsCount: 2,
};

const groupedLemmaRow: LemmaTableRowDto = {
  kind: 'lemma',
  lemmaId: 190500,
  displayText: 'SYNTH_LEMMA_TEXT',
  occurrencesCount: 5,
  ayahsCount: 4,
  surahsCount: 2,
};

const groupedSummaryDto: WordTypeGroupedSummaryDto = {
  kind: 'root',
  dimensionId: 190700,
  displayText: 'SYNTH_ROOT_TEXT',
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
  displayText: 'SYNTH_MEMBER_WORD_TEXT',
  typeCode: 'N',
  typeLabel: { ar: 'SYNTH_MEMBER_TYPE_LABEL' },
  broadLabel: { ar: 'SYNTH_MEMBER_BROAD_LABEL' },
  caseOrFeature: null,
  rootText: 'SYNTH_ROOT_TEXT',
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
  verseKey: '999:999',
  surahNumber: 999,
  ayahNumber: 999,
  pageNumber: 999,
  matchedWordPositions: [2],
  matchedWordIds: [9900002],
  words: [
    { quranWordId: 9900001, textUthmani: 'SYNTH_WORD_1', isAyahMarker: false },
    { quranWordId: 9900002, textUthmani: 'SYNTH_WORD_2', isAyahMarker: false },
  ],
};

function ok<T>(data: T): ApiResponse<T> {
  return { isSuccess: true, data, message: null, errors: null };
}

type Endpoint =
  | 'tree'
  | 'table'
  | 'scopeCounts'
  | 'wordSummary'
  | 'wordAyahs'
  | 'wordSurahs'
  | 'groupedSummary'
  | 'groupedWords'
  | 'groupedAyahs'
  | 'groupedSurahs';

const PENDING = Symbol('PENDING');
const TRANSPORT_ERROR = Symbol('TRANSPORT_ERROR');

type PlannedResponse =
  | ApiResponse<unknown>
  | typeof PENDING
  | typeof TRANSPORT_ERROR
  | ((request: TestRequest) => ApiResponse<unknown> | typeof PENDING | typeof TRANSPORT_ERROR);

interface RecordedRequest {
  endpoint: Endpoint;
  request: TestRequest['request'];
}

interface FakeServer {
  responses: Partial<Record<Endpoint, PlannedResponse>>;
  queues: Partial<Record<Endpoint, PlannedResponse[]>>;
  requests: RecordedRequest[];
  pending: Partial<Record<Endpoint, TestRequest[]>>;
}

function endpointFor(request: TestRequest): Endpoint {
  const url = request.request.url;
  if (url.endsWith('/api/words/word-types/tree')) return 'tree';
  if (url.endsWith('/api/words/word-types/scope-counts')) return 'scopeCounts';
  if (url.endsWith('/api/words/word-types/table')) return 'table';
  if (/\/api\/words\/word-types\/words\/\d+\/ayahs$/.test(url)) return 'wordAyahs';
  if (/\/api\/words\/word-types\/words\/\d+\/surahs$/.test(url)) return 'wordSurahs';
  if (/\/api\/words\/word-types\/words\/\d+$/.test(url)) return 'wordSummary';
  if (/\/api\/words\/word-types\/table\/(roots|stems|lemmas)\/\d+\/words$/.test(url)) return 'groupedWords';
  if (/\/api\/words\/word-types\/table\/(roots|stems|lemmas)\/\d+\/ayahs$/.test(url)) return 'groupedAyahs';
  if (/\/api\/words\/word-types\/table\/(roots|stems|lemmas)\/\d+\/surahs$/.test(url)) return 'groupedSurahs';
  if (/\/api\/words\/word-types\/table\/(roots|stems|lemmas)\/\d+$/.test(url)) return 'groupedSummary';
  throw new Error(`Unexpected Word Types request: ${request.request.method} ${url}`);
}

function defaultResponse(endpoint: Endpoint): ApiResponse<unknown> {
  const responses: Record<Endpoint, ApiResponse<unknown>> = {
    tree: ok(tree),
    table: ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [row] }),
    scopeCounts: ok({ wordsCount: 1, rootsCount: 1, stemsCount: 0, lemmasCount: 0 }),
    wordSummary: ok(properRow),
    wordAyahs: ok<PagedResultDto<WordTypeAyahMatchDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [ayahMatch] }),
    wordSurahs: ok({ surahs: [{ surahNumber: 999, nameArabic: 'SYNTH_WORD_SURAH_NAME', occurrencesCount: 1 }], missingSurahs: [] }),
    groupedSummary: ok(groupedSummaryDto),
    groupedWords: ok(groupedMemberWordsPage),
    groupedAyahs: ok<PagedResultDto<WordTypeAyahMatchDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [ayahMatch] }),
    groupedSurahs: ok({ surahs: [{ surahNumber: 999, nameArabic: 'SYNTH_SURAH_NAME', occurrencesCount: 1 }], missingSurahs: [] }),
  };
  return responses[endpoint];
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
  let router: Router;
  let http: HttpTestingController;
  let server: FakeServer;

  beforeEach(async () => {
    getTestBed().resetTestingModule();
    server = { responses: {}, queues: {}, requests: [], pending: {} };

    await TestBed.configureTestingModule({
      imports: [WordTypesExplorerPageComponent],
      providers: [
        provideRouter([{ path: 'types', component: WordTypesExplorerPageComponent }]),
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: ActivatedRoute,
          useValue: { queryParamMap: queryParamMap$.asObservable() },
        },
      ],
      teardown: { destroyAfterEach: true },
    }).compileComponents();

    router = TestBed.inject(Router);
    http = TestBed.inject(HttpTestingController);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);
    queryParamMap$.next(convertToParamMap({}));
  });

  afterEach(() => {
    http.verify({ ignoreCancelled: true });
    getTestBed().resetTestingModule();
  });

  function respond(endpoint: Endpoint, response: PlannedResponse): void {
    server.responses[endpoint] = response;
  }

  function respondOnce(endpoint: Endpoint, response: PlannedResponse): void {
    (server.queues[endpoint] ??= []).push(response);
  }

  function requestsFor(endpoint: Endpoint): RecordedRequest[] {
    return server.requests.filter((entry) => entry.endpoint === endpoint);
  }

  function resolvePlan(endpoint: Endpoint, request: TestRequest): ApiResponse<unknown> | typeof PENDING | typeof TRANSPORT_ERROR {
    const queued = server.queues[endpoint]?.shift();
    const plan = queued ?? server.responses[endpoint] ?? defaultResponse(endpoint);
    return typeof plan === 'function' ? plan(request) : plan;
  }

  function flushPendingRequests(): void {
    let pending = http.match(() => true);
    while (pending.length > 0) {
      for (const request of pending) {
        const endpoint = endpointFor(request);
        server.requests.push({ endpoint, request: request.request });
        const response = resolvePlan(endpoint, request);
        if (response === PENDING) {
          (server.pending[endpoint] ??= []).push(request);
        } else if (response === TRANSPORT_ERROR) {
          request.error(new ProgressEvent('error'));
        } else {
          request.flush(response);
        }
      }
      pending = http.match(() => true);
    }
  }

  function takePending(endpoint: Endpoint): TestRequest {
    const request = server.pending[endpoint]?.shift();
    if (!request) throw new Error(`No pending ${endpoint} request`);
    return request;
  }

  async function createPage() {
    const fixture = TestBed.createComponent(WordTypesExplorerPageComponent);
    fixture.detectChanges();
    flushPendingRequests();
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
  }

  function lastQueryParams(routerRef: Router): Record<string, unknown> {
    const calls = (routerRef.navigate as ReturnType<typeof vi.fn>).mock.calls;
    return calls[calls.length - 1][1].queryParams as Record<string, unknown>;
  }

  it('defaults route state to type=noun, loads tree only, and shows the select prompt', async () => {
    const fixture = await createPage();
    const root = fixture.nativeElement as HTMLElement;

    expect(requestsFor('table')).toHaveLength(0);
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

  it('mounts the scope-counts strip between the filter strip and the view tabs, showing the four scoped counts (US8)', async () => {
    respond('scopeCounts', ok({ wordsCount: 40, rootsCount: 12, stemsCount: 8, lemmasCount: 5 }));
    queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'N' }));
    const fixture = await createPage();
    const root = fixture.nativeElement as HTMLElement;

    const strip = root.querySelector('qd-word-type-scope-counts');
    const tabs = root.querySelector('qd-word-type-table-view-tabs');
    expect(strip).not.toBeNull();
    expect(tabs).not.toBeNull();

    const values = Array.from(strip!.querySelectorAll('[data-testid="word-type-scope-count-value"]')).map((el) => el.textContent?.trim());
    expect(values).toEqual(['40', '12', '8', '5']);

    // Placement: filters → scope summary → tabs → table (the tabs follow the strip in document order).
    expect(strip!.compareDocumentPosition(tabs!) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
    expect(requestsFor('scopeCounts')).toHaveLength(1);
  });

  it('renders the view tabs as the first layout child immediately preceding the table column (U3)', async () => {
    const fixture = await createPage();
    const root = fixture.nativeElement as HTMLElement;

    const layout = root.querySelector('.word-types-page__layout')!;
    const tabs = layout.querySelector('qd-word-type-table-view-tabs');
    expect(tabs).not.toBeNull();
    // The slot is the layout child that carries the U3 placement (N3 row 2); the tabs live inside it.
    const slot = layout.firstElementChild!;
    expect(slot.classList.contains('word-types-page__tabs')).toBe(true);
    expect(slot.contains(tabs)).toBe(true);
    expect(slot.nextElementSibling).toBe(layout.querySelector('main.word-types-page__main'));
  });

  // N3 row 2: the tabs are gated on the tree, so their whole row appeared on first load and pushed the
  // table + panel grid down. N3 row 3: the facade nulls `rows` on a view switch, unmounting the bar.
  it('keeps the tabs and pagination slots rendered before the tree and rows land (N3 rows 2-3)', async () => {
    // Hold the tree request open so the page paints its pre-tree state.
    respond('tree', PENDING);
    const fixture = await createPage();
    const root = fixture.nativeElement as HTMLElement;

    const tabsSlot = () => root.querySelector('.qd-explorer-layout__tabs-slot');
    const paginationSlot = () => root.querySelector('[data-testid="word-types-pagination-slot"]');
    expect(tabsSlot()).not.toBeNull();
    expect(paginationSlot()).not.toBeNull();
    // Both slots are deliberately empty here: they reserve the rows, they do not render the controls.
    expect(tabsSlot()!.querySelector('qd-word-type-table-view-tabs')).toBeNull();
    expect(paginationSlot()!.querySelector('qd-pagination')).toBeNull();

    // Once the tree lands the tabs mount into the same slot that was already holding their row.
    takePending('tree').flush(ok(tree));
    flushPendingRequests();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(tabsSlot()).not.toBeNull();
    expect(tabsSlot()!.querySelector('qd-word-type-table-view-tabs')).not.toBeNull();
    expect(paginationSlot()).not.toBeNull();
  });

  it('does not refetch the scope counts on a tableView or page change (US8)', async () => {
    queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'N' }));
    const fixture = await createPage();

    queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'N', tableView: 'roots' }));
    await fixture.whenStable();
    fixture.detectChanges();
    flushPendingRequests();

    queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'N', tableView: 'roots', page: '2' }));
    await fixture.whenStable();
    fixture.detectChanges();
    flushPendingRequests();

    // Only the initial scope load fetched counts; tab + page changes did not.
    expect(requestsFor('scopeCounts')).toHaveLength(1);
  });

  it('clears prior leaf rows for the in-table subtype prompt when returning to a parent, keeping the shells mounted', async () => {
    respond('table', ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [groupedRootRow] }));
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

  it('serializes a presence flag to the URL and resets the page (US6)', async () => {
    respond('table', ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 1000, totalCount: 1, items: [groupedRootRow] }));
    queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'PN', tableView: 'roots' }));
    const fixture = await createPage();

    const chip = fixture.nativeElement.querySelector('[data-testid="word-types-presence-root-present"]') as HTMLButtonElement;
    expect(chip).not.toBeNull();
    chip.click();
    fixture.detectChanges();

    expect(router.navigate).toHaveBeenCalledWith([], expect.objectContaining({
      queryParams: expect.objectContaining({ hasRoot: 'true', page: '1' }),
      queryParamsHandling: 'merge',
    }));
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
    flushPendingRequests();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(requestsFor('table').at(-1)?.request.params.get('type')).toBe('noun');
    expect(requestsFor('table').at(-1)?.request.params.get('childCode')).toBe('N');
    expect(requestsFor('table').at(-1)?.request.params.get('pageSize')).toBe('1000');
    expect((fixture.nativeElement as HTMLElement).querySelector('qd-word-types-table')).not.toBeNull();
  });

  it('restores the search term into the toolbar input and threads it to the list read', async () => {
    const fixture = await createPage();

    queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'N', search: 'SYNTH_SEARCH' }));
    flushPendingRequests();
    await fixture.whenStable();
    fixture.detectChanges();

    const searchInput = (fixture.nativeElement as HTMLElement).querySelector('[data-testid="word-types-search-input"]') as HTMLInputElement;
    expect(searchInput).not.toBeNull();
    expect(searchInput.value).toBe('SYNTH_SEARCH');
    expect(requestsFor('table').at(-1)?.request.params.get('search')).toBe('SYNTH_SEARCH');
  });

  it('renders scoped tabs and passes the complete grouped payload to the table while keeping the details host mounted', async () => {
    respondOnce('table', ok<PagedResultDto<WordTypeTableRowDto>>({
      page: 1,
      pageSize: 25,
      totalCount: 1,
      items: [groupedRootRow],
    }));
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
    respondOnce('table', ok<PagedResultDto<WordTypeTableRowDto>>({
      page: 1,
      pageSize: 25,
      totalCount: 1,
      items: [groupedRootRow],
    }));
    queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'PN', tableView: 'roots' }));
    const fixture = await createPage();

    expect((fixture.nativeElement as HTMLElement).querySelector('qd-word-type-details-panel')).not.toBeNull();

    queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'PN', tableView: 'words' }));
    flushPendingRequests();
    await fixture.whenStable();
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).querySelector('qd-word-type-details-panel')).not.toBeNull();
    expect((fixture.nativeElement as HTMLElement).querySelector('.word-types-page__layout')?.classList.contains('word-types-page__layout--grouped')).toBe(false);
  });

  it('uses the active grouped-view empty label', async () => {
    respondOnce('table', ok<PagedResultDto<WordTypeTableRowDto>>({
      page: 1,
      pageSize: 25,
      totalCount: 0,
      items: [],
    }));
    queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'PN', tableView: 'roots' }));

    const fixture = await createPage();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('لا توجد جذور لهذا النطاق');
  });

  it.each([
    { tableView: 'words', panelLabel: 'تفاصيل كلمة النوع', emptySelection: 'اختر كلمة من الجدول لعرض تفاصيلها.' },
    { tableView: 'roots', panelLabel: 'تفاصيل الجذر', emptySelection: 'اختر جذرًا من الجدول لعرض تفاصيله.' },
    { tableView: 'stems', panelLabel: 'تفاصيل الأصل الصرفي', emptySelection: 'اختر أصلًا صرفيًا من الجدول لعرض تفاصيله.' },
    { tableView: 'lemmas', panelLabel: 'تفاصيل الصيغة المعجمية', emptySelection: 'اختر صيغة معجمية من الجدول لعرض تفاصيلها.' },
  ] as const)('uses $tableView presentation for the idle details host', async ({ tableView, panelLabel, emptySelection }) => {
    queryParamMap$.next(convertToParamMap({ type: 'noun', tableView }));

    const fixture = await createPage();
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('[data-testid="word-type-details-panel-label"]')?.textContent?.trim()).toBe(panelLabel);
    expect(root.querySelector('[data-testid="word-type-details-empty-selection"]')?.textContent?.trim()).toBe(emptySelection);
  });

  it('keeps the table-view strip visible after the tree loads for parent and leaf scopes', async () => {
    queryParamMap$.next(convertToParamMap({ type: 'noun' }));
    const fixture = await createPage();
    expect((fixture.nativeElement as HTMLElement).querySelector('qd-word-type-table-view-tabs')).not.toBeNull();

    queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'N' }));
    flushPendingRequests();
    await fixture.whenStable();
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).querySelector('qd-word-type-table-view-tabs')).not.toBeNull();
  });

  it('keeps the active grouped view highlighted across scope filter changes', async () => {
    respond('table', ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [groupedRootRow] }));
    queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'N', tableView: 'roots' }));
    const fixture = await createPage();

    const rootsTab = () => fixture.nativeElement.querySelector('[data-testid="word-type-table-view-tab--roots"]') as HTMLButtonElement;
    expect(rootsTab().getAttribute('aria-selected')).toBe('true');

    queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'N', tableView: 'roots', case: 'genitive' }));
    flushPendingRequests();
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

    respond('table', ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [groupedRootRow] }));
    queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'N', tableView: 'roots' }));
    flushPendingRequests();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(host()).toBe(initial);

    respond('table', ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 0, items: [] }));
    queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'N', tableView: 'roots', case: 'genitive' }));
    flushPendingRequests();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(host()).toBe(initial);

    respond('table', { isSuccess: false, data: null, message: 'خطأ', errors: null });
    queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'N', tableView: 'roots', case: 'accusative' }));
    flushPendingRequests();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(host()).toBe(initial);
  });

  it('keeps the qd-word-type-details-panel host as the same node across words, roots, stems, lemmas, and empty selection', async () => {
    respond('table', ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [groupedRootRow] }));
    queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'N' }));
    const fixture = await createPage();
    const host = () => fixture.nativeElement.querySelector('qd-word-type-details-panel');
    const initial = host();
    expect(initial).not.toBeNull();

    for (const tableView of ['roots', 'stems', 'lemmas', 'words']) {
      queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'N', tableView }));
      flushPendingRequests();
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

    respond('table', ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 0, items: [] }));
    queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'N' }));
    flushPendingRequests();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(tableHost().querySelector('[data-testid="word-types-table-empty"]')).not.toBeNull();

    respond('table', { isSuccess: false, data: null, message: 'خطأ', errors: null });
    queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'PN' }));
    flushPendingRequests();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(tableHost().querySelector('[data-testid="word-types-table-error"]')).not.toBeNull();

    respond('table', PENDING);
    queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'N', tableView: 'stems' }));
    flushPendingRequests();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(tableHost().querySelector('[data-testid="word-types-table-loading"]')).not.toBeNull();
    takePending('table').flush(ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 0, items: [] }));
  });

  it('keeps the table shell visible and cancels the stale read during a table-view switch', async () => {
    queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'N' }));
    const fixture = await createPage();

    respond('table', PENDING);
    queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'N', tableView: 'roots' }));
    flushPendingRequests();
    await fixture.whenStable();
    fixture.detectChanges();

    const tableHost = fixture.nativeElement.querySelector('qd-word-types-table') as HTMLElement;
    expect(tableHost).not.toBeNull();
    expect(tableHost.querySelector('[data-testid="word-types-table-loading"]')).not.toBeNull();

    const staleRequest = takePending('table');
    respond('table', ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [groupedRootRow] }));
    queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'N', tableView: 'roots', sort: 'alpha' }));
    expect(staleRequest.cancelled).toBe(true);
    flushPendingRequests();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('qd-word-types-table')).not.toBeNull();
  });

  it('delegates the in-table retry to the explorer facade and keeps the shell mounted', async () => {
    respond('table', { isSuccess: false, data: null, message: 'خطأ', errors: null });
    queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'N' }));
    const fixture = await createPage();

    const retryButton = fixture.nativeElement.querySelector('[data-testid="word-types-table-retry"]') as HTMLButtonElement;
    expect(retryButton).not.toBeNull();
    respond('table', ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [row] }));
    retryButton.click();
    flushPendingRequests();
    fixture.detectChanges();

    expect(requestsFor('table')).toHaveLength(2);
    expect(TestBed.inject(WordTypesExplorerFacade).listState().status).toBe('success');
    expect(fixture.nativeElement.querySelector('qd-word-types-table')).not.toBeNull();
  });

  it('loads rows directly for inl', async () => {
    queryParamMap$.next(convertToParamMap({ type: 'inl' }));
    respondOnce('table', ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [verbRow] }));
    const fixture = await createPage();

    expect(requestsFor('table').at(-1)?.request.params.get('type')).toBe('inl');
    expect(requestsFor('table').at(-1)?.request.params.has('childCode')).toBe(false);
    expect(requestsFor('table').at(-1)?.request.params.get('pageSize')).toBe('1000');
    expect(TestBed.inject(WordTypesExplorerFacade).listState().status).toBe('success');
    expect((fixture.nativeElement as HTMLElement).querySelector('qd-word-types-table')).not.toBeNull();
  });

  it('restores a backend-shaped nullable identity row against the default URL filters', async () => {
    respondOnce('table', ok<PagedResultDto<WordTypeTableRowDto>>({
      page: 1,
      pageSize: 25,
      totalCount: 1,
      items: [nullableInlRow],
    }));
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
    respond('table', ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [verbRow] }));
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
    expect(fixture.nativeElement.querySelector('[data-testid="word-type-details-panel-entity"]')?.textContent).toContain('SYNTH_WORD_TEXT');
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
    respondOnce('table', ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 0, items: [] }));
    queryParamMap$.next(convertToParamMap({ type: 'particle', childCode: 'P', page: '1' }));
    const fixture = await createPage();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('لا توجد نتائج لهذا النوع');
    expect((fixture.nativeElement as HTMLElement).textContent).not.toContain('بدون تشكيل');

    respond('table', { isSuccess: false, data: null, message: 'تعذّر تحميل أنواع الكلمات', errors: null });
    queryParamMap$.next(convertToParamMap({ type: 'particle', childCode: 'PRO', page: '1' }));
    flushPendingRequests();
    await fixture.whenStable();
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('تعذّر تحميل أنواع الكلمات');
  });

  it('does not call detail APIs until a row is selected', async () => {
    await createPage();

    expect(requestsFor('wordSummary')).toHaveLength(0);
    expect(requestsFor('wordAyahs')).toHaveLength(0);
    expect(requestsFor('wordSurahs')).toHaveLength(0);
    expect(requestsFor('groupedSummary')).toHaveLength(0);
  });

  it('restores exact row context and view from route state, then clears on back navigation', async () => {
    respondOnce('table', ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [properRow] }));
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

    const summaryRequest = requestsFor('wordSummary').at(-1)?.request;
    expect(summaryRequest?.url).toMatch(/\/api\/words\/word-types\/words\/191001$/);
    expect(summaryRequest?.params.get('contextCode')).toBe('PN');
    expect(summaryRequest?.params.has('case')).toBe(false);
    expect(summaryRequest?.params.has('tense')).toBe(false);
    expect(summaryRequest?.params.has('voice')).toBe(false);
    expect(detailFacade.panelState().selectedRow?.contextCode).toBe('PN');
    expect(detailFacade.panelState().view).toBe('surahs');
    expect(detailFacade.panelState().surahs?.surahs).toHaveLength(1);
    expect(explorerFacade.listState().query.word).toBe(191001);
    expect(explorerFacade.listState().query.contextCode).toBe('PN');

    queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'PN', page: '1' }));
    flushPendingRequests();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(detailFacade.panelState().selectedRow).toBeNull();
    expect(detailFacade.panelState().status).toBe('idle');
    expect(explorerFacade.listState().status).toBe('success');
  });

  it('reloads summary when active feature changes for the same restored word context', async () => {
    respond('table', ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [properRow] }));
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

    expect(requestsFor('wordSummary').at(-1)?.request.params.get('case')).toBe('genitive');

    queryParamMap$.next(convertToParamMap(withDetailScope({
      type: 'noun',
      case: 'nominative',
      childCode: 'PN',
      page: '1',
      word: '191001',
      contextCode: 'PN',
      view: 'surahs',
    })));
    flushPendingRequests();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(requestsFor('wordSummary')).toHaveLength(2);
    expect(requestsFor('wordSummary').at(-1)?.request.params.get('case')).toBe('nominative');
  });

  it('falls back stale analysis deep-links to ayahs and removes the analysis action from the DOM', async () => {
    respondOnce('table', ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [properRow] }));
    queryParamMap$.next(convertToParamMap(withDetailScope({
      type: 'noun',
      childCode: 'PN',
      page: '1',
      word: '191001',
      contextCode: 'PN',
      view: 'analysis',
      detailPage: '1',
      location: '999:999:999',
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
    const originatingStatistic = (fixture.nativeElement as HTMLElement).querySelector(
      '[data-word-types-row="191001:N:all:all:all"] [data-word-count-column="occurrences"] [data-testid="word-count-chip"]',
    ) as HTMLButtonElement;

    const closeButton = fixture.nativeElement.querySelector('[data-testid="word-type-details-panel-close"]') as HTMLButtonElement;

    expect(originatingStatistic).not.toBeNull();

    closeButton.click();

    expect(document.activeElement).toBe(originatingStatistic);
  });

  it('renders a controlled not-found panel for missing restored rows while leaving the table active', async () => {
    respondOnce('wordSummary', { isSuccess: false, data: null, message: 'غير موجود', errors: null });
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
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('SYNTH_WORD_TEXT');
  });

  const groupedSelectionCases = [
    { kind: 'root', tableView: 'roots', groupedRow: groupedRootRow, urlKey: 'root', domId: 'root:190700', id: 190700 },
    { kind: 'stem', tableView: 'stems', groupedRow: groupedStemRow, urlKey: 'stem', domId: 'stem:190600', id: 190600 },
    { kind: 'lemma', tableView: 'lemmas', groupedRow: groupedLemmaRow, urlKey: 'lemma', domId: 'lemma:190500', id: 190500 },
  ] as const;

  it.each(groupedSelectionCases)(
    'selecting a $kind occurrence statistic writes its identity, full detail scope, and view=words',
    async ({ tableView, groupedRow, urlKey, domId, id }) => {
      respond('table', ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [groupedRow] }));
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
      flushPendingRequests();
    },
  );

  it('opens word details only from statistics, writes the full detail scope, and keeps the active row through tab changes', async () => {
    respond('table', ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [properRow] }));
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
    flushPendingRequests();

    (fixture.nativeElement.querySelector('[data-word-type-tab="surahs"]') as HTMLButtonElement).click();
    flushPendingRequests();
    fixture.detectChanges();
    expect(rowContainer.classList.contains('qd-is-selected')).toBe(true);
  });

  it('transfers the active grouped row on another statistic and clears it when details close', async () => {
    respond('table', ok<PagedResultDto<WordTypeTableRowDto>>({
      page: 1,
      pageSize: 25,
      totalCount: 2,
      items: [groupedRootRow, secondGroupedRootRow],
    }));
    queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'PN', tableView: 'roots' }));
    const fixture = await createPage();
    const first = fixture.nativeElement.querySelector('[data-word-types-row="root:190700"]') as HTMLElement;
    const second = fixture.nativeElement.querySelector('[data-word-types-row="root:190701"]') as HTMLElement;

    (first.querySelector('[data-word-count-column="ayahs"] button') as HTMLButtonElement).click();
    flushPendingRequests();
    fixture.detectChanges();
    expect(first.classList.contains('qd-is-selected')).toBe(true);
    expect(second.classList.contains('qd-is-selected')).toBe(false);

    (second.querySelector('[data-word-count-column="surahs"] button') as HTMLButtonElement).click();
    flushPendingRequests();
    fixture.detectChanges();
    expect(first.classList.contains('qd-is-selected')).toBe(false);
    expect(second.classList.contains('qd-is-selected')).toBe(true);

    (fixture.nativeElement.querySelector('[data-testid="word-type-details-panel-close"]') as HTMLButtonElement).click();
    fixture.detectChanges();
    expect(first.classList.contains('qd-is-selected')).toBe(false);
    expect(second.classList.contains('qd-is-selected')).toBe(false);
  });

  it('restores list and detail scopes independently through history without cross-scope row highlighting', async () => {
    respond('table', ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [groupedRootRow] }));
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
    flushPendingRequests();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(rowContainer.classList.contains('qd-is-selected')).toBe(true);

    queryParamMap$.next(convertToParamMap(nounListWithVerbDetail));
    flushPendingRequests();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(rowContainer.classList.contains('qd-is-selected')).toBe(false);
  });

  it('keeps mismatched stem details populated across table views and restores the exact active row on return', async () => {
    respond('table', (request) => ok<PagedResultDto<WordTypeTableRowDto>>({
      page: 1,
      pageSize: 25,
      totalCount: 1,
      items: [request.request.params.get('tableView') === 'stems' ? groupedStemRow : groupedRootRow],
    }));
    respond('groupedSummary', ok({
      ...groupedSummaryDto,
      kind: 'stem',
      dimensionId: 190600,
      displayText: 'SYNTH_STEM_TEXT',
    }));
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
    const summaryCalls = requestsFor('groupedSummary').length;
    const detailCalls = requestsFor('groupedAyahs').length;

    expect(fixture.nativeElement.querySelector('[data-word-types-row="root:190700"].qd-is-selected')).toBeNull();
    expect(panelBefore).toMatchObject({
      selection: { kind: 'stem', stemId: 190600 },
      view: 'ayahs',
      detailPage: 2,
      groupedSummary: { displayText: 'SYNTH_STEM_TEXT' },
    });

    for (const tableView of ['words', 'lemmas', 'roots']) {
      queryParamMap$.next(convertToParamMap({ ...preserved, tableView }));
      flushPendingRequests();
      await fixture.whenStable();
      fixture.detectChanges();
      expect(detailFacade.panelState()).toEqual(panelBefore);
    }
    expect(requestsFor('groupedSummary')).toHaveLength(summaryCalls);
    expect(requestsFor('groupedAyahs')).toHaveLength(detailCalls);

    queryParamMap$.next(convertToParamMap({ ...preserved, tableView: 'stems' }));
    flushPendingRequests();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[data-word-types-row="stem:190600"].qd-is-selected')).not.toBeNull();
  });

  it('atomically replaces a preserved mismatched identity when a new statistic opens details', async () => {
    respond('table', ok<PagedResultDto<WordTypeTableRowDto>>({
      page: 1,
      pageSize: 25,
      totalCount: 1,
      items: [groupedRootRow],
    }));
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
    flushPendingRequests();
  });

  it.each([
    { tableView: 'words', tableRow: properRow, domId: '191001:PN:all:all:all' },
    ...groupedSelectionCases.map(({ tableView, groupedRow, domId }) => ({ tableView, tableRow: groupedRow, domId })),
  ] as const)('keeps the $tableView row container inert for pointer and keyboard interaction', async ({ tableView, tableRow, domId }) => {
    respond('table', ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [tableRow] }));
    queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'PN', tableView }));
    const fixture = await createPage();
    const rowContainer = fixture.nativeElement.querySelector(`[data-word-types-row="${domId}"]`) as HTMLElement;

    rowContainer.click();
    rowContainer.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));
    rowContainer.dispatchEvent(new KeyboardEvent('keydown', { key: ' ', bubbles: true }));
    flushPendingRequests();

    expect(router.navigate).not.toHaveBeenCalled();
    expect(requestsFor('wordSummary')).toHaveLength(0);
    expect(requestsFor('groupedSummary')).toHaveLength(0);
  });

  it('gives the restored grouped row aria-selected and a distinct selected state', async () => {
    respond('table', ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [groupedRootRow] }));
    queryParamMap$.next(convertToParamMap(withDetailScope({ type: 'noun', childCode: 'PN', tableView: 'roots', root: '190700' })));
    const fixture = await createPage();

    const rowContainer = fixture.nativeElement.querySelector('[data-word-types-row="root:190700"]') as HTMLElement;
    expect(rowContainer.getAttribute('aria-selected')).toBe('true');
    expect(rowContainer.getAttribute('aria-current')).toBe('true');
    expect(rowContainer.classList.contains('qd-is-selected')).toBe(true);
  });

  it('opens grouped details immediately from the table row without requesting its summary', async () => {
    respond('table', ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [groupedRootRow] }));
    queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'PN', tableView: 'roots' }));
    const fixture = await createPage();

    (fixture.nativeElement.querySelector('[data-word-types-row="root:190700"] [data-word-count-column="occurrences"] button') as HTMLButtonElement).click();

    expect(TestBed.inject(WordTypesDetailFacade).panelState()).toEqual(expect.objectContaining({
      selection: {
        kind: 'root',
        rootId: 190700,
        scope: { type: 'noun', childCode: 'PN', case: 'all', tense: 'all', voice: 'all' },
      },
      groupedSummary: groupedSummaryDto,
      view: 'words',
    }));
    flushPendingRequests();
    expect(requestsFor('groupedSummary')).toHaveLength(0);
    const wordsRequest = requestsFor('groupedWords').at(-1)?.request;
    expect(wordsRequest?.url).toMatch(/\/api\/words\/word-types\/table\/roots\/190700\/words$/);
    expect(wordsRequest?.params.get('type')).toBe('noun');
    expect(wordsRequest?.params.get('childCode')).toBe('PN');
    expect(wordsRequest?.params.get('page')).toBe('1');
    expect(wordsRequest?.params.get('pageSize')).toBe('100');
  });

  it('defaults a new grouped selection to the words tab and renders its member words', async () => {
    respond('table', ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [groupedRootRow] }));
    queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'PN', tableView: 'roots' }));
    const fixture = await createPage();

    (fixture.nativeElement.querySelector('[data-word-types-row="root:190700"] [data-word-count-column="occurrences"] button') as HTMLButtonElement).click();
    flushPendingRequests();
    await fixture.whenStable();
    fixture.detectChanges();

    const detailFacade = TestBed.inject(WordTypesDetailFacade);
    expect(detailFacade.panelState().kind).toBe('root');
    expect(detailFacade.panelState().view).toBe('words');
    expect(requestsFor('groupedWords')).toHaveLength(1);
    expect(fixture.nativeElement.querySelector('[data-word-type-tab="words"]')?.getAttribute('aria-selected')).toBe('true');
    expect(fixture.nativeElement.querySelector('[data-testid="word-type-grouped-word-row"]')).not.toBeNull();
  });

  it('omits the grouped summary card while retaining the details header, tabs, and content', async () => {
    respond('table', ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [groupedRootRow] }));
    queryParamMap$.next(convertToParamMap(withDetailScope({ type: 'noun', childCode: 'PN', tableView: 'roots', root: '190700' })));
    const fixture = await createPage();

    const summaryRequest = requestsFor('groupedSummary').at(-1)?.request;
    expect(summaryRequest?.url).toMatch(/\/api\/words\/word-types\/table\/roots\/190700$/);
    expect(summaryRequest?.params.get('type')).toBe('noun');
    expect(summaryRequest?.params.get('childCode')).toBe('PN');
    const detailScroll = fixture.nativeElement.querySelector('.word-types-details__scroll') as HTMLElement;
    expect(detailScroll.firstElementChild?.tagName).toBe('QD-WORD-TYPE-GROUPED-WORDS-LIST');
    expect(fixture.nativeElement.querySelector('[data-testid="word-type-details-panel-entity"]')?.textContent).toContain('SYNTH_ROOT_TEXT');
    expect(fixture.nativeElement.querySelector('[data-word-type-tab="words"]')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('[data-testid="word-type-grouped-word-row"]')).not.toBeNull();
  });

  it('keeps grouped words/ayahs at internal page one, omits page one from the URL, and writes only pages above one', async () => {
    respond('table', ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [groupedRootRow] }));
    queryParamMap$.next(convertToParamMap(withDetailScope({ type: 'noun', childCode: 'PN', tableView: 'roots', root: '190700' })));
    const fixture = await createPage();
    const detailFacade = TestBed.inject(WordTypesDetailFacade);

    expect(detailFacade.panelState().view).toBe('words');
    expect(detailFacade.panelState().detailPage).toBe(1);

    const page2 = fixture.nativeElement.querySelector('qd-word-type-grouped-words-list [data-testid="qd-pagination-page-2"]') as HTMLButtonElement;
    expect(page2).not.toBeNull();
    page2.click();
    flushPendingRequests();
    expect(router.navigate).toHaveBeenLastCalledWith([], expect.objectContaining({
      queryParams: { detailPage: '2' },
      queryParamsHandling: 'merge',
    }));

    const ayahsTab = fixture.nativeElement.querySelector('[data-word-type-tab="ayahs"]') as HTMLButtonElement;
    ayahsTab.click();
    flushPendingRequests();
    expect(router.navigate).toHaveBeenLastCalledWith([], expect.objectContaining({
      queryParams: { view: 'ayahs', detailPage: null },
      queryParamsHandling: 'merge',
    }));
    expect(detailFacade.panelState().detailPage).toBe(1);
  });

  it('always removes detailPage for the surahs view while staying at internal page one', async () => {
    respond('table', ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [groupedRootRow] }));
    queryParamMap$.next(convertToParamMap(withDetailScope({ type: 'noun', childCode: 'PN', tableView: 'roots', root: '190700', view: 'ayahs', detailPage: '3' })));
    const fixture = await createPage();
    const detailFacade = TestBed.inject(WordTypesDetailFacade);

    const surahsTab = fixture.nativeElement.querySelector('[data-word-type-tab="surahs"]') as HTMLButtonElement;
    surahsTab.click();
    flushPendingRequests();

    expect(router.navigate).toHaveBeenLastCalledWith([], expect.objectContaining({
      queryParams: { view: 'surahs', detailPage: null },
      queryParamsHandling: 'merge',
    }));
    expect(detailFacade.panelState().view).toBe('surahs');
    expect(detailFacade.panelState().detailPage).toBe(1);
  });

  it('renders grouped error with retry inside the mounted details region and retries the failed view', async () => {
    respond('table', ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [groupedRootRow] }));
    respond('groupedWords', { isSuccess: false, data: null, message: 'تعذّر تحميل الكلمات', errors: null });
    queryParamMap$.next(convertToParamMap(withDetailScope({ type: 'noun', childCode: 'PN', tableView: 'roots', root: '190700' })));
    const fixture = await createPage();

    expect(fixture.nativeElement.querySelector('qd-word-type-details-panel')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('[data-testid="word-type-details-error"]')?.textContent).toContain('تعذّر تحميل الكلمات');

    respond('groupedWords', ok(groupedMemberWordsPage));
    (fixture.nativeElement.querySelector('[data-testid="word-type-details-retry"]') as HTMLButtonElement).click();
    flushPendingRequests();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(requestsFor('groupedWords')).toHaveLength(2);
    expect(fixture.nativeElement.querySelector('qd-word-type-details-panel')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('[data-testid="word-type-grouped-word-row"]')).not.toBeNull();
  });

  it('renders a loading state in the details panel while the grouped summary is still in flight', async () => {
    respond('table', ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [groupedRootRow] }));
    respond('groupedSummary', PENDING);
    queryParamMap$.next(convertToParamMap(withDetailScope({ type: 'noun', childCode: 'PN', tableView: 'roots', root: '190700' })));

    const fixture = await createPage();
    const detailFacade = TestBed.inject(WordTypesDetailFacade);

    // The grouped summary has not arrived, so activeSummary() is null; the panel must still show a
    // visible loading state (not a blank surface) rather than gating everything behind the summary.
    expect(detailFacade.panelState().status).toBe('loading');
    expect(detailFacade.panelState().groupedSummary).toBeNull();
    const panel = fixture.nativeElement.querySelector('qd-word-type-details-panel') as HTMLElement;
    expect(panel.querySelector('[data-testid="word-type-grouped-words-loading"]')).not.toBeNull();

    takePending('groupedSummary').flush(ok(groupedSummaryDto));
    flushPendingRequests();
  });

  it('renders an error with retry in the details panel when the grouped summary transport fails', async () => {
    respond('table', ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [groupedRootRow] }));
    respond('groupedSummary', TRANSPORT_ERROR);
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
    respond('table', ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [groupedRootRow] }));
    respond('groupedSummary', { isSuccess: false, data: null, message: 'المجموعة غير موجودة', errors: null });
    queryParamMap$.next(convertToParamMap(withDetailScope({ type: 'noun', childCode: 'PN', tableView: 'roots', root: '190700' })));
    const fixture = await createPage();

    expect(TestBed.inject(WordTypesDetailFacade).panelState().status).toBe('notFound');
    expect(fixture.nativeElement.querySelector('qd-word-type-details-panel')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('[data-testid="word-type-details-not-found"]')).not.toBeNull();
  });

  it('describes an empty stem-detail ayah view independently of the roots table view', async () => {
    respond('table', ok<PagedResultDto<WordTypeTableRowDto>>({
      page: 1,
      pageSize: 25,
      totalCount: 1,
      items: [groupedRootRow],
    }));
    respond('groupedSummary', ok({
      ...groupedSummaryDto,
      kind: 'stem',
      dimensionId: 900600,
      displayText: 'SYNTH_STEM',
    }));
    respond('groupedAyahs', ok<PagedResultDto<WordTypeAyahMatchDto>>({
      page: 1,
      pageSize: 25,
      totalCount: 0,
      items: [],
    }));
    queryParamMap$.next(convertToParamMap(withDetailScope({
      type: 'noun',
      childCode: 'N',
      tableView: 'roots',
      stem: '900600',
      view: 'ayahs',
    })));

    const fixture = await createPage();
    const detailSurface = (fixture.nativeElement as HTMLElement).querySelector(
      '[data-testid="word-type-details-panel-surface"]',
    );

    expect(detailSurface?.textContent).toContain('لا توجد آيات مرتبطة بالأصل الصرفي المحدد');
    expect(detailSurface?.textContent).not.toContain('لا توجد جذور لهذا النطاق');
    const ayahsTab = (fixture.nativeElement as HTMLElement).querySelector('[data-word-type-tab="ayahs"]');
    expect(ayahsTab?.textContent?.trim()).toBe('آيات الأصل الصرفي');
    expect(ayahsTab?.textContent).not.toContain('آيات الجذر');
  });

  it('exposes the Words hub access route for Word Types', () => {
    expect(ADDITIONAL_ACTIVE_HUB_SECTIONS).toContainEqual(expect.objectContaining({
      labelAr: 'أنواع الكلمات',
      route: '/dashboard/words/types',
    }));
  });

  describe('sorting (Feature 030, N8)', () => {
    async function pageWithRows() {
      respond('table', ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [groupedRootRow] }));
      queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'PN', tableView: 'roots' }));
      return createPage();
    }

    it('has no desktop sort dropdown: the only select sits in the ≤1023px fallback wrapper', async () => {
      const fixture = await pageWithRows();
      const root = fixture.nativeElement as HTMLElement;

      // ≥1024px the table header row is visible and owns sorting, so the fallback is CSS-hidden
      // there; below that the header row is display:none and this select is the only way in.
      const select = root.querySelector('[data-testid="word-types-sort-select"]') as HTMLElement;
      expect(select).toBeTruthy();
      expect(select.closest('.qd-explorer-sort-fallback')).not.toBeNull();
    });

    it('navigates { sort: token, page: 1 } when a header cycle step is emitted', async () => {
      const fixture = await pageWithRows();
      const root = fixture.nativeElement as HTMLElement;

      (root.querySelector('[data-testid="word-types-table-sort-ayahs"]') as HTMLButtonElement).click();

      expect(lastQueryParams(router)).toEqual(
        expect.objectContaining({ sort: 'ayahs', page: '1' }),
      );
    });

    it('navigates { sort: null } when a non-default column releases', async () => {
      respond('table', ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [groupedRootRow] }));
      queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'PN', tableView: 'roots', sort: 'ayahs-asc' }));
      const fixture = await createPage();
      const root = fixture.nativeElement as HTMLElement;

      (root.querySelector('[data-testid="word-types-table-sort-ayahs"]') as HTMLButtonElement).click();

      // Release removes the param, landing on this explorer's default: المواضع desc.
      expect(lastQueryParams(router)).toEqual(expect.objectContaining({ sort: null, page: '1' }));
    });

    // The WORD-TYPES QUIRK: المواضع IS the default, so its cycle collapses to desc(default) ⇄ asc —
    // the two halves of that collapse, from each starting state.
    it('moves المواضع from its default desc straight to asc (no unsorted step first)', async () => {
      const fixture = await pageWithRows();
      const root = fixture.nativeElement as HTMLElement;

      (root.querySelector('[data-testid="word-types-table-sort-occurrences"]') as HTMLButtonElement).click();

      expect(lastQueryParams(router)).toEqual(
        expect.objectContaining({ sort: 'occurrences-asc', page: '1' }),
      );
    });

    it('returns المواضع from asc to the param-free default, closing the 2-state collapse', async () => {
      respond('table', ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [groupedRootRow] }));
      queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'PN', tableView: 'roots', sort: 'occurrences-asc' }));
      const fixture = await createPage();
      const root = fixture.nativeElement as HTMLElement;

      (root.querySelector('[data-testid="word-types-table-sort-occurrences"]') as HTMLButtonElement).click();

      expect(lastQueryParams(router)).toEqual(expect.objectContaining({ sort: null, page: '1' }));
    });

    it('drives the same URL contract from the fallback select', async () => {
      const fixture = await pageWithRows();
      const root = fixture.nativeElement as HTMLElement;
      const select = root.querySelector('[data-testid="word-types-sort-select"]') as HTMLSelectElement;

      select.value = 'surahs-asc';
      select.dispatchEvent(new Event('change'));

      expect(lastQueryParams(router)).toEqual(
        expect.objectContaining({ sort: 'surahs-asc', page: '1' }),
      );
    });

    it('releases the param when the fallback select picks المواضع, this explorer\'s default', async () => {
      respond('table', ok<PagedResultDto<WordTypeTableRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [groupedRootRow] }));
      queryParamMap$.next(convertToParamMap({ type: 'noun', childCode: 'PN', tableView: 'roots', sort: 'alpha' }));
      const fixture = await createPage();
      const root = fixture.nativeElement as HTMLElement;
      const select = root.querySelector('[data-testid="word-types-sort-select"]') as HTMLSelectElement;

      select.value = 'occurrences';
      select.dispatchEvent(new Event('change'));

      expect(lastQueryParams(router)).toEqual(expect.objectContaining({ sort: null, page: '1' }));
    });

    it('still offers mushaf-order, which is an ordinary ordering here rather than the default', async () => {
      const fixture = await pageWithRows();
      const root = fixture.nativeElement as HTMLElement;
      const select = root.querySelector('[data-testid="word-types-sort-select"]') as HTMLSelectElement;

      select.value = 'mushaf-order';
      select.dispatchEvent(new Event('change'));

      expect(lastQueryParams(router)).toEqual(
        expect.objectContaining({ sort: 'mushaf-order', page: '1' }),
      );
    });
  });
});
