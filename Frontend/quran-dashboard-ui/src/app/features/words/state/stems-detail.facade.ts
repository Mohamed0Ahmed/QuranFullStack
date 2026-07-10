import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, ParamMap } from '@angular/router';
import { Observable, Subscription, of } from 'rxjs';
import { catchError, distinctUntilChanged, map, switchMap, tap } from 'rxjs/operators';

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
import { parseStemsQueryParams } from './stems-url-sync';
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

interface PanelUrlState {
  readonly stemId: number;
  readonly view: StemView;
  readonly wordView: StemWordView;
  readonly surahView: StemSurahView;
  readonly detailPage: number;
  readonly typeCode: string | null;
}

/**
 * Stems Explorer detail panel facade (Feature 016). Sibling of
 * `RootsDetailFacade`. Owns selected summary, active view/sub-view, detail
 * pagination, per-session cache, and not-found. The skeleton wires route
 * hydration and the view loader; live reads are exercised from US2 onward once
 * the stem catalogue and summary endpoints exist.
 */
@Injectable({ providedIn: 'root' })
export class StemsDetailFacade {
  private readonly api = inject(StemsApi);
  private readonly cache = inject(StemsCache);
  private readonly viewLoader = inject(StemsDetailViewLoader);

  private readonly _panel = signal<StemsPanelState>(INITIAL_PANEL);

  private routeSub?: Subscription;
  private detailSub?: Subscription;
  private summarySub?: Subscription;
  private activeUrlState: PanelUrlState | null = null;

  readonly panelState = computed(() => this._panel());

