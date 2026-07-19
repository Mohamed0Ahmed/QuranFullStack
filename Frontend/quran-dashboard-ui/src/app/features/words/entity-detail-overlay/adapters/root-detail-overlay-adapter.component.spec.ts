import { ComponentFixture, getTestBed, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { of } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { DetailOverlayHistoryService } from '../../../../core/navigation/detail-overlay/detail-overlay-history.service';
import { RootDetailFrame } from '../../../../core/navigation/detail-overlay/detail-overlay.models';
import { ApiResponse } from '../../../../core/data-access/api-response.model';
import { PaginationComponent } from '../../../../shared/ui/pagination/pagination.component';
import { RootsApi } from '../../data-access/roots.api';
import {
  PagedResultDto,
  RootAyahMatchDto,
  RootSummaryDto,
  RootWordItemDto,
} from '../../models/roots.models';
import { RootsDetailFacade } from '../../state/roots-detail.facade';
import { EntityDetailOverlayTitleStore } from '../entity-detail-overlay-title.store';
import { RootDetailOverlayAdapterComponent } from './root-detail-overlay-adapter.component';

// Synthetic, non-scriptural Arabic placeholder: keeps RTL rendering real without faking Quranic text.
const SYNTHETIC_WORD_TEXT = 'كلمة-اختبار';

const ROOT_SUMMARY: RootSummaryDto = {
  id: 999,
  rootText: 'كتب',
  occurrencesCount: 5,
  ayahsCount: 4,
  surahsCount: 3,
  simpleWordsCount: 2,
  tashkeelWordsCount: 2,
  lemmasCount: 1,
  stemsCount: 1,
};

const WORDS_PAGE: PagedResultDto<RootWordItemDto> = {
  page: 1,
  pageSize: 100,
  totalCount: 2,
  items: [
    { displayText: 'كتاب', kind: 'simple', occurrencesCount: 3, uniqueWordId: 11 },
    { displayText: 'كاتب', kind: 'simple', occurrencesCount: 2, uniqueWordId: 12 },
  ],
};

const AYAHS_PAGE: PagedResultDto<RootAyahMatchDto> = {
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

function frameOf(overrides: Partial<RootDetailFrame> = {}): RootDetailFrame {
  return { kind: 'root', id: 999, view: 'words', wordView: 'simple', surahView: 'mentioned', detailPage: 1, ...overrides };
}

describe('RootDetailOverlayAdapterComponent (Feature 029 B4)', () => {
  let replaceTopFrame: ReturnType<typeof vi.fn>;
  let apiStub: {
    getRootSummary: ReturnType<typeof vi.fn>;
    getRootWords: ReturnType<typeof vi.fn>;
    getRootAyahMatches: ReturnType<typeof vi.fn>;
    getRootMentionedSurahs: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    replaceTopFrame = vi.fn();
    apiStub = {
      getRootSummary: vi.fn(() => of(ok(ROOT_SUMMARY))),
      getRootWords: vi.fn(() => of(ok(WORDS_PAGE))),
      getRootAyahMatches: vi.fn(() => of(ok(AYAHS_PAGE))),
      getRootMentionedSurahs: vi.fn(() => of(ok({ surahs: [] }))),
    };

    TestBed.configureTestingModule({
      imports: [RootDetailOverlayAdapterComponent],
      providers: [
        EntityDetailOverlayTitleStore,
        { provide: RootsApi, useValue: apiStub },
        { provide: DetailOverlayHistoryService, useValue: { replaceTopFrame, urlEpoch: () => 0, buildFrameHref: () => '/SYNTH_OVERLAY_HREF', buildBaseWithOverlayHref: () => '/SYNTH_AYAH_HREF', navigateBaseWithOverlay: () => undefined } },
      ],
      teardown: { destroyAfterEach: true },
    });
  });

  afterEach(() => {
    getTestBed().resetTestingModule();
  });

  async function createAdapter(frame: RootDetailFrame): Promise<ComponentFixture<RootDetailOverlayAdapterComponent>> {
    const fixture = TestBed.createComponent(RootDetailOverlayAdapterComponent);
    fixture.componentRef.setInput('frame', frame);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
  }

  it('renders the panel content frameless: no dialog chrome, no close, no panel header', async () => {
    const fixture = await createAdapter(frameOf());
    const host = fixture.nativeElement as HTMLElement;

    expect(host.querySelector('[data-testid="root-details-panel-frameless"]')).not.toBeNull();
    expect(host.querySelector('[role="dialog"]')).toBeNull();
    expect(host.querySelector('[data-testid="root-details-panel-close"]')).toBeNull();
    expect(host.querySelector('[data-testid="root-details-panel-label"]')).toBeNull();
    expect(host.querySelector('.explorer-panel-header')).toBeNull();
    expect(host.querySelector('.qd-card')).toBeNull();

    expect(host.querySelectorAll('[role="tab"]').length).toBeGreaterThanOrEqual(5);
    expect(host.querySelector('[data-testid="overlay-roots-word-view-tabs"]')).not.toBeNull();
    expect(host.querySelector('[data-testid="overlay-roots-words-view"]')).not.toBeNull();
    expect(host.textContent).toContain('كتاب');
  });

  it('publishes the loaded entity title and ayah count to the shared store and clears them on destroy', async () => {
    const store = TestBed.inject(EntityDetailOverlayTitleStore);
    const fixture = await createAdapter(frameOf());

    expect(store.title()).toBe('كتب');
    expect(store.ayahCount()).toBe(4);

    fixture.destroy();
    expect(store.title()).toBe('');
    expect(store.ayahCount()).toBeNull();
  });

  it('routes a view tab change through replaceTopFrame with setView reset semantics', async () => {
    const fixture = await createAdapter(frameOf({ view: 'words', wordView: 'tashkeel', detailPage: 4 }));
    const host = fixture.nativeElement as HTMLElement;

    (host.querySelector('[data-testid="root-details-tab-ayahs"]') as HTMLButtonElement).click();

    expect(replaceTopFrame).toHaveBeenCalledTimes(1);
    expect(replaceTopFrame).toHaveBeenCalledWith({
      kind: 'root',
      id: 999,
      view: 'ayahs',
      wordView: 'simple',
      surahView: 'mentioned',
      detailPage: 1,
    });
  });

  it('routes a word-view sub-tab change through replaceTopFrame with page reset', async () => {
    const fixture = await createAdapter(frameOf({ view: 'words', wordView: 'simple', detailPage: 3 }));
    const host = fixture.nativeElement as HTMLElement;

    (host.querySelector('[data-testid="overlay-roots-word-view-tashkeel"]') as HTMLButtonElement).click();

    expect(replaceTopFrame).toHaveBeenCalledTimes(1);
    expect(replaceTopFrame).toHaveBeenCalledWith({
      kind: 'root',
      id: 999,
      view: 'words',
      wordView: 'tashkeel',
      surahView: 'mentioned',
      detailPage: 1,
    });
  });

  it('routes a surah sub-tab change through replaceTopFrame keeping the page', async () => {
    const fixture = await createAdapter(frameOf({ view: 'surahs', surahView: 'mentioned' }));
    const host = fixture.nativeElement as HTMLElement;

    (host.querySelector('[data-testid="overlay-roots-surah-view-missing"]') as HTMLButtonElement).click();

    expect(replaceTopFrame).toHaveBeenCalledTimes(1);
    expect(replaceTopFrame).toHaveBeenCalledWith({
      kind: 'root',
      id: 999,
      view: 'surahs',
      wordView: 'simple',
      surahView: 'missing',
      detailPage: 1,
    });
  });

  it('routes ayah pagination through replaceTopFrame with the new detail page', async () => {
    const fixture = await createAdapter(frameOf({ view: 'ayahs' }));

    const pagination = fixture.debugElement.query(By.directive(PaginationComponent));
    expect(pagination).not.toBeNull();
    (pagination.componentInstance as PaginationComponent).pageChange.emit(2);

    expect(replaceTopFrame).toHaveBeenCalledTimes(1);
    expect(replaceTopFrame).toHaveBeenCalledWith({
      kind: 'root',
      id: 999,
      view: 'ayahs',
      wordView: 'simple',
      surahView: 'mentioned',
      detailPage: 2,
    });
  });

  it('re-drives its own controller from a frame input change (URL sync loop)', async () => {
    const fixture = await createAdapter(frameOf({ view: 'words', wordView: 'simple' }));

    fixture.componentRef.setInput('frame', frameOf({ view: 'words', wordView: 'tashkeel' }));
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(apiStub.getRootWords).toHaveBeenLastCalledWith(999, 'tashkeel', 1, expect.any(Number));
    const tashkeelTab = (fixture.nativeElement as HTMLElement).querySelector(
      '[data-testid="overlay-roots-word-view-tashkeel"]',
    );
    expect(tashkeelTab?.getAttribute('aria-selected')).toBe('true');
    // The summary is reused for the same root: still exactly one summary read.
    expect(apiStub.getRootSummary).toHaveBeenCalledTimes(1);
  });

  it('never touches the page facade singleton state', async () => {
    const facade = TestBed.inject(RootsDetailFacade);
    const fixture = await createAdapter(frameOf());
    const host = fixture.nativeElement as HTMLElement;

    (host.querySelector('[data-testid="root-details-tab-ayahs"]') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(facade.panelState().status).toBe('idle');
    expect(facade.panelState().selectedRootId).toBeNull();
    expect(facade.panelState().summary).toBeNull();
  });
});
