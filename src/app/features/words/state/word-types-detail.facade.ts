import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, ParamMap } from '@angular/router';
import { Observable, Subscription, of } from 'rxjs';
import { catchError, distinctUntilChanged, map, switchMap, tap } from 'rxjs/operators';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { WordAnalysisViewModel } from '../../mushaf/models/mushaf.models';
import { toWordAnalysisViewModel } from '../../mushaf/state/mushaf-reader-view-mappers';
import { WordTypesApi } from '../data-access/word-types.api';
import { WORD_TYPES_ERROR_LABEL, WORD_TYPES_NOT_FOUND_LABEL } from '../models/word-types.labels';
import {
  DEFAULT_WORD_TYPE_CASE,
  DEFAULT_WORD_TYPE_TENSE,
  DEFAULT_WORD_TYPE_VOICE,
  DEFAULT_WORD_TYPES_DETAIL_PAGE,
  DEFAULT_WORD_TYPES_DETAIL_VIEW,
  WordTypeDetailView,
  WordTypeRowDto,
  WordTypeRowIdentity,
  WordTypeSummaryDto,
  WordTypesDetailState,
} from '../models/word-types.models';
import { parseWordTypesQueryParams } from './word-types-url-sync';
import { WordTypesCache, WordTypesCacheKeys } from './word-types-cache';
import {
  buildAyahsPanelUpdate,
  buildDetailErrorUpdate,
  buildSummaryPanelUpdate,
  buildSurahsPanelUpdate,
  extractPanelErrorMessage,
  isPaginatedWordTypeView,
  restoredRowNotFoundUpdate,
} from './word-types-detail-panel.updates';
import { WordTypesDetailViewLoader } from './word-types-detail-view.loader';

const INITIAL_PANEL: WordTypesDetailState = {
  selectedRow: null,
  view: DEFAULT_WORD_TYPES_DETAIL_VIEW,
  detailPage: DEFAULT_WORD_TYPES_DETAIL_PAGE,
  location: null,
  summary: null,
  ayahs: null,
  surahs: null,
  analysis: null,
  status: 'idle',
  errorMessage: '',
};

interface PanelUrlState {
  readonly identity: WordTypeRowIdentity;
  readonly view: WordTypeDetailView;
  readonly detailPage: number;
  readonly location: string | null;
}

@Injectable({ providedIn: 'root' })
export class WordTypesDetailFacade {
  private readonly api = inject(WordTypesApi);
  private readonly cache = inject(WordTypesCache);
  private readonly viewLoader = inject(WordTypesDetailViewLoader);

  private readonly _panel = signal<WordTypesDetailState>(INITIAL_PANEL);
  private routeSub?: Subscription;
  private detailSub?: Subscription;
  private summarySub?: Subscription;
  private activeUrlState: PanelUrlState | null = null;

  readonly panelState = computed(() => this._panel());
  readonly view = computed(() => this._panel().view);
  readonly detailPage = computed(() => this._panel().detailPage);
  readonly status = computed(() => this._panel().status);

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

  selectRow(row: WordTypeRowDto, view: WordTypeDetailView = DEFAULT_WORD_TYPES_DETAIL_VIEW): void {
    const identity = toIdentity(row);
    this.activeUrlState = {
      identity,
      view,
      detailPage: DEFAULT_WORD_TYPES_DETAIL_PAGE,
      location: null,
    };
    this._panel.set({
      ...INITIAL_PANEL,
      selectedRow: identity,
      view,
      status: 'loading',
    });
    this.loadSummaryAndActiveView(identity, view, DEFAULT_WORD_TYPES_DETAIL_PAGE, null);
  }

  clearSelection(): void {
    this.summarySub?.unsubscribe();
    this.detailSub?.unsubscribe();
    this.summarySub = undefined;
    this.detailSub = undefined;
    this.activeUrlState = null;
    this._panel.set(INITIAL_PANEL);
  }

