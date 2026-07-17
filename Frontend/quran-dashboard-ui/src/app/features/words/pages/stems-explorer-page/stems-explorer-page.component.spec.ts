import { describe, expect, it, beforeEach, vi } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { provideRouter, ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { BehaviorSubject, of, Subject, throwError } from 'rxjs';

import { ApiResponse } from '../../../../core/data-access/api-response.model';
import { WordsAssociationOptionsService } from '../../data-access/words-association-options.service';
import { STEMS_COLUMN_HEADERS, STEMS_RESULT_COUNT_LABEL } from '../../models/stems.labels';
import {
  STEM_DETAIL_PAGE_SIZE,
  STEMS_QUERY_KEYS,
  StemAyahMatchDto,
  StemListItemViewModel,
  TypeSummaryDto,
  StemWordItemDto,
  StemLemmasDto,
  StemSummaryDto,
  StemMissingSurahsDto,
  StemSurahsDto,
  PagedResultDto,
} from '../../models/stems.models';
import { StemsApi } from '../../data-access/stems.api';
import { StemsDetailFacade } from '../../state/stems-detail.facade';
import { StemsExplorerFacade } from '../../state/stems-explorer.facade';
import { StemsExplorerPageComponent } from './stems-explorer-page.component';

const STUB_ASSOCIATION_OPTIONS = {
  searchRoots: () => of([]),
  searchLemmas: () => of([]),
  wordTypeOptions: () => of([]),
};

function listRow(id: number, overrides: Partial<StemListItemViewModel> = {}): StemListItemViewModel {
  return {
    id,
    stemText: `أصل-${id}`,
    displayText: `أصل-${id}`,
    lemmaId: 700,
    lemmaText: 'صيغة-700',
    rootId: 800,
    rootText: 'جذر-800',
    occurrencesCount: 5,
    ayahsCount: 3,
    surahsCount: 2,
    simpleWordsCount: 2,
    tashkeelWordsCount: 2,
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
    surahNameArabic: 'النساء',
    pageNumber: 92,
    words: [
      {
        textUthmani: 'كلمة-تجريبية-١',
        isMatched: true,
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

function wordItem(uniqueWordId: number): StemWordItemDto {
  return {
    uniqueWordId,
    displayText: `كلمة-${uniqueWordId}`,
    occurrencesCount: 2,
  };
}

function successWordsResponse() {
  return of<ApiResponse<PagedResultDto<StemWordItemDto>>>({
    isSuccess: true,
    data: {
      page: 1,
      pageSize: STEM_DETAIL_PAGE_SIZE,
      totalCount: 1,
      items: [wordItem(9001)],
    },
    message: null,
    errors: null,
  });
}

function typeSummary(code: string, arabicLabel: string, occurrencesCount: number): TypeSummaryDto {
  return {
    code,
    arabicLabel,
    occurrencesCount,
  };
}

function multiTypeSummary(): TypeSummaryDto[] {
  return [typeSummary('N', 'اسم', 10), typeSummary('V', 'فعل', 1)];
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
          data: { ...listRow(500), typeDistribution: [typeSummary('N', 'اسم', 5)] },
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
        { provide: WordsAssociationOptionsService, useValue: STUB_ASSOCIATION_OPTIONS },
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

  it('shows the headline result count equal to the list totalCount (US4)', async () => {
    const fixture = await initLifecycle();
    await fixture.whenStable();
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    const stat = root.querySelector('[data-testid="stems-result-count"] [data-testid="explorer-result-count"]');
    expect(stat).toBeTruthy();
    expect(stat?.getAttribute('aria-label')).toBe(`${STEMS_RESULT_COUNT_LABEL}: 1`);
    expect(
      root
        .querySelector('[data-testid="stems-result-count"] [data-testid="explorer-result-count-value"]')
        ?.textContent?.trim(),
    ).toBe('1');
  });

  it('hides the headline result count when the list read fails (US4)', async () => {
    stemsApi.getStemsList.mockReturnValue(
      of<ApiResponse<{ page: number; pageSize: number; totalCount: number; items: StemListItemViewModel[] }>>({
        isSuccess: false,
        data: null,
        message: 'boom',
        errors: null,
      }),
    );

    const fixture = await initLifecycle();
    await fixture.whenStable();
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('[data-testid="stems-result-count"] [data-testid="explorer-result-count"]')).toBeNull();
  });

  it('serializes a count-range chip to the URL and resets the page (US5)', async () => {
    const fixture = await initLifecycle();
    await fixture.whenStable();
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('[data-testid="stems-range-filter"]')).toBeTruthy();
    (root.querySelector('[data-testid="range-filter-chip-occurrences-gt"]') as HTMLButtonElement).click();

    expect(router.navigate).toHaveBeenCalledWith([], {
      relativeTo: expect.anything(),
      queryParams: expect.objectContaining({ occ: '101..', page: null }),
      queryParamsHandling: 'merge',
    });
  });

  it('serializes primary root/lemma selections to the URL and resets the page (US7)', async () => {
    const fixture = await initLifecycle();

    fixture.componentInstance['onPrimaryRootChange']({ id: 701, label: 'ع ل م' });
    expect(router.navigate).toHaveBeenCalledWith([], {
      relativeTo: expect.anything(),
      queryParams: expect.objectContaining({ rootId: '701', page: null }),
      queryParamsHandling: 'merge',
    });

    fixture.componentInstance['onPrimaryLemmaChange']({ id: 502, label: 'عِلْم' });
    expect(router.navigate).toHaveBeenCalledWith([], {
      relativeTo: expect.anything(),
      queryParams: expect.objectContaining({ lemmaId: '502', page: null }),
      queryParamsHandling: 'merge',
    });
  });

  it('renders the catalogue table with the locked stem headers', async () => {
    const fixture = await initLifecycle();
    const root = fixture.nativeElement as HTMLElement;

    const headers = Array.from(root.querySelectorAll('[role="columnheader"]')).map((h) =>
      h.textContent?.trim() ?? '',
    );
    expect(headers).toContain(STEMS_COLUMN_HEADERS.stem);
    expect(headers).toContain(STEMS_COLUMN_HEADERS.lemma);
    expect(headers).toContain(STEMS_COLUMN_HEADERS.root);
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
    expect(stemsApi.getStemAyahMatches).toHaveBeenCalledWith(500, 1, STEM_DETAIL_PAGE_SIZE, null);
    expect(stemsApi.getStemWords).not.toHaveBeenCalled();
    expect(stemsApi.getStemMentionedSurahs).not.toHaveBeenCalled();
    expect(stemsApi.getStemMissingSurahs).not.toHaveBeenCalled();
    expect(stemsApi.getStemLemmas).not.toHaveBeenCalled();

    expect(root.querySelector('qd-ayah-matches-list')).toBeTruthy();
    expect(root.querySelector('[data-testid="stems-ayahs-view"]')).toBeTruthy();
    expect(root.querySelectorAll('[data-testid="ayah-match-card"]')).toHaveLength(1);
  });

  it('loads only the word detail endpoint and renders the simple/tashkeel list when view=words', async () => {
    stemsApi.getStemWords.mockReturnValue(successWordsResponse());
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

  it('loads related lemmas for view=lemmas without rendering ayah type filters', async () => {
    const pendingLemmas$ = new Subject<ApiResponse<StemLemmasDto>>();
    stemsApi.getStemLemmas.mockReturnValue(pendingLemmas$.asObservable());
    queryParamMap$.next(convertToParamMap({ stem: '500', view: 'lemmas' }));

    const fixture = await initLifecycle();
    const root = fixture.nativeElement as HTMLElement;

    expect(stemsApi.getStemSummary).toHaveBeenCalledWith(500);
    expect(stemsApi.getStemLemmas).toHaveBeenCalledWith(500);
    expect(TestBed.inject(StemsDetailFacade).status()).toBe('loading');
    expect(root.querySelector('[data-testid="stem-ayah-type-filters"]')).toBeNull();
    expect(root.querySelector('[data-testid="stem-lemmas-list-loading"]')).toBeTruthy();
  });

  it('loads ayah filters and passes typeCode to the ayah endpoint when view=ayahs', async () => {
    stemsApi.getStemAyahMatches.mockReturnValue(successAyahsResponse());
    queryParamMap$.next(convertToParamMap({ stem: '500', view: 'ayahs', detailPage: '1', typeCode: 'N' }));

    const fixture = await initLifecycle();
    const root = fixture.nativeElement as HTMLElement;

    expect(stemsApi.getStemSummary).toHaveBeenCalledWith(500);
    expect(stemsApi.getStemAyahMatches).toHaveBeenCalledWith(500, 1, STEM_DETAIL_PAGE_SIZE, 'N');
    expect(root.querySelector('[data-testid="stem-ayah-type-filters"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="stem-ayah-type-filter-all"]')).toBeNull();
    expect(root.querySelector('[data-testid="stem-ayah-type-filter-N"]')?.getAttribute('aria-pressed')).toBe('true');
    expect(root.querySelector('qd-ayah-matches-list')).toBeTruthy();
    expect(root.querySelector('[data-testid="stems-ayahs-view"]')).toBeTruthy();
    expect(root.querySelectorAll('[data-testid="ayah-match-card"]')).toHaveLength(1);
  });

  it('updates URL with typeCode and resets detail page when a type filter is selected', async () => {
    stemsApi.getStemSummary.mockReturnValue(
      of<ApiResponse<StemSummaryDto>>({
        isSuccess: true,
        data: { ...listRow(500), typeDistribution: multiTypeSummary() },
        message: null,
        errors: null,
      }),
    );
    stemsApi.getStemAyahMatches.mockReturnValue(successAyahsResponse());
    queryParamMap$.next(convertToParamMap({ stem: '500', view: 'ayahs', detailPage: '1' }));

    const fixture = await initLifecycle();
    vi.mocked(router.navigate).mockClear();

    (fixture.nativeElement.querySelector('[data-testid="stem-ayah-type-filter-N"]') as HTMLButtonElement).click();

    expect(router.navigate).toHaveBeenCalledWith([], {
      relativeTo: expect.anything(),
      queryParams: expect.objectContaining({
        [STEMS_QUERY_KEYS.view]: 'ayahs',
        [STEMS_QUERY_KEYS.detailPage]: '1',
        [STEMS_QUERY_KEYS.typeCode]: 'N',
      }),
      queryParamsHandling: 'merge',
    });
  });

  it('does not navigate or refetch when the already-active single type chip is clicked', async () => {
    stemsApi.getStemAyahMatches.mockReturnValue(successAyahsResponse());
    queryParamMap$.next(convertToParamMap({ stem: '500', view: 'ayahs', detailPage: '1' }));

    const fixture = await initLifecycle();
    const chip = fixture.nativeElement.querySelector('[data-testid="stem-ayah-type-filter-N"]') as HTMLButtonElement;
    expect(chip.getAttribute('aria-pressed')).toBe('true');

    vi.mocked(router.navigate).mockClear();
    const ayahCallsBeforeClick = stemsApi.getStemAyahMatches.mock.calls.length;

    chip.click();
    await fixture.whenStable();

    expect(router.navigate).not.toHaveBeenCalled();
    expect(stemsApi.getStemAyahMatches.mock.calls.length).toBe(ayahCallsBeforeClick);
  });

  it('does not navigate or refetch when the already-active عرض الكل chip is clicked', async () => {
    stemsApi.getStemSummary.mockReturnValue(
      of<ApiResponse<StemSummaryDto>>({
        isSuccess: true,
        data: { ...listRow(500), typeDistribution: multiTypeSummary() },
        message: null,
        errors: null,
      }),
    );
    stemsApi.getStemAyahMatches.mockReturnValue(successAyahsResponse());
    queryParamMap$.next(convertToParamMap({ stem: '500', view: 'ayahs', detailPage: '1' }));

    const fixture = await initLifecycle();
    const allChip = fixture.nativeElement.querySelector(
      '[data-testid="stem-ayah-type-filter-all"]',
    ) as HTMLButtonElement;
    expect(allChip.getAttribute('aria-pressed')).toBe('true');

    vi.mocked(router.navigate).mockClear();
    const ayahCallsBeforeClick = stemsApi.getStemAyahMatches.mock.calls.length;

    allChip.click();
    await fixture.whenStable();

    expect(router.navigate).not.toHaveBeenCalled();
    expect(stemsApi.getStemAyahMatches.mock.calls.length).toBe(ayahCallsBeforeClick);
  });

  it('clears typeCode when عرض الكل is selected', async () => {
    stemsApi.getStemSummary.mockReturnValue(
      of<ApiResponse<StemSummaryDto>>({
        isSuccess: true,
        data: { ...listRow(500), typeDistribution: multiTypeSummary() },
        message: null,
        errors: null,
      }),
    );
    stemsApi.getStemAyahMatches.mockReturnValue(successAyahsResponse());
    queryParamMap$.next(convertToParamMap({ stem: '500', view: 'ayahs', detailPage: '1', typeCode: 'N' }));

    const fixture = await initLifecycle();
    vi.mocked(router.navigate).mockClear();

    (fixture.nativeElement.querySelector('[data-testid="stem-ayah-type-filter-all"]') as HTMLButtonElement).click();

    expect(router.navigate).toHaveBeenCalledWith([], {
      relativeTo: expect.anything(),
      queryParams: expect.objectContaining({
        [STEMS_QUERY_KEYS.view]: 'ayahs',
        [STEMS_QUERY_KEYS.detailPage]: '1',
        [STEMS_QUERY_KEYS.typeCode]: null,
      }),
      queryParamsHandling: 'merge',
    });
  });

  it('clears stale typeCode after summary loads', async () => {
    stemsApi.getStemSummary.mockReturnValue(
      of<ApiResponse<StemSummaryDto>>({
        isSuccess: true,
        data: { ...listRow(500), typeDistribution: [typeSummary('N', 'اسم', 10)] },
        message: null,
        errors: null,
      }),
    );
    stemsApi.getStemAyahMatches.mockReturnValue(successAyahsResponse());

    queryParamMap$.next(convertToParamMap({ stem: '500', view: 'ayahs', typeCode: 'V', detailPage: '1' }));
    const fixture = await initLifecycle();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(TestBed.inject(StemsDetailFacade).panelState().ayahTypeCode).toBeNull();
    expect(router.navigate).toHaveBeenCalledWith([], {
      relativeTo: expect.anything(),
      queryParams: expect.objectContaining({
        [STEMS_QUERY_KEYS.typeCode]: null,
        [STEMS_QUERY_KEYS.detailPage]: '1',
      }),
      queryParamsHandling: 'merge',
    });
    expect(fixture.nativeElement.querySelector('[data-testid="stem-ayah-type-filter-all"]')).toBeNull();
    expect(fixture.nativeElement.querySelector('[data-testid="stem-ayah-type-filter-N"]')?.getAttribute('aria-pressed')).toBe('true');
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
        [STEMS_QUERY_KEYS.typeCode]: null,
      }),
      queryParamsHandling: 'merge',
    });
    expect(TestBed.inject(StemsDetailFacade).selectedStemId()).toBeNull();
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
        [STEMS_QUERY_KEYS.typeCode]: null,
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
          data: { ...listRow(500), typeDistribution: [typeSummary('N', 'اسم', 5)] },
          message: null,
          errors: null,
        }),
      ),
      getStemWords: vi.fn(),
      getStemAyahMatches: vi.fn(),
      getStemMentionedSurahs: vi.fn().mockReturnValue(
        of<ApiResponse<StemSurahsDto>>({
          isSuccess: true,
          data: { surahs: [{ surahNumber: 1, nameArabic: 'سورة-اختبار', occurrencesInSurah: 2 }] },
          message: null,
          errors: null,
        }),
      ),
      getStemMissingSurahs: vi.fn().mockReturnValue(
        of<ApiResponse<StemMissingSurahsDto>>({
          isSuccess: true,
          data: { surahs: [] },
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
        { provide: WordsAssociationOptionsService, useValue: STUB_ASSOCIATION_OPTIONS },
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
        [STEMS_QUERY_KEYS.typeCode]: null,
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
        [STEMS_QUERY_KEYS.typeCode]: null,
      }),
      queryParamsHandling: 'merge',
    });
    expect(TestBed.inject(StemsDetailFacade).selectedStemId()).toBeNull();
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

