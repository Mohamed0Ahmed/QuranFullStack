import { Injectable, OnDestroy, computed, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { of } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';

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
import { DetailRequestLifecycle } from './detail-request-lifecycle';
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
import { StemsDetailViewLoader } from './stems-detail-view.loader';

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

/**
 * Complete stem detail identity: every field participates in equality. Unlike
 * roots, the ayahs view carries a `typeCode` filter, so it is part of the
 * identity (and of `StemsCacheKeys.ayahs`).
 */
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

/**
 * Route-independent stem detail controller (Feature 029, Change B4). Sibling
 * of `RootsDetailController` — see that class for the pattern rationale.
 *
 * Owns the stem detail panel signal state, the summary/detail subscriptions,
 * and every load path — with zero knowledge of routes or URLs. Consumers drive
 * it either through `applyUrlState` (the route-free entry point: the page
 * facade forwards parsed query state, an overlay adapter forwards its typed
 * frame) or through the direct selection methods. The root-scoped
 * `StemsApi`/`StemsCache`/`StemsDetailViewLoader` collaborators stay shared,
 * so the explorer side panel and the global overlay de-duplicate the same
 * reads.
 *
 * Every complete-identity transition abandons BOTH the summary and the detail
 * request and opens a new generation, so a late response from the previously
 * selected stem can never populate or overwrite this one — see
 * {@link DetailRequestLifecycle}. Not `providedIn: 'root'`: the page facade owns
 * one instance, and each overlay adapter provides its own component-scoped
 * instance (destroyed with the adapter).
 */
@Injectable()
export class StemsDetailController implements OnDestroy {
  private readonly _panel = signal<StemsPanelState>(INITIAL_PANEL);

  private readonly requests = new DetailRequestLifecycle();
  private activeUrlState: StemsDetailUrlState | null = null;

  readonly panelState = computed(() => this._panel());

  constructor(
    private readonly api: StemsApi,
    private readonly cache: StemsCache,
    private readonly viewLoader: StemsDetailViewLoader,
  ) {}

  ngOnDestroy(): void {
    this.cancelPendingLoads();
  }

  /**
   * Route-free entry point: synchronize the panel to a complete detail state
   * (`null` clears the selection). Identical states short-circuit via complete
   * identity comparison, leaving an in-flight load for that identity alone.
   */
  applyUrlState(state: StemsDetailUrlState | null): void {
    if (state === null) {
      this.clearSelection();
      return;
    }

    if (stemsDetailUrlStatesEqual(this.activeUrlState, state)) {
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
    this.activeUrlState = { stemId: summary.id, view, wordView, surahView, detailPage, typeCode: null };
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
    this.loadActiveView(summary.id, view, wordView, surahView, detailPage, null, token);
  }

  clearSelection(): void {
    this.requests.cancelAll();
    this.activeUrlState = null;
    this._panel.set(INITIAL_PANEL);
  }

  setView(view: StemView): void {
    const current = this._panel();
    if (current.selectedStemId === null || current.summary === null || view === current.view) {
      return;
    }

    const detailPage = DEFAULT_STEM_DETAIL_PAGE;
    const wordView = view === 'words' ? current.wordView : DEFAULT_STEM_WORD_VIEW;
    const surahView = view === 'surahs' ? current.surahView : DEFAULT_STEM_SURAHS_VIEW;

    const token = this.requests.beginTransition();
    this.activeUrlState = {
      stemId: current.selectedStemId,
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
    this.loadActiveView(current.selectedStemId, view, wordView, surahView, detailPage, null, token);
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
    this.activeUrlState = {
      stemId: current.selectedStemId,
      view: 'words',
      wordView,
      surahView: current.surahView,
      detailPage: DEFAULT_STEM_DETAIL_PAGE,
      typeCode: null,
    };
    this._panel.update((s) => ({
      ...s,
      wordView,
      detailPage: DEFAULT_STEM_DETAIL_PAGE,
      ayahTypeCode: null,
      status: 'loading',
      errorMessage: '',
    }));
    this.loadActiveView(
      current.selectedStemId,
      'words',
      wordView,
      current.surahView,
      DEFAULT_STEM_DETAIL_PAGE,
      null,
      token,
    );
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
    this.activeUrlState = {
      stemId: current.selectedStemId,
      view: 'surahs',
      wordView: current.wordView,
      surahView,
      detailPage: current.detailPage,
      typeCode: null,
    };
    this._panel.update((s) => ({
      ...s,
      surahView,
      ayahTypeCode: null,
      status: 'loading',
      errorMessage: '',
    }));
    this.loadActiveView(
      current.selectedStemId,
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
    if (current.selectedStemId === null || current.summary === null || page < 1) {
      return;
    }

    if (!isPaginatedStemView(current.view)) {
      return;
    }

    const token = this.requests.beginTransition();
    this.activeUrlState = {
      stemId: current.selectedStemId,
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
      current.selectedStemId,
      current.view,
      current.wordView,
      current.surahView,
      page,
      current.view === 'ayahs' ? current.ayahTypeCode : null,
      token,
    );
  }

  setAyahTypeCode(typeCode: string | null): void {
    const current = this._panel();
    if (current.selectedStemId === null || current.summary === null || current.view !== 'ayahs') {
      return;
    }

    const normalizedTypeCode = this.normalizeTypeCode(typeCode);
    if (normalizedTypeCode === current.ayahTypeCode && current.detailPage === DEFAULT_STEM_DETAIL_PAGE) {
      return;
    }

    const token = this.requests.beginTransition();
    this.activeUrlState = {
      stemId: current.selectedStemId,
      view: 'ayahs',
      wordView: current.wordView,
      surahView: current.surahView,
      detailPage: DEFAULT_STEM_DETAIL_PAGE,
      typeCode: normalizedTypeCode,
    };
    this._panel.update((s) => ({
      ...s,
      ayahTypeCode: normalizedTypeCode,
      detailPage: DEFAULT_STEM_DETAIL_PAGE,
      status: 'loading',
      errorMessage: '',
    }));
    this.loadActiveView(
      current.selectedStemId,
      'ayahs',
      current.wordView,
      current.surahView,
      DEFAULT_STEM_DETAIL_PAGE,
      normalizedTypeCode,
      token,
    );
  }

  /**
   * Drives a complete identity: abandons the previous identity's summary and
   * detail requests, then either reloads only the active view (same stem, loaded
   * summary) or reloads the summary first.
   */
  private applyIdentity(state: StemsDetailUrlState): void {
    const token = this.requests.beginTransition();
    this.activeUrlState = state;
    const current = this._panel();

    if (current.selectedStemId === state.stemId && current.summary !== null) {
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
        state.stemId,
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

  private loadSummaryAndRestore(state: StemsDetailUrlState, token: number): void {
    this._panel.set({
      ...INITIAL_PANEL,
      selectedStemId: state.stemId,
      view: state.view,
      wordView: state.wordView,
      surahView: state.surahView,
      ayahTypeCode: state.typeCode,
      detailPage: state.detailPage,
      status: 'loading',
    });

    this.requests.trackSummary(
      this.cache
        .getOrLoad(StemsCacheKeys.summary(state.stemId), () => this.api.getStemSummary(state.stemId))
        .pipe(
          tap((response) => {
            if (!this.requests.isCurrent(token)) {
              return;
            }

            if (!response.isSuccess || !response.data) {
              this.handleRestoredStemNotFound(response.message ?? '', state);
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
              state.stemId,
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
              this.handleRestoredStemNotFound(this.extractErrorMessage(err, STEMS_NOT_FOUND_LABEL), state);
              return of(undefined);
            }

            this.handleRestoredStemLoadError(this.extractErrorMessage(err, STEMS_ERROR_LABEL), state);
            return of(undefined);
          }),
        )
        .subscribe(),
    );
  }

  private loadActiveView(
    stemId: number,
    view: StemView,
    wordView: StemWordView,
    surahView: StemSurahView,
    detailPage: number,
    ayahTypeCode: string | null,
    token: number,
  ): void {
    const current = this._panel();
    this.requests.trackDetail(
      this.viewLoader.loadActiveView(
        {
          stemId,
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
          onLemmas: (response) => this.applyIfCurrent(token, (s) => ({ ...s, ...buildLemmasPanelUpdate(response) })),
          onError: (err) =>
            this.applyIfCurrent(token, (s) => ({ ...s, ...buildDetailErrorUpdate(err, STEMS_ERROR_LABEL) })),
        },
      ),
    );
  }

  /** Applies a panel update only while `token` still owns the panel. */
  private applyIfCurrent(token: number, update: (state: StemsPanelState) => StemsPanelState): void {
    if (this.requests.isCurrent(token)) {
      this._panel.update(update);
    }
  }

  private handleRestoredStemNotFound(message: string, state: StemsDetailUrlState): void {
    this._panel.set(restoredStemNotFoundUpdate(message, STEMS_NOT_FOUND_LABEL, state.stemId));
  }

  private handleRestoredStemLoadError(message: string, state: StemsDetailUrlState): void {
    this._panel.set({
      ...INITIAL_PANEL,
      selectedStemId: state.stemId,
      status: 'error',
      errorMessage: message || STEMS_ERROR_LABEL,
    });
  }

  private extractErrorMessage(err: unknown, fallback: string): string {
    return extractPanelErrorMessage(err, fallback);
  }

  private normalizeTypeCode(typeCode: string | null): string | null {
    return typeCode === null || typeCode.trim().length === 0 ? null : typeCode.trim();
  }
}
