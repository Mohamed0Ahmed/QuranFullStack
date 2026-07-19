import { Injectable } from '@angular/core';
import { Observable, Subscription } from 'rxjs';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { LemmasApi } from '../data-access/lemmas.api';
import {
  LEMMAS_ERROR_LABEL,
  LEMMAS_NOT_FOUND_LABEL,
} from '../models/lemmas.labels';
import {
  DEFAULT_LEMMA_DETAIL_PAGE,
  DEFAULT_LEMMA_SURAHS_VIEW,
  DEFAULT_LEMMA_VIEW,
  DEFAULT_LEMMA_WORD_VIEW,
  LemmaSummaryDto,
  LemmaSurahView,
  LemmaView,
  LemmaWordView,
  LemmasPanelState,
  isPaginatedLemmaView,
} from '../models/lemmas.models';
import { AbstractDetailController } from './abstract-detail.controller';
import { LemmasCache, LemmasCacheKeys } from './lemmas-cache';
import {
  buildAyahsPanelUpdate,
  buildDetailErrorUpdate,
  buildMentionedSurahsPanelUpdate,
  buildMissingSurahsPanelUpdate,
  buildStemsPanelUpdate,
  buildWordsPanelUpdate,
  extractPanelErrorMessage,
  restoredLemmaNotFoundUpdate,
} from './lemmas-detail-panel.updates';
import { LemmasDetailViewHandlers, LemmasDetailViewLoader } from './lemmas-detail-view.loader';

const INITIAL_PANEL: LemmasPanelState = {
  selectedLemmaId: null,
  summary: null,
  view: DEFAULT_LEMMA_VIEW,
  wordView: DEFAULT_LEMMA_WORD_VIEW,
  surahView: DEFAULT_LEMMA_SURAHS_VIEW,
  ayahTypeCode: null,
  detailPage: DEFAULT_LEMMA_DETAIL_PAGE,
  ayahs: null,
  words: null,
  mentionedSurahs: null,
  missingSurahs: null,
  stems: null,
  status: 'idle',
  errorMessage: '',
};

// Complete lemma detail identity: every field participates in equality. Unlike
// roots, the ayahs view carries a `typeCode` filter, so it is part of the identity
// (and of LemmasCacheKeys.ayahs).
export interface LemmasDetailUrlState {
  readonly lemmaId: number;
  readonly view: LemmaView;
  readonly wordView: LemmaWordView;
  readonly surahView: LemmaSurahView;
  readonly detailPage: number;
  readonly typeCode: string | null;
}

export function lemmasDetailUrlStatesEqual(
  current: LemmasDetailUrlState | null,
  next: LemmasDetailUrlState | null,
): boolean {
  if (current === null || next === null) {
    return current === next;
  }

  return (
    current.lemmaId === next.lemmaId &&
    current.view === next.view &&
    current.wordView === next.wordView &&
    current.surahView === next.surahView &&
    current.detailPage === next.detailPage &&
    current.typeCode === next.typeCode
  );
}

// Lemma detail controller (Feature 029 B4; consolidated onto
// AbstractDetailController in Feature 033 DRY). Sibling of RootsDetailController.
// The root-scoped LemmasApi/LemmasCache/LemmasDetailViewLoader collaborators stay
// shared, so the explorer side panel and the global overlay de-duplicate the same
// reads. Not providedIn 'root': the page facade owns one instance, and each
// overlay adapter provides its own component-scoped instance (destroyed with it).
@Injectable()
export class LemmasDetailController extends AbstractDetailController<
  LemmasPanelState,
  LemmasDetailUrlState,
  LemmaSummaryDto,
  LemmasDetailViewHandlers
