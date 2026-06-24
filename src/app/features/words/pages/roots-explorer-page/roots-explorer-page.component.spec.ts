import { describe, expect, it, beforeEach, vi } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { BehaviorSubject, of } from 'rxjs';

import { ApiResponse } from '../../../../core/data-access/api-response.model';
import { RootsApi } from '../../data-access/roots.api';
import { RootListItemViewModel } from '../../models/roots.models';
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

function successListResponse() {
  return of<ApiResponse<{ page: number; pageSize: number; totalCount: number; items: RootListItemViewModel[] }>>({
    isSuccess: true,
    data: { page: 1, pageSize: 1000, totalCount: 1, items: [listRow(10)] },
    message: null,
    errors: null,
  });
}

describe('RootsExplorerPageComponent', () => {
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
      getRootSummary: vi.fn(),
      getRootWords: vi.fn(),
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

  function createComponentInstance(): RootsExplorerPageComponent {
    const fixture = TestBed.createComponent(RootsExplorerPageComponent);
    return fixture.componentInstance;
  }

  async function initListLifecycle(): Promise<RootsExplorerPageComponent> {
    const fixture = TestBed.createComponent(RootsExplorerPageComponent);
    const component = fixture.componentInstance;
    component.ngOnInit();
    await fixture.whenStable();
    return component;
  }

  it('does not call any detail API endpoint on table render', async () => {
    await initListLifecycle();

    expect(rootsApi.getRootsList).toHaveBeenCalled();
    expect(rootsApi.getRootSummary).not.toHaveBeenCalled();
    expect(rootsApi.getRootWords).not.toHaveBeenCalled();
    expect(rootsApi.getRootAyahMatches).not.toHaveBeenCalled();
    expect(rootsApi.getRootMentionedSurahs).not.toHaveBeenCalled();
    expect(rootsApi.getRootMissingSurahs).not.toHaveBeenCalled();
    expect(rootsApi.getRootLemmas).not.toHaveBeenCalled();
    expect(rootsApi.getRootStems).not.toHaveBeenCalled();
  });

  it('maps count-click to the correct view and sub-view URL params', async () => {
    const component = createComponentInstance();
    const row = listRow(10);

    component['onCountOpened']({ root: row, view: 'words', wordView: 'tashkeel' });

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
    const component = createComponentInstance();
    const row = listRow(10);

    component['onCountOpened']({ root: row, view: 'words', wordView: 'simple' });

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
    const component = createComponentInstance();

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

    await initListLifecycle();

    const listFacade = TestBed.inject(RootsExplorerFacade);
    expect(listFacade.search()).toBe('رحم');
    expect(listFacade.sort()).toBe('alpha');
    expect(listFacade.page()).toBe(2);
  });
});