  readonly selectedStemId = computed(() => this._panel().selectedStemId);
  readonly view = computed(() => this._panel().view);
  readonly wordView = computed(() => this._panel().wordView);
  readonly surahView = computed(() => this._panel().surahView);
  readonly status = computed(() => this._panel().status);
  readonly ayahs = computed(() => this._panel().ayahs);
  readonly words = computed(() => this._panel().words);
  readonly mentionedSurahs = computed(() => this._panel().mentionedSurahs);
  readonly missingSurahs = computed(() => this._panel().missingSurahs);
  readonly lemmas = computed(() => this._panel().lemmas);
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
    this.summarySub?.unsubscribe();
    this.detailSub?.unsubscribe();
    this.summarySub = undefined;
    this.detailSub = undefined;
  }

  selectStem(summary: StemSummaryDto, view: StemView = DEFAULT_STEM_VIEW): void {
    this.activeUrlState = {
      stemId: summary.id,
      view,
      wordView: DEFAULT_STEM_WORD_VIEW,
      surahView: DEFAULT_STEM_SURAHS_VIEW,
      detailPage: DEFAULT_STEM_DETAIL_PAGE,
      typeCode: null,
    };
    this._panel.set({
      ...INITIAL_PANEL,
      selectedStemId: summary.id,
      summary,
      view,
      status: 'loading',
    });
    this.loadActiveView(
      summary.id,
      view,
      DEFAULT_STEM_WORD_VIEW,
      DEFAULT_STEM_SURAHS_VIEW,
      DEFAULT_STEM_DETAIL_PAGE,
      null,
    );
  }

  selectStemWithPanel(
    summary: StemSummaryDto,
    view: StemView,
    wordView: StemWordView = DEFAULT_STEM_WORD_VIEW,
    surahView: StemSurahView = DEFAULT_STEM_SURAHS_VIEW,
    detailPage: number = DEFAULT_STEM_DETAIL_PAGE,
  ): void {
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
    this.loadActiveView(summary.id, view, wordView, surahView, detailPage, null);
  }

  clearSelection(): void {
    this.summarySub?.unsubscribe();
    this.detailSub?.unsubscribe();
    this.summarySub = undefined;
    this.detailSub = undefined;
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
    this.loadActiveView(current.selectedStemId, view, wordView, surahView, detailPage, null);
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
    this.loadActiveView(current.selectedStemId, 'words', wordView, current.surahView, DEFAULT_STEM_DETAIL_PAGE, null);
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
    this.loadActiveView(current.selectedStemId, 'surahs', current.wordView, surahView, current.detailPage, null);
  }

  setDetailPage(page: number): void {
    const current = this._panel();
    if (current.selectedStemId === null || current.summary === null || page < 1) {
      return;
    }

    if (!isPaginatedStemView(current.view)) {
      return;
    }

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
    );
  }

  private toPanelUrlState(params: ParamMap): PanelUrlState | null {
    const parsed = parseStemsQueryParams(params);
    if (parsed.stemId === null) {
      return null;
    }

    return {
      stemId: parsed.stemId,
      view: parsed.view,
      wordView: parsed.wordView,
      surahView: parsed.surahView,
      detailPage: parsed.detailPage,
      typeCode: parsed.typeCode,
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
      );
      return of(undefined);
    }

    return this.loadSummaryAndRestore(state);
  }

  private loadSummaryAndRestore(state: PanelUrlState): Observable<void> {
    this.summarySub?.unsubscribe();
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

    return this.cache
      .getOrLoad(StemsCacheKeys.summary(state.stemId), () =>
        this.api.getStemSummary(state.stemId),
      )
      .pipe(
        tap((response) => {
          if (!response.isSuccess || !response.data) {
            this.handleRestoredStemNotFound(response.message ?? '');
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
          );
        }),
        catchError((err) => {
          if (err instanceof HttpErrorResponse && err.status === 404) {
            this.handleRestoredStemNotFound(this.extractErrorMessage(err, STEMS_NOT_FOUND_LABEL));
            return of(undefined);
          }

          this.handleRestoredStemLoadError(this.extractErrorMessage(err, STEMS_ERROR_LABEL));
          return of(undefined);
        }),
        map(() => undefined),
      );
  }

  private loadActiveView(
    stemId: number,
    view: StemView,
    wordView: StemWordView,
    surahView: StemSurahView,
    detailPage: number,
    ayahTypeCode: string | null,
  ): void {
    this.detailSub?.unsubscribe();

    const current = this._panel();
    this.detailSub = this.viewLoader.loadActiveView(
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
        onAyahs: (response) => this._panel.update((s) => ({ ...s, ...buildAyahsPanelUpdate(response) })),
        onWords: (response) => this._panel.update((s) => ({ ...s, ...buildWordsPanelUpdate(response) })),
        onMentionedSurahs: (response) =>
          this._panel.update((s) => ({ ...s, ...buildMentionedSurahsPanelUpdate(response) })),
        onMissingSurahs: (response) =>
          this._panel.update((s) => ({ ...s, ...buildMissingSurahsPanelUpdate(response) })),
        onLemmas: (response) => this._panel.update((s) => ({ ...s, ...buildLemmasPanelUpdate(response) })),
        onError: (err) =>
          this._panel.update((s) => ({ ...s, ...buildDetailErrorUpdate(err, STEMS_ERROR_LABEL) })),
      },
    );
  }

  private handleRestoredStemNotFound(message: string): void {
    this._panel.set(
      restoredStemNotFoundUpdate(message, STEMS_NOT_FOUND_LABEL, this.activeUrlState?.stemId ?? null),
    );
  }

  private handleRestoredStemLoadError(message: string): void {
    this._panel.set({
      ...INITIAL_PANEL,
      selectedStemId: this.activeUrlState?.stemId ?? null,
      status: 'error',
      errorMessage: message || STEMS_ERROR_LABEL,
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
      current.stemId === next.stemId &&
      current.view === next.view &&
      current.wordView === next.wordView &&
      current.surahView === next.surahView &&
      current.detailPage === next.detailPage &&
      current.typeCode === next.typeCode
    );
  }

  private normalizeTypeCode(typeCode: string | null): string | null {
    return typeCode === null || typeCode.trim().length === 0 ? null : typeCode.trim();
  }
}
