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
  RootSummaryDto,
  RootSurahView,
  RootView,
  RootWordView,
  RootsPanelState,
  isPaginatedRootView,
} from '../models/roots.models';
import { parseRootsQueryParams } from './roots-url-sync';
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

interface PanelUrlState {
  readonly rootId: number;
  readonly view: RootView;
  readonly wordView: RootWordView;
  readonly surahView: RootSurahView;
  readonly detailPage: number;
}

@Injectable({ providedIn: 'root' })
export class RootsDetailFacade {
  private readonly api = inject(RootsApi);
  private readonly cache = inject(RootsCache);
  private readonly viewLoader = inject(RootsDetailViewLoader);

  private readonly _panel = signal<RootsPanelState>(INITIAL_PANEL);

  private routeSub?: Subscription;
  private detailSub?: Subscription;
  private summarySub?: Subscription;
  private activeUrlState: PanelUrlState | null = null;

  readonly panelState = computed(() => this._panel());

  readonly selectedRootId = computed(() => this._panel().selectedRootId);
  readonly view = computed(() => this._panel().view);
  readonly wordView = computed(() => this._panel().wordView);
  readonly surahView = computed(() => this._panel().surahView);
  readonly status = computed(() => this._panel().status);
  readonly ayahs = computed(() => this._panel().ayahs);
  readonly words = computed(() => this._panel().words);
  readonly mentionedSurahs = computed(() => this._panel().mentionedSurahs);
  readonly missingSurahs = computed(() => this._panel().missingSurahs);
  readonly lemmas = computed(() => this._panel().lemmas);
  readonly stems = computed(() => this._panel().stems);
  readonly detailPage = computed(() => this._panel().detailPage);

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
    this.loadActiveView(summary.id, view, DEFAULT_ROOT_WORD_VIEW, DEFAULT_ROOT_SURAHS_VIEW, DEFAULT_ROOT_DETAIL_PAGE);
  }

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
    this.loadActiveView(summary.id, view, wordView, surahView, detailPage);
  }

  clearSelection(): void {
    this.summarySub?.unsubscribe();
    this.detailSub?.unsubscribe();
    this.summarySub = undefined;
    this.detailSub = undefined;
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
    this.loadActiveView(current.selectedRootId, view, wordView, surahView, detailPage);
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
    this.loadActiveView(current.selectedRootId, 'words', wordView, current.surahView, DEFAULT_ROOT_DETAIL_PAGE);
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
    this.loadActiveView(current.selectedRootId, 'surahs', current.wordView, surahView, current.detailPage);
  }

  setDetailPage(page: number): void {
    const current = this._panel();
    if (current.selectedRootId === null || current.summary === null || page < 1) {
      return;
    }

    if (!isPaginatedRootView(current.view)) {
      return;
    }

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
    );
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
      this.loadActiveView(state.rootId, state.view, state.wordView, state.surahView, state.detailPage);
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
          this.loadActiveView(
            state.rootId,
            state.view,
            state.wordView,
            state.surahView,
            state.detailPage,
          );
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

  private loadActiveView(
    rootId: number,
    view: RootView,
    wordView: RootWordView,
    surahView: RootSurahView,
    detailPage: number,
  ): void {
    this.detailSub?.unsubscribe();

    const current = this._panel();
    this.detailSub = this.viewLoader.loadActiveView(
      {
        rootId,
        view,
        wordView,
        surahView,
        detailPage,
        cachedMissingSurahs: current.missingSurahs,
      },
      {
        onAyahs: (response) => this._panel.update((s) => ({ ...s, ...buildAyahsPanelUpdate(response) })),
        onWords: (response) => this._panel.update((s) => ({ ...s, ...buildWordsPanelUpdate(response) })),
        onMentionedSurahs: (response) =>
          this._panel.update((s) => ({ ...s, ...buildMentionedSurahsPanelUpdate(response) })),
        onMissingSurahs: (response) =>
          this._panel.update((s) => ({ ...s, ...buildMissingSurahsPanelUpdate(response) })),
        onLemmas: (response) => this._panel.update((s) => ({ ...s, ...buildLemmasPanelUpdate(response) })),
        onStems: (response) => this._panel.update((s) => ({ ...s, ...buildStemsPanelUpdate(response) })),
        onError: (err) =>
          this._panel.update((s) => ({ ...s, ...buildDetailErrorUpdate(err, ROOTS_ERROR_LABEL) })),
      },
    );
  }

  private handleRestoredRootNotFound(message: string): void {
    this._panel.set(
      restoredRootNotFoundUpdate(message, ROOTS_NOT_FOUND_LABEL, this.activeUrlState?.rootId ?? null),
    );
  }

  private handleRestoredRootLoadError(message: string): void {
    this._panel.set({
      ...INITIAL_PANEL,
      selectedRootId: this.activeUrlState?.rootId ?? null,
      status: 'error',
      errorMessage: message || ROOTS_ERROR_LABEL,
    });
  }

  private extractErrorMessage(err: unknown, fallback: string): string {
    return extractPanelErrorMessage(err, fallback);
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
