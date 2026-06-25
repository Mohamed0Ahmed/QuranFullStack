import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, ParamMap } from '@angular/router';
import { Observable, Subscription, of } from 'rxjs';
import { catchError, distinctUntilChanged, map, switchMap, tap } from 'rxjs/operators';

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
import { parseLemmasQueryParams } from './lemmas-url-sync';
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
  detailPage: DEFAULT_LEMMA_DETAIL_PAGE,
  ayahs: null,
  words: null,
  mentionedSurahs: null,
  missingSurahs: null,
  stems: null,
  status: 'idle',
  errorMessage: '',
};

interface PanelUrlState {
  readonly lemmaId: number;
  readonly view: LemmaView;
  readonly wordView: LemmaWordView;
  readonly surahView: LemmaSurahView;
  readonly detailPage: number;
}

/**
 * Lemmas Explorer detail panel facade (Feature 016). Sibling of
 * `RootsDetailFacade`. Owns selected summary, active view/sub-view, detail
 * pagination, per-session cache, and not-found. The skeleton wires route
 * hydration and the view loader; live reads are exercised from US1 onward once
 * the lemma catalogue and summary endpoints exist.
 */
@Injectable({ providedIn: 'root' })
export class LemmasDetailFacade {
  private readonly api = inject(LemmasApi);
  private readonly cache = inject(LemmasCache);
  private readonly viewLoader = inject(LemmasDetailViewLoader);

  private readonly _panel = signal<LemmasPanelState>(INITIAL_PANEL);

  private routeSub?: Subscription;
  private detailSub?: Subscription;
  private summarySub?: Subscription;
  private activeUrlState: PanelUrlState | null = null;

  readonly panelState = computed(() => this._panel());

  readonly selectedLemmaId = computed(() => this._panel().selectedLemmaId);
  readonly view = computed(() => this._panel().view);
  readonly wordView = computed(() => this._panel().wordView);
  readonly surahView = computed(() => this._panel().surahView);
  readonly status = computed(() => this._panel().status);
  readonly ayahs = computed(() => this._panel().ayahs);
  readonly words = computed(() => this._panel().words);
  readonly mentionedSurahs = computed(() => this._panel().mentionedSurahs);
  readonly missingSurahs = computed(() => this._panel().missingSurahs);
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

  selectLemma(summary: LemmaSummaryDto, view: LemmaView = DEFAULT_LEMMA_VIEW): void {
    this.activeUrlState = {
      lemmaId: summary.id,
      view,
      wordView: DEFAULT_LEMMA_WORD_VIEW,
      surahView: DEFAULT_LEMMA_SURAHS_VIEW,
      detailPage: DEFAULT_LEMMA_DETAIL_PAGE,
    };
    this._panel.set({
      ...INITIAL_PANEL,
      selectedLemmaId: summary.id,
      summary,
      view,
      status: 'loading',
    });
    this.loadActiveView(summary.id, view, DEFAULT_LEMMA_WORD_VIEW, DEFAULT_LEMMA_SURAHS_VIEW, DEFAULT_LEMMA_DETAIL_PAGE);
  }