  setView(view: WordTypeDetailView): void {
    const current = this._panel();
    if (current.selectedRow === null || current.summary === null || view === current.view) {
      return;
    }

    const detailPage = DEFAULT_WORD_TYPES_DETAIL_PAGE;
    this.activeUrlState = {
      identity: current.selectedRow,
      view,
      detailPage,
      location: view === 'analysis' ? current.location : null,
    };
    this._panel.update((state) => ({
      ...state,
      view,
      detailPage,
      status: 'loading',
      errorMessage: '',
    }));
    this.loadActiveView(current.selectedRow, view, detailPage, this.activeUrlState.location);
  }

  setDetailPage(page: number): void {
    const current = this._panel();
    if (current.selectedRow === null || current.summary === null || page < 1 || !isPaginatedWordTypeView(current.view)) {
      return;
    }

    this.activeUrlState = {
      identity: current.selectedRow,
      view: current.view,
      detailPage: page,
      location: current.location,
    };
    this._panel.update((state) => ({
      ...state,
      detailPage: page,
      status: 'loading',
      errorMessage: '',
    }));
    this.loadActiveView(current.selectedRow, current.view, page, current.location);
  }

  setAnalysisLocation(location: string | null): void {
    const current = this._panel();
    if (current.selectedRow === null || current.summary === null || current.view !== 'analysis') {
      return;
    }

    this.activeUrlState = {
      identity: current.selectedRow,
      view: 'analysis',
      detailPage: current.detailPage,
      location,
    };
    this._panel.update((state) => ({
      ...state,
      location,
      status: location ? 'loading' : 'success',
      analysis: null,
      errorMessage: '',
    }));

    if (location) {
      this.loadActiveView(current.selectedRow, 'analysis', current.detailPage, location);
    }
  }

