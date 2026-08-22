import { Injectable } from '@angular/core';
import { Observable, Subscription } from 'rxjs';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { StemsApi } from '../data-access/stems.api';
import {
  STEMS_ERROR_LABEL,
  STEMS_NOT_FOUND_LABEL,
} from '../models/stems.labels';
import {
  DEFAULT_STEM_DETAIL_PAGE,
  DEFAULT_STEM_SURAHS_VIEW,
  DEFAULT_STEM_VIEW,
  DEFAULT_STEM_WORD_VIEW,
  StemSummaryDto,
  StemSurahView,
  StemView,
  StemWordView,
  StemsPanelState,
  isPaginatedStemView,
} from '../models/stems.models';
import { AbstractDetailController } from './abstract-detail.controller';
import { StemsCache, StemsCacheKeys } from './stems-cache';
import {
  buildAyahsPanelUpdate,
  buildDetailErrorUpdate,
  buildLemmasPanelUpdate,
  buildMentionedSurahsPanelUpdate,
  buildMissingSurahsPanelUpdate,
  buildWordsPanelUpdate,
  extractPanelErrorMessage,
  restoredStemNotFoundUpdate,
} from './stems-detail-panel.updates';
import { StemsDetailViewHandlers, StemsDetailViewLoader } from './stems-detail-view.loader';

const INITIAL_PANEL: StemsPanelState = {
  selectedStemId: null,
  summary: null,
  view: DEFAULT_STEM_VIEW,
  wordView: DEFAULT_STEM_WORD_VIEW,
  surahView: DEFAULT_STEM_SURAHS_VIEW,
  ayahTypeCode: null,
  detailPage: DEFAULT_STEM_DETAIL_PAGE,
  ayahs: null,
  words: null,
  mentionedSurahs: null,
  missingSurahs: null,
  lemmas: null,
  status: 'idle',
  errorMessage: '',
};

export interface StemsDetailUrlState {
  readonly stemId: number;
  readonly view: StemView;
  readonly wordView: StemWordView;
  readonly surahView: StemSurahView;
  readonly detailPage: number;
  readonly typeCode: string | null;
}

export function stemsDetailUrlStatesEqual(
  current: StemsDetailUrlState | null,
  next: StemsDetailUrlState | null,
): boolean {
  if (current === null || next === null) {
    return current === next;
  }

  return (
    current.stemId === next.stemId &&
    current.view === next.view &&
    current.wordView === next.wordView &&
    current.surahView === next.surahView &&
    current.detailPage === next.detailPage &&
    current.typeCode === next.typeCode
  );
}

// Not providedIn: 'root': the page facade owns one instance and each overlay adapter provides
// its own component-scoped instance (destroyed with the adapter), so their panel state stays isolated.
// Every complete-identity transition abandons both the in-flight summary and detail request (new
// generation), so a late response from a previously selected stem can never overwrite this one.
@Injectable()
export class StemsDetailController extends AbstractDetailController<
  StemsPanelState,
  StemsDetailUrlState,
  StemSummaryDto,
  StemsDetailViewHandlers