describe('StemsExplorerPageComponent US8 — restore and navigate exact state', () => {
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
          data: { ...listRow(500), typeDistribution: [typeSummary('N', 'اسم', 5)] },
          message: null,
          errors: null,
        }),
      ),
      getStemWords: vi.fn().mockImplementation(() => successWordsResponse()),
      getStemAyahMatches: vi.fn(),
      getStemMentionedSurahs: vi.fn(),
      getStemMissingSurahs: vi.fn(),
      getStemLemmas: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [StemsExplorerPageComponent],
      providers: [
        provideRouter([{ path: 'stems', component: StemsExplorerPageComponent }]),
        { provide: StemsApi, useValue: stemsApi },
        { provide: WordsAssociationOptionsService, useValue: STUB_ASSOCIATION_OPTIONS },
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

  it('restores a copied deep link (refresh): loads summary then the active words view and sub-view', async () => {
    queryParamMap$.next(
      convertToParamMap({ stem: '500', view: 'words', wordView: 'tashkeel', detailPage: '2' }),
    );
    const fixture = await initLifecycle();

    expect(stemsApi.getStemSummary).toHaveBeenCalledWith(500);
    expect(stemsApi.getStemWords).toHaveBeenCalledWith(500, 'tashkeel', 2, STEM_DETAIL_PAGE_SIZE);

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('[data-testid="stems-words-view"]')).toBeTruthy();
    expect(TestBed.inject(StemsDetailFacade).view()).toBe('words');
    expect(TestBed.inject(StemsDetailFacade).wordView()).toBe('tashkeel');
  });

  it('ignores irrelevant query keys when restoring a deep link', async () => {
    queryParamMap$.next(
      convertToParamMap({
        stem: '500',
        view: 'words',
        wordView: 'simple',
        detailPage: '1',
        foo: 'bar',
        debug: '1',
        random: 'xyz',
      }),
    );
    const fixture = await initLifecycle();

    expect(stemsApi.getStemSummary).toHaveBeenCalledWith(500);
    expect(stemsApi.getStemWords).toHaveBeenCalledWith(500, 'simple', 1, STEM_DETAIL_PAGE_SIZE);
    expect(fixture.nativeElement.querySelector('[data-testid="stems-words-view"]')).toBeTruthy();
  });

  it('shows restored-not-found and keeps the catalogue intact when the identity is unknown (summary unsuccessful)', async () => {
    stemsApi.getStemSummary.mockReturnValue(
      of<ApiResponse<StemSummaryDto>>({
        isSuccess: false,
        data: null,
        message: 'غير موجود',
        errors: null,
      }),
    );
    queryParamMap$.next(convertToParamMap({ stem: '99999', view: 'words' }));
    const fixture = await initLifecycle();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(stemsApi.getStemSummary).toHaveBeenCalledWith(99999);
    expect(stemsApi.getStemWords).not.toHaveBeenCalled();
    expect(stemsApi.getStemsList).toHaveBeenCalled();

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('[data-testid="stems-restored-not-found"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="stem-details-not-found"]')).toBeTruthy();
    expect(root.querySelectorAll('[data-stem-tab]')).toHaveLength(0);
    expect(root.querySelector('qd-stems-table')).toBeTruthy();
  });

  it('shows restored-not-found on a repeated unknown identity and does not reload the list repeatedly', async () => {
    stemsApi.getStemSummary.mockReturnValue(
      of<ApiResponse<StemSummaryDto>>({
        isSuccess: false,
        data: null,
        message: 'غير موجود',
        errors: null,
      }),
    );
    queryParamMap$.next(convertToParamMap({ stem: '99999', view: 'words' }));
    const fixture = await initLifecycle();
    await fixture.whenStable();
    fixture.detectChanges();

    const listCallsBefore = stemsApi.getStemsList.mock.calls.length;
    queryParamMap$.next(convertToParamMap({ stem: '99999', view: 'words' }));
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[data-testid="stems-restored-not-found"]')).toBeTruthy();
    expect(stemsApi.getStemsList.mock.calls.length).toBe(listCallsBefore);
  });

  it('preserves a positive out-of-range detailPage and renders the controlled empty result', async () => {
    stemsApi.getStemWords.mockReturnValue(
      of<ApiResponse<PagedResultDto<StemWordItemDto>>>({
        isSuccess: true,
        data: { page: 99999, pageSize: STEM_DETAIL_PAGE_SIZE, totalCount: 0, items: [] },
        message: null,
        errors: null,
      }),
    );
    queryParamMap$.next(
      convertToParamMap({ stem: '500', view: 'words', wordView: 'simple', detailPage: '99999' }),
    );
    const fixture = await initLifecycle();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(stemsApi.getStemWords).toHaveBeenCalledWith(500, 'simple', 99999, STEM_DETAIL_PAGE_SIZE);
    expect(TestBed.inject(StemsDetailFacade).status()).toBe('empty');
    expect(fixture.nativeElement.querySelector('[data-testid="stems-panel-empty"]')).toBeTruthy();
  });

  it('clears the panel via onClearSelection while preserving list state (search/sort/page untouched)', async () => {
    queryParamMap$.next(
      convertToParamMap({
        stem: '500',
        view: 'words',
        wordView: 'simple',
        detailPage: '1',
        page: '2',
        search: 'أصل',
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
    expect(lastArgs.queryParams[STEMS_QUERY_KEYS.stem]).toBeNull();
    expect(lastArgs.queryParams[STEMS_QUERY_KEYS.view]).toBeNull();
    expect(lastArgs.queryParams[STEMS_QUERY_KEYS.column]).toBeNull();
    expect(lastArgs.queryParams[STEMS_QUERY_KEYS.wordView]).toBeNull();
    expect(lastArgs.queryParams[STEMS_QUERY_KEYS.detailPage]).toBeNull();
    expect(lastArgs.queryParams[STEMS_QUERY_KEYS.typeCode]).toBeNull();
    expect(lastArgs.queryParams[STEMS_QUERY_KEYS.search]).toBeUndefined();
    expect(lastArgs.queryParams[STEMS_QUERY_KEYS.sort]).toBeUndefined();
    expect(lastArgs.queryParams[STEMS_QUERY_KEYS.page]).toBeUndefined();

    expect(TestBed.inject(StemsDetailFacade).selectedStemId()).toBeNull();
  });

  it('reuses cached view data for the same selection/view/page (no duplicate detail call)', async () => {
    queryParamMap$.next(
      convertToParamMap({ stem: '500', view: 'words', wordView: 'simple', detailPage: '1' }),
    );
    await initLifecycle();
    const wordsCallsAfterFirst = stemsApi.getStemWords.mock.calls.length;
    const summaryCallsAfterFirst = stemsApi.getStemSummary.mock.calls.length;

    queryParamMap$.next(
      convertToParamMap({ stem: '500', view: 'words', wordView: 'simple', detailPage: '1' }),
    );
    await new Promise((resolve) => setTimeout(resolve, 0));

    expect(stemsApi.getStemWords.mock.calls.length).toBe(wordsCallsAfterFirst);
    expect(stemsApi.getStemSummary.mock.calls.length).toBe(summaryCallsAfterFirst);
  });

  it('survives a back/forward sequence: select → clear → re-select restores the panel', async () => {
    const fixture = await initLifecycle();
    const detailFacade = TestBed.inject(StemsDetailFacade);

    queryParamMap$.next(
      convertToParamMap({ stem: '500', view: 'words', wordView: 'simple', detailPage: '1' }),
    );
    await fixture.whenStable();
    fixture.detectChanges();
    expect(detailFacade.selectedStemId()).toBe(500);

    queryParamMap$.next(convertToParamMap({ page: '1' }));
    await fixture.whenStable();
    fixture.detectChanges();
    expect(detailFacade.selectedStemId()).toBeNull();

    stemsApi.getStemAyahMatches.mockReturnValue(successAyahsResponse());
    queryParamMap$.next(convertToParamMap({ stem: '500', view: 'ayahs', detailPage: '1' }));
    await fixture.whenStable();
    fixture.detectChanges();

    expect(detailFacade.selectedStemId()).toBe(500);
    expect(detailFacade.view()).toBe('ayahs');
    expect(stemsApi.getStemAyahMatches).toHaveBeenCalledWith(500, 1, STEM_DETAIL_PAGE_SIZE, null);
  });

  it('maps a summary HTTP 404 to restored-not-found without surfacing a generic error', async () => {
    const http404 = new HttpErrorResponse({ status: 404, statusText: 'Not Found' });
    stemsApi.getStemSummary.mockReturnValue(throwError(() => http404));
    queryParamMap$.next(convertToParamMap({ stem: '424242', view: 'lemmas' }));
    const fixture = await initLifecycle();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[data-testid="stems-restored-not-found"]')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('[data-testid="stem-details-not-found"]')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('[data-testid="stems-panel-error"]')).toBeFalsy();
  });
});