  private toPanelUrlState(params: ParamMap): PanelUrlState | null {
    const parsed = parseWordTypesQueryParams(params);
    if (parsed.word === null || parsed.contextCode.length === 0) {
      return null;
    }

    return {
      identity: {
        tashkeelWordId: parsed.word,
        contextCode: parsed.contextCode,
        case: parsed.case,
        tense: parsed.tense,
        voice: parsed.voice,
      },
      view: parsed.view,
      detailPage: parsed.detailPage,
      location: parsed.location,
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
    const sameRow =
      current.selectedRow?.tashkeelWordId === state.identity.tashkeelWordId
      && current.selectedRow.contextCode === state.identity.contextCode;

    if (sameRow && current.summary !== null) {
      this._panel.update((panel) => ({
        ...panel,
        selectedRow: state.identity,
        view: state.view,
        detailPage: state.detailPage,
        location: state.location,
        status: 'loading',
        errorMessage: '',
      }));
      this.loadActiveView(state.identity, state.view, state.detailPage, state.location);
      return of(undefined);
    }

    this._panel.set({
      ...INITIAL_PANEL,
      selectedRow: state.identity,
      view: state.view,
      detailPage: state.detailPage,
      location: state.location,
      status: 'loading',
    });

    return this.loadSummaryAndRestore(state);
  }

  private loadSummaryAndRestore(state: PanelUrlState): Observable<void> {
    this.summarySub?.unsubscribe();
    return this.cache
      .getOrLoad(WordTypesCacheKeys.summary(state.identity), () => this.api.getSummary(state.identity))
      .pipe(
        tap((response) => {
          if (!response.isSuccess || !response.data) {
            this.handleRestoredRowNotFound(response.message ?? '');
            return;
          }

          this._panel.update((panel) => ({
            ...panel,
            ...buildSummaryPanelUpdate(response),
          }));
          this.loadActiveView(state.identity, state.view, state.detailPage, state.location);
        }),
        catchError((err) => {
          if (err instanceof HttpErrorResponse && err.status === 404) {
            this.handleRestoredRowNotFound(extractPanelErrorMessage(err, WORD_TYPES_NOT_FOUND_LABEL));
            return of(undefined);
          }

          this._panel.set({
            ...INITIAL_PANEL,
            selectedRow: state.identity,
            status: 'error',
            errorMessage: extractPanelErrorMessage(err, WORD_TYPES_ERROR_LABEL),
          });
          return of(undefined);
        }),
        map(() => undefined),
      );
  }

  private loadSummaryAndActiveView(
    identity: WordTypeRowIdentity,
    view: WordTypeDetailView,
    detailPage: number,
    location: string | null,
  ): void {
    this.summarySub?.unsubscribe();
    this.summarySub = this.cache
      .getOrLoad(WordTypesCacheKeys.summary(identity), () => this.api.getSummary(identity))
      .subscribe({
        next: (response) => {
          if (!response.isSuccess || !response.data) {
            this.handleRestoredRowNotFound(response.message ?? '');
            return;
          }

          this._panel.update((panel) => ({
            ...panel,
            ...buildSummaryPanelUpdate(response),
          }));
          this.loadActiveView(identity, view, detailPage, location);
        },
        error: (err) => {
          this._panel.update((panel) => ({
            ...panel,
            ...buildDetailErrorUpdate(err, WORD_TYPES_ERROR_LABEL),
          }));
        },
      });
  }

  private loadActiveView(
    identity: WordTypeRowIdentity,
    view: WordTypeDetailView,
    detailPage: number,
    location: string | null,
  ): void {
    this.detailSub?.unsubscribe();

    if (view === 'analysis' && !location) {
      this._panel.update((panel) => ({
        ...panel,
        analysis: null,
        status: 'empty',
        errorMessage: '',
      }));
      return;
    }

    this.detailSub = this.viewLoader.loadActiveView(
      { identity, view, detailPage, location },
      {
        onAyahs: (response) => this._panel.update((panel) => ({ ...panel, ...buildAyahsPanelUpdate(response) })),
        onSurahs: (response) => this._panel.update((panel) => ({ ...panel, ...buildSurahsPanelUpdate(response) })),
        onAnalysis: (response) => this._panel.update((panel) => ({
          ...panel,
          analysis: response.isSuccess && response.data ? toWordAnalysisViewModel(response.data) : null,
          status: response.isSuccess && response.data ? 'success' : 'error',
          errorMessage: response.isSuccess ? '' : response.message ?? WORD_TYPES_ERROR_LABEL,
        })),
        onError: (err) => this._panel.update((panel) => ({
          ...panel,
          ...buildDetailErrorUpdate(err, WORD_TYPES_ERROR_LABEL),
        })),
      },
    );
  }

  private handleRestoredRowNotFound(message: string): void {
    this._panel.set(restoredRowNotFoundUpdate(message, WORD_TYPES_NOT_FOUND_LABEL, this.activeUrlState?.identity ?? null));
  }

  private isSamePanelUrlState(current: PanelUrlState | null, next: PanelUrlState | null): boolean {
    if (current === null || next === null) {
      return current === next;
    }

    return (
      current.identity.tashkeelWordId === next.identity.tashkeelWordId
      && current.identity.contextCode === next.identity.contextCode
      && current.identity.case === next.identity.case
      && current.identity.tense === next.identity.tense
      && current.identity.voice === next.identity.voice
      && current.view === next.view
      && current.detailPage === next.detailPage
      && current.location === next.location
    );
  }
}

function toIdentity(row: WordTypeRowIdentity): WordTypeRowIdentity {
  return {
    tashkeelWordId: row.tashkeelWordId,
    contextCode: row.contextCode,
    case: row.case ?? DEFAULT_WORD_TYPE_CASE,
    tense: row.tense ?? DEFAULT_WORD_TYPE_TENSE,
    voice: row.voice ?? DEFAULT_WORD_TYPE_VOICE,
  };
}
