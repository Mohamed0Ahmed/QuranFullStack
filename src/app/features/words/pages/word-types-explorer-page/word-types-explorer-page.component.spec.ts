import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { getTestBed, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { By } from '@angular/platform-browser';
import { BehaviorSubject, of } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { ApiResponse } from '../../../../core/data-access/api-response.model';
import { WordTypesApi } from '../../data-access/word-types.api';
import { ADDITIONAL_ACTIVE_HUB_SECTIONS } from '../../models/unique-words.labels';
import { WORD_TYPE_SORT_OPTIONS, WORD_TYPES_SORT_LABEL } from '../../models/word-types.labels';
import { PagedResultDto, WordTypeAyahMatchDto, WordTypeRowDto, WordTypeTreeDto } from '../../models/word-types.models';
import { WordTypesTableComponent } from '../../components/word-types-table/word-types-table.component';
import { WordTypesDetailFacade } from '../../state/word-types-detail.facade';
import { WordTypesExplorerFacade } from '../../state/word-types-explorer.facade';
import { WordTypesExplorerPageComponent } from './word-types-explorer-page.component';

const queryParamMap$ = new BehaviorSubject(convertToParamMap({}));

const tree: WordTypeTreeDto = {
  mainTypes: [
    { code: 'noun', label: { ar: 'اسم' }, count: 1, secondaryFilter: { kind: 'case', options: [], voiceOptions: [] }, children: [] },
    { code: 'verb', label: { ar: 'فعل' }, count: 1, secondaryFilter: { kind: 'tense+voice', options: [], voiceOptions: [] }, children: [] },
    { code: 'particle', label: { ar: 'حرف وأداة' }, count: 1, secondaryFilter: { kind: 'none', options: [], voiceOptions: [] }, children: [] },
    { code: 'inl', label: { ar: 'حروف مقطّعة' }, count: 1, secondaryFilter: { kind: 'none', options: [], voiceOptions: [] }, children: [] },
  ],
};

const row: WordTypeRowDto = {
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

const properRow: WordTypeRowDto = {
  ...row,
  contextCode: 'PN',
  typeCode: 'PN',
  typeLabel: { ar: 'اسم علم' },
  broadLabel: { ar: 'اسم' },
};

const ayahMatch: WordTypeAyahMatchDto = {
  verseKey: '1:1',
  surahNumber: 1,
  ayahNumber: 1,
  ayahText: 'AYAH_TEXT_PLACEHOLDER',
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

describe('WordTypesExplorerPageComponent', () => {
  let api: {
    getTree: ReturnType<typeof vi.fn>;
    getRows: ReturnType<typeof vi.fn>;
    getSummary: ReturnType<typeof vi.fn>;
    getAyahMatches: ReturnType<typeof vi.fn>;
    getSurahs: ReturnType<typeof vi.fn>;
  };
  let router: Router;

  beforeEach(async () => {
    getTestBed().resetTestingModule();
    api = {
      getTree: vi.fn().mockReturnValue(of(ok(tree))),
      getRows: vi.fn().mockReturnValue(of(ok<PagedResultDto<WordTypeRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [row] }))),
      getSummary: vi.fn().mockReturnValue(of(ok(properRow))),
      getAyahMatches: vi.fn(),
      getSurahs: vi.fn().mockReturnValue(of(ok({ surahs: [{ surahNumber: 1, nameArabic: 'الفاتحة', occurrencesCount: 1 }], missingSurahs: [] }))),
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

  it('defaults route state to type=noun, loads tree and rows, and does not call details eagerly', async () => {
    const fixture = await createPage();

    expect(api.getRows).toHaveBeenCalledWith(expect.objectContaining({ type: 'noun', page: 1, pageSize: 25 }));
    expect(TestBed.inject(WordTypesExplorerFacade).listState().status).toBe('success');
    expect(TestBed.inject(WordTypesDetailFacade).panelState().summary).toBeNull();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('كَلِمَة');
  });

  it('renders sort label and options from centralized labels', async () => {
    const fixture = await createPage();
    const root = fixture.nativeElement as HTMLElement;
    const sortSelect = root.querySelector('.word-types-page__sort select') as HTMLSelectElement;
    const optionLabels = Array.from(sortSelect.querySelectorAll('option')).map((option) => option.textContent?.trim());

    expect(root.textContent).toContain(WORD_TYPES_SORT_LABEL);
    expect(optionLabels).toEqual(WORD_TYPE_SORT_OPTIONS.map((option) => option.label));
  });

  it('routes main type selection and clears selected row state', async () => {
    const fixture = await createPage();
    const buttons = fixture.nativeElement.querySelectorAll('qd-word-type-filter button') as NodeListOf<HTMLButtonElement>;

    buttons[1].click();

    expect(router.navigate).toHaveBeenCalledWith([], expect.objectContaining({
      queryParams: expect.objectContaining({ type: 'verb', page: '1', word: null, contextCode: null }),
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
    api.getRows.mockReturnValueOnce(of(ok<PagedResultDto<WordTypeRowDto>>({ page: 1, pageSize: 25, totalCount: 0, items: [] })));
    queryParamMap$.next(convertToParamMap({ page: '2' }));
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
            getRows: vi.fn().mockReturnValue(of({ isSuccess: false, data: null, message: 'تعذّر تحميل أنواع الكلمات', errors: null })),
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
    api.getRows.mockReturnValueOnce(of(ok<PagedResultDto<WordTypeRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [properRow] })));
    queryParamMap$.next(convertToParamMap({
      type: 'noun',
      page: '1',
      word: '191001',
      contextCode: 'PN',
      view: 'surahs',
      detailPage: '1',
      column: 'analysis',
    }));

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

    queryParamMap$.next(convertToParamMap({ type: 'noun', page: '1' }));
    await fixture.whenStable();
    fixture.detectChanges();

    expect(detailFacade.panelState().selectedRow).toBeNull();
    expect(detailFacade.panelState().status).toBe('idle');
    expect(explorerFacade.listState().status).toBe('success');
  });

  it('reloads summary when active feature changes for the same restored word context', async () => {
    api.getRows.mockReturnValue(of(ok<PagedResultDto<WordTypeRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [properRow] })));
    queryParamMap$.next(convertToParamMap({
      type: 'noun',
      case: 'genitive',
      page: '1',
      word: '191001',
      contextCode: 'PN',
      view: 'surahs',
    }));

    const fixture = await createPage();

    expect(api.getSummary).toHaveBeenLastCalledWith(expect.objectContaining({ case: 'genitive' }));

    queryParamMap$.next(convertToParamMap({
      type: 'noun',
      case: 'nominative',
      page: '1',
      word: '191001',
      contextCode: 'PN',
      view: 'surahs',
    }));
    await fixture.whenStable();
    fixture.detectChanges();

    expect(api.getSummary).toHaveBeenCalledTimes(2);
    expect(api.getSummary).toHaveBeenLastCalledWith(expect.objectContaining({ case: 'nominative' }));
  });

  it('routes a matched ayah occurrence to the analysis view with its exact location', async () => {
    api.getRows.mockReturnValueOnce(of(ok<PagedResultDto<WordTypeRowDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [properRow] })));
    api.getAyahMatches.mockReturnValueOnce(of(ok<PagedResultDto<WordTypeAyahMatchDto>>({ page: 1, pageSize: 25, totalCount: 1, items: [ayahMatch] })));
    queryParamMap$.next(convertToParamMap({
      type: 'noun',
      page: '1',
      word: '191001',
      contextCode: 'PN',
      view: 'ayahs',
      detailPage: '1',
    }));

    const fixture = await createPage();
    const analysisButton = fixture.nativeElement.querySelector('[data-testid="ayah-match-analysis"]') as HTMLButtonElement;

    analysisButton.click();

    expect(router.navigate).toHaveBeenCalledWith([], expect.objectContaining({
      queryParams: expect.objectContaining({
        view: 'analysis',
        detailPage: '1',
        location: '1:1:2',
        column: 'analysis',
      }),
      queryParamsHandling: 'merge',
    }));
  });

  it('returns focus to selected row after selection clears', async () => {
    queryParamMap$.next(convertToParamMap({
      type: 'noun',
      page: '1',
      word: '191001',
      contextCode: 'N',
      view: 'ayahs',
    }));

    const fixture = await createPage();
    const table = fixture.debugElement.query(By.directive(WordTypesTableComponent)).componentInstance as WordTypesTableComponent;
    const focusSpy = vi.spyOn(table, 'focusRow');

    const closeButton = fixture.nativeElement.querySelector('[data-testid="word-type-details-panel-close"]') as HTMLButtonElement;

    closeButton.click();

    expect(focusSpy).toHaveBeenCalledWith(row);
  });

  it('renders a controlled not-found panel for missing restored rows while leaving the table active', async () => {
    api.getSummary.mockReturnValueOnce(of({ isSuccess: false, data: null, message: 'غير موجود', errors: null }));
    queryParamMap$.next(convertToParamMap({
      type: 'noun',
      page: '1',
      word: '999999',
      contextCode: 'PN',
      view: 'ayahs',
      detailPage: '1',
    }));

    const fixture = await createPage();
    const detailFacade = TestBed.inject(WordTypesDetailFacade);
    const explorerFacade = TestBed.inject(WordTypesExplorerFacade);

    expect(detailFacade.panelState().status).toBe('notFound');
    expect(detailFacade.panelState().selectedRow?.tashkeelWordId).toBe(999999);
    expect(explorerFacade.listState().status).toBe('success');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('كَلِمَة');
  });

  it('exposes the Words hub access route for Word Types', () => {
    expect(ADDITIONAL_ACTIVE_HUB_SECTIONS).toContainEqual(expect.objectContaining({
      labelAr: 'أنواع الكلمات',
      route: '/dashboard/words/types',
    }));
  });
});
