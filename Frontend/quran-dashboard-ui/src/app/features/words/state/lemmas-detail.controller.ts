import { Injectable, OnDestroy, computed, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { of } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';

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
import { DetailRequestLifecycle } from './detail-request-lifecycle';
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
import { LemmasDetailViewLoader } from './lemmas-detail-view.loader';

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

/**
 * Complete lemma detail identity: every field participates in equality. Unlike
 * roots, the ayahs view carries a `typeCode` filter, so it is part of the
 * identity (and of `LemmasCacheKeys.ayahs`).
 */
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

/**
 * Route-independent lemma detail controller (Feature 029, Change B4). Sibling
 * of `RootsDetailController` — see that class for the pattern rationale.
 *
 * Owns the lemma detail panel signal state, the summary/detail subscriptions,
 * and every load path — with zero knowledge of routes or URLs. Consumers drive
 * it either through `applyUrlState` (the route-free entry point: the page
 * facade forwards parsed query state, an overlay adapter forwards its typed
 * frame) or through the direct selection methods. The root-scoped
 * `LemmasApi`/`LemmasCache`/`LemmasDetailViewLoader` collaborators stay shared,
 * so the explorer side panel and the global overlay de-duplicate the same
 * reads.
 *
 * Every complete-identity transition abandons BOTH the summary and the detail
 * request and opens a new generation, so a late response from the previously
 * selected lemma can never populate or overwrite this one — see
 * {@link DetailRequestLifecycle}. Not `providedIn: 'root'`: the page facade owns
 * one instance, and each overlay adapter provides its own component-scoped
 * instance (destroyed with the adapter).
 */
@Injectable()
export class LemmasDetailController implements OnDestroy {
  private readonly _panel = signal<LemmasPanelState>(INITIAL_PANEL);

  private readonly requests = new DetailRequestLifecycle();
  private activeUrlState: LemmasDetailUrlState | null = null;

  readonly panelState = computed(() => this._panel());

  constructor(
    private readonly api: LemmasApi,
    private readonly cache: LemmasCache,
    private readonly viewLoader: LemmasDetailViewLoader,
  ) {}

  ngOnDestroy(): void {
    this.cancelPendingLoads();
  }

  /**
   * Route-free entry point: synchronize the panel to a complete detail state
   * (`null` clears the selection). Identical states short-circuit via complete
   * identity comparison, leaving an in-flight load for that identity alone.
   */
  applyUrlState(state: LemmasDetailUrlState | null): void {
    if (state === null) {
      this.clearSelection();
      return;
    }

    if (lemmasDetailUrlStatesEqual(this.activeUrlState, state)) {
      return;
    }

    this.applyIdentity(state);
  }

  /**
   * Re-drives the current complete identity after a failed load (Feature 030,
   * M3). The identity is unchanged, so {@link applyUrlState} would short-circuit
   * it; retry re-enters the load path directly. A failed read is never cached,
   * so this issues a real request, while an intact summary still resolves from
   * cache and only the detail view reloads.
   */
  retryCurrentIdentity(): void {
    const state = this.activeUrlState;
    if (state === null) {
      return;
    }

    this.applyIdentity(state);
  }

  /** Cancels the pending summary/detail loads without resetting panel state. */
  cancelPendingLoads(): void {
    this.requests.cancelAll();
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
    this.activeUrlState = { lemmaId: summary.id, view, wordView, surahView, detailPage, typeCode: ayahTypeCode };
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
    this.loadActiveView(summary.id, view, wordView, surahView, detailPage, ayahTypeCode, token);
  }

  clearSelection(): void {
    this.requests.cancelAll();
    this.activeUrlState = null;
    this._panel.set(INITIAL_PANEL);
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
    this.activeUrlState = {
      lemmaId: current.selectedLemmaId,
      view: 'ayahs',
      wordView: current.wordView,
      surahView: current.surahView,
      detailPage: DEFAULT_LEMMA_DETAIL_PAGE,
      typeCode: normalizedTypeCode,
    };
    this._panel.update((s) => ({
      ...s,
      ayahTypeCode: normalizedTypeCode,
      detailPage: DEFAULT_LEMMA_DETAIL_PAGE,
      status: 'loading',
      errorMessage: '',
    }));
    this.loadActiveView(
      current.selectedLemmaId,
      'ayahs',
      current.wordView,
      current.surahView,
      DEFAULT_LEMMA_DETAIL_PAGE,
      normalizedTypeCode,
      token,
    );
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
    this.activeUrlState = {
      lemmaId: current.selectedLemmaId,
      view,
      wordView,
      surahView,
      detailPage,
      typeCode: null,
    };
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
    this.loadActiveView(current.selectedLemmaId, view, wordView, surahView, detailPage, null, token);
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
    this.activeUrlState = {
      lemmaId: current.selectedLemmaId,
      view: 'words',
      wordView,
      surahView: current.surahView,
      detailPage: DEFAULT_LEMMA_DETAIL_PAGE,
      typeCode: null,
    };
    this._panel.update((s) => ({
      ...s,
      wordView,
      detailPage: DEFAULT_LEMMA_DETAIL_PAGE,
      status: 'loading',
      errorMessage: '',
    }));
    this.loadActiveView(
      current.selectedLemmaId,
      'words',
      wordView,
      current.surahView,
      DEFAULT_LEMMA_DETAIL_PAGE,
      null,
      token,
    );
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
    this.activeUrlState = {
      lemmaId: current.selectedLemmaId,
      view: 'surahs',
      wordView: current.wordView,
      surahView,
      detailPage: current.detailPage,
      typeCode: null,
    };
    this._panel.update((s) => ({
      ...s,
      surahView,
      status: 'loading',
      errorMessage: '',
    }));
    this.loadActiveView(
      current.selectedLemmaId,
      'surahs',
      current.wordView,
      surahView,
      current.detailPage,
      null,
      token,
    );
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
    this.activeUrlState = {
      lemmaId: current.selectedLemmaId,
      view: current.view,
      wordView: current.wordView,
      surahView: current.surahView,
      detailPage: page,
      typeCode: current.view === 'ayahs' ? current.ayahTypeCode : null,
    };
    this._panel.update((s) => ({
      ...s,
      detailPage: page,
      status: 'loading',
      errorMessage: '',
    }));
    this.loadActiveView(
      current.selectedLemmaId,
      current.view,
      current.wordView,
      current.surahView,
      page,
      current.view === 'ayahs' ? current.ayahTypeCode : null,
      token,
    );
  }

  /**
   * Drives a complete identity: abandons the previous identity's summary and
   * detail requests, then either reloads only the active view (same lemma,
   * loaded summary) or reloads the summary first.
   */
  private applyIdentity(state: LemmasDetailUrlState): void {
    const token = this.requests.beginTransition();
    this.activeUrlState = state;
    const current = this._panel();

    if (current.selectedLemmaId === state.lemmaId && current.summary !== null) {
      this._panel.update((s) => ({
        ...s,
        view: state.view,
        wordView: state.wordView,
        surahView: state.surahView,
        ayahTypeCode: state.typeCode,
        detailPage: state.detailPage,
        status: 'loading',
        errorMessage: '',
      }));
      this.loadActiveView(
        state.lemmaId,
        state.view,
        state.wordView,
        state.surahView,
        state.detailPage,
        state.typeCode,
        token,
      );
      return;
    }

    this.loadSummaryAndRestore(state, token);
  }

  private loadSummaryAndRestore(state: LemmasDetailUrlState, token: number): void {
    this._panel.set({
      ...INITIAL_PANEL,
      selectedLemmaId: state.lemmaId,
      view: state.view,
      wordView: state.wordView,
      surahView: state.surahView,
      ayahTypeCode: state.typeCode,
      detailPage: state.detailPage,
      status: 'loading',
    });

    this.requests.trackSummary(
      this.cache
        .getOrLoad(LemmasCacheKeys.summary(state.lemmaId), () =>
          this.api.getLemmaSummary(state.lemmaId),
        )
        .pipe(
          tap((response) => {
            if (!this.requests.isCurrent(token)) {
              return;
            }

            if (!response.isSuccess || !response.data) {
              this.handleRestoredLemmaNotFound(response.message ?? '', state);
              return;
            }

            const summary = response.data;
            this._panel.update((s) => ({
              ...s,
              summary,
              ayahTypeCode: state.typeCode,
              status: 'loading',
            }));
            this.loadActiveView(
              state.lemmaId,
              state.view,
              state.wordView,
              state.surahView,
              state.detailPage,
              state.typeCode,
              token,
            );
          }),
          catchError((err) => {
            if (!this.requests.isCurrent(token)) {
              return of(undefined);
            }

            if (err instanceof HttpErrorResponse && err.status === 404) {
              this.handleRestoredLemmaNotFound(this.extractErrorMessage(err, LEMMAS_NOT_FOUND_LABEL), state);
              return of(undefined);
            }

            this.handleRestoredLemmaLoadError(this.extractErrorMessage(err, LEMMAS_ERROR_LABEL), state);
            return of(undefined);
          }),
        )
        .subscribe(),
    );
  }

  private loadActiveView(
    lemmaId: number,
    view: LemmaView,
    wordView: LemmaWordView,
    surahView: LemmaSurahView,
    detailPage: number,
    ayahTypeCode: string | null,
    token: number,
  ): void {
    const current = this._panel();
    this.requests.trackDetail(
      this.viewLoader.loadActiveView(
        {
          lemmaId,
          view,
          wordView,
          surahView,
          ayahTypeCode,
          detailPage,
          cachedMissingSurahs: current.missingSurahs,
        },
        {
          onAyahs: (response) => this.applyIfCurrent(token, (s) => ({ ...s, ...buildAyahsPanelUpdate(response) })),
          onWords: (response) => this.applyIfCurrent(token, (s) => ({ ...s, ...buildWordsPanelUpdate(response) })),
          onMentionedSurahs: (response) =>
            this.applyIfCurrent(token, (s) => ({ ...s, ...buildMentionedSurahsPanelUpdate(response) })),
          onMissingSurahs: (response) =>
            this.applyIfCurrent(token, (s) => ({ ...s, ...buildMissingSurahsPanelUpdate(response) })),
          onStems: (response) => this.applyIfCurrent(token, (s) => ({ ...s, ...buildStemsPanelUpdate(response) })),
          onError: (err) =>
            this.applyIfCurrent(token, (s) => ({ ...s, ...buildDetailErrorUpdate(err, LEMMAS_ERROR_LABEL) })),
        },
      ),
    );
  }

  /** Applies a panel update only while `token` still owns the panel. */
  private applyIfCurrent(token: number, update: (state: LemmasPanelState) => LemmasPanelState): void {
    if (this.requests.isCurrent(token)) {
      this._panel.update(update);
    }
  }

  private handleRestoredLemmaNotFound(message: string, state: LemmasDetailUrlState): void {
    this._panel.set(restoredLemmaNotFoundUpdate(message, LEMMAS_NOT_FOUND_LABEL, state.lemmaId));
  }

  private handleRestoredLemmaLoadError(message: string, state: LemmasDetailUrlState): void {
    this._panel.set({
      ...INITIAL_PANEL,
      selectedLemmaId: state.lemmaId,
      status: 'error',
      errorMessage: message || LEMMAS_ERROR_LABEL,
    });
  }

  private extractErrorMessage(err: unknown, fallback: string): string {
    return extractPanelErrorMessage(err, fallback);
  }

  private normalizeTypeCode(typeCode: string | null): string | null {
    return typeCode === null || typeCode.trim().length === 0 ? null : typeCode.trim();
  }
}
