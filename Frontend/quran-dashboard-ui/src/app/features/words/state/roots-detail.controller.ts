import { Injectable, OnDestroy, computed, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { of } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';

import { RootsApi } from '../data-access/roots.api';
import {
  ROOTS_ERROR_LABEL,
  ROOTS_NOT_FOUND_LABEL,
} from '../models/roots.labels';
import {
  DEFAULT_ROOT_DETAIL_PAGE,
  DEFAULT_ROOT_SURAHS_VIEW,
  DEFAULT_ROOT_VIEW,
  DEFAULT_ROOT_WORD_VIEW,
  RootSummaryDto,
  RootSurahView,
  RootView,
  RootWordView,
  RootsPanelState,
  isPaginatedRootView,
} from '../models/roots.models';
import { DetailRequestLifecycle } from './detail-request-lifecycle';
import { RootsCache, RootsCacheKeys } from './roots-cache';
import {
  buildAyahsPanelUpdate,
  buildDetailErrorUpdate,
  buildLemmasPanelUpdate,
  buildMentionedSurahsPanelUpdate,
  buildMissingSurahsPanelUpdate,
  buildStemsPanelUpdate,
  buildWordsPanelUpdate,
  extractPanelErrorMessage,
  restoredRootNotFoundUpdate,
} from './roots-detail-panel.updates';
import { RootsDetailViewLoader } from './roots-detail-view.loader';

const INITIAL_PANEL: RootsPanelState = {
  selectedRootId: null,
  summary: null,
  view: DEFAULT_ROOT_VIEW,
  wordView: DEFAULT_ROOT_WORD_VIEW,
  surahView: DEFAULT_ROOT_SURAHS_VIEW,
  detailPage: DEFAULT_ROOT_DETAIL_PAGE,
  ayahs: null,
  words: null,
  mentionedSurahs: null,
  missingSurahs: null,
  lemmas: null,
  stems: null,
  status: 'idle',
  errorMessage: '',
};

/** Complete root detail identity: every field participates in equality. */
export interface RootsDetailUrlState {
  readonly rootId: number;
  readonly view: RootView;
  readonly wordView: RootWordView;
  readonly surahView: RootSurahView;
  readonly detailPage: number;
}

export function rootsDetailUrlStatesEqual(
  current: RootsDetailUrlState | null,
  next: RootsDetailUrlState | null,
): boolean {
  if (current === null || next === null) {
    return current === next;
  }

  return (
    current.rootId === next.rootId &&
    current.view === next.view &&
    current.wordView === next.wordView &&
    current.surahView === next.surahView &&
    current.detailPage === next.detailPage
  );
}

/**
 * Route-independent root detail controller (Feature 029, Change B4).
 *
 * Owns the root detail panel signal state, the summary/detail subscriptions,
 * and every load path — with zero knowledge of routes or URLs. Consumers drive
 * it either through `applyUrlState` (the route-free entry point: the page
 * facade forwards parsed query state, an overlay adapter forwards its typed
 * frame) or through the direct selection methods. The root-scoped
 * `RootsApi`/`RootsCache`/`RootsDetailViewLoader` collaborators stay shared, so
 * the explorer side panel and the global overlay de-duplicate the same reads.
 *
 * Every complete-identity transition abandons BOTH the summary and the detail
 * request and opens a new generation, so a late response from the previously
 * selected root can never populate or overwrite this one — see
 * {@link DetailRequestLifecycle}. Not `providedIn: 'root'`: the page facade owns
 * one instance, and each overlay adapter provides its own component-scoped
 * instance (destroyed with the adapter).
 */
@Injectable()
export class RootsDetailController implements OnDestroy {
  private readonly _panel = signal<RootsPanelState>(INITIAL_PANEL);

  private readonly requests = new DetailRequestLifecycle();
  private activeUrlState: RootsDetailUrlState | null = null;

  readonly panelState = computed(() => this._panel());

  constructor(
    private readonly api: RootsApi,
    private readonly cache: RootsCache,
    private readonly viewLoader: RootsDetailViewLoader,
  ) {}

  ngOnDestroy(): void {
    this.cancelPendingLoads();
  }

  /**
   * Route-free entry point: synchronize the panel to a complete detail state
   * (`null` clears the selection). Identical states short-circuit via complete
   * identity comparison, leaving an in-flight load for that identity alone.
   */
  applyUrlState(state: RootsDetailUrlState | null): void {
    if (state === null) {
      this.clearSelection();
      return;
    }

    if (rootsDetailUrlStatesEqual(this.activeUrlState, state)) {
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

  selectRoot(summary: RootSummaryDto, view: RootView = DEFAULT_ROOT_VIEW): void {
    this.selectRootWithPanel(summary, view);
  }

  selectRootWithPanel(
    summary: RootSummaryDto,
    view: RootView,
    wordView: RootWordView = DEFAULT_ROOT_WORD_VIEW,
    surahView: RootSurahView = DEFAULT_ROOT_SURAHS_VIEW,
    detailPage: number = DEFAULT_ROOT_DETAIL_PAGE,
  ): void {
    const token = this.requests.beginTransition();
    this.activeUrlState = { rootId: summary.id, view, wordView, surahView, detailPage };
    this._panel.set({
      ...INITIAL_PANEL,
      selectedRootId: summary.id,
      summary,
      view,
      wordView,
      surahView,
      detailPage,
      status: 'loading',
    });
    this.loadActiveView(summary.id, view, wordView, surahView, detailPage, token);
  }

  clearSelection(): void {
    this.requests.cancelAll();
    this.activeUrlState = null;
    this._panel.set(INITIAL_PANEL);
  }

  setView(view: RootView): void {
    const current = this._panel();
    if (current.selectedRootId === null || current.summary === null || view === current.view) {
      return;
    }

    const detailPage = DEFAULT_ROOT_DETAIL_PAGE;
    const wordView = view === 'words' ? current.wordView : DEFAULT_ROOT_WORD_VIEW;
    const surahView = view === 'surahs' ? current.surahView : DEFAULT_ROOT_SURAHS_VIEW;

    const token = this.requests.beginTransition();
    this.activeUrlState = {
      rootId: current.selectedRootId,
      view,
      wordView,
      surahView,
      detailPage,
    };
    this._panel.update((s) => ({
      ...s,
      view,
      wordView,
      surahView,
      detailPage,
      status: 'loading',
      errorMessage: '',
    }));
    this.loadActiveView(current.selectedRootId, view, wordView, surahView, detailPage, token);
  }

  setWordView(wordView: RootWordView): void {
    const current = this._panel();
    if (
      current.selectedRootId === null ||
      current.summary === null ||
      current.view !== 'words' ||
      wordView === current.wordView
    ) {
      return;
    }

    const token = this.requests.beginTransition();
    this.activeUrlState = {
      rootId: current.selectedRootId,
      view: 'words',
      wordView,
      surahView: current.surahView,
      detailPage: DEFAULT_ROOT_DETAIL_PAGE,
    };
    this._panel.update((s) => ({
      ...s,
      wordView,
      detailPage: DEFAULT_ROOT_DETAIL_PAGE,
      status: 'loading',
      errorMessage: '',
    }));
    this.loadActiveView(
      current.selectedRootId,
      'words',
      wordView,
      current.surahView,
      DEFAULT_ROOT_DETAIL_PAGE,
      token,
    );
  }

  setSurahView(surahView: RootSurahView): void {
    const current = this._panel();
    if (
      current.selectedRootId === null ||
      current.summary === null ||
      current.view !== 'surahs' ||
      surahView === current.surahView
    ) {
      return;
    }

    const token = this.requests.beginTransition();
    this.activeUrlState = {
      rootId: current.selectedRootId,
      view: 'surahs',
      wordView: current.wordView,
      surahView,
      detailPage: current.detailPage,
    };
    this._panel.update((s) => ({
      ...s,
      surahView,
      status: 'loading',
      errorMessage: '',
    }));
    this.loadActiveView(current.selectedRootId, 'surahs', current.wordView, surahView, current.detailPage, token);
  }

  setDetailPage(page: number): void {
    const current = this._panel();
    if (current.selectedRootId === null || current.summary === null || page < 1) {
      return;
    }

    if (!isPaginatedRootView(current.view)) {
      return;
    }

    const token = this.requests.beginTransition();
    this.activeUrlState = {
      rootId: current.selectedRootId,
      view: current.view,
      wordView: current.wordView,
      surahView: current.surahView,
      detailPage: page,
    };
    this._panel.update((s) => ({
      ...s,
      detailPage: page,
      status: 'loading',
      errorMessage: '',
    }));
    this.loadActiveView(
      current.selectedRootId,
      current.view,
      current.wordView,
      current.surahView,
      page,
      token,
    );
  }

  /**
   * Drives a complete identity: abandons the previous identity's summary and
   * detail requests, then either reloads only the active view (same root, loaded
   * summary) or reloads the summary first.
   */
  private applyIdentity(state: RootsDetailUrlState): void {
    const token = this.requests.beginTransition();
    this.activeUrlState = state;
    const current = this._panel();

    if (current.selectedRootId === state.rootId && current.summary !== null) {
      this._panel.update((s) => ({
        ...s,
        view: state.view,
        wordView: state.wordView,
        surahView: state.surahView,
        detailPage: state.detailPage,
        status: 'loading',
        errorMessage: '',
      }));
      this.loadActiveView(state.rootId, state.view, state.wordView, state.surahView, state.detailPage, token);
      return;
    }

    this.loadSummaryAndRestore(state, token);
  }

  private loadSummaryAndRestore(state: RootsDetailUrlState, token: number): void {
    this._panel.set({
      ...INITIAL_PANEL,
      selectedRootId: state.rootId,
      view: state.view,
      wordView: state.wordView,
      surahView: state.surahView,
      detailPage: state.detailPage,
      status: 'loading',
    });

    this.requests.trackSummary(
      this.cache
        .getOrLoad(RootsCacheKeys.summary(state.rootId), () => this.api.getRootSummary(state.rootId))
        .pipe(
          tap((response) => {
            if (!this.requests.isCurrent(token)) {
              return;
            }

            if (!response.isSuccess || !response.data) {
              this.handleRestoredRootNotFound(response.message ?? '', state);
              return;
            }

            const summary = response.data;
            this._panel.update((s) => ({
              ...s,
              summary,
              status: 'loading',
            }));
            this.loadActiveView(
              state.rootId,
              state.view,
              state.wordView,
              state.surahView,
              state.detailPage,
              token,
            );
          }),
          catchError((err) => {
            if (!this.requests.isCurrent(token)) {
              return of(undefined);
            }

            if (err instanceof HttpErrorResponse && err.status === 404) {
              this.handleRestoredRootNotFound(this.extractErrorMessage(err, ROOTS_NOT_FOUND_LABEL), state);
              return of(undefined);
            }

            this.handleRestoredRootLoadError(this.extractErrorMessage(err, ROOTS_ERROR_LABEL), state);
            return of(undefined);
          }),
        )
        .subscribe(),
    );
  }

  private loadActiveView(
    rootId: number,
    view: RootView,
    wordView: RootWordView,
    surahView: RootSurahView,
    detailPage: number,
    token: number,
  ): void {
    const current = this._panel();
    this.requests.trackDetail(
      this.viewLoader.loadActiveView(
        {
          rootId,
          view,
          wordView,
          surahView,
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
          onStems: (response) => this.applyIfCurrent(token, (s) => ({ ...s, ...buildStemsPanelUpdate(response) })),
          onError: (err) =>
            this.applyIfCurrent(token, (s) => ({ ...s, ...buildDetailErrorUpdate(err, ROOTS_ERROR_LABEL) })),
        },
      ),
    );
  }

  /** Applies a panel update only while `token` still owns the panel. */
  private applyIfCurrent(token: number, update: (state: RootsPanelState) => RootsPanelState): void {
    if (this.requests.isCurrent(token)) {
      this._panel.update(update);
    }
  }

  private handleRestoredRootNotFound(message: string, state: RootsDetailUrlState): void {
    this._panel.set(restoredRootNotFoundUpdate(message, ROOTS_NOT_FOUND_LABEL, state.rootId));
  }

  private handleRestoredRootLoadError(message: string, state: RootsDetailUrlState): void {
    this._panel.set({
      ...INITIAL_PANEL,
      selectedRootId: state.rootId,
      status: 'error',
      errorMessage: message || ROOTS_ERROR_LABEL,
    });
  }

  private extractErrorMessage(err: unknown, fallback: string): string {
    return extractPanelErrorMessage(err, fallback);
  }
}