  selectLemmaWithPanel(
    summary: LemmaSummaryDto,
    view: LemmaView,
    wordView: LemmaWordView = DEFAULT_LEMMA_WORD_VIEW,
    surahView: LemmaSurahView = DEFAULT_LEMMA_SURAHS_VIEW,
    detailPage: number = DEFAULT_LEMMA_DETAIL_PAGE,
  ): void {
    this.activeUrlState = { lemmaId: summary.id, view, wordView, surahView, detailPage };
    this._panel.set({
      ...INITIAL_PANEL,
      selectedLemmaId: summary.id,
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

  setView(view: LemmaView): void {
    const current = this._panel();
    if (current.selectedLemmaId === null || current.summary === null || view === current.view) {
      return;
    }

    const detailPage = DEFAULT_LEMMA_DETAIL_PAGE;
    const wordView = view === 'words' ? current.wordView : DEFAULT_LEMMA_WORD_VIEW;
    const surahView = view === 'surahs' ? current.surahView : DEFAULT_LEMMA_SURAHS_VIEW;

    this.activeUrlState = {
      lemmaId: current.selectedLemmaId,
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
    this.loadActiveView(current.selectedLemmaId, view, wordView, surahView, detailPage);
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

    this.activeUrlState = {
      lemmaId: current.selectedLemmaId,
      view: 'words',
      wordView,
      surahView: current.surahView,
      detailPage: DEFAULT_LEMMA_DETAIL_PAGE,
    };
    this._panel.update((s) => ({
      ...s,
      wordView,
      detailPage: DEFAULT_LEMMA_DETAIL_PAGE,
      status: 'loading',
      errorMessage: '',
    }));
    this.loadActiveView(current.selectedLemmaId, 'words', wordView, current.surahView, DEFAULT_LEMMA_DETAIL_PAGE);
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

    this.activeUrlState = {
      lemmaId: current.selectedLemmaId,
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
    this.loadActiveView(current.selectedLemmaId, 'surahs', current.wordView, surahView, current.detailPage);
  }

  setDetailPage(page: number): void {
    const current = this._panel();
    if (current.selectedLemmaId === null || current.summary === null || page < 1) {
      return;
    }

    if (!isPaginatedLemmaView(current.view)) {
      return;
    }

    this.activeUrlState = {
      lemmaId: current.selectedLemmaId,
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
      current.selectedLemmaId,
      current.view,
      current.wordView,
      current.surahView,
      page,
    );
  }

  private toPanelUrlState(params: ParamMap): PanelUrlState | null {
    const parsed = parseLemmasQueryParams(params);
    if (parsed.lemmaId === null) {
      return null;
    }

    return {
      lemmaId: parsed.lemmaId,
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

    if (current.selectedLemmaId === state.lemmaId && current.summary !== null) {
      this._panel.update((s) => ({
        ...s,
        view: state.view,
        wordView: state.wordView,
        surahView: state.surahView,
        detailPage: state.detailPage,
        status: 'loading',
        errorMessage: '',
      }));
      this.loadActiveView(state.lemmaId, state.view, state.wordView, state.surahView, state.detailPage);
      return of(undefined);
    }

    return this.loadSummaryAndRestore(state);
  }

  private loadSummaryAndRestore(state: PanelUrlState): Observable<void> {
    this.summarySub?.unsubscribe();
    this._panel.set({
      ...INITIAL_PANEL,
      selectedLemmaId: state.lemmaId,
      view: state.view,
      wordView: state.wordView,
      surahView: state.surahView,
      detailPage: state.detailPage,
      status: 'loading',
    });

    return this.cache
      .getOrLoad(LemmasCacheKeys.summary(state.lemmaId), () =>
        this.api.getLemmaSummary(state.lemmaId),
      )
      .pipe(
        tap((response) => {
          if (!response.isSuccess || !response.data) {
            this.handleRestoredLemmaNotFound(response.message ?? '');
            return;
          }

          const summary = response.data;
          this._panel.update((s) => ({
            ...s,
            summary,
            status: 'loading',
          }));
          this.loadActiveView(
            state.lemmaId,
            state.view,
            state.wordView,
            state.surahView,
            state.detailPage,
          );
        }),
        catchError((err) => {
          if (err instanceof HttpErrorResponse && err.status === 404) {
            this.handleRestoredLemmaNotFound(this.extractErrorMessage(err, LEMMAS_NOT_FOUND_LABEL));
            return of(undefined);
          }

          this.handleRestoredLemmaLoadError(this.extractErrorMessage(err, LEMMAS_ERROR_LABEL));
          return of(undefined);
        }),
        map(() => undefined),
      );
  }

  private loadActiveView(
    lemmaId: number,
    view: LemmaView,
    wordView: LemmaWordView,
    surahView: LemmaSurahView,
    detailPage: number,
  ): void {
    this.detailSub?.unsubscribe();

    const current = this._panel();
    this.detailSub = this.viewLoader.loadActiveView(
      {
        lemmaId,
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
        onStems: (response) => this._panel.update((s) => ({ ...s, ...buildStemsPanelUpdate(response) })),
        onError: (err) =>
          this._panel.update((s) => ({ ...s, ...buildDetailErrorUpdate(err, LEMMAS_ERROR_LABEL) })),
      },
    );
  }

  private handleRestoredLemmaNotFound(message: string): void {
    this._panel.set(
      restoredLemmaNotFoundUpdate(message, LEMMAS_NOT_FOUND_LABEL, this.activeUrlState?.lemmaId ?? null),
    );
  }

  private handleRestoredLemmaLoadError(message: string): void {
    this._panel.set({
      ...INITIAL_PANEL,
      selectedLemmaId: this.activeUrlState?.lemmaId ?? null,
      status: 'error',
      errorMessage: message || LEMMAS_ERROR_LABEL,
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
      current.lemmaId === next.lemmaId &&
      current.view === next.view &&
      current.wordView === next.wordView &&
      current.surahView === next.surahView &&
      current.detailPage === next.detailPage
    );
  }
}
