import { describe, expect, it, beforeEach, vi } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { BehaviorSubject, of } from 'rxjs';

import { ApiResponse } from '../../../../core/data-access/api-response.model';
import { ROOTS_PANEL_TAB_LABELS } from '../../models/roots.labels';
import { ROOT_VIEW_KEYS, RootAyahMatchDto, RootListItemViewModel } from '../../models/roots.models';
import { RootsApi } from '../../data-access/roots.api';
import { ROOTS_QUERY_KEYS } from '../../models/roots.models';
import { RootsDetailFacade } from '../../state/roots-detail.facade';
import { RootsExplorerFacade } from '../../state/roots-explorer.facade';
import { RootsExplorerPageComponent } from './roots-explorer-page.component';

function listRow(id: number): RootListItemViewModel {
  return {
    id,
    rootText: `جذر-${id}`,
    displayText: `جذر-${id}`,
    occurrencesCount: 5,
    ayahsCount: 3,
    surahsCount: 2,
    simpleWordsCount: 2,
    tashkeelWordsCount: 2,
    lemmasCount: 2,
    stemsCount: 1,
    firstVerseKey: '1:1',
  };
}

function ayahMatch(verseKey: string, matchedIds: number[]): RootAyahMatchDto {
  return {
    ayahId: verseKey === '1:1' ? 11 : 13,
    verseKey,
    surahNumber: 1,
    surahNameArabic: 'الفاتحة',
    ayahNumber: verseKey === '1:1' ? 1 : 3,
    pageNumber: 1,
    matchedQuranWordIds: matchedIds,
    words: [
      { quranWordId: 10, wordNumber: 1, textUthmani: 'ألف', isAyahMarker: false },
      { quranWordId: 11, wordNumber: 2, textUthmani: 'باء', isAyahMarker: false },
      { quranWordId: 12, wordNumber: 3, textUthmani: 'جيم', isAyahMarker: false },
    ],
  };
}

function successListResponse() {
  return of<ApiResponse<{ page: number; pageSize: number; totalCount: number; items: RootListItemViewModel[] }>>({
    isSuccess: true,
    data: { page: 1, pageSize: 1000, totalCount: 1, items: [listRow(10)] },
    message: null,
    errors: null,
  });
}

