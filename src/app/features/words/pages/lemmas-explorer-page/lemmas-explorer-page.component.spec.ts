import { describe, expect, it, beforeEach, vi } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { BehaviorSubject, of, Subject } from 'rxjs';

import { ApiResponse } from '../../../../core/data-access/api-response.model';
import { LEMMAS_COLUMN_HEADERS } from '../../models/lemmas.labels';
import {
  LEMMA_DETAIL_PAGE_SIZE,
  LEMMAS_QUERY_KEYS,
  LemmaAyahMatchDto,
  LemmaListItemViewModel,
  LemmaWordItemDto,
  LemmaMissingSurahsDto,
  LemmaSummaryDto,
  LemmaSurahsDto,
  LemmaWordView,
  PagedResultDto,
} from '../../models/lemmas.models';
import { LemmasApi } from '../../data-access/lemmas.api';
import { LemmasDetailFacade } from '../../state/lemmas-detail.facade';
import { LemmasExplorerFacade } from '../../state/lemmas-explorer.facade';
import { LemmasExplorerPageComponent } from './lemmas-explorer-page.component';

function listRow(id: number, overrides: Partial<LemmaListItemViewModel> = {}): LemmaListItemViewModel {
  return {
    id,
    lemmaText: `صيغة-${id}`,
    displayText: `صيغة-${id}`,
    lemmaBuckwalter: null,
    rootId: 700,
    rootText: 'ك ل م',
    rootBuckwalter: null,
    dominantType: {
      code: 'N',
      arabicLabel: 'اسم',
      englishLabel: 'Noun',
      occurrencesCount: 5,
      firstSurahNumber: 1,
      firstAyahNumber: 1,
      firstWordNumber: 1,
    },
    otherTypesCount: 0,
    occurrencesCount: 5,
    ayahsCount: 3,
    surahsCount: 2,
    simpleWordsCount: 2,
    tashkeelWordsCount: 2,
    stemsCount: 1,
    firstVerseKey: '1:1',
    ...overrides,
  };
}

function successListResponse() {
  return of<ApiResponse<{ page: number; pageSize: number; totalCount: number; items: LemmaListItemViewModel[] }>>({
    isSuccess: true,
    data: { page: 1, pageSize: 1000, totalCount: 1, items: [listRow(500)] },
    message: null,
    errors: null,
  });
}

function ayahMatch(): LemmaAyahMatchDto {
  return {
    ayahId: 7001,
    verseKey: '4:57',
    surahNumber: 4,
    surahNameArabic: 'النساء',
    ayahNumber: 57,
    pageNumber: 92,
    matchedQuranWordIds: [9001],
    words: [
      {
        quranWordId: 9001,
        wordNumber: 1,
        textUthmani: 'كلمة-تجريبية-١',
        isAyahMarker: false,
      },
    ],
  };
}

function successAyahsResponse() {
  return of<ApiResponse<PagedResultDto<LemmaAyahMatchDto>>>({
    isSuccess: true,
    data: {
      page: 1,
      pageSize: LEMMA_DETAIL_PAGE_SIZE,
      totalCount: 1,
      items: [ayahMatch()],
    },
    message: null,
    errors: null,
  });
}

function wordItem(uniqueWordId: number, kind: LemmaWordView): LemmaWordItemDto {
  return {
    uniqueWordId,
    kind,
    displayTextUthmani: `كلمة-${uniqueWordId}`,
    occurrencesCount: 2,
    firstVerseKey: '1:1',
  };
}

function successWordsResponse(kind: LemmaWordView) {
  return of<ApiResponse<PagedResultDto<LemmaWordItemDto>>>({
    isSuccess: true,
    data: {
      page: 1,
      pageSize: LEMMA_DETAIL_PAGE_SIZE,
      totalCount: 1,
      items: [wordItem(9001, kind)],
    },
    message: null,
    errors: null,
  });
}

