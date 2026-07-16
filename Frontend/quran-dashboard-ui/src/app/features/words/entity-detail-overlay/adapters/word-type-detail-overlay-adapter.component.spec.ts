import { ComponentFixture, getTestBed, TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { By } from '@angular/platform-browser';
import { of, throwError } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { DetailOverlayHistoryService } from '../../../../core/navigation/detail-overlay/detail-overlay-history.service';
import { WordTypeDetailFrame } from '../../../../core/navigation/detail-overlay/detail-overlay.models';
import { ApiResponse } from '../../../../core/data-access/api-response.model';
import { PaginationComponent } from '../../../../shared/ui/pagination/pagination.component';
import { WordTypesApi } from '../../data-access/word-types.api';
import {
  PagedResultDto,
  WordTypeAyahMatchDto,
  WordTypeRowIdentity,
  WordTypeSummaryDto,
} from '../../models/word-types.models';
import { WordTypesDetailFacade } from '../../state/word-types-detail.facade';
import { EntityDetailOverlayTitleStore } from '../entity-detail-overlay-title.store';
import { WordTypeDetailOverlayAdapterComponent } from './word-type-detail-overlay-adapter.component';

const IDENTITY: WordTypeRowIdentity = {
  tashkeelWordId: 42,
  contextCode: 'noun',
  case: 'all',
  tense: 'all',
  voice: 'all',
};

const SUMMARY: WordTypeSummaryDto = {
  ...IDENTITY,
  displayText: 'ٱلْكِتَـٰبُ',
  typeCode: 'noun',
  typeLabel: { ar: 'اسم' },
  broadLabel: { ar: 'اسم' },
  caseOrFeature: null,
  rootText: null,
  lemmaText: null,
  stemText: null,
  occurrencesCount: 5,
  ayahsCount: 4,
  surahsCount: 3,
};

const AYAHS_PAGE: PagedResultDto<WordTypeAyahMatchDto> = {
  page: 1,
  pageSize: 100,
  totalCount: 250,
  items: [
    {
      ayahNumber: 2,
      verseKey: '2:2',
      pageNumber: 2,
      surahNumber: 2,
      matchedWordIds: [1],
      matchedWordPositions: [1],
      words: [{ quranWordId: 1, textUthmani: 'ٱلْكِتَـٰبُ', isAyahMarker: false }],
    },
  ],
};

function ok<T>(data: T): ApiResponse<T> {
  return { isSuccess: true, data, message: null, errors: null };
}

function frameOf(overrides: Partial<WordTypeDetailFrame> = {}): WordTypeDetailFrame {
  return {
    kind: 'wordType',
    tashkeelWordId: 42,
    contextCode: 'noun',
    case: 'all',
    tense: 'all',
    voice: 'all',
    view: 'ayahs',
    detailPage: 1,
    ...overrides,
  };
}

describe('WordTypeDetailOverlayAdapterComponent (Feature 029 B4)', () => {
  let replaceTopFrame: ReturnType<typeof vi.fn>;
  let apiStub: {
    getSummary: ReturnType<typeof vi.fn>;
    getAyahMatches: ReturnType<typeof vi.fn>;
    getSurahs: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    replaceTopFrame = vi.fn();
    apiStub = {
      getSummary: vi.fn(() => of(ok(SUMMARY))),
      getAyahMatches: vi.fn(() => of(ok(AYAHS_PAGE))),
      getSurahs: vi.fn(() =>
        of(ok({
          surahs: [{ surahNumber: 1, nameArabic: 'الفاتحة', occurrencesCount: 2 }],
          missingSurahs: [{ surahNumber: 9, nameArabic: 'التوبة' }],
        })),
      ),
    };

    TestBed.configureTestingModule({
      imports: [WordTypeDetailOverlayAdapterComponent],
      providers: [
        EntityDetailOverlayTitleStore,
        { provide: WordTypesApi, useValue: apiStub },
        { provide: DetailOverlayHistoryService, useValue: { replaceTopFrame, urlEpoch: () => 0, buildFrameHref: () => '/SYNTH_OVERLAY_HREF' } },
      ],
      teardown: { destroyAfterEach: true },
    });
  });

  afterEach(() => {
    getTestBed().resetTestingModule();
  });

  async function createAdapter(frame: WordTypeDetailFrame): Promise<ComponentFixture<WordTypeDetailOverlayAdapterComponent>> {
    const fixture = TestBed.createComponent(WordTypeDetailOverlayAdapterComponent);
    fixture.componentRef.setInput('frame', frame);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
  }

  it('renders the panel content frameless: no dialog chrome, no close, no panel header', async () => {
    const fixture = await createAdapter(frameOf());
    const host = fixture.nativeElement as HTMLElement;

    expect(host.querySelector('[data-testid="word-type-details-panel-frameless"]')).not.toBeNull();
    expect(host.querySelector('[role="dialog"]')).toBeNull();
    expect(host.querySelector('[data-testid="word-type-details-panel-close"]')).toBeNull();
    expect(host.querySelector('[data-testid="word-type-details-panel-label"]')).toBeNull();
    expect(host.querySelector('.explorer-panel-header')).toBeNull();
    expect(host.querySelector('.qd-card')).toBeNull();

    // Word-kind panel: exactly the ayahs/surahs tabs, no member-words tab.
    expect(host.querySelectorAll('[role="tab"]').length).toBe(2);
    expect(host.querySelector('[data-testid="word-type-details-tab-words"]')).toBeNull();
    expect(host.querySelector('[data-testid="overlay-word-type-ayahs-view"]')).not.toBeNull();
    expect(host.textContent).toContain('ٱلْكِتَـٰبُ');
  });

  it('publishes the loaded summary display text to the shared store and clears it on destroy', async () => {
    const store = TestBed.inject(EntityDetailOverlayTitleStore);
    const fixture = await createAdapter(frameOf());

    expect(store.title()).toBe('ٱلْكِتَـٰبُ');

    fixture.destroy();
    expect(store.title()).toBe('');
  });

  it('applies the full composite identity from the frame to its controller loads', async () => {
    await createAdapter(frameOf({ case: 'genitive', tense: 'past', voice: 'passive', contextCode: 'past' }));

    const expectedIdentity = { ...IDENTITY, case: 'genitive', tense: 'past', voice: 'passive', contextCode: 'past' };
    expect(apiStub.getSummary).toHaveBeenCalledWith(expectedIdentity);
    expect(apiStub.getAyahMatches).toHaveBeenCalledWith(expectedIdentity, 1, expect.any(Number));
  });

  it('routes a view tab change through replaceTopFrame with a page-1 reset (canonical surahs frame)', async () => {
    const fixture = await createAdapter(frameOf({ view: 'ayahs', detailPage: 4 }));
    const host = fixture.nativeElement as HTMLElement;

    (host.querySelector('[data-testid="word-type-details-tab-surahs"]') as HTMLButtonElement).click();

    expect(replaceTopFrame).toHaveBeenCalledTimes(1);
    expect(replaceTopFrame).toHaveBeenCalledWith({
      kind: 'wordType',
      tashkeelWordId: 42,
      contextCode: 'noun',
      case: 'all',
      tense: 'all',
      voice: 'all',
      view: 'surahs',
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
      kind: 'wordType',
      tashkeelWordId: 42,
      contextCode: 'noun',
      case: 'all',
      tense: 'all',
      voice: 'all',
      view: 'ayahs',
      detailPage: 2,
    });
  });

  it("clamps the grouped-only 'words' frame view to the word detail default 'ayahs'", async () => {
    const fixture = await createAdapter(frameOf({ view: 'words' }));
    const host = fixture.nativeElement as HTMLElement;

    expect(apiStub.getAyahMatches).toHaveBeenCalledTimes(1);
    expect(apiStub.getSurahs).not.toHaveBeenCalled();
    expect(host.querySelector('[data-testid="overlay-word-type-ayahs-view"]')).not.toBeNull();
    expect(
      host.querySelector('[data-testid="word-type-details-tab-ayahs"]')?.getAttribute('aria-selected'),
    ).toBe('true');
  });

  it('re-drives its own controller from a frame input change (URL sync loop)', async () => {
    const fixture = await createAdapter(frameOf({ view: 'ayahs' }));

    fixture.componentRef.setInput('frame', frameOf({ view: 'surahs' }));
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(apiStub.getSurahs).toHaveBeenCalledTimes(1);
    const host = fixture.nativeElement as HTMLElement;
    expect(host.querySelector('[data-testid="overlay-word-type-mentioned-surahs-view"]')).not.toBeNull();
    expect(host.querySelector('[data-testid="overlay-word-type-missing-surahs-view"]')).not.toBeNull();
    expect(host.textContent).toContain('الفاتحة');
    expect(host.textContent).toContain('التوبة');
    // The summary is reused for the same composite identity: one summary read.
    expect(apiStub.getSummary).toHaveBeenCalledTimes(1);
  });

  it('renders the in-panel notFound branch when the summary read 404s', async () => {
    apiStub.getSummary.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 404 })));
    const fixture = await createAdapter(frameOf());
    const host = fixture.nativeElement as HTMLElement;

    expect(host.querySelector('[data-testid="overlay-word-type-not-found"]')).not.toBeNull();
    expect(host.querySelector('[data-testid="overlay-word-type-ayahs-view"]')).toBeNull();
  });

  it('never touches the page facade singleton state', async () => {
    const facade = TestBed.inject(WordTypesDetailFacade);
    const fixture = await createAdapter(frameOf());
    const host = fixture.nativeElement as HTMLElement;

    (host.querySelector('[data-testid="word-type-details-tab-surahs"]') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(facade.panelState().status).toBe('idle');
    expect(facade.panelState().selection).toBeNull();
    expect(facade.panelState().summary).toBeNull();
  });
});
