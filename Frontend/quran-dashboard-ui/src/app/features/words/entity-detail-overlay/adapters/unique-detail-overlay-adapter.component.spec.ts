import { ComponentFixture, getTestBed, TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { By } from '@angular/platform-browser';
import { of, throwError } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { DetailOverlayHistoryService } from '../../../../core/navigation/detail-overlay/detail-overlay-history.service';
import { UniqueDetailFrame } from '../../../../core/navigation/detail-overlay/detail-overlay.models';
import { ApiResponse } from '../../../../core/data-access/api-response.model';
import { PaginationComponent } from '../../../../shared/ui/pagination/pagination.component';
import { UniqueWordsApi } from '../../data-access/unique-words.api';
import {
  PagedResultDto,
  UniqueWordAyahMatchDto,
  UniqueWordKind,
  UniqueWordSummaryDto,
} from '../../models/unique-words.models';
import { UniqueWordsDrilldownFacade } from '../../state/unique-words-drilldown.facade';
import { EntityDetailOverlayTitleStore } from '../entity-detail-overlay-title.store';
import { UniqueDetailOverlayAdapterComponent } from './unique-detail-overlay-adapter.component';

const SYNTHETIC_WORD_TEXT = 'كلمة-اختبار';

function summaryOf(mode: UniqueWordKind, id: number): UniqueWordSummaryDto {
  return {
    id,
    kind: mode,
    displayText: `كلمة-${mode}-${id}`,
    occurrencesCount: 6,
    ayahsCount: 5,
    surahsCount: 3,
    missingSurahsCount: 111,
  };
}

const AYAHS_PAGE: PagedResultDto<UniqueWordAyahMatchDto> = {
  page: 1,
  pageSize: 100,
  totalCount: 250,
  items: [
    {
      ayahId: 1,
      ayahNumber: 2,
      pageNumber: 2,
      surahNameArabic: 'البقرة',
      verseKey: '2:2',
      matchedQuranWordIds: [0],
      words: [{ quranWordId: 0, textUthmani: SYNTHETIC_WORD_TEXT, isAyahMarker: false }],
    },
  ],
};

function ok<T>(data: T): ApiResponse<T> {
  return { isSuccess: true, data, message: null, errors: null };
}

function frameOf(overrides: Partial<UniqueDetailFrame> = {}): UniqueDetailFrame {
  return { kind: 'unique', mode: 'tashkeel', id: 5, view: 'surahs', ayahPage: 1, ...overrides };
}

describe('UniqueDetailOverlayAdapterComponent (Feature 029 B4)', () => {
  let replaceTopFrame: ReturnType<typeof vi.fn>;
  let apiStub: {
    getSummary: ReturnType<typeof vi.fn>;
    getMentionedSurahs: ReturnType<typeof vi.fn>;
    getMissingSurahs: ReturnType<typeof vi.fn>;
    getAyahMatches: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    replaceTopFrame = vi.fn();
    apiStub = {
      getSummary: vi.fn((mode: UniqueWordKind, id: number) => of(ok(summaryOf(mode, id)))),
      getMentionedSurahs: vi.fn(() =>
        of(ok({ surahs: [{ surahNumber: 1, nameArabic: 'الفاتحة', occurrencesInSurah: 2 }] })),
      ),
      getMissingSurahs: vi.fn(() => of(ok({ surahs: [{ surahNumber: 9, nameArabic: 'التوبة' }] }))),
      getAyahMatches: vi.fn(() => of(ok(AYAHS_PAGE))),
    };

    TestBed.configureTestingModule({
      imports: [UniqueDetailOverlayAdapterComponent],
      providers: [
        EntityDetailOverlayTitleStore,
        { provide: UniqueWordsApi, useValue: apiStub },
        { provide: DetailOverlayHistoryService, useValue: { replaceTopFrame, urlEpoch: () => 0, buildFrameHref: () => '/SYNTH_OVERLAY_HREF', buildBaseWithOverlayHref: () => '/SYNTH_AYAH_HREF', navigateBaseWithOverlay: () => undefined } },
      ],
      teardown: { destroyAfterEach: true },
    });
  });

  afterEach(() => {
    getTestBed().resetTestingModule();
  });

  async function createAdapter(frame: UniqueDetailFrame): Promise<ComponentFixture<UniqueDetailOverlayAdapterComponent>> {
    const fixture = TestBed.createComponent(UniqueDetailOverlayAdapterComponent);
    fixture.componentRef.setInput('frame', frame);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
  }

  it('renders the drilldown content frameless: no dialog chrome, no close, no panel header', async () => {
    const fixture = await createAdapter(frameOf());
    const host = fixture.nativeElement as HTMLElement;

    expect(host.querySelector('[data-testid="word-drilldown-frameless"]')).not.toBeNull();
    expect(host.querySelector('[role="dialog"]')).toBeNull();
    expect(host.querySelector('[data-testid="word-drilldown-backdrop"]')).toBeNull();
    expect(host.querySelector('[data-testid="word-drilldown-close"]')).toBeNull();
    expect(host.querySelector('[data-testid="word-drilldown-panel-label"]')).toBeNull();
    expect(host.querySelector('.explorer-panel-header')).toBeNull();
    expect(host.querySelector('.qd-card')).toBeNull();

    expect(host.querySelector('[data-testid="word-drilldown-view-surahs"]')).not.toBeNull();
    expect(host.querySelector('[data-testid="word-drilldown-view-missing"]')).not.toBeNull();
    expect(host.querySelector('[data-testid="word-drilldown-view-ayahs"]')).not.toBeNull();
    expect(host.textContent).toContain('الفاتحة');
  });

  it('publishes the loaded summary display text and ayah count to the shared store and clears them on destroy', async () => {
    const store = TestBed.inject(EntityDetailOverlayTitleStore);
    const fixture = await createAdapter(frameOf());

    expect(store.title()).toBe('كلمة-tashkeel-5');
    expect(store.ayahCount()).toBe(5);

    fixture.destroy();
    expect(store.title()).toBe('');
    expect(store.ayahCount()).toBeNull();
  });

  it('routes a view tab change through replaceTopFrame with an ayah-page reset', async () => {
    const fixture = await createAdapter(frameOf({ view: 'surahs' }));
    const host = fixture.nativeElement as HTMLElement;

    (host.querySelector('[data-testid="word-drilldown-view-ayahs"]') as HTMLButtonElement).click();

    expect(replaceTopFrame).toHaveBeenCalledTimes(1);
    expect(replaceTopFrame).toHaveBeenCalledWith({
      kind: 'unique',
      mode: 'tashkeel',
      id: 5,
      view: 'ayahs',
      ayahPage: 1,
    });
  });

  it('routes ayah pagination through replaceTopFrame with the new ayah page', async () => {
    const fixture = await createAdapter(frameOf({ view: 'ayahs' }));

    const pagination = fixture.debugElement.query(By.directive(PaginationComponent));
    expect(pagination).not.toBeNull();
    (pagination.componentInstance as PaginationComponent).pageChange.emit(2);

    expect(replaceTopFrame).toHaveBeenCalledTimes(1);
    expect(replaceTopFrame).toHaveBeenCalledWith({
      kind: 'unique',
      mode: 'tashkeel',
      id: 5,
      view: 'ayahs',
      ayahPage: 2,
    });
  });

  it('re-drives its own controller from a frame input change (URL sync loop)', async () => {
    const fixture = await createAdapter(frameOf({ view: 'surahs' }));

    fixture.componentRef.setInput('frame', frameOf({ view: 'missing' }));
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(apiStub.getMissingSurahs).toHaveBeenCalledWith('tashkeel', 5);
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('التوبة');
    expect(apiStub.getSummary).toHaveBeenCalledTimes(1);
  });

  it('treats mode as identity: the same word id in the other mode reloads its own summary', async () => {
    const fixture = await createAdapter(frameOf({ mode: 'tashkeel', id: 5 }));
    const store = TestBed.inject(EntityDetailOverlayTitleStore);

    fixture.componentRef.setInput('frame', frameOf({ mode: 'simple', id: 5 }));
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(apiStub.getSummary).toHaveBeenCalledTimes(2);
    expect(apiStub.getSummary).toHaveBeenLastCalledWith('simple', 5);
    expect(store.title()).toBe('كلمة-simple-5');
  });

  it('renders the in-panel notFound branch when the summary read 404s', async () => {
    apiStub.getSummary.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 404 })));
    const fixture = await createAdapter(frameOf());
    const host = fixture.nativeElement as HTMLElement;

    expect(host.querySelector('[data-testid="overlay-unique-not-found"]')).not.toBeNull();
    expect(host.querySelector('[data-testid="word-drilldown-frameless"]')).toBeNull();
  });

  it('never touches the page facade singleton state', async () => {
    const facade = TestBed.inject(UniqueWordsDrilldownFacade);
    const fixture = await createAdapter(frameOf());
    const host = fixture.nativeElement as HTMLElement;

    (host.querySelector('[data-testid="word-drilldown-view-ayahs"]') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(facade.drilldownState().isOpen).toBe(false);
    expect(facade.drilldownState().status).toBe('idle');
    expect(facade.drilldownState().selectedWordId).toBeNull();
    expect(facade.drilldownState().summary).toBeNull();
  });
});