> {
  constructor(
    private readonly api: LemmasApi,
    private readonly cache: LemmasCache,
    private readonly viewLoader: LemmasDetailViewLoader,
  ) {
    super(INITIAL_PANEL);
  }

  selectLemma(summary: LemmaSummaryDto, view: LemmaView = DEFAULT_LEMMA_VIEW): void {
    this.selectLemmaWithPanel(summary, view);
  }

  selectLemmaWithPanel(
    summary: LemmaSummaryDto,
    view: LemmaView,
    wordView: LemmaWordView = DEFAULT_LEMMA_WORD_VIEW,
    surahView: LemmaSurahView = DEFAULT_LEMMA_SURAHS_VIEW,
    detailPage: number = DEFAULT_LEMMA_DETAIL_PAGE,
    ayahTypeCode: string | null = null,
  ): void {
    const token = this.requests.beginTransition();
    const nextState: LemmasDetailUrlState = {
      lemmaId: summary.id,
      view,
      wordView,
      surahView,
      detailPage,
      typeCode: ayahTypeCode,
    };
    this.activeUrlState = nextState;
    this._panel.set({
      ...INITIAL_PANEL,
      selectedLemmaId: summary.id,
      summary,
      view,
      wordView,
      surahView,
      ayahTypeCode,
      detailPage,
      status: 'loading',
    });
    this.loadActiveView(nextState, token);
  }

  setAyahTypeCode(typeCode: string | null): void {
    const current = this._panel();
    if (current.selectedLemmaId === null || current.summary === null || current.view !== 'ayahs') {
      return;
    }

    const normalizedTypeCode = this.normalizeTypeCode(typeCode);
    if (normalizedTypeCode === current.ayahTypeCode && current.detailPage === DEFAULT_LEMMA_DETAIL_PAGE) {
      return;
    }

    const token = this.requests.beginTransition();
    const nextState: LemmasDetailUrlState = {
      lemmaId: current.selectedLemmaId,
      view: 'ayahs',
      wordView: current.wordView,
      surahView: current.surahView,
      detailPage: DEFAULT_LEMMA_DETAIL_PAGE,
      typeCode: normalizedTypeCode,
    };
    this.activeUrlState = nextState;
    this._panel.update((s) => ({
      ...s,
      ayahTypeCode: normalizedTypeCode,
      detailPage: DEFAULT_LEMMA_DETAIL_PAGE,
      status: 'loading',
      errorMessage: '',
    }));
    this.loadActiveView(nextState, token);
  }

  setView(view: LemmaView): void {
    const current = this._panel();
    if (current.selectedLemmaId === null || current.summary === null || view === current.view) {
      return;
    }

    const detailPage = DEFAULT_LEMMA_DETAIL_PAGE;
    const wordView = view === 'words' ? current.wordView : DEFAULT_LEMMA_WORD_VIEW;
    const surahView = view === 'surahs' ? current.surahView : DEFAULT_LEMMA_SURAHS_VIEW;

    const token = this.requests.beginTransition();
    const nextState: LemmasDetailUrlState = {
      lemmaId: current.selectedLemmaId,
      view,
      wordView,
      surahView,
      detailPage,
      typeCode: null,
    };
    this.activeUrlState = nextState;
    this._panel.update((s) => ({
      ...s,
      view,
      wordView,
      surahView,
      ayahTypeCode: null,
      detailPage,
      status: 'loading',
      errorMessage: '',
    }));
    this.loadActiveView(nextState, token);
  }

  setWordView(wordView: LemmaWordView): void {
    const current = this._panel();
    if (
      current.selectedLemmaId === null ||
      current.summary === null ||
      current.view !== 'words' ||
      wordView === current.wordView
    ) {
      return;
    }

    const token = this.requests.beginTransition();
    const nextState: LemmasDetailUrlState = {
      lemmaId: current.selectedLemmaId,
      view: 'words',
      wordView,
      surahView: current.surahView,
      detailPage: DEFAULT_LEMMA_DETAIL_PAGE,
      typeCode: null,
    };
    this.activeUrlState = nextState;
    this._panel.update((s) => ({
      ...s,
      wordView,
      detailPage: DEFAULT_LEMMA_DETAIL_PAGE,
      status: 'loading',
      errorMessage: '',
    }));
    this.loadActiveView(nextState, token);
  }

  setSurahView(surahView: LemmaSurahView): void {
    const current = this._panel();
    if (
      current.selectedLemmaId === null ||
      current.summary === null ||
      current.view !== 'surahs' ||
      surahView === current.surahView
    ) {
      return;
    }

    const token = this.requests.beginTransition();
    const nextState: LemmasDetailUrlState = {
      lemmaId: current.selectedLemmaId,
      view: 'surahs',
      wordView: current.wordView,
      surahView,
      detailPage: current.detailPage,
      typeCode: null,
    };
    this.activeUrlState = nextState;
    this._panel.update((s) => ({
      ...s,
      surahView,
      status: 'loading',
      errorMessage: '',
    }));
    this.loadActiveView(nextState, token);
  }

  setDetailPage(page: number): void {
    const current = this._panel();
    if (current.selectedLemmaId === null || current.summary === null || page < 1) {
      return;
    }

    if (!isPaginatedLemmaView(current.view)) {
      return;
    }

    const token = this.requests.beginTransition();
    const nextState: LemmasDetailUrlState = {
      lemmaId: current.selectedLemmaId,
      view: current.view,
      wordView: current.wordView,
      surahView: current.surahView,
      detailPage: page,
      typeCode: current.view === 'ayahs' ? current.ayahTypeCode : null,
    };
    this.activeUrlState = nextState;
    this._panel.update((s) => ({
      ...s,
      detailPage: page,
      status: 'loading',
      errorMessage: '',
    }));
    this.loadActiveView(nextState, token);
  }

  protected override readonly notFoundLabel = LEMMAS_NOT_FOUND_LABEL;
  protected override readonly errorLabel = LEMMAS_ERROR_LABEL;

  protected override urlStatesEqual(a: LemmasDetailUrlState | null, b: LemmasDetailUrlState | null): boolean {
    return lemmasDetailUrlStatesEqual(a, b);
  }

  protected override sameIdentity(current: LemmasPanelState, state: LemmasDetailUrlState): boolean {
    return current.selectedLemmaId === state.lemmaId && current.summary !== null;
  }

  protected override applyUrlStateFields(panel: LemmasPanelState, state: LemmasDetailUrlState): LemmasPanelState {
    return {
      ...panel,
      selectedLemmaId: state.lemmaId,
      view: state.view,
      wordView: state.wordView,
      surahView: state.surahView,
      ayahTypeCode: state.typeCode,
      detailPage: state.detailPage,
    };
  }

  protected override applySummary(state: LemmasDetailUrlState, data: LemmaSummaryDto): Partial<LemmasPanelState> {
    return { summary: data, ayahTypeCode: state.typeCode };
  }

  protected override loadSummary(state: LemmasDetailUrlState): Observable<ApiResponse<LemmaSummaryDto>> {
    return this.cache.getOrLoad(LemmasCacheKeys.summary(state.lemmaId), () =>
      this.api.getLemmaSummary(state.lemmaId),
    );
  }

  protected override notFoundPanel(state: LemmasDetailUrlState, message: string): LemmasPanelState {
    return restoredLemmaNotFoundUpdate(message, LEMMAS_NOT_FOUND_LABEL, state.lemmaId);
  }

  protected override errorPanel(state: LemmasDetailUrlState, message: string): LemmasPanelState {
    return {
      ...INITIAL_PANEL,
      selectedLemmaId: state.lemmaId,
      status: 'error',
      errorMessage: message || LEMMAS_ERROR_LABEL,
    };
  }

  protected override extractErrorMessage(err: unknown, fallback: string): string {
    return extractPanelErrorMessage(err, fallback);
  }

  protected override requestActiveView(
    state: LemmasDetailUrlState,
    handlers: LemmasDetailViewHandlers,
  ): Subscription | undefined {
    const current = this._panel();
    return this.viewLoader.loadActiveView(
      {
        lemmaId: state.lemmaId,
        view: state.view,
        wordView: state.wordView,
        surahView: state.surahView,
        ayahTypeCode: state.typeCode,
        detailPage: state.detailPage,
        cachedMissingSurahs: current.missingSurahs,
      },
      handlers,
    );
  }

  protected override buildViewHandlers(token: number): LemmasDetailViewHandlers {
    return {
      onAyahs: (response) => this.applyIfCurrent(token, (s) => ({ ...s, ...buildAyahsPanelUpdate(response) })),
      onWords: (response) => this.applyIfCurrent(token, (s) => ({ ...s, ...buildWordsPanelUpdate(response) })),
      onMentionedSurahs: (response) =>
        this.applyIfCurrent(token, (s) => ({ ...s, ...buildMentionedSurahsPanelUpdate(response) })),
      onMissingSurahs: (response) =>
        this.applyIfCurrent(token, (s) => ({ ...s, ...buildMissingSurahsPanelUpdate(response) })),
      onStems: (response) => this.applyIfCurrent(token, (s) => ({ ...s, ...buildStemsPanelUpdate(response) })),
      onError: (err) =>
        this.applyIfCurrent(token, (s) => ({ ...s, ...buildDetailErrorUpdate(err, LEMMAS_ERROR_LABEL) })),
    };
  }

  private normalizeTypeCode(typeCode: string | null): string | null {
    return typeCode === null || typeCode.trim().length === 0 ? null : typeCode.trim();
  }
}
