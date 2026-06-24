import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, ParamMap } from '@angular/router';
import { Observable, Subscription, of } from 'rxjs';
import { catchError, distinctUntilChanged, map, switchMap, tap } from 'rxjs/operators';

import { ApiResponse } from '../../../core/data-access/api-response.model';
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
  LoadStatus,
  PagedResultDto,
  ROOT_DETAIL_PAGE_SIZE,
  RootAyahMatchDto,
  RootSummaryDto,
  RootSurahView,
  RootView,
  RootWordView,
  RootsPanelState,
} from '../models/roots.models';
import { parseRootsQueryParams } from './roots-url-sync';
import { RootsCache, RootsCacheKeys } from './roots-cache';

const INITIAL_PANEL: RootsPanelState = {
  selectedRootId: null,
  summary: null,
  view: DEFAULT_ROOT_VIEW,
  wordView: DEFAULT_ROOT_WORD_VIEW,
  surahView: DEFAULT_ROOT_SURAHS_VIEW,
  detailPage: DEFAULT_ROOT_DETAIL_PAGE,
  ayahs: null,
  status: 'idle',
  errorMessage: '',
};

interface PanelUrlState {
  readonly rootId: number;
  readonly view: RootView;
  readonly wordView: RootWordView;
  readonly surahView: RootSurahView;
  readonly detailPage: number;
}

/**
 * Roots Explorer (Feature 015) persistent detail-panel facade. Modeled on
 * `UniqueWordsDrilldownFacade`, but the detail surface is a **persistent side
 * panel**, not a modal: there is no `isOpen`/modal-close. Selection drives
 * visibility — when `selectedRootId` is null the panel shows the empty-selection
 * state (`اختر جذرًا لعرض تفاصيله`).
 *
 * US2 (T039): ayahs lazy-load on tab activation, cache via `RootsCache`, URL
 * restore, and controlled not-found/error handling. Later stories add words,
 * surahs, lemmas, and stems.
 */
@Injectable({ providedIn: 'root' })
export class RootsDetailFacade {
  private readonly api = inject(RootsApi);
  private readonly cache = inject(RootsCache);

  private readonly _panel = signal<RootsPanelState>(INITIAL_PANEL);

  private routeSub?: Subscription;
  private detailSub?: Subscription;
  private summarySub?: Subscription;
  private activeUrlState: PanelUrlState | null = null;

  readonly panelState = computed(() => this._panel());

  readonly selectedRootId = computed(() => this._panel().selectedRootId);
  readonly view = computed(() => this._panel().view);
  readonly status = computed(() => this._panel().status);
  readonly ayahs = computed(() => this._panel().ayahs);
  readonly detailPage = computed(() => this._panel().detailPage);

  /** Binds panel state to selection/panel URL params. */
  bindToRoute(route: ActivatedRoute): void {
    this.unbindFromRoute();

    this.routeSub = route.queryParamMap
      .pipe(
        map((params) => this.toPanelUrlState(params)),
        distinctUntilChanged((a, b) => this.isSamePanelUrlState(a, b)),
        switchMap((state) => this.syncFromUrlState(state)),
      )
      .subscribe();
  }

  unbindFromRoute(): void {
    this.routeSub?.unsubscribe();
    this.routeSub = undefined;
  }

  /**
   * Selects a root from an in-memory summary (US1: the summary is built from the
   * list item, so NO detail API call fires until the active view loads). Sets
   * the requested view (default ayahs).
   */
  selectRoot(summary: RootSummaryDto, view: RootView = DEFAULT_ROOT_VIEW): void {
    this.activeUrlState = {
      rootId: summary.id,
      view,
      wordView: DEFAULT_ROOT_WORD_VIEW,
      surahView: DEFAULT_ROOT_SURAHS_VIEW,
      detailPage: DEFAULT_ROOT_DETAIL_PAGE,
    };
    this._panel.set({
      ...INITIAL_PANEL,
      selectedRootId: summary.id,
      summary,
      view,
      status: 'loading',
    });
    this.loadActiveView(summary.id, view, DEFAULT_ROOT_DETAIL_PAGE);
  }

  /**
   * Selects a root from the list with explicit panel sub-state (count-cell
   * mapping). Uses the in-memory summary; per-view data loads lazily.
   */
  selectRootWithPanel(
    summary: RootSummaryDto,
    view: RootView,
    wordView: RootWordView = DEFAULT_ROOT_WORD_VIEW,
    surahView: RootSurahView = DEFAULT_ROOT_SURAHS_VIEW,
    detailPage: number = DEFAULT_ROOT_DETAIL_PAGE,
  ): void {
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
    this.loadActiveView(summary.id, view, detailPage);
  }

  /** Clears the selection, returning to the empty-selection state. */
  clearSelection(): void {
    this.summarySub?.unsubscribe();
    this.detailSub?.unsubscribe();
    this.summarySub = undefined;
    this.detailSub = undefined;
    this.activeUrlState = null;
    this._panel.set(INITIAL_PANEL);
  }

  /** Sets the active panel tab and lazy-loads its data when needed. */
  setView(view: RootView): void {
    const current = this._panel();
    if (current.selectedRootId === null || current.summary === null || view === current.view) {
      return;
    }

    const detailPage =
      view === 'ayahs'
        ? current.view === 'ayahs'
          ? current.detailPage
          : DEFAULT_ROOT_DETAIL_PAGE
        : DEFAULT_ROOT_DETAIL_PAGE;
    this.activeUrlState = {
      rootId: current.selectedRootId,
      view,
      wordView: current.wordView,
      surahView: current.surahView,
      detailPage,
    };
    this._panel.update((s) => ({
      ...s,
      view,
      detailPage,
      status: 'loading',
      errorMessage: '',
    }));
    this.loadActiveView(current.selectedRootId, view, detailPage);
  }

