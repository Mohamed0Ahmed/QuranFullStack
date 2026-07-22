import { ComponentFixture, getTestBed, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { of } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { DetailOverlayHistoryService } from '../../../../core/navigation/detail-overlay/detail-overlay-history.service';
import { LemmaDetailFrame } from '../../../../core/navigation/detail-overlay/detail-overlay.models';
import { ApiResponse } from '../../../../core/data-access/api-response.model';
import { PaginationComponent } from '../../../../shared/ui/pagination/pagination.component';
import { LemmaAyahTypeFiltersComponent } from '../../components/lemma-ayah-type-filters/lemma-ayah-type-filters.component';
import { LemmasApi } from '../../data-access/lemmas.api';
import {
  LemmaAyahMatchDto,
  LemmaSummaryDto,
  LemmaWordItemDto,
  PagedResultDto,
} from '../../models/lemmas.models';
import { LemmasDetailFacade } from '../../state/lemmas-detail.facade';
import { EntityDetailOverlayTitleStore } from '../entity-detail-overlay-title.store';
import { LemmaDetailOverlayAdapterComponent } from './lemma-detail-overlay-adapter.component';

const SYNTHETIC_WORD_TEXT = 'كلمة-اختبار';

const LEMMA_SUMMARY: LemmaSummaryDto = {
  id: 999,
  lemmaText: 'كِتَاب',
  occurrencesCount: 5,
  ayahsCount: 4,
  surahsCount: 3,
  simpleWordsCount: 2,
  tashkeelWordsCount: 2,
  stemsCount: 1,
  rootId: null,
  rootText: null,
  typeDistribution: [
    { code: 'N', arabicLabel: 'اسم', occurrencesCount: 3 },
    { code: 'V', arabicLabel: 'فعل', occurrencesCount: 2 },
  ],
};

const WORDS_PAGE: PagedResultDto<LemmaWordItemDto> = {
  page: 1,
  pageSize: 100,
  totalCount: 2,
  items: [
    { displayText: 'كتاب', occurrencesCount: 3, uniqueWordId: 11 },
    { displayText: 'كتابا', occurrencesCount: 2, uniqueWordId: 12 },
  ],
};

const AYAHS_PAGE: PagedResultDto<LemmaAyahMatchDto> = {
  page: 1,
  pageSize: 100,
  totalCount: 250,
  items: [
    {
      ayahId: 1,
      pageNumber: 2,
      surahNameArabic: 'البقرة',
      verseKey: '2:2',
      words: [{ textUthmani: SYNTHETIC_WORD_TEXT, isMatched: true }],
    },
  ],
};

function ok<T>(data: T): ApiResponse<T> {
  return { isSuccess: true, data, message: null, errors: null };
}

function frameOf(overrides: Partial<LemmaDetailFrame> = {}): LemmaDetailFrame {
  return {
    kind: 'lemma',
    id: 999,
    view: 'words',
    wordView: 'simple',
    surahView: 'mentioned',
    detailPage: 1,
    typeCode: null,
    ...overrides,
  };
}

describe('LemmaDetailOverlayAdapterComponent (Feature 029 B4)', () => {
  let replaceTopFrame: ReturnType<typeof vi.fn>;
  let apiStub: {
    getLemmaSummary: ReturnType<typeof vi.fn>;
    getLemmaWords: ReturnType<typeof vi.fn>;
    getLemmaAyahMatches: ReturnType<typeof vi.fn>;
    getLemmaMentionedSurahs: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    replaceTopFrame = vi.fn();
    apiStub = {
      getLemmaSummary: vi.fn(() => of(ok(LEMMA_SUMMARY))),
      getLemmaWords: vi.fn(() => of(ok(WORDS_PAGE))),
      getLemmaAyahMatches: vi.fn(() => of(ok(AYAHS_PAGE))),
      getLemmaMentionedSurahs: vi.fn(() => of(ok({ surahs: [] }))),
    };

    TestBed.configureTestingModule({
      imports: [LemmaDetailOverlayAdapterComponent],
      providers: [
        EntityDetailOverlayTitleStore,
        { provide: LemmasApi, useValue: apiStub },
        { provide: DetailOverlayHistoryService, useValue: { replaceTopFrame, urlEpoch: () => 0, buildFrameHref: () => '/SYNTH_OVERLAY_HREF', buildBaseWithOverlayHref: () => '/SYNTH_AYAH_HREF', navigateBaseWithOverlay: () => undefined } },
      ],
      teardown: { destroyAfterEach: true },
    });
  });

  afterEach(() => {
    getTestBed().resetTestingModule();
  });

  async function createAdapter(frame: LemmaDetailFrame): Promise<ComponentFixture<LemmaDetailOverlayAdapterComponent>> {
    const fixture = TestBed.createComponent(LemmaDetailOverlayAdapterComponent);
    fixture.componentRef.setInput('frame', frame);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
  }

  it('renders the panel content frameless: no dialog chrome, no close, no panel header', async () => {
    const fixture = await createAdapter(frameOf());
    const host = fixture.nativeElement as HTMLElement;

    expect(host.querySelector('[data-testid="lemma-details-panel-frameless"]')).not.toBeNull();
    expect(host.querySelector('[role="dialog"]')).toBeNull();
    expect(host.querySelector('[data-testid="lemma-details-panel-close"]')).toBeNull();
    expect(host.querySelector('[data-testid="lemma-details-panel-label"]')).toBeNull();
    expect(host.querySelector('.explorer-panel-header')).toBeNull();
    expect(host.querySelector('.qd-card')).toBeNull();

    expect(host.querySelectorAll('[role="tab"]').length).toBeGreaterThanOrEqual(4);
    expect(host.querySelector('[data-testid="overlay-lemmas-word-view-tabs"]')).not.toBeNull();
    expect(host.querySelector('[data-testid="overlay-lemmas-words-view"]')).not.toBeNull();
    expect(host.textContent).toContain('كتاب');
  });

  it('publishes the loaded entity title and ayah count to the shared store and clears them on destroy', async () => {
    const store = TestBed.inject(EntityDetailOverlayTitleStore);
    const fixture = await createAdapter(frameOf());

    expect(store.title()).toBe('كِتَاب');
    expect(store.ayahCount()).toBe(4);

    fixture.destroy();
    expect(store.title()).toBe('');
    expect(store.ayahCount()).toBeNull();
  });

  it('routes a view tab change through replaceTopFrame with setView reset semantics', async () => {
    const fixture = await createAdapter(frameOf({ view: 'words', wordView: 'tashkeel', detailPage: 4 }));
    const host = fixture.nativeElement as HTMLElement;

    (host.querySelector('[data-testid="lemma-details-tab-ayahs"]') as HTMLButtonElement).click();

    expect(replaceTopFrame).toHaveBeenCalledTimes(1);
    expect(replaceTopFrame).toHaveBeenCalledWith({
      kind: 'lemma',
      id: 999,
      view: 'ayahs',
      wordView: 'simple',
      surahView: 'mentioned',
      detailPage: 1,
      typeCode: null,
    });
  });

  it('resets the typeCode when leaving the ayahs view', async () => {
    const fixture = await createAdapter(frameOf({ view: 'ayahs', typeCode: 'V', detailPage: 2 }));
    const host = fixture.nativeElement as HTMLElement;

    (host.querySelector('[data-testid="lemma-details-tab-words"]') as HTMLButtonElement).click();

    expect(replaceTopFrame).toHaveBeenCalledTimes(1);
    expect(replaceTopFrame).toHaveBeenCalledWith({
      kind: 'lemma',
      id: 999,
      view: 'words',
      wordView: 'simple',
      surahView: 'mentioned',
      detailPage: 1,
      typeCode: null,
    });
  });

  it('routes a word-view sub-tab change through replaceTopFrame with page reset', async () => {
    const fixture = await createAdapter(frameOf({ view: 'words', wordView: 'simple', detailPage: 3 }));
    const host = fixture.nativeElement as HTMLElement;

    (host.querySelector('[data-testid="overlay-lemmas-word-view-tashkeel"]') as HTMLButtonElement).click();

    expect(replaceTopFrame).toHaveBeenCalledTimes(1);
    expect(replaceTopFrame).toHaveBeenCalledWith({
      kind: 'lemma',
      id: 999,
      view: 'words',
      wordView: 'tashkeel',
      surahView: 'mentioned',
      detailPage: 1,
      typeCode: null,
    });
  });

  it('routes a surah sub-tab change through replaceTopFrame keeping the page', async () => {
    const fixture = await createAdapter(frameOf({ view: 'surahs', surahView: 'mentioned' }));
    const host = fixture.nativeElement as HTMLElement;

    (host.querySelector('[data-testid="overlay-lemmas-surah-view-missing"]') as HTMLButtonElement).click();

    expect(replaceTopFrame).toHaveBeenCalledTimes(1);
    expect(replaceTopFrame).toHaveBeenCalledWith({
      kind: 'lemma',
      id: 999,
      view: 'surahs',
      wordView: 'simple',
      surahView: 'missing',
      detailPage: 1,
      typeCode: null,
    });
  });

  it('routes ayah pagination through replaceTopFrame preserving the active typeCode', async () => {
    const fixture = await createAdapter(frameOf({ view: 'ayahs', typeCode: 'V' }));

    const pagination = fixture.debugElement.query(By.directive(PaginationComponent));
    expect(pagination).not.toBeNull();
    (pagination.componentInstance as PaginationComponent).pageChange.emit(2);

    expect(replaceTopFrame).toHaveBeenCalledTimes(1);
    expect(replaceTopFrame).toHaveBeenCalledWith({
      kind: 'lemma',
      id: 999,
      view: 'ayahs',
      wordView: 'simple',
      surahView: 'mentioned',
      detailPage: 2,
      typeCode: 'V',
    });
  });

  it('routes an ayah type filter change through replaceTopFrame with page reset', async () => {
    const fixture = await createAdapter(frameOf({ view: 'ayahs', typeCode: null, detailPage: 4 }));

    const filters = fixture.debugElement.query(By.directive(LemmaAyahTypeFiltersComponent));
    expect(filters).not.toBeNull();
    (filters.componentInstance as LemmaAyahTypeFiltersComponent).typeCodeChange.emit('V');

    expect(replaceTopFrame).toHaveBeenCalledTimes(1);
    expect(replaceTopFrame).toHaveBeenCalledWith({
      kind: 'lemma',
      id: 999,
      view: 'ayahs',
      wordView: 'simple',
      surahView: 'mentioned',
      detailPage: 1,
      typeCode: 'V',
    });
  });

  it('does not replace the frame or refetch when the already-active type chip is clicked', async () => {
    const fixture = await createAdapter(frameOf({ view: 'ayahs', typeCode: 'V', detailPage: 2 }));
    const host = fixture.nativeElement as HTMLElement;

    const chip = host.querySelector('[data-testid="lemma-ayah-type-filter-V"]') as HTMLButtonElement;
    expect(chip.getAttribute('aria-pressed')).toBe('true');
    const ayahCallsBeforeClick = apiStub.getLemmaAyahMatches.mock.calls.length;

    chip.click();
    await fixture.whenStable();

    expect(replaceTopFrame).not.toHaveBeenCalled();
    expect(apiStub.getLemmaAyahMatches.mock.calls.length).toBe(ayahCallsBeforeClick);
  });

  it('does not replace the frame or refetch when the already-active عرض الكل chip is clicked', async () => {
    const fixture = await createAdapter(frameOf({ view: 'ayahs', typeCode: null, detailPage: 2 }));
    const host = fixture.nativeElement as HTMLElement;

    const allChip = host.querySelector('[data-testid="lemma-ayah-type-filter-all"]') as HTMLButtonElement;
    expect(allChip.getAttribute('aria-pressed')).toBe('true');
    const ayahCallsBeforeClick = apiStub.getLemmaAyahMatches.mock.calls.length;

    allChip.click();
    await fixture.whenStable();

    expect(replaceTopFrame).not.toHaveBeenCalled();
    expect(apiStub.getLemmaAyahMatches.mock.calls.length).toBe(ayahCallsBeforeClick);
  });

  it('re-drives its own controller from a frame input change (URL sync loop)', async () => {
    const fixture = await createAdapter(frameOf({ view: 'words', wordView: 'simple' }));

    fixture.componentRef.setInput('frame', frameOf({ view: 'words', wordView: 'tashkeel' }));
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(apiStub.getLemmaWords).toHaveBeenLastCalledWith(999, 'tashkeel', 1, expect.any(Number));
    const tashkeelTab = (fixture.nativeElement as HTMLElement).querySelector(
      '[data-testid="overlay-lemmas-word-view-tashkeel"]',
    );
    expect(tashkeelTab?.getAttribute('aria-selected')).toBe('true');
    expect(apiStub.getLemmaSummary).toHaveBeenCalledTimes(1);
  });

  it('never touches the page facade singleton state', async () => {
    const facade = TestBed.inject(LemmasDetailFacade);
    const fixture = await createAdapter(frameOf());
    const host = fixture.nativeElement as HTMLElement;

    (host.querySelector('[data-testid="lemma-details-tab-ayahs"]') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(facade.panelState().status).toBe('idle');
    expect(facade.panelState().selectedLemmaId).toBeNull();
    expect(facade.panelState().summary).toBeNull();
  });
});