> {
  constructor(
    private readonly api: StemsApi,
    private readonly cache: StemsCache,
    private readonly viewLoader: StemsDetailViewLoader,
  ) {
    super(INITIAL_PANEL);
  }

  selectStem(summary: StemSummaryDto, view: StemView = DEFAULT_STEM_VIEW): void {
    this.selectStemWithPanel(summary, view);
  }

  selectStemWithPanel(
    summary: StemSummaryDto,
    view: StemView,
    wordView: StemWordView = DEFAULT_STEM_WORD_VIEW,
    surahView: StemSurahView = DEFAULT_STEM_SURAHS_VIEW,
    detailPage: number = DEFAULT_STEM_DETAIL_PAGE,
  ): void {
    const token = this.requests.beginTransition();
    const nextState: StemsDetailUrlState = {
      stemId: summary.id,
      view,
      wordView,
      surahView,
      detailPage,
      typeCode: null,
    };
    this.activeUrlState = nextState;
    this._panel.set({
      ...INITIAL_PANEL,
      selectedStemId: summary.id,
      summary,
      view,
      wordView,
      surahView,
      detailPage,
      status: 'loading',
    });
    this.loadActiveView(nextState, token);
  }

  setView(view: StemView): void {
    const current = this._panel();
    if (current.selectedStemId === null || current.summary === null || view === current.view) {
      return;
    }

    const detailPage = DEFAULT_STEM_DETAIL_PAGE;
    const wordView = view === 'words' ? current.wordView : DEFAULT_STEM_WORD_VIEW;
    const surahView = view === 'surahs' ? current.surahView : DEFAULT_STEM_SURAHS_VIEW;
    const typeCode = view === 'ayahs' || view === 'words' ? current.ayahTypeCode : null;

    const token = this.requests.beginTransition();
    const nextState: StemsDetailUrlState = {
      stemId: current.selectedStemId,
      view,
      wordView,
      surahView,
      detailPage,
      typeCode,
    };
    this.activeUrlState = nextState;
    this._panel.update((s) => ({
      ...s,
      view,
      wordView,
      surahView,
      ayahTypeCode: typeCode,
      detailPage,
      status: 'loading',
      errorMessage: '',
    }));
    this.loadActiveView(nextState, token);
  }

  setWordView(wordView: StemWordView): void {
    const current = this._panel();
    if (
      current.selectedStemId === null ||
      current.summary === null ||
      current.view !== 'words' ||
      wordView === current.wordView
    ) {
      return;
    }

    const token = this.requests.beginTransition();
    const nextState: StemsDetailUrlState = {
      stemId: current.selectedStemId,
      view: 'words',
      wordView,
      surahView: current.surahView,
      detailPage: DEFAULT_STEM_DETAIL_PAGE,
      typeCode: current.ayahTypeCode,
    };
    this.activeUrlState = nextState;
    this._panel.update((s) => ({
      ...s,
      wordView,
      detailPage: DEFAULT_STEM_DETAIL_PAGE,
      status: 'loading',
      errorMessage: '',
    }));
    this.loadActiveView(nextState, token);
  }

  setSurahView(surahView: StemSurahView): void {
    const current = this._panel();
    if (
      current.selectedStemId === null ||
      current.summary === null ||
      current.view !== 'surahs' ||
      surahView === current.surahView
    ) {
      return;
    }

    const token = this.requests.beginTransition();
    const nextState: StemsDetailUrlState = {
      stemId: current.selectedStemId,
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
      ayahTypeCode: null,
      status: 'loading',
      errorMessage: '',
    }));
    this.loadActiveView(nextState, token);
  }

  setDetailPage(page: number): void {
    const current = this._panel();
    if (current.selectedStemId === null || current.summary === null || page < 1) {
      return;
    }

    if (!isPaginatedStemView(current.view)) {
      return;
    }

    const token = this.requests.beginTransition();
    const nextState: StemsDetailUrlState = {
      stemId: current.selectedStemId,
      view: current.view,
      wordView: current.wordView,
      surahView: current.surahView,
      detailPage: page,
      typeCode: current.view === 'ayahs' || current.view === 'words' ? current.ayahTypeCode : null,
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

  setAyahTypeCode(typeCode: string | null): void {
    const current = this._panel();
    if (
      current.selectedStemId === null ||
      current.summary === null ||
      (current.view !== 'ayahs' && current.view !== 'words')
    ) {
      return;
    }

    const normalizedTypeCode = this.normalizeTypeCode(typeCode);
    if (normalizedTypeCode === current.ayahTypeCode && current.detailPage === DEFAULT_STEM_DETAIL_PAGE) {
      return;
    }

    const token = this.requests.beginTransition();
    const nextState: StemsDetailUrlState = {
      stemId: current.selectedStemId,
      view: current.view,
      wordView: current.wordView,
      surahView: current.surahView,
      detailPage: DEFAULT_STEM_DETAIL_PAGE,
      typeCode: normalizedTypeCode,
    };
    this.activeUrlState = nextState;
    this._panel.update((s) => ({
      ...s,
      ayahTypeCode: normalizedTypeCode,
      detailPage: DEFAULT_STEM_DETAIL_PAGE,
      status: 'loading',
      errorMessage: '',
    }));
    this.loadActiveView(nextState, token);
  }

  protected override readonly notFoundLabel = STEMS_NOT_FOUND_LABEL;
  protected override readonly errorLabel = STEMS_ERROR_LABEL;

  protected override urlStatesEqual(a: StemsDetailUrlState | null, b: StemsDetailUrlState | null): boolean {
    return stemsDetailUrlStatesEqual(a, b);
  }

  protected override sameIdentity(current: StemsPanelState, state: StemsDetailUrlState): boolean {
    return current.selectedStemId === state.stemId && current.summary !== null;
  }

  protected override applyUrlStateFields(panel: StemsPanelState, state: StemsDetailUrlState): StemsPanelState {
    return {
      ...panel,
      selectedStemId: state.stemId,
      view: state.view,
      wordView: state.wordView,
      surahView: state.surahView,
      ayahTypeCode: state.typeCode,
      detailPage: state.detailPage,
    };
  }

  protected override applySummary(state: StemsDetailUrlState, data: StemSummaryDto): Partial<StemsPanelState> {
    return { summary: data, ayahTypeCode: state.typeCode };
  }

  protected override loadSummary(state: StemsDetailUrlState): Observable<ApiResponse<StemSummaryDto>> {
    return this.cache.getOrLoad(StemsCacheKeys.summary(state.stemId), () => this.api.getStemSummary(state.stemId));
  }

  protected override notFoundPanel(state: StemsDetailUrlState, message: string): StemsPanelState {
    return restoredStemNotFoundUpdate(message, STEMS_NOT_FOUND_LABEL, state.stemId);
  }

  protected override errorPanel(state: StemsDetailUrlState, message: string): StemsPanelState {
    return {
      ...INITIAL_PANEL,
      selectedStemId: state.stemId,
      status: 'error',
      errorMessage: message || STEMS_ERROR_LABEL,
    };
  }

  protected override extractErrorMessage(err: unknown, fallback: string): string {
    return extractPanelErrorMessage(err, fallback);
  }

  protected override requestActiveView(
    state: StemsDetailUrlState,
    handlers: StemsDetailViewHandlers,
  ): Subscription | undefined {
    const current = this._panel();
    return this.viewLoader.loadActiveView(
      {
        stemId: state.stemId,
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

  protected override buildViewHandlers(token: number): StemsDetailViewHandlers {
    return {
      onAyahs: (response) => this.applyIfCurrent(token, (s) => ({ ...s, ...buildAyahsPanelUpdate(response) })),
      onWords: (response) => this.applyIfCurrent(token, (s) => ({ ...s, ...buildWordsPanelUpdate(response) })),
      onMentionedSurahs: (response) =>
        this.applyIfCurrent(token, (s) => ({ ...s, ...buildMentionedSurahsPanelUpdate(response) })),
      onMissingSurahs: (response) =>
        this.applyIfCurrent(token, (s) => ({ ...s, ...buildMissingSurahsPanelUpdate(response) })),
      onLemmas: (response) => this.applyIfCurrent(token, (s) => ({ ...s, ...buildLemmasPanelUpdate(response) })),
      onError: (err) =>
        this.applyIfCurrent(token, (s) => ({ ...s, ...buildDetailErrorUpdate(err, STEMS_ERROR_LABEL) })),
    };
  }

  private normalizeTypeCode(typeCode: string | null): string | null {
    return typeCode === null || typeCode.trim().length === 0 ? null : typeCode.trim();
  }
}