describe('LemmasExplorerPageComponent US1', () => {
  let router: Router;
  let lemmasApi: {
    getLemmasList: ReturnType<typeof vi.fn>;
    getLemmaSummary: ReturnType<typeof vi.fn>;
    getLemmaWords: ReturnType<typeof vi.fn>;
    getLemmaAyahMatches: ReturnType<typeof vi.fn>;
    getLemmaMentionedSurahs: ReturnType<typeof vi.fn>;
    getLemmaMissingSurahs: ReturnType<typeof vi.fn>;
    getLemmaStems: ReturnType<typeof vi.fn>;
  };

  const queryParamMap$ = new BehaviorSubject(convertToParamMap({}));

  beforeEach(async () => {
    getTestBed().resetTestingModule();

    lemmasApi = {
      getLemmasList: vi.fn().mockImplementation(successListResponse),
      getLemmaSummary: vi.fn().mockReturnValue(
        of<ApiResponse<LemmaSummaryDto>>({
          isSuccess: true,
          data: { ...listRow(500), typeDistribution: [listRow(500).dominantType] },
          message: null,
          errors: null,
        }),
      ),
      getLemmaWords: vi.fn().mockReturnValue(
        of<ApiResponse<{ page: number; pageSize: number; totalCount: number; items: unknown[] }>>({
          isSuccess: true,
          data: { page: 1, pageSize: 100, totalCount: 0, items: [] },
          message: null,
          errors: null,
        }),
      ),
      getLemmaAyahMatches: vi.fn().mockReturnValue(
        of<ApiResponse<{ page: number; pageSize: number; totalCount: number; items: unknown[] }>>({
          isSuccess: true,
          data: { page: 1, pageSize: 100, totalCount: 0, items: [] },
          message: null,
          errors: null,
        }),
      ),
      getLemmaMentionedSurahs: vi.fn(),
      getLemmaMissingSurahs: vi.fn(),
      getLemmaStems: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [LemmasExplorerPageComponent],
      providers: [
        provideRouter([{ path: 'lemmas', component: LemmasExplorerPageComponent }]),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: LemmasApi, useValue: lemmasApi },
        {
          provide: ActivatedRoute,
          useValue: {
            paramMap: of(convertToParamMap({})),
            queryParamMap: queryParamMap$.asObservable(),
          },
        },
      ],
      teardown: { destroyAfterEach: true },
    }).compileComponents();

    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);
    queryParamMap$.next(convertToParamMap({}));
  });

  async function initLifecycle(): Promise<ReturnType<typeof TestBed.createComponent<LemmasExplorerPageComponent>>> {
    const fixture = TestBed.createComponent(LemmasExplorerPageComponent);
    fixture.componentInstance.ngOnInit();
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
  }

  it('renders the catalogue table with the nine locked column headers', async () => {
    const fixture = await initLifecycle();
    const root = fixture.nativeElement as HTMLElement;

    const headers = Array.from(root.querySelectorAll('[role="columnheader"]')).map((h) =>
      h.textContent?.trim() ?? '',
    );
    expect(headers).toContain(LEMMAS_COLUMN_HEADERS.lemma);
    expect(headers).toContain(LEMMAS_COLUMN_HEADERS.root);
    expect(headers).toContain(LEMMAS_COLUMN_HEADERS.type);
    expect(headers).toContain(LEMMAS_COLUMN_HEADERS.stems);
  });

  it('does not call detail APIs on catalogue render without a selected lemma', async () => {
    await initLifecycle();

    expect(lemmasApi.getLemmasList).toHaveBeenCalled();
    expect(lemmasApi.getLemmaSummary).not.toHaveBeenCalled();
    expect(lemmasApi.getLemmaWords).not.toHaveBeenCalled();
    expect(lemmasApi.getLemmaAyahMatches).not.toHaveBeenCalled();
    expect(lemmasApi.getLemmaMentionedSurahs).not.toHaveBeenCalled();
    expect(lemmasApi.getLemmaMissingSurahs).not.toHaveBeenCalled();
    expect(lemmasApi.getLemmaStems).not.toHaveBeenCalled();
  });

  it('loads only the ayah detail endpoint and renders the ayah list when view=ayahs', async () => {
    lemmasApi.getLemmaAyahMatches.mockReturnValue(successAyahsResponse());
    queryParamMap$.next(convertToParamMap({ lemma: '500', view: 'ayahs', detailPage: '1' }));

    const fixture = await initLifecycle();
    const root = fixture.nativeElement as HTMLElement;

    expect(lemmasApi.getLemmaSummary).toHaveBeenCalledWith(500);
    expect(lemmasApi.getLemmaAyahMatches).toHaveBeenCalledWith(500, 1, LEMMA_DETAIL_PAGE_SIZE);
    expect(lemmasApi.getLemmaWords).not.toHaveBeenCalled();
    expect(lemmasApi.getLemmaMentionedSurahs).not.toHaveBeenCalled();
    expect(lemmasApi.getLemmaMissingSurahs).not.toHaveBeenCalled();
    expect(lemmasApi.getLemmaStems).not.toHaveBeenCalled();

    expect(root.querySelector('qd-ayah-matches-list')).toBeTruthy();
    expect(root.querySelector('[data-testid="lemmas-ayahs-view"]')).toBeTruthy();
    expect(root.querySelectorAll('.ayah-matches-list__card')).toHaveLength(1);
  });

  it('loads only the word detail endpoint and renders the simple/tashkeel list when view=words', async () => {
    lemmasApi.getLemmaWords.mockReturnValue(successWordsResponse('tashkeel'));
    queryParamMap$.next(convertToParamMap({ lemma: '500', view: 'words', wordView: 'tashkeel', detailPage: '1' }));

    const fixture = await initLifecycle();
    const root = fixture.nativeElement as HTMLElement;

    expect(lemmasApi.getLemmaSummary).toHaveBeenCalledWith(500);
    expect(lemmasApi.getLemmaWords).toHaveBeenCalledWith(500, 'tashkeel', 1, LEMMA_DETAIL_PAGE_SIZE);
    expect(lemmasApi.getLemmaAyahMatches).not.toHaveBeenCalled();
    expect(lemmasApi.getLemmaMentionedSurahs).not.toHaveBeenCalled();
    expect(lemmasApi.getLemmaMissingSurahs).not.toHaveBeenCalled();
    expect(lemmasApi.getLemmaStems).not.toHaveBeenCalled();

    expect(root.querySelector('qd-lemma-words-list')).toBeTruthy();
    expect(root.querySelector('[data-testid="lemmas-words-view"]')).toBeTruthy();
    expect(root.querySelectorAll('[data-testid="lemma-word-link"]')).toHaveLength(1);
  });

  it('maps row selection to the default words/simple detail state', async () => {
    const fixture = TestBed.createComponent(LemmasExplorerPageComponent);
    const component = fixture.componentInstance;

    component['onRowSelected'](listRow(500));

    expect(router.navigate).toHaveBeenCalledWith([], {
      relativeTo: expect.anything(),
      queryParams: expect.objectContaining({
        [LEMMAS_QUERY_KEYS.lemma]: '500',
        [LEMMAS_QUERY_KEYS.view]: 'words',
        [LEMMAS_QUERY_KEYS.wordView]: 'simple',
      }),
      queryParamsHandling: 'merge',
    });
    expect(TestBed.inject(LemmasDetailFacade).view()).toBe('words');
  });

  it('maps count-click to the correct view and sub-view URL params', async () => {
    const fixture = TestBed.createComponent(LemmasExplorerPageComponent);
    const component = fixture.componentInstance;

    component['onCountOpened']({ lemma: listRow(500), view: 'words', wordView: 'tashkeel' });

    expect(router.navigate).toHaveBeenCalledWith([], {
      relativeTo: expect.anything(),
      queryParams: expect.objectContaining({
        [LEMMAS_QUERY_KEYS.lemma]: '500',
        [LEMMAS_QUERY_KEYS.view]: 'words',
        [LEMMAS_QUERY_KEYS.wordView]: 'tashkeel',
        [LEMMAS_QUERY_KEYS.detailPage]: null,
      }),
      queryParamsHandling: 'merge',
    });
  });

  it('maps word-view sub-tab changes to the words URL params and resets detail page', async () => {
    const fixture = TestBed.createComponent(LemmasExplorerPageComponent);
    const component = fixture.componentInstance;

    component['onWordViewChange']('tashkeel');

    expect(router.navigate).toHaveBeenCalledWith([], {
      relativeTo: expect.anything(),
      queryParams: expect.objectContaining({
        [LEMMAS_QUERY_KEYS.view]: 'words',
        [LEMMAS_QUERY_KEYS.wordView]: 'tashkeel',
        [LEMMAS_QUERY_KEYS.detailPage]: null,
      }),
      queryParamsHandling: 'merge',
    });
  });

  it('maps zero-count activation to the same detail mapping as non-zero counts', async () => {
    const fixture = TestBed.createComponent(LemmasExplorerPageComponent);
    const component = fixture.componentInstance;
    const zeroRow = listRow(500, { ayahsCount: 0 });

    component['onCountOpened']({ lemma: zeroRow, view: 'ayahs' });

    expect(router.navigate).toHaveBeenCalledWith([], {
      relativeTo: expect.anything(),
      queryParams: expect.objectContaining({
        [LEMMAS_QUERY_KEYS.lemma]: '500',
        [LEMMAS_QUERY_KEYS.view]: 'ayahs',
      }),
      queryParamsHandling: 'merge',
    });
  });

  it('search and sort changes reset only the list page while preserving selection', async () => {
    queryParamMap$.next(
      convertToParamMap({
        lemma: '500',
        view: 'words',
        wordView: 'simple',
        page: '2',
        search: 'صيغة',
        sort: 'alpha',
      }),
    );
    const fixture = await initLifecycle();
    vi.mocked(router.navigate).mockClear();

    fixture.componentInstance['onSortChange']('occurrences');

    expect(router.navigate).toHaveBeenCalledWith([], {
      relativeTo: expect.anything(),
      queryParams: expect.objectContaining({
        [LEMMAS_QUERY_KEYS.sort]: 'occurrences',
        [LEMMAS_QUERY_KEYS.page]: null,
      }),
      queryParamsHandling: 'merge',
    });
    const lastNavigateArgs = vi.mocked(router.navigate).mock.calls.at(-1)?.[1] as {
      queryParams: Record<string, string | null>;
    };
    expect(lastNavigateArgs.queryParams[LEMMAS_QUERY_KEYS.lemma] ?? undefined).toBeUndefined();
  });

  it('shows list error and empty states from the facade', async () => {
    lemmasApi.getLemmasList.mockReturnValue(
      of<ApiResponse<{ page: number; pageSize: number; totalCount: number; items: LemmaListItemViewModel[] }>>({
        isSuccess: false,
        data: null,
        message: 'خطأ',
        errors: null,
      }),
    );

    const fixture = await initLifecycle();
    expect(fixture.nativeElement.querySelector('[data-testid="lemmas-list-error"]')).toBeTruthy();

    lemmasApi.getLemmasList.mockReturnValue(
      of<ApiResponse<{ page: number; pageSize: number; totalCount: number; items: LemmaListItemViewModel[] }>>({
        isSuccess: true,
        data: { page: 1, pageSize: 1000, totalCount: 0, items: [] },
        message: null,
        errors: null,
      }),
    );
    queryParamMap$.next(convertToParamMap({}));
    vi.mocked(router.navigate).mockClear();
    const emptyFixture = await initLifecycle();
    expect(emptyFixture.nativeElement.querySelector('[data-testid="lemmas-list-no-results"]')).toBeTruthy();
  });
});

