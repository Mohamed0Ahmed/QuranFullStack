import { describe, expect, it, beforeEach, vi } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { HttpErrorResponse, provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { BehaviorSubject, of, Subject, throwError } from 'rxjs';

import { ApiResponse } from '../../../../core/data-access/api-response.model';
import { LEMMAS_COLUMN_HEADERS } from '../../models/lemmas.labels';
import {
  LEMMA_DETAIL_PAGE_SIZE,
  LEMMAS_QUERY_KEYS,
  LemmaAyahMatchDto,
  LemmaListItemViewModel,
  LemmaStemsDto,
  LemmaWordItemDto,
  LemmaMissingSurahsDto,
  LemmaSummaryDto,
  LemmaSurahsDto,
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
    rootId: 700,
    rootText: 'ك ل م',
    occurrencesCount: 5,
    ayahsCount: 3,
    surahsCount: 2,
    simpleWordsCount: 2,
    tashkeelWordsCount: 2,
    stemsCount: 1,
    ...overrides,
  };
}

function typeSummary() {
  return {
    code: 'N',
    arabicLabel: 'اسم',
    occurrencesCount: 5,
  };
}

function multiTypeSummary() {
    return [
      typeSummary(),
      {
        code: 'V',
        arabicLabel: 'فعل',
        occurrencesCount: 2,
      },
    ];
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
    surahNameArabic: 'النساء',
    pageNumber: 92,
    words: [
      {
        textUthmani: 'كلمة-تجريبية-١',
        isMatched: true,
      },
      {
        textUthmani: 'كلمة-تجريبية-٢',
        isMatched: false,
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

function wordItem(uniqueWordId: number): LemmaWordItemDto {
  return {
    uniqueWordId,
    displayText: `كلمة-${uniqueWordId}`,
    occurrencesCount: 2,
  };
}

function successWordsResponse() {
  return of<ApiResponse<PagedResultDto<LemmaWordItemDto>>>({
    isSuccess: true,
    data: {
      page: 1,
      pageSize: LEMMA_DETAIL_PAGE_SIZE,
      totalCount: 1,
      items: [wordItem(9001)],
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
          data: { ...listRow(500), typeDistribution: [typeSummary()] },
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
    expect(headers).toContain(LEMMAS_COLUMN_HEADERS.occurrences);
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
    expect(lemmasApi.getLemmaAyahMatches).toHaveBeenCalledWith(500, 1, LEMMA_DETAIL_PAGE_SIZE, null);
    expect(lemmasApi.getLemmaWords).not.toHaveBeenCalled();
    expect(lemmasApi.getLemmaMentionedSurahs).not.toHaveBeenCalled();
    expect(lemmasApi.getLemmaMissingSurahs).not.toHaveBeenCalled();
    expect(lemmasApi.getLemmaStems).not.toHaveBeenCalled();

    expect(root.querySelector('[data-testid="lemma-ayah-type-filters"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="lemma-ayah-type-filter-all"]')).toBeNull();
    expect(root.querySelector('[data-testid="lemma-ayah-type-filter-N"]')?.getAttribute('aria-pressed')).toBe('true');
    expect(root.querySelector('qd-ayah-matches-list')).toBeTruthy();
    expect(root.querySelector('[data-testid="lemmas-ayahs-view"]')).toBeTruthy();
    expect(root.querySelectorAll('.ayah-matches-list__card')).toHaveLength(1);
    expect(root.querySelectorAll('.highlighted-ayah__word--matched')).toHaveLength(1);
    expect((root.querySelector('[data-testid="ayah-matches-open-mushaf"]') as HTMLAnchorElement).getAttribute('href')).toContain('page=92');
  });

  it('updates URL with typeCode and resets detail page when a type filter is selected', async () => {
    lemmasApi.getLemmaAyahMatches.mockReturnValue(successAyahsResponse());
    queryParamMap$.next(convertToParamMap({ lemma: '500', view: 'ayahs', detailPage: '1' }));

    const fixture = await initLifecycle();
    vi.mocked(router.navigate).mockClear();

    (fixture.nativeElement.querySelector('[data-testid="lemma-ayah-type-filter-N"]') as HTMLButtonElement).click();

    expect(router.navigate).toHaveBeenCalledWith([], {
      relativeTo: expect.anything(),
      queryParams: expect.objectContaining({
        [LEMMAS_QUERY_KEYS.view]: 'ayahs',
        [LEMMAS_QUERY_KEYS.detailPage]: '1',
        [LEMMAS_QUERY_KEYS.typeCode]: 'N',
      }),
      queryParamsHandling: 'merge',
    });
  });

  it('clears typeCode when عرض الكل is selected', async () => {
    lemmasApi.getLemmaSummary.mockReturnValue(
      of<ApiResponse<LemmaSummaryDto>>({
        isSuccess: true,
        data: { ...listRow(500), typeDistribution: multiTypeSummary() },
        message: null,
        errors: null,
      }),
    );
    lemmasApi.getLemmaAyahMatches.mockReturnValue(successAyahsResponse());
    queryParamMap$.next(convertToParamMap({ lemma: '500', view: 'ayahs', detailPage: '1', typeCode: 'N' }));

    const fixture = await initLifecycle();
    vi.mocked(router.navigate).mockClear();

    (fixture.nativeElement.querySelector('[data-testid="lemma-ayah-type-filter-all"]') as HTMLButtonElement).click();

    expect(router.navigate).toHaveBeenCalledWith([], {
      relativeTo: expect.anything(),
      queryParams: expect.objectContaining({
        [LEMMAS_QUERY_KEYS.view]: 'ayahs',
        [LEMMAS_QUERY_KEYS.detailPage]: '1',
        [LEMMAS_QUERY_KEYS.typeCode]: null,
      }),
      queryParamsHandling: 'merge',
    });
  });

  it('loads only the word detail endpoint and renders the simple/tashkeel list when view=words', async () => {
    lemmasApi.getLemmaWords.mockReturnValue(successWordsResponse());
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

  it('loads related stems for view=stems without rendering Lemmas ayah type filters', async () => {
    const pendingStems$ = new Subject<ApiResponse<LemmaStemsDto>>();
    lemmasApi.getLemmaStems.mockReturnValue(pendingStems$.asObservable());
    queryParamMap$.next(convertToParamMap({ lemma: '500', view: 'stems' }));

    const fixture = await initLifecycle();
    const root = fixture.nativeElement as HTMLElement;

    expect(lemmasApi.getLemmaSummary).toHaveBeenCalledWith(500);
    expect(lemmasApi.getLemmaStems).toHaveBeenCalledWith(500);
    expect(TestBed.inject(LemmasDetailFacade).status()).toBe('loading');
    expect(root.querySelector('[data-testid="lemma-ayah-type-filters"]')).toBeNull();
    expect(root.querySelector('[data-testid="type-distribution-list"]')).toBeNull();
    expect(root.querySelector('[data-testid="lemma-stems-list-loading"]')).toBeTruthy();
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
    expect(TestBed.inject(LemmasDetailFacade).selectedLemmaId()).toBeNull();
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
        [LEMMAS_QUERY_KEYS.detailPage]: '1',
      }),
      queryParamsHandling: 'merge',
    });
    expect(TestBed.inject(LemmasDetailFacade).selectedLemmaId()).toBeNull();
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
        [LEMMAS_QUERY_KEYS.detailPage]: '1',
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
          data: { ...listRow(500), typeDistribution: [typeSummary()] },
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
            surahs: [{ surahNumber: 1, nameArabic: 'سورة-اختبار', occurrencesInSurah: 2 }],
          },
          message: null,
          errors: null,
        }),
      ),
      getLemmaMissingSurahs: vi.fn().mockReturnValue(
        of<ApiResponse<LemmaMissingSurahsDto>>({
          isSuccess: true,
          data: { surahs: [] },
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

describe('LemmasExplorerPageComponent US8 — restore and navigate exact state', () => {
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
          data: { ...listRow(500), typeDistribution: [typeSummary()] },
          message: null,
          errors: null,
        }),
      ),
      getLemmaWords: vi.fn().mockImplementation(() => successWordsResponse()),
      getLemmaAyahMatches: vi.fn(),
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

  it('restores a copied deep link (refresh): loads summary then the active words view and sub-view', async () => {
    queryParamMap$.next(
      convertToParamMap({ lemma: '500', view: 'words', wordView: 'tashkeel', detailPage: '2' }),
    );
    const fixture = await initLifecycle();

    expect(lemmasApi.getLemmaSummary).toHaveBeenCalledWith(500);
    expect(lemmasApi.getLemmaWords).toHaveBeenCalledWith(500, 'tashkeel', 2, LEMMA_DETAIL_PAGE_SIZE);

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('[data-testid="lemmas-words-view"]')).toBeTruthy();
    expect(TestBed.inject(LemmasDetailFacade).view()).toBe('words');
    expect(TestBed.inject(LemmasDetailFacade).wordView()).toBe('tashkeel');
  });

  it('ignores irrelevant query keys when restoring a deep link', async () => {
    queryParamMap$.next(
      convertToParamMap({
        lemma: '500',
        view: 'words',
        wordView: 'simple',
        detailPage: '1',
        foo: 'bar',
        debug: '1',
        random: 'xyz',
      }),
    );
    const fixture = await initLifecycle();

    expect(lemmasApi.getLemmaSummary).toHaveBeenCalledWith(500);
    expect(lemmasApi.getLemmaWords).toHaveBeenCalledWith(500, 'simple', 1, LEMMA_DETAIL_PAGE_SIZE);
    expect(fixture.nativeElement.querySelector('[data-testid="lemmas-words-view"]')).toBeTruthy();
  });

  it('shows restored-not-found and keeps the catalogue intact when the identity is unknown (summary unsuccessful)', async () => {
    lemmasApi.getLemmaSummary.mockReturnValue(
      of<ApiResponse<LemmaSummaryDto>>({
        isSuccess: false,
        data: null,
        message: 'غير موجود',
        errors: null,
      }),
    );
    queryParamMap$.next(convertToParamMap({ lemma: '99999', view: 'words' }));
    const fixture = await initLifecycle();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(lemmasApi.getLemmaSummary).toHaveBeenCalledWith(99999);
    expect(lemmasApi.getLemmaWords).not.toHaveBeenCalled();
    expect(lemmasApi.getLemmasList).toHaveBeenCalled();

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('[data-testid="lemmas-restored-not-found"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="lemma-details-not-found"]')).toBeTruthy();
    expect(root.querySelectorAll('[data-lemma-tab]')).toHaveLength(0);
    expect(root.querySelector('qd-lemmas-table')).toBeTruthy();
  });

  it('shows restored-not-found on a repeated unknown identity and does not reload the list repeatedly', async () => {
    lemmasApi.getLemmaSummary.mockReturnValue(
      of<ApiResponse<LemmaSummaryDto>>({
        isSuccess: false,
        data: null,
        message: 'غير موجود',
        errors: null,
      }),
    );
    queryParamMap$.next(convertToParamMap({ lemma: '99999', view: 'words' }));
    const fixture = await initLifecycle();
    await fixture.whenStable();
    fixture.detectChanges();

    const listCallsBefore = lemmasApi.getLemmasList.mock.calls.length;
    queryParamMap$.next(convertToParamMap({ lemma: '99999', view: 'words' }));
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[data-testid="lemmas-restored-not-found"]')).toBeTruthy();
    expect(lemmasApi.getLemmasList.mock.calls.length).toBe(listCallsBefore);
  });

  it('preserves a positive out-of-range detailPage and renders the controlled empty result', async () => {
    lemmasApi.getLemmaWords.mockReturnValue(
      of<ApiResponse<PagedResultDto<LemmaWordItemDto>>>({
        isSuccess: true,
        data: { page: 99999, pageSize: LEMMA_DETAIL_PAGE_SIZE, totalCount: 0, items: [] },
        message: null,
        errors: null,
      }),
    );
    queryParamMap$.next(
      convertToParamMap({ lemma: '500', view: 'words', wordView: 'simple', detailPage: '99999' }),
    );
    const fixture = await initLifecycle();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(lemmasApi.getLemmaWords).toHaveBeenCalledWith(500, 'simple', 99999, LEMMA_DETAIL_PAGE_SIZE);
    expect(TestBed.inject(LemmasDetailFacade).status()).toBe('empty');
    expect(fixture.nativeElement.querySelector('[data-testid="lemmas-panel-empty"]')).toBeTruthy();
  });

  it('clears the panel via onClearSelection while preserving list state (search/sort/page untouched)', async () => {
    queryParamMap$.next(
      convertToParamMap({
        lemma: '500',
        view: 'words',
        wordView: 'simple',
        detailPage: '1',
        page: '2',
        search: 'صيغة',
        sort: 'alpha',
      }),
    );
    const fixture = await initLifecycle();
    vi.mocked(router.navigate).mockClear();

    fixture.componentInstance['onClearSelection']();

    expect(router.navigate).toHaveBeenCalledWith(
      [],
      expect.objectContaining({ queryParamsHandling: 'merge' }),
    );
    const lastArgs = vi.mocked(router.navigate).mock.calls.at(-1)?.[1] as {
      queryParams: Record<string, string | null>;
    };
    expect(lastArgs.queryParams[LEMMAS_QUERY_KEYS.lemma]).toBeNull();
    expect(lastArgs.queryParams[LEMMAS_QUERY_KEYS.view]).toBeNull();
    expect(lastArgs.queryParams[LEMMAS_QUERY_KEYS.wordView]).toBeNull();
    expect(lastArgs.queryParams[LEMMAS_QUERY_KEYS.detailPage]).toBeNull();
    expect(lastArgs.queryParams[LEMMAS_QUERY_KEYS.search]).toBeUndefined();
    expect(lastArgs.queryParams[LEMMAS_QUERY_KEYS.sort]).toBeUndefined();
    expect(lastArgs.queryParams[LEMMAS_QUERY_KEYS.page]).toBeUndefined();

    expect(TestBed.inject(LemmasDetailFacade).selectedLemmaId()).toBeNull();
  });

  it('reuses cached view data for the same selection/view/page (no duplicate detail call)', async () => {
    queryParamMap$.next(
      convertToParamMap({ lemma: '500', view: 'words', wordView: 'simple', detailPage: '1' }),
    );
    await initLifecycle();
    const wordsCallsAfterFirst = lemmasApi.getLemmaWords.mock.calls.length;
    const summaryCallsAfterFirst = lemmasApi.getLemmaSummary.mock.calls.length;

    queryParamMap$.next(
      convertToParamMap({ lemma: '500', view: 'words', wordView: 'simple', detailPage: '1' }),
    );
    await new Promise((resolve) => setTimeout(resolve, 0));

    expect(lemmasApi.getLemmaWords.mock.calls.length).toBe(wordsCallsAfterFirst);
    expect(lemmasApi.getLemmaSummary.mock.calls.length).toBe(summaryCallsAfterFirst);
  });

  it('survives a back/forward sequence: select → clear → re-select restores the panel', async () => {
    const fixture = await initLifecycle();
    const detailFacade = TestBed.inject(LemmasDetailFacade);

    queryParamMap$.next(
      convertToParamMap({ lemma: '500', view: 'words', wordView: 'simple', detailPage: '1' }),
    );
    await fixture.whenStable();
    fixture.detectChanges();
    expect(detailFacade.selectedLemmaId()).toBe(500);

    queryParamMap$.next(convertToParamMap({ page: '1' }));
    await fixture.whenStable();
    fixture.detectChanges();
    expect(detailFacade.selectedLemmaId()).toBeNull();

    lemmasApi.getLemmaAyahMatches.mockReturnValue(successAyahsResponse());
    queryParamMap$.next(convertToParamMap({ lemma: '500', view: 'ayahs', detailPage: '1' }));
    await fixture.whenStable();
    fixture.detectChanges();

    expect(detailFacade.selectedLemmaId()).toBe(500);
    expect(detailFacade.view()).toBe('ayahs');
    expect(lemmasApi.getLemmaAyahMatches).toHaveBeenCalledWith(500, 1, LEMMA_DETAIL_PAGE_SIZE, null);
  });

  it('clears stale typeCode after summary loads', async () => {
    lemmasApi.getLemmaSummary.mockReturnValue(
      of<ApiResponse<LemmaSummaryDto>>({
        isSuccess: true,
        data: { ...listRow(500), typeDistribution: [typeSummary()] },
        message: null,
        errors: null,
      }),
    );
    lemmasApi.getLemmaAyahMatches.mockReturnValue(successAyahsResponse());

    queryParamMap$.next(convertToParamMap({ lemma: '500', view: 'ayahs', typeCode: 'V', detailPage: '1' }));
    const fixture = await initLifecycle();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(TestBed.inject(LemmasDetailFacade).panelState().ayahTypeCode).toBeNull();
    expect(router.navigate).toHaveBeenCalledWith([], {
      relativeTo: expect.anything(),
      queryParams: expect.objectContaining({
        [LEMMAS_QUERY_KEYS.typeCode]: null,
        [LEMMAS_QUERY_KEYS.detailPage]: '1',
      }),
      queryParamsHandling: 'merge',
    });
    expect(fixture.nativeElement.querySelector('[data-testid="lemma-ayah-type-filter-all"]')).toBeNull();
    expect(fixture.nativeElement.querySelector('[data-testid="lemma-ayah-type-filter-N"]')?.getAttribute('aria-pressed')).toBe('true');
  });

  it('maps a summary HTTP 404 to restored-not-found without surfacing a generic error', async () => {
    const http404 = new HttpErrorResponse({ status: 404, statusText: 'Not Found' });
    lemmasApi.getLemmaSummary.mockReturnValue(throwError(() => http404));
    queryParamMap$.next(convertToParamMap({ lemma: '424242', view: 'stems' }));
    const fixture = await initLifecycle();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[data-testid="lemmas-restored-not-found"]')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('[data-testid="lemma-details-not-found"]')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('[data-testid="lemmas-panel-error"]')).toBeFalsy();
  });
});
