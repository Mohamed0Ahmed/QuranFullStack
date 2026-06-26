import { describe, expect, it, beforeEach, vi } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { provideRouter, ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { BehaviorSubject, of, Subject } from 'rxjs';

import { ApiResponse } from '../../../../core/data-access/api-response.model';
import { deepLinkToHref } from '../../../../shared/url/deep-link-href';
import { STEMS_COLUMN_HEADERS } from '../../models/stems.labels';
import {
  STEM_DETAIL_PAGE_SIZE,
  STEMS_QUERY_KEYS,
  StemAyahMatchDto,
  StemListItemViewModel,
  StemWordItemDto,
  StemLemmasDto,
  StemWordView,
  StemSummaryDto,
  StemMissingSurahsDto,
  StemSurahsDto,
  PagedResultDto,
} from '../../models/stems.models';
import { StemsApi } from '../../data-access/stems.api';
import { StemsDetailFacade } from '../../state/stems-detail.facade';
import { StemsExplorerFacade } from '../../state/stems-explorer.facade';
import { buildLemmasDeepLink } from '../../state/lemmas-url-sync';
import { StemsExplorerPageComponent } from './stems-explorer-page.component';

function listRow(id: number, overrides: Partial<StemListItemViewModel> = {}): StemListItemViewModel {
  return {
    id,
    stemText: `أصل-${id}`,
    displayText: `أصل-${id}`,
    lemmaId: 700,
    lemmaText: 'صيغة-700',
    lemmaBuckwalter: null,
    rootId: 800,
    rootText: 'جذر-800',
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
    firstVerseKey: '1:1',
    ...overrides,
  };
}

function successListResponse() {
  return of<ApiResponse<{ page: number; pageSize: number; totalCount: number; items: StemListItemViewModel[] }>>({
    isSuccess: true,
    data: { page: 1, pageSize: 1000, totalCount: 1, items: [listRow(500)] },
    message: null,
    errors: null,
  });
}

function ayahMatch(): StemAyahMatchDto {
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
  return of<ApiResponse<PagedResultDto<StemAyahMatchDto>>>({
    isSuccess: true,
    data: {
      page: 1,
      pageSize: STEM_DETAIL_PAGE_SIZE,
      totalCount: 1,
      items: [ayahMatch()],
    },
    message: null,
    errors: null,
  });
}

function wordItem(uniqueWordId: number, kind: StemWordView): StemWordItemDto {
  return {
    uniqueWordId,
    kind,
    displayTextUthmani: `كلمة-${uniqueWordId}`,
    occurrencesCount: 2,
    firstVerseKey: '1:1',
  };
}

function successWordsResponse(kind: StemWordView) {
  return of<ApiResponse<PagedResultDto<StemWordItemDto>>>({
    isSuccess: true,
    data: {
      page: 1,
      pageSize: STEM_DETAIL_PAGE_SIZE,
      totalCount: 1,
      items: [wordItem(9001, kind)],
    },
    message: null,
    errors: null,
  });
}

function relatedLemmasResponse(): ApiResponse<StemLemmasDto> {
  return {
    isSuccess: true,
    data: {
      id: 500,
      stemText: 'أصل-500',
      lemmasCount: 2,
      lemmas: [
        { lemmaId: 502, lemmaText: 'عِلْم', lemmaBuckwalter: 'Ailm', occurrencesCount: 3 },
        { lemmaId: 504, lemmaText: 'مَعْرِفَة', lemmaBuckwalter: null, occurrencesCount: 1 },
      ],
    },
    message: null,
    errors: null,
  };
}

describe('StemsExplorerPageComponent US2', () => {
  let router: Router;
  let stemsApi: {
    getStemsList: ReturnType<typeof vi.fn>;
    getStemSummary: ReturnType<typeof vi.fn>;
    getStemWords: ReturnType<typeof vi.fn>;
    getStemAyahMatches: ReturnType<typeof vi.fn>;
    getStemMentionedSurahs: ReturnType<typeof vi.fn>;
    getStemMissingSurahs: ReturnType<typeof vi.fn>;
    getStemLemmas: ReturnType<typeof vi.fn>;
  };

  const queryParamMap$ = new BehaviorSubject(convertToParamMap({}));

  beforeEach(async () => {
    getTestBed().resetTestingModule();

    stemsApi = {
      getStemsList: vi.fn().mockImplementation(successListResponse),
      getStemSummary: vi.fn().mockReturnValue(
        of<ApiResponse<StemSummaryDto>>({
          isSuccess: true,
          data: { ...listRow(500), typeDistribution: [listRow(500).dominantType] },
          message: null,
          errors: null,
        }),
      ),
      getStemWords: vi.fn().mockReturnValue(
        of<ApiResponse<{ page: number; pageSize: number; totalCount: number; items: unknown[] }>>({
          isSuccess: true,
          data: { page: 1, pageSize: 100, totalCount: 0, items: [] },
          message: null,
          errors: null,
        }),
      ),
      getStemAyahMatches: vi.fn().mockReturnValue(
        of<ApiResponse<{ page: number; pageSize: number; totalCount: number; items: unknown[] }>>({
          isSuccess: true,
          data: { page: 1, pageSize: 100, totalCount: 0, items: [] },
          message: null,
          errors: null,
        }),
      ),
      getStemMentionedSurahs: vi.fn(),
      getStemMissingSurahs: vi.fn(),
      getStemLemmas: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [StemsExplorerPageComponent],
      providers: [
        provideRouter([{ path: 'stems', component: StemsExplorerPageComponent }]),
        { provide: StemsApi, useValue: stemsApi },
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

  async function initLifecycle(): Promise<ReturnType<typeof TestBed.createComponent<StemsExplorerPageComponent>>> {
    const fixture = TestBed.createComponent(StemsExplorerPageComponent);
    fixture.componentInstance.ngOnInit();
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
  }

  it('renders the catalogue table with the locked stem headers', async () => {
    const fixture = await initLifecycle();
    const root = fixture.nativeElement as HTMLElement;

    const headers = Array.from(root.querySelectorAll('[role="columnheader"]')).map((h) =>
      h.textContent?.trim() ?? '',
    );
    expect(headers).toContain(STEMS_COLUMN_HEADERS.stem);
    expect(headers).toContain(STEMS_COLUMN_HEADERS.lemma);
    expect(headers).toContain(STEMS_COLUMN_HEADERS.root);
    expect(headers).toContain(STEMS_COLUMN_HEADERS.type);
  });

  it('does not call detail APIs on catalogue render without a selected stem', async () => {
    await initLifecycle();

    expect(stemsApi.getStemsList).toHaveBeenCalled();
    expect(stemsApi.getStemSummary).not.toHaveBeenCalled();
    expect(stemsApi.getStemWords).not.toHaveBeenCalled();
    expect(stemsApi.getStemAyahMatches).not.toHaveBeenCalled();
    expect(stemsApi.getStemMentionedSurahs).not.toHaveBeenCalled();
    expect(stemsApi.getStemMissingSurahs).not.toHaveBeenCalled();
    expect(stemsApi.getStemLemmas).not.toHaveBeenCalled();
  });

  it('loads only the ayah detail endpoint and renders the ayah list when view=ayahs', async () => {
    stemsApi.getStemAyahMatches.mockReturnValue(successAyahsResponse());
    queryParamMap$.next(convertToParamMap({ stem: '500', view: 'ayahs', detailPage: '1' }));

    const fixture = await initLifecycle();
    const root = fixture.nativeElement as HTMLElement;

    expect(stemsApi.getStemSummary).toHaveBeenCalledWith(500);
    expect(stemsApi.getStemAyahMatches).toHaveBeenCalledWith(500, 1, STEM_DETAIL_PAGE_SIZE);
    expect(stemsApi.getStemWords).not.toHaveBeenCalled();
    expect(stemsApi.getStemMentionedSurahs).not.toHaveBeenCalled();
    expect(stemsApi.getStemMissingSurahs).not.toHaveBeenCalled();
    expect(stemsApi.getStemLemmas).not.toHaveBeenCalled();

    expect(root.querySelector('qd-ayah-matches-list')).toBeTruthy();
    expect(root.querySelector('[data-testid="stems-ayahs-view"]')).toBeTruthy();
    expect(root.querySelectorAll('.ayah-matches-list__card')).toHaveLength(1);
  });

  it('loads only the word detail endpoint and renders the simple/tashkeel list when view=words', async () => {
    stemsApi.getStemWords.mockReturnValue(successWordsResponse('simple'));
    queryParamMap$.next(convertToParamMap({ stem: '500', view: 'words', wordView: 'simple', detailPage: '1' }));

    const fixture = await initLifecycle();
    const root = fixture.nativeElement as HTMLElement;

    expect(stemsApi.getStemSummary).toHaveBeenCalledWith(500);
    expect(stemsApi.getStemWords).toHaveBeenCalledWith(500, 'simple', 1, STEM_DETAIL_PAGE_SIZE);
    expect(stemsApi.getStemAyahMatches).not.toHaveBeenCalled();
    expect(stemsApi.getStemMentionedSurahs).not.toHaveBeenCalled();
    expect(stemsApi.getStemMissingSurahs).not.toHaveBeenCalled();
    expect(stemsApi.getStemLemmas).not.toHaveBeenCalled();

    expect(root.querySelector('qd-stem-words-list')).toBeTruthy();
    expect(root.querySelector('[data-testid="stems-words-view"]')).toBeTruthy();
    expect(root.querySelectorAll('[data-testid="stem-word-link"]')).toHaveLength(1);
  });

  it('loads related lemmas for view=lemmas and keeps the type distribution visible while the related list loads', async () => {
    const pendingLemmas$ = new Subject<ApiResponse<StemLemmasDto>>();
    stemsApi.getStemLemmas.mockReturnValue(pendingLemmas$.asObservable());
    queryParamMap$.next(convertToParamMap({ stem: '500', view: 'lemmas' }));

    const fixture = await initLifecycle();
    const root = fixture.nativeElement as HTMLElement;

    expect(stemsApi.getStemSummary).toHaveBeenCalledWith(500);
    expect(stemsApi.getStemLemmas).toHaveBeenCalledWith(500);
    expect(TestBed.inject(StemsDetailFacade).status()).toBe('loading');
    expect(root.querySelector('[data-testid="type-distribution-dominant"]')).toBeTruthy();
    expect(root.querySelectorAll('.type-distribution-list__row--loading')).toHaveLength(0);
    expect(root.querySelector('[data-testid="stem-lemmas-list-loading"]')).toBeTruthy();

    pendingLemmas$.next(relatedLemmasResponse());
    pendingLemmas$.complete();
    await fixture.whenStable();
    fixture.detectChanges();

    const links = root.querySelectorAll('[data-testid="stem-lemmas-list-link"]');
    expect(links).toHaveLength(2);
    expect((links[0] as HTMLAnchorElement).getAttribute('href')).toBe(
      deepLinkToHref(buildLemmasDeepLink({ lemmaId: 502, view: 'words', wordView: 'simple' })),
    );
    expect((links[0] as HTMLAnchorElement).target).toBe('_blank');
    expect((links[0] as HTMLAnchorElement).rel).toBe('noopener noreferrer');
    expect((links[1] as HTMLAnchorElement).getAttribute('href')).toBe(
      deepLinkToHref(buildLemmasDeepLink({ lemmaId: 504, view: 'words', wordView: 'simple' })),
    );
    expect(Array.from(root.querySelectorAll('.stem-lemmas-list__count')).map((el) => el.textContent?.trim())).toEqual([
      '3',
      '1',
    ]);
    expect(root.querySelector('[data-testid="stem-lemmas-list-buckwalter"]')?.textContent?.trim()).toBe('Ailm');
    expect(root.querySelector('[data-testid="stem-lemmas-list-buckwalter-missing"]')).toBeTruthy();
  });

  it('maps row selection to the default words/simple detail state', async () => {
    const fixture = TestBed.createComponent(StemsExplorerPageComponent);
    const component = fixture.componentInstance;

    component['onRowSelected'](listRow(500));

    expect(router.navigate).toHaveBeenCalledWith([], {
      relativeTo: expect.anything(),
      queryParams: expect.objectContaining({
        [STEMS_QUERY_KEYS.stem]: '500',
        [STEMS_QUERY_KEYS.view]: 'words',
        [STEMS_QUERY_KEYS.wordView]: 'simple',
      }),
      queryParamsHandling: 'merge',
    });
    expect(TestBed.inject(StemsDetailFacade).view()).toBe('words');
  });

  it('search and sort changes reset only the list page while preserving selection', async () => {
    queryParamMap$.next(
      convertToParamMap({
        stem: '500',
        view: 'words',
        wordView: 'simple',
        page: '2',
        search: 'أصل',
        sort: 'alpha',
      }),
    );
    const fixture = await initLifecycle();
    vi.mocked(router.navigate).mockClear();

    fixture.componentInstance['onSortChange']('occurrences');

    expect(router.navigate).toHaveBeenCalledWith([], {
      relativeTo: expect.anything(),
      queryParams: expect.objectContaining({
        [STEMS_QUERY_KEYS.sort]: 'occurrences',
        [STEMS_QUERY_KEYS.page]: null,
      }),
      queryParamsHandling: 'merge',
    });

    const lastNavigateArgs = vi.mocked(router.navigate).mock.calls.at(-1)?.[1] as {
      queryParams: Record<string, string | null>;
    };
    expect(lastNavigateArgs.queryParams[STEMS_QUERY_KEYS.stem] ?? undefined).toBeUndefined();
  });

  it('maps word-view sub-tab changes to the words URL params and resets detail page', async () => {
    const fixture = TestBed.createComponent(StemsExplorerPageComponent);
    const component = fixture.componentInstance;

    component['onWordViewChange']('tashkeel');

    expect(router.navigate).toHaveBeenCalledWith([], {
      relativeTo: expect.anything(),
      queryParams: expect.objectContaining({
        [STEMS_QUERY_KEYS.view]: 'words',
        [STEMS_QUERY_KEYS.wordView]: 'tashkeel',
        [STEMS_QUERY_KEYS.detailPage]: '1',
      }),
      queryParamsHandling: 'merge',
    });
  });

  it('page changes update the catalogue page without clearing selection state', async () => {
    queryParamMap$.next(
      convertToParamMap({
        stem: '500',
        view: 'words',
        wordView: 'simple',
        page: '2',
      }),
    );
    const fixture = await initLifecycle();
    vi.mocked(router.navigate).mockClear();

    fixture.componentInstance['onPageChange'](3);

    expect(router.navigate).toHaveBeenCalledWith([], {
      relativeTo: expect.anything(),
      queryParams: expect.objectContaining({
        [STEMS_QUERY_KEYS.page]: '3',
      }),
      queryParamsHandling: 'merge',
    });
  });

  it('shows list error and empty states from the facade', async () => {
    stemsApi.getStemsList.mockReturnValue(
      of<ApiResponse<{ page: number; pageSize: number; totalCount: number; items: StemListItemViewModel[] }>>({
        isSuccess: false,
        data: null,
        message: 'خطأ',
        errors: null,
      }),
    );

    const fixture = await initLifecycle();
    expect(fixture.nativeElement.querySelector('[data-testid="stems-list-error"]')).toBeTruthy();

    stemsApi.getStemsList.mockReturnValue(
      of<ApiResponse<{ page: number; pageSize: number; totalCount: number; items: StemListItemViewModel[] }>>({
        isSuccess: true,
        data: { page: 1, pageSize: 1000, totalCount: 0, items: [] },
        message: null,
        errors: null,
      }),
    );
    queryParamMap$.next(convertToParamMap({}));
    vi.mocked(router.navigate).mockClear();
    const emptyFixture = await initLifecycle();
    expect(emptyFixture.nativeElement.querySelector('[data-testid="stems-list-no-results"]')).toBeTruthy();
  });
});

describe('StemsExplorerPageComponent US5', () => {
  let router: Router;
  let stemsApi: {
    getStemsList: ReturnType<typeof vi.fn>;
    getStemSummary: ReturnType<typeof vi.fn>;
    getStemWords: ReturnType<typeof vi.fn>;
    getStemAyahMatches: ReturnType<typeof vi.fn>;
    getStemMentionedSurahs: ReturnType<typeof vi.fn>;
    getStemMissingSurahs: ReturnType<typeof vi.fn>;
    getStemLemmas: ReturnType<typeof vi.fn>;
  };

  const queryParamMap$ = new BehaviorSubject(convertToParamMap({}));

  beforeEach(async () => {
    getTestBed().resetTestingModule();

    stemsApi = {
      getStemsList: vi.fn().mockImplementation(successListResponse),
      getStemSummary: vi.fn().mockReturnValue(
        of<ApiResponse<StemSummaryDto>>({
          isSuccess: true,
          data: { ...listRow(500), typeDistribution: [listRow(500).dominantType] },
          message: null,
          errors: null,
        }),
      ),
      getStemWords: vi.fn(),
      getStemAyahMatches: vi.fn(),
      getStemMentionedSurahs: vi.fn().mockReturnValue(
        of<ApiResponse<StemSurahsDto>>({
          isSuccess: true,
          data: {
            id: 500,
            stemText: 'أصل-500',
            surahsCount: 1,
            surahs: [{ surahNumber: 1, nameArabic: 'سورة-اختبار', occurrencesInSurah: 2 }],
          },
          message: null,
          errors: null,
        }),
      ),
      getStemMissingSurahs: vi.fn().mockReturnValue(
        of<ApiResponse<StemMissingSurahsDto>>({
          isSuccess: true,
          data: { id: 500, stemText: 'أصل-500', missingSurahsCount: 0, surahs: [] },
          message: null,
          errors: null,
        }),
      ),
      getStemLemmas: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [StemsExplorerPageComponent],
      providers: [
        provideRouter([{ path: 'stems', component: StemsExplorerPageComponent }]),
        { provide: StemsApi, useValue: stemsApi },
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

  async function initLifecycle(): Promise<ReturnType<typeof TestBed.createComponent<StemsExplorerPageComponent>>> {
    const fixture = TestBed.createComponent(StemsExplorerPageComponent);
    fixture.componentInstance.ngOnInit();
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
  }

  it('loads mentioned surahs whole (no paging) and maps the row count when surahView=mentioned', async () => {
    queryParamMap$.next(convertToParamMap({ stem: '500', view: 'surahs', surahView: 'mentioned' }));
    const fixture = await initLifecycle();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(stemsApi.getStemMentionedSurahs).toHaveBeenCalledWith(500);
    expect(stemsApi.getStemMissingSurahs).not.toHaveBeenCalled();

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('[data-testid="stems-mentioned-surahs-view"]')).toBeTruthy();
    expect(root.querySelector('qd-surah-occurrences-list')).toBeTruthy();
    expect(root.querySelectorAll('.surah-occurrences-list__row:not(.surah-occurrences-list__row--loading)')).toHaveLength(1);
  });

  it('routes surah sub-view changes through the URL and loads missing whole when surahView=missing', async () => {
    queryParamMap$.next(convertToParamMap({ stem: '500', view: 'surahs', surahView: 'missing' }));
    await initLifecycle();

    expect(stemsApi.getStemMissingSurahs).toHaveBeenCalledWith(500);
    expect(stemsApi.getStemMentionedSurahs).not.toHaveBeenCalled();

    const fixture = TestBed.createComponent(StemsExplorerPageComponent);
    const component = fixture.componentInstance;
    component.ngOnInit();
    component['onSurahViewChange']('mentioned');

    expect(router.navigate).toHaveBeenCalledWith([], {
      relativeTo: expect.anything(),
      queryParams: expect.objectContaining({
        [STEMS_QUERY_KEYS.view]: 'surahs',
        [STEMS_QUERY_KEYS.surahView]: 'mentioned',
      }),
      queryParamsHandling: 'merge',
    });
  });

  it('renders empty missing-surahs state cleanly when the missing list is empty', async () => {
    queryParamMap$.next(convertToParamMap({ stem: '500', view: 'surahs', surahView: 'missing' }));
    const fixture = await initLifecycle();
    await fixture.whenStable();
    fixture.detectChanges();

    const detailFacade = TestBed.inject(StemsDetailFacade);
    expect(detailFacade.status()).toBe('empty');
    expect(fixture.nativeElement.querySelector('[data-testid="stems-panel-empty"]')).toBeTruthy();
  });

  it('maps a surahs count-click to the mentioned surah view URL params', async () => {
    const fixture = TestBed.createComponent(StemsExplorerPageComponent);
    const component = fixture.componentInstance;

    component['onCountOpened']({ stem: listRow(500), view: 'surahs', surahView: 'mentioned' });

    expect(router.navigate).toHaveBeenCalledWith([], {
      relativeTo: expect.anything(),
      queryParams: expect.objectContaining({
        [STEMS_QUERY_KEYS.stem]: '500',
        [STEMS_QUERY_KEYS.view]: 'surahs',
        [STEMS_QUERY_KEYS.surahView]: 'mentioned',
        [STEMS_QUERY_KEYS.detailPage]: null,
      }),
      queryParamsHandling: 'merge',
    });
  });

  it('keeps the four detail tabs and surah sub-tabs visible while mentioned surahs load', async () => {
    const pendingSurahs$ = new Subject<ApiResponse<StemSurahsDto>>();
    stemsApi.getStemMentionedSurahs.mockReturnValue(pendingSurahs$.asObservable());

    queryParamMap$.next(convertToParamMap({ stem: '500', view: 'surahs', surahView: 'mentioned' }));
    const fixture = TestBed.createComponent(StemsExplorerPageComponent);
    fixture.componentInstance.ngOnInit();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(TestBed.inject(StemsDetailFacade).status()).toBe('loading');

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelectorAll('[data-stem-tab]')).toHaveLength(4);
    expect(root.querySelector('[data-testid="stems-surah-view-tabs"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="surah-occurrences-list-loading"]')).toBeTruthy();
  });
});