  /** Changes the paginated detail page for the active ayahs (or future words) view. */
  setDetailPage(page: number): void {
    const current = this._panel();
    if (current.selectedRootId === null || current.summary === null || page < 1) {
      return;
    }

    if (current.view !== 'ayahs') {
      return;
    }

    this.activeUrlState = {
      rootId: current.selectedRootId,
      view: 'ayahs',
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
    this.loadActiveView(current.selectedRootId, 'ayahs', page);
  }

  private toPanelUrlState(params: ParamMap): PanelUrlState | null {
    const parsed = parseRootsQueryParams(params);
    if (parsed.rootId === null) {
      return null;
    }

    return {
      rootId: parsed.rootId,
      view: parsed.view,
      wordView: parsed.wordView,
      surahView: parsed.surahView,
      detailPage: parsed.detailPage,
    };
  }

  private syncFromUrlState(state: PanelUrlState | null): Observable<void> {
    if (state === null) {
      this.clearSelection();
      return of(undefined);
    }

    if (this.isSamePanelUrlState(this.activeUrlState, state)) {
      return of(undefined);
    }

    this.activeUrlState = state;
    const current = this._panel();

    if (
      current.selectedRootId === state.rootId &&
      current.summary !== null
    ) {
      this._panel.update((s) => ({
        ...s,
        view: state.view,
        wordView: state.wordView,
        surahView: state.surahView,
        detailPage: state.detailPage,
        status: 'loading',
        errorMessage: '',
      }));
      this.loadActiveView(state.rootId, state.view, state.detailPage);
      return of(undefined);
    }

    return this.loadSummaryAndRestore(state);
  }

  private loadSummaryAndRestore(state: PanelUrlState): Observable<void> {
    this.summarySub?.unsubscribe();
    this._panel.set({
      ...INITIAL_PANEL,
      selectedRootId: state.rootId,
      view: state.view,
      wordView: state.wordView,
      surahView: state.surahView,
      detailPage: state.detailPage,
      status: 'loading',
    });

    return this.cache
      .getOrLoad(RootsCacheKeys.summary(state.rootId), () =>
        this.api.getRootSummary(state.rootId),
      )
      .pipe(
        tap((response) => {
          if (!response.isSuccess || !response.data) {
            this.handleRestoredRootNotFound(response.message ?? '');
            return;
          }

          const summary = response.data;
          this._panel.update((s) => ({
            ...s,
            summary,
            status: 'loading',
          }));
          this.loadActiveView(state.rootId, state.view, state.detailPage);
        }),
        catchError((err) => {
          if (err instanceof HttpErrorResponse && err.status === 404) {
            this.handleRestoredRootNotFound(this.extractErrorMessage(err, ROOTS_NOT_FOUND_LABEL));
            return of(undefined);
          }

          this.handleRestoredRootLoadError(this.extractErrorMessage(err, ROOTS_ERROR_LABEL));
          return of(undefined);
        }),
        map(() => undefined),
      );
  }

  private loadActiveView(rootId: number, view: RootView, detailPage: number): void {
    this.detailSub?.unsubscribe();

    if (view !== 'ayahs') {
      // US3–US5 add words/surahs/lemmas/stems loading.
      this._panel.update((s) => ({
        ...s,
        status: 'success',
        errorMessage: '',
      }));
      return;
    }

    this.detailSub = this.cache
      .getOrLoad(RootsCacheKeys.ayahs(rootId, detailPage), () =>
        this.api.getRootAyahMatches(rootId, detailPage, ROOT_DETAIL_PAGE_SIZE),
      )
      .pipe(
        tap((response) => this.handleAyahsResponse(response)),
        catchError((err) => {
          this.handleDetailError(err);
          return of(undefined);
        }),
      )
      .subscribe();
  }

  private handleAyahsResponse(response: ApiResponse<PagedResultDto<RootAyahMatchDto>>): void {
    if (!response.isSuccess || !response.data) {
      this._panel.update((s) => ({
        ...s,
        ayahs: null,
        status: 'error',
        errorMessage: response.message ?? ROOTS_ERROR_LABEL,
      }));
      return;
    }

    const data = response.data;
    this._panel.update((s) => ({
      ...s,
      ayahs: data,
      detailPage: data.page,
      status: data.totalCount === 0 ? 'empty' : 'success',
      errorMessage: '',
    }));
  }

  private handleRestoredRootNotFound(message: string): void {
    this._panel.set({
      ...INITIAL_PANEL,
      selectedRootId: this.activeUrlState?.rootId ?? null,
      status: 'notFound',
      errorMessage: message || ROOTS_NOT_FOUND_LABEL,
    });
  }

  private handleRestoredRootLoadError(message: string): void {
    this._panel.set({
      ...INITIAL_PANEL,
      selectedRootId: this.activeUrlState?.rootId ?? null,
      status: 'error',
      errorMessage: message || ROOTS_ERROR_LABEL,
    });
  }

  private handleDetailError(err: unknown): void {
    this._panel.update((s) => ({
      ...s,
      status: 'error',
      errorMessage: this.extractErrorMessage(err, ROOTS_ERROR_LABEL),
    }));
  }

  private extractErrorMessage(err: unknown, fallback: string): string {
    if (err instanceof HttpErrorResponse) {
      const body = err.error as ApiResponse<unknown> | null | undefined;
      return typeof body?.message === 'string' && body.message.length > 0 ? body.message : fallback;
    }

    return fallback;
  }

  private isSamePanelUrlState(
    current: PanelUrlState | null,
    next: PanelUrlState | null,
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
}