describe('LemmasExplorerPageComponent US5', () => {
  let router: Router;
  let lemmasApi: {
    getLemmasList: ReturnType<typeof vi.fn>;
    getLemmaSummary: ReturnType<typeof vi.fn>;
    getLemmaWords: ReturnType<typeof vi.fn>;
    getLemmaAyahMatches: ReturnType<typeof vi.fn>;
    getLemmaMentionedSurahs: ReturnType<typeof vi.fn>;
    getLemmaMissingSurahs: ReturnType<typeof vi.fn>;
    getLemmaStems: ReturnType<typeof vi.fn>;
  };

  const queryParamMap$ = new BehaviorSubject(convertToParamMap({}));

  beforeEach(async () => {
    getTestBed().resetTestingModule();

    lemmasApi = {
      getLemmasList: vi.fn().mockImplementation(successListResponse),
      getLemmaSummary: vi.fn().mockReturnValue(
        of<ApiResponse<LemmaSummaryDto>>({
          isSuccess: true,
          data: { ...listRow(500), typeDistribution: [listRow(500).dominantType] },
          message: null,
          errors: null,
        }),
      ),
      getLemmaWords: vi.fn(),
      getLemmaAyahMatches: vi.fn(),
      getLemmaMentionedSurahs: vi.fn().mockReturnValue(
        of<ApiResponse<LemmaSurahsDto>>({
          isSuccess: true,
          data: {
            id: 500,
            lemmaText: 'صيغة-500',
            surahsCount: 1,
            surahs: [{ surahNumber: 1, nameArabic: 'سورة-اختبار', occurrencesInSurah: 2 }],
          },
          message: null,
          errors: null,
        }),
      ),
      getLemmaMissingSurahs: vi.fn().mockReturnValue(
        of<ApiResponse<LemmaMissingSurahsDto>>({
          isSuccess: true,
          data: { id: 500, lemmaText: 'صيغة-500', missingSurahsCount: 0, surahs: [] },
          message: null,
          errors: null,
        }),
      ),
      getLemmaStems: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [LemmasExplorerPageComponent],
      providers: [
        provideRouter([{ path: 'lemmas', component: LemmasExplorerPageComponent }]),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: LemmasApi, useValue: lemmasApi },
        {
          provide: ActivatedRoute,
          useValue: {
            paramMap: of(convertToParamMap({})),
            queryParamMap: queryParamMap$.asObservable(),
          },
        },
      ],
      teardown: { destroyAfterEach: true },
    }).compileComponents();

    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);
    queryParamMap$.next(convertToParamMap({}));
  });

  async function initLifecycle(): Promise<ReturnType<typeof TestBed.createComponent<LemmasExplorerPageComponent>>> {
    const fixture = TestBed.createComponent(LemmasExplorerPageComponent);
    fixture.componentInstance.ngOnInit();
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
  }

  it('loads mentioned surahs whole (no paging) and maps the row count when surahView=mentioned', async () => {
    queryParamMap$.next(convertToParamMap({ lemma: '500', view: 'surahs', surahView: 'mentioned' }));
    const fixture = await initLifecycle();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(lemmasApi.getLemmaMentionedSurahs).toHaveBeenCalledWith(500);
    expect(lemmasApi.getLemmaMissingSurahs).not.toHaveBeenCalled();

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('[data-testid="lemmas-mentioned-surahs-view"]')).toBeTruthy();
    expect(root.querySelector('qd-surah-occurrences-list')).toBeTruthy();
    expect(root.querySelectorAll('.surah-occurrences-list__row:not(.surah-occurrences-list__row--loading)')).toHaveLength(1);
  });

  it('routes surah sub-view changes through the URL and loads missing whole when surahView=missing', async () => {
    queryParamMap$.next(convertToParamMap({ lemma: '500', view: 'surahs', surahView: 'missing' }));
    await initLifecycle();

    expect(lemmasApi.getLemmaMissingSurahs).toHaveBeenCalledWith(500);
    expect(lemmasApi.getLemmaMentionedSurahs).not.toHaveBeenCalled();

    const fixture = TestBed.createComponent(LemmasExplorerPageComponent);
    const component = fixture.componentInstance;
    component.ngOnInit();
    component['onSurahViewChange']('mentioned');

    expect(router.navigate).toHaveBeenCalledWith([], {
      relativeTo: expect.anything(),
      queryParams: expect.objectContaining({
        [LEMMAS_QUERY_KEYS.view]: 'surahs',
        [LEMMAS_QUERY_KEYS.surahView]: 'mentioned',
      }),
      queryParamsHandling: 'merge',
    });
  });

  it('renders empty missing-surahs state cleanly when the missing list is empty', async () => {
    queryParamMap$.next(convertToParamMap({ lemma: '500', view: 'surahs', surahView: 'missing' }));
    const fixture = await initLifecycle();
    await fixture.whenStable();
    fixture.detectChanges();

    const detailFacade = TestBed.inject(LemmasDetailFacade);
    expect(detailFacade.status()).toBe('empty');
    expect(fixture.nativeElement.querySelector('[data-testid="lemmas-panel-empty"]')).toBeTruthy();
  });

  it('maps a surahs count-click to the mentioned surah view URL params', async () => {
    const fixture = TestBed.createComponent(LemmasExplorerPageComponent);
    const component = fixture.componentInstance;

    component['onCountOpened']({ lemma: listRow(500), view: 'surahs', surahView: 'mentioned' });

    expect(router.navigate).toHaveBeenCalledWith([], {
      relativeTo: expect.anything(),
      queryParams: expect.objectContaining({
        [LEMMAS_QUERY_KEYS.lemma]: '500',
        [LEMMAS_QUERY_KEYS.view]: 'surahs',
        [LEMMAS_QUERY_KEYS.surahView]: 'mentioned',
        [LEMMAS_QUERY_KEYS.detailPage]: null,
      }),
      queryParamsHandling: 'merge',
    });
  });

  it('keeps the four detail tabs and surah sub-tabs visible while mentioned surahs load', async () => {
    const pendingSurahs$ = new Subject<ApiResponse<LemmaSurahsDto>>();
    lemmasApi.getLemmaMentionedSurahs.mockReturnValue(pendingSurahs$.asObservable());

    queryParamMap$.next(convertToParamMap({ lemma: '500', view: 'surahs', surahView: 'mentioned' }));
    const fixture = TestBed.createComponent(LemmasExplorerPageComponent);
    fixture.componentInstance.ngOnInit();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(TestBed.inject(LemmasDetailFacade).status()).toBe('loading');

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelectorAll('[data-lemma-tab]')).toHaveLength(4);
    expect(root.querySelector('[data-testid="lemmas-surah-view-tabs"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="surah-occurrences-list-loading"]')).toBeTruthy();
  });
});
