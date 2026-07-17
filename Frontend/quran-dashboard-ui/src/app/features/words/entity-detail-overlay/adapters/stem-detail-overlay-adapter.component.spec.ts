import { ComponentFixture, getTestBed, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { of } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { DetailOverlayHistoryService } from '../../../../core/navigation/detail-overlay/detail-overlay-history.service';
import { StemDetailFrame } from '../../../../core/navigation/detail-overlay/detail-overlay.models';
import { ApiResponse } from '../../../../core/data-access/api-response.model';
import { PaginationComponent } from '../../../../shared/ui/pagination/pagination.component';
import { StemAyahTypeFiltersComponent } from '../../components/stem-ayah-type-filters/stem-ayah-type-filters.component';
import { StemsApi } from '../../data-access/stems.api';
import {
  PagedResultDto,
  StemAyahMatchDto,
  StemSummaryDto,
  StemWordItemDto,
} from '../../models/stems.models';
import { StemsDetailFacade } from '../../state/stems-detail.facade';
import { EntityDetailOverlayTitleStore } from '../entity-detail-overlay-title.store';
import { StemDetailOverlayAdapterComponent } from './stem-detail-overlay-adapter.component';

const STEM_SUMMARY: StemSummaryDto = {
  id: 999,
  stemText: 'كَاتِب',
  occurrencesCount: 5,
  ayahsCount: 4,
  surahsCount: 3,
  simpleWordsCount: 2,
  tashkeelWordsCount: 2,
  lemmaId: null,
  lemmaText: null,
  rootId: null,
  rootText: null,
  typeDistribution: [
    { code: 'N', arabicLabel: 'اسم', occurrencesCount: 3 },
    { code: 'V', arabicLabel: 'فعل', occurrencesCount: 2 },
  ],
};

const WORDS_PAGE: PagedResultDto<StemWordItemDto> = {
  page: 1,
  pageSize: 100,
  totalCount: 2,
  items: [
    { displayText: 'كاتب', occurrencesCount: 3, uniqueWordId: 11 },
    { displayText: 'كاتبون', occurrencesCount: 2, uniqueWordId: 12 },
  ],
};

const AYAHS_PAGE: PagedResultDto<StemAyahMatchDto> = {
  page: 1,
  pageSize: 100,
  totalCount: 250,
  items: [
    {
      ayahId: 1,
      pageNumber: 2,
      surahNameArabic: 'البقرة',
      verseKey: '2:2',
      words: [{ textUthmani: 'ٱلْكِتَـٰبُ', isMatched: true }],
    },
  ],
};

function ok<T>(data: T): ApiResponse<T> {
  return { isSuccess: true, data, message: null, errors: null };
}

function frameOf(overrides: Partial<StemDetailFrame> = {}): StemDetailFrame {
  return {
    kind: 'stem',
    id: 999,
    view: 'words',
    wordView: 'simple',
    surahView: 'mentioned',
    detailPage: 1,
    typeCode: null,
    ...overrides,
  };
}

describe('StemDetailOverlayAdapterComponent (Feature 029 B4)', () => {
  let replaceTopFrame: ReturnType<typeof vi.fn>;
  let apiStub: {
    getStemSummary: ReturnType<typeof vi.fn>;
    getStemWords: ReturnType<typeof vi.fn>;
    getStemAyahMatches: ReturnType<typeof vi.fn>;
    getStemMentionedSurahs: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    replaceTopFrame = vi.fn();
    apiStub = {
      getStemSummary: vi.fn(() => of(ok(STEM_SUMMARY))),
      getStemWords: vi.fn(() => of(ok(WORDS_PAGE))),
      getStemAyahMatches: vi.fn(() => of(ok(AYAHS_PAGE))),
      getStemMentionedSurahs: vi.fn(() => of(ok({ surahs: [] }))),
    };

    TestBed.configureTestingModule({
      imports: [StemDetailOverlayAdapterComponent],
      providers: [
        EntityDetailOverlayTitleStore,
        { provide: StemsApi, useValue: apiStub },
        { provide: DetailOverlayHistoryService, useValue: { replaceTopFrame, urlEpoch: () => 0, buildFrameHref: () => '/SYNTH_OVERLAY_HREF', buildBaseWithOverlayHref: () => '/SYNTH_AYAH_HREF', navigateBaseWithOverlay: () => undefined } },
      ],
      teardown: { destroyAfterEach: true },
    });
  });

  afterEach(() => {
    getTestBed().resetTestingModule();
  });

  async function createAdapter(frame: StemDetailFrame): Promise<ComponentFixture<StemDetailOverlayAdapterComponent>> {
    const fixture = TestBed.createComponent(StemDetailOverlayAdapterComponent);
    fixture.componentRef.setInput('frame', frame);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
  }

  it('renders the panel content frameless: no dialog chrome, no close, no panel header', async () => {
    const fixture = await createAdapter(frameOf());
    const host = fixture.nativeElement as HTMLElement;

    expect(host.querySelector('[data-testid="stem-details-panel-frameless"]')).not.toBeNull();
    expect(host.querySelector('[role="dialog"]')).toBeNull();
    expect(host.querySelector('[data-testid="stem-details-panel-close"]')).toBeNull();
    expect(host.querySelector('[data-testid="stem-details-panel-label"]')).toBeNull();
    expect(host.querySelector('.explorer-panel-header')).toBeNull();
    expect(host.querySelector('.qd-card')).toBeNull();

    expect(host.querySelectorAll('[role="tab"]').length).toBeGreaterThanOrEqual(4);
    expect(host.querySelector('[data-testid="overlay-stems-word-view-tabs"]')).not.toBeNull();
    expect(host.querySelector('[data-testid="overlay-stems-words-view"]')).not.toBeNull();
    expect(host.textContent).toContain('كاتب');
  });

  it('publishes the loaded entity title to the shared store and clears it on destroy', async () => {
    const store = TestBed.inject(EntityDetailOverlayTitleStore);
    const fixture = await createAdapter(frameOf());

    expect(store.title()).toBe('كَاتِب');

    fixture.destroy();
    expect(store.title()).toBe('');
  });

  it('routes a view tab change through replaceTopFrame with setView reset semantics', async () => {
    const fixture = await createAdapter(frameOf({ view: 'words', wordView: 'tashkeel', detailPage: 4 }));
    const host = fixture.nativeElement as HTMLElement;

    (host.querySelector('[data-testid="stem-details-tab-ayahs"]') as HTMLButtonElement).click();

    expect(replaceTopFrame).toHaveBeenCalledTimes(1);
    expect(replaceTopFrame).toHaveBeenCalledWith({
      kind: 'stem',
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

    (host.querySelector('[data-testid="stem-details-tab-words"]') as HTMLButtonElement).click();

    expect(replaceTopFrame).toHaveBeenCalledTimes(1);
    expect(replaceTopFrame).toHaveBeenCalledWith({
      kind: 'stem',
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

    (host.querySelector('[data-testid="overlay-stems-word-view-tashkeel"]') as HTMLButtonElement).click();

    expect(replaceTopFrame).toHaveBeenCalledTimes(1);
    expect(replaceTopFrame).toHaveBeenCalledWith({
      kind: 'stem',
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

    (host.querySelector('[data-testid="overlay-stems-surah-view-missing"]') as HTMLButtonElement).click();

    expect(replaceTopFrame).toHaveBeenCalledTimes(1);
    expect(replaceTopFrame).toHaveBeenCalledWith({
      kind: 'stem',
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
      kind: 'stem',
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

    const filters = fixture.debugElement.query(By.directive(StemAyahTypeFiltersComponent));
    expect(filters).not.toBeNull();
    (filters.componentInstance as StemAyahTypeFiltersComponent).typeCodeChange.emit('V');

    expect(replaceTopFrame).toHaveBeenCalledTimes(1);
    expect(replaceTopFrame).toHaveBeenCalledWith({
      kind: 'stem',
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

    const chip = host.querySelector('[data-testid="stem-ayah-type-filter-V"]') as HTMLButtonElement;
    expect(chip.getAttribute('aria-pressed')).toBe('true');
    const ayahCallsBeforeClick = apiStub.getStemAyahMatches.mock.calls.length;

    chip.click();
    await fixture.whenStable();

    expect(replaceTopFrame).not.toHaveBeenCalled();
    expect(apiStub.getStemAyahMatches.mock.calls.length).toBe(ayahCallsBeforeClick);
  });

  it('does not replace the frame or refetch when the already-active عرض الكل chip is clicked', async () => {
    const fixture = await createAdapter(frameOf({ view: 'ayahs', typeCode: null, detailPage: 2 }));
    const host = fixture.nativeElement as HTMLElement;

    const allChip = host.querySelector('[data-testid="stem-ayah-type-filter-all"]') as HTMLButtonElement;
    expect(allChip.getAttribute('aria-pressed')).toBe('true');
    const ayahCallsBeforeClick = apiStub.getStemAyahMatches.mock.calls.length;

    allChip.click();
    await fixture.whenStable();

    expect(replaceTopFrame).not.toHaveBeenCalled();
    expect(apiStub.getStemAyahMatches.mock.calls.length).toBe(ayahCallsBeforeClick);
  });

  it('re-drives its own controller from a frame input change (URL sync loop)', async () => {
    const fixture = await createAdapter(frameOf({ view: 'words', wordView: 'simple' }));

    fixture.componentRef.setInput('frame', frameOf({ view: 'words', wordView: 'tashkeel' }));
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(apiStub.getStemWords).toHaveBeenLastCalledWith(999, 'tashkeel', 1, expect.any(Number));
    const tashkeelTab = (fixture.nativeElement as HTMLElement).querySelector(
      '[data-testid="overlay-stems-word-view-tashkeel"]',
    );
    expect(tashkeelTab?.getAttribute('aria-selected')).toBe('true');
    // The summary is reused for the same stem: still exactly one summary read.
    expect(apiStub.getStemSummary).toHaveBeenCalledTimes(1);
  });

  it('never touches the page facade singleton state', async () => {
    const facade = TestBed.inject(StemsDetailFacade);
    const fixture = await createAdapter(frameOf());
    const host = fixture.nativeElement as HTMLElement;

    (host.querySelector('[data-testid="stem-details-tab-ayahs"]') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(facade.panelState().status).toBe('idle');
    expect(facade.panelState().selectedStemId).toBeNull();
    expect(facade.panelState().summary).toBeNull();
  });
});
