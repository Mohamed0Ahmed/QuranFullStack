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
      getRootWords: vi.fn(),
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