describe('RootsExplorerPageComponent US2', () => {
  let router: Router;
  let rootsApi: {
    getRootsList: ReturnType<typeof vi.fn>;
    getRootSummary: ReturnType<typeof vi.fn>;
    getRootWords: ReturnType<typeof vi.fn>;
    getRootAyahMatches: ReturnType<typeof vi.fn>;
    getRootMentionedSurahs: ReturnType<typeof vi.fn>;
    getRootMissingSurahs: ReturnType<typeof vi.fn>;
    getRootLemmas: ReturnType<typeof vi.fn>;
    getRootStems: ReturnType<typeof vi.fn>;
  };

  const queryParamMap$ = new BehaviorSubject(convertToParamMap({}));

  beforeEach(async () => {
    getTestBed().resetTestingModule();

    rootsApi = {
      getRootsList: vi.fn().mockImplementation(successListResponse),
      getRootSummary: vi.fn().mockReturnValue(
        of<ApiResponse<RootListItemViewModel>>({
          isSuccess: true,
          data: listRow(10),
          message: null,
          errors: null,
        }),
      ),
      getRootWords: vi.fn().mockReturnValue(
        of<ApiResponse<{ page: number; pageSize: number; totalCount: number; items: unknown[] }>>({
          isSuccess: true,
          data: { page: 1, pageSize: 100, totalCount: 0, items: [] },
          message: null,
          errors: null,
        }),
      ),
      getRootAyahMatches: vi.fn().mockReturnValue(
        of<ApiResponse<{ page: number; pageSize: number; totalCount: number; items: RootAyahMatchDto[] }>>({
          isSuccess: true,
          data: { page: 1, pageSize: 100, totalCount: 0, items: [] },
          message: null,
          errors: null,
        }),
      ),
      getRootMentionedSurahs: vi.fn(),
      getRootMissingSurahs: vi.fn(),
      getRootLemmas: vi.fn(),
      getRootStems: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [RootsExplorerPageComponent],
      providers: [
        provideRouter([{ path: 'roots', component: RootsExplorerPageComponent }]),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: RootsApi, useValue: rootsApi },
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

  async function initLifecycle(): Promise<ReturnType<typeof TestBed.createComponent<RootsExplorerPageComponent>>> {
    const fixture = TestBed.createComponent(RootsExplorerPageComponent);
    fixture.componentInstance.ngOnInit();
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
  }

  it('renders exactly the five named panel tabs and no overview tab', async () => {
    queryParamMap$.next(convertToParamMap({ root: '10', view: 'ayahs' }));
    const fixture = await initLifecycle();
    await fixture.whenStable();
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;

    for (const key of ROOT_VIEW_KEYS) {
      expect(root.querySelector(`[data-testid="root-details-tab-${key}"]`)?.textContent?.trim()).toBe(
        ROOTS_PANEL_TAB_LABELS[key],
      );
    }

    expect(root.textContent).not.toContain('نظرة عامة');
    expect(root.querySelectorAll('[data-root-tab]').length).toBe(5);
  });

  it('loads ayahs only when the ayahs tab is active from the URL', async () => {
    queryParamMap$.next(convertToParamMap({ root: '10', view: 'lemmas' }));
    await initLifecycle();

    expect(rootsApi.getRootAyahMatches).not.toHaveBeenCalled();
    expect(rootsApi.getRootSummary).toHaveBeenCalled();
  });

  it('restores ayahs from URL and highlights only matched ids', async () => {
    rootsApi.getRootSummary.mockReturnValue(
      of<ApiResponse<ReturnType<typeof listRow>>>({
        isSuccess: true,
        data: listRow(10),
        message: null,
        errors: null,
      }),
    );
    rootsApi.getRootAyahMatches.mockReturnValue(
      of<ApiResponse<{ page: number; pageSize: number; totalCount: number; items: RootAyahMatchDto[] }>>({
        isSuccess: true,
        data: {
          page: 1,
          pageSize: 100,
          totalCount: 1,
          items: [ayahMatch('1:1', [11])],
        },
        message: null,
        errors: null,
      }),
    );

    queryParamMap$.next(convertToParamMap({ root: '10', view: 'ayahs', detailPage: '1' }));
    const fixture = await initLifecycle();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(rootsApi.getRootAyahMatches).toHaveBeenCalledWith(10, 1, 100);
    expect(fixture.nativeElement.querySelector('[data-testid="roots-ayahs-view"]')).toBeTruthy();

    const matched = fixture.nativeElement.querySelector('.highlighted-ayah__word--matched');
    expect(matched).toBeTruthy();
    expect(fixture.nativeElement.querySelectorAll('.highlighted-ayah__word--matched')).toHaveLength(1);
  });

  it('panel scroll container is independent from the table region', async () => {
    queryParamMap$.next(convertToParamMap({ root: '10', view: 'ayahs' }));
    const fixture = await initLifecycle();
    await fixture.whenStable();
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;

    const panelSurface = root.querySelector('[data-testid="root-details-panel-surface"]') as HTMLElement | null;
    expect(panelSurface).toBeTruthy();

    const panelStyles = getComputedStyle(panelSurface!);
    expect(panelStyles.overflowY).toBe('auto');
  });

  it('maps occurrences count-click to ayahs view and triggers ayah load', async () => {
    const fixture = TestBed.createComponent(RootsExplorerPageComponent);
    const component = fixture.componentInstance;
    component.ngOnInit();
    await fixture.whenStable();

    component['onCountOpened']({ root: listRow(10), view: 'ayahs' });

    expect(router.navigate).toHaveBeenCalledWith([], {
      relativeTo: expect.anything(),
      queryParams: expect.objectContaining({
        [ROOTS_QUERY_KEYS.root]: '10',
        [ROOTS_QUERY_KEYS.view]: 'ayahs',
      }),
      queryParamsHandling: 'merge',
    });
    expect(TestBed.inject(RootsDetailFacade).view()).toBe('ayahs');
  });

  it('does not call detail APIs on table render without a selected root', async () => {
    await initLifecycle();

    expect(rootsApi.getRootsList).toHaveBeenCalled();
    expect(rootsApi.getRootSummary).not.toHaveBeenCalled();
    expect(rootsApi.getRootAyahMatches).not.toHaveBeenCalled();
    expect(rootsApi.getRootWords).not.toHaveBeenCalled();
    expect(rootsApi.getRootMentionedSurahs).not.toHaveBeenCalled();
    expect(rootsApi.getRootMissingSurahs).not.toHaveBeenCalled();
    expect(rootsApi.getRootLemmas).not.toHaveBeenCalled();
    expect(rootsApi.getRootStems).not.toHaveBeenCalled();
  });

  it('maps count-click to the correct view and sub-view URL params', async () => {
    const fixture = TestBed.createComponent(RootsExplorerPageComponent);
    const component = fixture.componentInstance;

    component['onCountOpened']({ root: listRow(10), view: 'words', wordView: 'tashkeel' });

    expect(router.navigate).toHaveBeenCalledWith([], {
      relativeTo: expect.anything(),
      queryParams: expect.objectContaining({
        [ROOTS_QUERY_KEYS.root]: '10',
        [ROOTS_QUERY_KEYS.view]: 'words',
        [ROOTS_QUERY_KEYS.wordView]: 'tashkeel',
        [ROOTS_QUERY_KEYS.detailPage]: null,
      }),
      queryParamsHandling: 'merge',
    });
  });

  it('maps simple-words count-click to wordView=simple', async () => {
    const fixture = TestBed.createComponent(RootsExplorerPageComponent);
    const component = fixture.componentInstance;

    component['onCountOpened']({ root: listRow(10), view: 'words', wordView: 'simple' });

    expect(router.navigate).toHaveBeenCalledWith([], {
      relativeTo: expect.anything(),
      queryParams: expect.objectContaining({
        [ROOTS_QUERY_KEYS.view]: 'words',
        [ROOTS_QUERY_KEYS.wordView]: 'simple',
      }),
      queryParamsHandling: 'merge',
    });
  });

  it('clears only selection params while preserving list context (FR-038)', () => {
    const fixture = TestBed.createComponent(RootsExplorerPageComponent);
    const component = fixture.componentInstance;

    component['onClearSelection']();

    expect(router.navigate).toHaveBeenCalledWith([], {
      relativeTo: expect.anything(),
      queryParams: {
        [ROOTS_QUERY_KEYS.root]: null,
        [ROOTS_QUERY_KEYS.view]: null,
        [ROOTS_QUERY_KEYS.wordView]: null,
        [ROOTS_QUERY_KEYS.surahView]: null,
        [ROOTS_QUERY_KEYS.detailPage]: null,
      },
      queryParamsHandling: 'merge',
    });
    expect(TestBed.inject(RootsDetailFacade).selectedRootId()).toBeNull();
  });

  it('restores list URL state from query params on bind', async () => {
    queryParamMap$.next(convertToParamMap({ search: 'رحم', sort: 'alpha', page: '2' }));

    await initLifecycle();

    const listFacade = TestBed.inject(RootsExplorerFacade);
    expect(listFacade.search()).toBe('رحم');
    expect(listFacade.sort()).toBe('alpha');
    expect(listFacade.page()).toBe(2);
  });
});

describe('RootsExplorerPageComponent US3', () => {
  let router: Router;
  let rootsApi: {
    getRootsList: ReturnType<typeof vi.fn>;
    getRootSummary: ReturnType<typeof vi.fn>;
    getRootWords: ReturnType<typeof vi.fn>;
    getRootAyahMatches: ReturnType<typeof vi.fn>;
    getRootMentionedSurahs: ReturnType<typeof vi.fn>;
    getRootMissingSurahs: ReturnType<typeof vi.fn>;
    getRootLemmas: ReturnType<typeof vi.fn>;
    getRootStems: ReturnType<typeof vi.fn>;
  };

  const queryParamMap$ = new BehaviorSubject(convertToParamMap({}));

  beforeEach(async () => {
    getTestBed().resetTestingModule();

    rootsApi = {
      getRootsList: vi.fn().mockImplementation(successListResponse),
      getRootSummary: vi.fn().mockReturnValue(
        of<ApiResponse<RootListItemViewModel>>({
          isSuccess: true,
          data: listRow(10),
          message: null,
          errors: null,
        }),
      ),
      getRootWords: vi.fn().mockReturnValue(
        of<ApiResponse<{ page: number; pageSize: number; totalCount: number; items: unknown[] }>>({
          isSuccess: true,
          data: { page: 1, pageSize: 100, totalCount: 1, items: [] },
          message: null,
          errors: null,
        }),
      ),
      getRootAyahMatches: vi.fn(),
      getRootMentionedSurahs: vi.fn(),
      getRootMissingSurahs: vi.fn(),
      getRootLemmas: vi.fn(),
      getRootStems: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [RootsExplorerPageComponent],
      providers: [
        provideRouter([{ path: 'roots', component: RootsExplorerPageComponent }]),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: RootsApi, useValue: rootsApi },
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

  async function initLifecycle(): Promise<ReturnType<typeof TestBed.createComponent<RootsExplorerPageComponent>>> {
    const fixture = TestBed.createComponent(RootsExplorerPageComponent);
    fixture.componentInstance.ngOnInit();
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
  }

  it('loads words only when the words tab and sub-view are active', async () => {
    queryParamMap$.next(convertToParamMap({ root: '10', view: 'words', wordView: 'simple' }));
    await initLifecycle();

    expect(rootsApi.getRootWords).toHaveBeenCalledWith(10, 'simple', 1, 100);
    expect(rootsApi.getRootAyahMatches).not.toHaveBeenCalled();
  });

  it('routes word sub-view changes through the URL', async () => {
    const fixture = TestBed.createComponent(RootsExplorerPageComponent);
    const component = fixture.componentInstance;
    component.ngOnInit();

    component['onWordViewChange']('tashkeel');

    expect(router.navigate).toHaveBeenCalledWith([], {
      relativeTo: expect.anything(),
      queryParams: expect.objectContaining({
        [ROOTS_QUERY_KEYS.view]: 'words',
        [ROOTS_QUERY_KEYS.wordView]: 'tashkeel',
        [ROOTS_QUERY_KEYS.detailPage]: null,
      }),
      queryParamsHandling: 'merge',
    });
  });
});

describe('RootsExplorerPageComponent US4', () => {
  let router: Router;
  let rootsApi: {
    getRootsList: ReturnType<typeof vi.fn>;
    getRootSummary: ReturnType<typeof vi.fn>;
    getRootWords: ReturnType<typeof vi.fn>;
    getRootAyahMatches: ReturnType<typeof vi.fn>;
    getRootMentionedSurahs: ReturnType<typeof vi.fn>;
    getRootMissingSurahs: ReturnType<typeof vi.fn>;
    getRootLemmas: ReturnType<typeof vi.fn>;
    getRootStems: ReturnType<typeof vi.fn>;
  };

  const queryParamMap$ = new BehaviorSubject(convertToParamMap({}));

  beforeEach(async () => {
    getTestBed().resetTestingModule();

    rootsApi = {
      getRootsList: vi.fn().mockImplementation(successListResponse),
      getRootSummary: vi.fn().mockReturnValue(
        of<ApiResponse<RootListItemViewModel>>({
          isSuccess: true,
          data: listRow(10),
          message: null,
          errors: null,
        }),
      ),
      getRootWords: vi.fn(),
      getRootAyahMatches: vi.fn(),
      getRootMentionedSurahs: vi.fn().mockReturnValue(
        of<ApiResponse<{ id: number; rootText: string; surahsCount: number; surahs: unknown[] }>>({
          isSuccess: true,
          data: { id: 10, rootText: 'جذر-10', surahsCount: 1, surahs: [{ surahNumber: 1, nameArabic: 'سورة-اختبار', occurrencesInSurah: 2 }] },
          message: null,
          errors: null,
        }),
      ),
      getRootMissingSurahs: vi.fn().mockReturnValue(
        of<ApiResponse<{ id: number; rootText: string; missingSurahsCount: number; surahs: unknown[] }>>({
          isSuccess: true,
          data: { id: 10, rootText: 'جذر-10', missingSurahsCount: 0, surahs: [] },
          message: null,
          errors: null,
        }),
      ),
      getRootLemmas: vi.fn(),
      getRootStems: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [RootsExplorerPageComponent],
      providers: [
        provideRouter([{ path: 'roots', component: RootsExplorerPageComponent }]),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: RootsApi, useValue: rootsApi },
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

  async function initLifecycle(): Promise<ReturnType<typeof TestBed.createComponent<RootsExplorerPageComponent>>> {
    const fixture = TestBed.createComponent(RootsExplorerPageComponent);
    fixture.componentInstance.ngOnInit();
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
  }

  it('loads mentioned surahs whole when surahView=mentioned', async () => {
    queryParamMap$.next(convertToParamMap({ root: '10', view: 'surahs', surahView: 'mentioned' }));
    const fixture = await initLifecycle();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(rootsApi.getRootMentionedSurahs).toHaveBeenCalledWith(10);
    expect(fixture.nativeElement.querySelector('[data-testid="roots-mentioned-surahs-view"]')).toBeTruthy();
  });

  it('routes surah sub-view changes through the URL and loads missing whole', async () => {
    queryParamMap$.next(convertToParamMap({ root: '10', view: 'surahs', surahView: 'missing' }));
    await initLifecycle();

    expect(rootsApi.getRootMissingSurahs).toHaveBeenCalledWith(10);

    const fixture = TestBed.createComponent(RootsExplorerPageComponent);
    const component = fixture.componentInstance;
    component.ngOnInit();
    component['onSurahViewChange']('mentioned');

    expect(router.navigate).toHaveBeenCalledWith([], {
      relativeTo: expect.anything(),
      queryParams: expect.objectContaining({
        [ROOTS_QUERY_KEYS.view]: 'surahs',
        [ROOTS_QUERY_KEYS.surahView]: 'mentioned',
      }),
      queryParamsHandling: 'merge',
    });
  });

  it('renders empty missing-surahs state cleanly', async () => {
    queryParamMap$.next(convertToParamMap({ root: '10', view: 'surahs', surahView: 'missing' }));
    const fixture = await initLifecycle();
    await fixture.whenStable();
    fixture.detectChanges();

    const detailFacade = TestBed.inject(RootsDetailFacade);
    expect(detailFacade.status()).toBe('empty');
    expect(fixture.nativeElement.querySelector('[data-testid="roots-panel-empty"]')).toBeTruthy();
  });
});

describe('RootsExplorerPageComponent US5', () => {
  let rootsApi: {
    getRootsList: ReturnType<typeof vi.fn>;
    getRootSummary: ReturnType<typeof vi.fn>;
    getRootWords: ReturnType<typeof vi.fn>;
    getRootAyahMatches: ReturnType<typeof vi.fn>;
    getRootMentionedSurahs: ReturnType<typeof vi.fn>;
    getRootMissingSurahs: ReturnType<typeof vi.fn>;
    getRootLemmas: ReturnType<typeof vi.fn>;
    getRootStems: ReturnType<typeof vi.fn>;
  };

  const queryParamMap$ = new BehaviorSubject(convertToParamMap({}));

  beforeEach(async () => {
    getTestBed().resetTestingModule();

    rootsApi = {
      getRootsList: vi.fn().mockImplementation(successListResponse),
      getRootSummary: vi.fn().mockReturnValue(
        of<ApiResponse<RootListItemViewModel>>({
          isSuccess: true,
          data: listRow(10),
          message: null,
          errors: null,
        }),
      ),
      getRootWords: vi.fn(),
      getRootAyahMatches: vi.fn(),
      getRootMentionedSurahs: vi.fn(),
      getRootMissingSurahs: vi.fn(),
      getRootLemmas: vi.fn().mockReturnValue(
        of<ApiResponse<{ id: number; rootText: string; lemmasCount: number; lemmas: unknown[] }>>({
          isSuccess: true,
          data: {
            id: 10,
            rootText: 'جذر-10',
            lemmasCount: 1,
            lemmas: [{ lemmaId: 100, lemmaText: 'صيغة-اختبار', occurrencesCount: 2 }],
          },
          message: null,
          errors: null,
        }),
      ),
      getRootStems: vi.fn().mockReturnValue(
        of<ApiResponse<{ id: number; rootText: string; stemsCount: number; stems: unknown[] }>>({
          isSuccess: true,
          data: {
            id: 10,
            rootText: 'جذر-10',
            stemsCount: 1,
            stems: [{ stemId: 200, stemText: 'أصل-اختبار', occurrencesCount: 1 }],
          },
          message: null,
          errors: null,
        }),
      ),
    };

    await TestBed.configureTestingModule({
      imports: [RootsExplorerPageComponent],
      providers: [
        provideRouter([{ path: 'roots', component: RootsExplorerPageComponent }]),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: RootsApi, useValue: rootsApi },
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

    queryParamMap$.next(convertToParamMap({}));
  });

  async function initLifecycle(): Promise<ReturnType<typeof TestBed.createComponent<RootsExplorerPageComponent>>> {
    const fixture = TestBed.createComponent(RootsExplorerPageComponent);
    fixture.componentInstance.ngOnInit();
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
  }

  it('renders lemmas and stems as non-interactive lists with counts', async () => {
    queryParamMap$.next(convertToParamMap({ root: '10', view: 'lemmas' }));
    const lemmasFixture = await initLifecycle();
    await lemmasFixture.whenStable();
    lemmasFixture.detectChanges();

    const lemmasRoot = lemmasFixture.nativeElement as HTMLElement;
    expect(lemmasRoot.querySelector('[data-testid="roots-lemmas-view"]')).toBeTruthy();
    expect(lemmasRoot.querySelector('[data-testid="root-lemma-item"]')).toBeTruthy();
    expect(lemmasRoot.querySelector('[data-testid="roots-lemmas-view"] button')).toBeNull();

    getTestBed().resetTestingModule();
    queryParamMap$.next(convertToParamMap({ root: '10', view: 'stems' }));

    rootsApi = {
      getRootsList: vi.fn().mockImplementation(successListResponse),
      getRootSummary: vi.fn().mockReturnValue(
        of<ApiResponse<RootListItemViewModel>>({
          isSuccess: true,
          data: listRow(10),
          message: null,
          errors: null,
        }),
      ),
      getRootWords: vi.fn(),
      getRootAyahMatches: vi.fn(),
      getRootMentionedSurahs: vi.fn(),
      getRootMissingSurahs: vi.fn(),
      getRootLemmas: vi.fn(),
      getRootStems: vi.fn().mockReturnValue(
        of<ApiResponse<{ id: number; rootText: string; stemsCount: number; stems: unknown[] }>>({
          isSuccess: true,
          data: {
            id: 10,
            rootText: 'جذر-10',
            stemsCount: 1,
            stems: [{ stemId: 200, stemText: 'أصل-اختبار', occurrencesCount: 1 }],
          },
          message: null,
          errors: null,
        }),
      ),
    };

    await TestBed.configureTestingModule({
      imports: [RootsExplorerPageComponent],
      providers: [
        provideRouter([{ path: 'roots', component: RootsExplorerPageComponent }]),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: RootsApi, useValue: rootsApi },
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

    const stemsFixture = TestBed.createComponent(RootsExplorerPageComponent);
    stemsFixture.componentInstance.ngOnInit();
    await stemsFixture.whenStable();
    stemsFixture.detectChanges();

    const stemsRoot = stemsFixture.nativeElement as HTMLElement;
    expect(stemsRoot.querySelector('[data-testid="roots-stems-view"]')).toBeTruthy();
    expect(stemsRoot.querySelector('[data-testid="root-stem-item"]')).toBeTruthy();
    expect(stemsRoot.querySelector('[data-testid="roots-stems-view"] a')).toBeNull();
  });
});

describe('RootsExplorerPageComponent state matrix (T073)', () => {
  let rootsApi: {
    getRootsList: ReturnType<typeof vi.fn>;
    getRootSummary: ReturnType<typeof vi.fn>;
    getRootWords: ReturnType<typeof vi.fn>;
    getRootAyahMatches: ReturnType<typeof vi.fn>;
    getRootMentionedSurahs: ReturnType<typeof vi.fn>;
    getRootMissingSurahs: ReturnType<typeof vi.fn>;
    getRootLemmas: ReturnType<typeof vi.fn>;
    getRootStems: ReturnType<typeof vi.fn>;
  };

  const queryParamMap$ = new BehaviorSubject(convertToParamMap({}));

  function configure(): void {
    getTestBed().resetTestingModule();
    TestBed.configureTestingModule({
      imports: [RootsExplorerPageComponent],
      providers: [
        provideRouter([{ path: 'roots', component: RootsExplorerPageComponent }]),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: RootsApi, useValue: rootsApi },
        {
          provide: ActivatedRoute,
          useValue: {
            paramMap: of(convertToParamMap({})),
            queryParamMap: queryParamMap$.asObservable(),
          },
        },
      ],
      teardown: { destroyAfterEach: true },
    });
    const router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);
  }

  async function initLifecycle(): Promise<ReturnType<typeof TestBed.createComponent<RootsExplorerPageComponent>>> {
    const fixture = TestBed.createComponent(RootsExplorerPageComponent);
    fixture.componentInstance.ngOnInit();
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => {
    rootsApi = {
      getRootsList: vi.fn().mockImplementation(successListResponse),
      getRootSummary: vi.fn().mockReturnValue(
        of<ApiResponse<RootListItemViewModel>>({ isSuccess: true, data: listRow(10), message: null, errors: null }),
      ),
      getRootWords: vi.fn(),
      getRootAyahMatches: vi.fn(),
      getRootMentionedSurahs: vi.fn(),
      getRootMissingSurahs: vi.fn(),
      getRootLemmas: vi.fn(),
      getRootStems: vi.fn(),
    };
    queryParamMap$.next(convertToParamMap({}));
  });

  it('renders the panel error state when a detail load fails', async () => {
    rootsApi.getRootAyahMatches.mockReturnValue(
      of<ApiResponse<unknown>>({ isSuccess: false, data: null, message: 'تعذّر تحميل التفاصيل', errors: null }),
    );
    configure();

    queryParamMap$.next(convertToParamMap({ root: '10', view: 'ayahs' }));
    const fixture = await initLifecycle();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(TestBed.inject(RootsDetailFacade).status()).toBe('error');
    expect(fixture.nativeElement.querySelector('[data-testid="roots-panel-error"]')).toBeTruthy();
  });

  it('shows controlled not-found for an unknown root while the table stays usable', async () => {
    rootsApi.getRootSummary.mockReturnValue(
      of<ApiResponse<RootListItemViewModel | null>>({
        isSuccess: false,
        data: null,
        message: 'الجذر غير موجود',
        errors: null,
      }),
    );
    configure();

    queryParamMap$.next(convertToParamMap({ root: '999', view: 'ayahs' }));
    const fixture = await initLifecycle();
    await fixture.whenStable();
    fixture.detectChanges();

    const host = fixture.nativeElement as HTMLElement;
    expect(TestBed.inject(RootsDetailFacade).status()).toBe('notFound');
    expect(host.querySelector('[data-testid="roots-restored-not-found"]')).toBeTruthy();
    // The table remains rendered and usable alongside the not-found message.
    expect(host.querySelector('qd-roots-table')).toBeTruthy();
    expect(host.querySelector('[data-testid="roots-table-root-button"]')).toBeTruthy();
  });

  it('shows the no-results list state for an unmatched search', async () => {
    rootsApi.getRootsList.mockReturnValue(
      of<ApiResponse<{ page: number; pageSize: number; totalCount: number; items: RootListItemViewModel[] }>>({
        isSuccess: true,
        data: { page: 1, pageSize: 1000, totalCount: 0, items: [] },
        message: null,
        errors: null,
      }),
    );
    configure();

    queryParamMap$.next(convertToParamMap({ search: 'لا-تطابق' }));
    const fixture = await initLifecycle();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(TestBed.inject(RootsExplorerFacade).status()).toBe('empty');
    expect(fixture.nativeElement.querySelector('[data-testid="roots-list-no-results"]')).toBeTruthy();
  });
});
