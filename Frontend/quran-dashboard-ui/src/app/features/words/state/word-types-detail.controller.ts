import { Injectable, OnDestroy, computed, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { of } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';

import { WordTypesApi } from '../data-access/word-types.api';
import { WORD_TYPES_ERROR_LABEL, WORD_TYPES_NOT_FOUND_LABEL } from '../models/word-types.labels';
import {
  DEFAULT_WORD_TYPE,
  DEFAULT_WORD_TYPES_DETAIL_PAGE,
  DEFAULT_WORD_TYPES_DETAIL_VIEW,
  WordTypeDetailView,
  WordTypeRowIdentity,
} from '../models/word-types.models';
import { WordTypeDetailSelection, WordTypesDetailState } from '../models/word-types-detail.models';
import { DetailRequestLifecycle } from './detail-request-lifecycle';
import { WordTypesCache, WordTypesCacheKeys } from './word-types-cache';
import {
  buildAyahsPanelUpdate,
  buildDetailErrorUpdate,
  buildSurahsPanelUpdate,
  buildWordsPanelUpdate,
  extractPanelErrorMessage,
} from './word-types-detail-panel.updates';
import { WordTypesDetailViewLoader } from './word-types-detail-view.loader';

const INITIAL_PANEL: WordTypesDetailState = {
  status: 'idle',
  selection: null,
  kind: 'word',
  selectedRow: null,
  view: DEFAULT_WORD_TYPES_DETAIL_VIEW,
  detailPage: DEFAULT_WORD_TYPES_DETAIL_PAGE,
  location: null,
  summary: null,
  groupedSummary: null,
  words: null,
  ayahs: null,
  surahs: null,
  errorMessage: '',
};

// The full composite row identity (tashkeelWordId, contextCode, case, tense, voice) plus view
// and page all participate in equality: the same tashkeel word id under a different
// context/case/tense/voice is a DIFFERENT detail and must never cross-serve.
export interface WordTypesWordDetailUrlState {
  readonly identity: WordTypeRowIdentity;
  readonly view: WordTypeDetailView;
  readonly detailPage: number;
}

export function wordTypesWordDetailUrlStatesEqual(
  current: WordTypesWordDetailUrlState | null,
  next: WordTypesWordDetailUrlState | null,
): boolean {
  if (current === null || next === null) {
    return current === next;
  }

  return (
    isSameWordIdentity(current.identity, next.identity) &&
    current.view === next.view &&
    current.detailPage === next.detailPage
  );
}

function isSameWordIdentity(current: WordTypeRowIdentity, next: WordTypeRowIdentity): boolean {
  return (
    current.tashkeelWordId === next.tashkeelWordId &&
    current.contextCode === next.contextCode &&
    current.case === next.case &&
    current.tense === next.tense &&
    current.voice === next.voice
  );
}

// A word-kind selection carries a WordTypeDetailScope only to satisfy the
// shared selection shape: for kind 'word' the view loader and every cache key
// read exclusively from `identity` (see WordTypesDetailViewLoader /
// WordTypesCacheKeys), so this synthetic scope never reaches an API call or a
// cache key. The overlay has no list scope to forward.
function wordSelectionOf(identity: WordTypeRowIdentity): WordTypeDetailSelection {
  return {
    kind: 'word',
    identity,
    scope: {
      type: DEFAULT_WORD_TYPE,
      childCode: null,
      case: identity.case,
      tense: identity.tense,
      voice: identity.voice,
    },
  };
}

// Handles ONLY word-kind details (the overlay's frame carries the full composite row identity);
// grouped root/stem/lemma selections stay in WordTypesDetailFacade.
// Every complete-identity transition abandons both the summary and the detail request and opens
// a new generation (see DetailRequestLifecycle), so a late response from the previously selected
// composite identity can never overwrite this one.
// Not `providedIn: 'root'`: each overlay adapter provides its own component-scoped instance, so
// overlay activity can never mutate the page panel.
@Injectable()
export class WordTypesDetailController implements OnDestroy {
  private readonly _panel = signal<WordTypesDetailState>(INITIAL_PANEL);

  private readonly requests = new DetailRequestLifecycle();
  private activeUrlState: WordTypesWordDetailUrlState | null = null;

  readonly panelState = computed(() => this._panel());

  constructor(
    private readonly api: WordTypesApi,
    private readonly cache: WordTypesCache,
    private readonly viewLoader: WordTypesDetailViewLoader,
  ) {}

  ngOnDestroy(): void {
    this.cancelPendingLoads();
  }

  applyUrlState(state: WordTypesWordDetailUrlState | null): void {
    if (state === null) {
      this.clearSelection();
      return;
    }

    if (wordTypesWordDetailUrlStatesEqual(this.activeUrlState, state)) {
      return;
    }

    this.applyIdentity(state);
  }

  // Identity is unchanged, so applyUrlState would short-circuit it; retry re-enters the load
  // path directly. A failed read is never cached, so this re-fetches; an intact summary still
  // resolves from cache and only the detail view reloads.
  retryCurrentIdentity(): void {
    const state = this.activeUrlState;
    if (state === null) {
      return;
    }

    this.applyIdentity(state);
  }

  // Cancels the pending summary/detail loads without resetting panel state.
  cancelPendingLoads(): void {
    this.requests.cancelAll();
  }

  private clearSelection(): void {
    this.requests.cancelAll();
    this.activeUrlState = null;
    this._panel.set(INITIAL_PANEL);
  }

  private applyIdentity(state: WordTypesWordDetailUrlState): void {
    const token = this.requests.beginTransition();
    this.activeUrlState = state;
    const current = this._panel();

    if (
      current.summary !== null &&
      current.selectedRow !== null &&
      isSameWordIdentity(current.selectedRow, state.identity)
    ) {
      const selection = wordSelectionOf(state.identity);
      this._panel.update((panel) => ({
        ...panel,
        selection,
        view: state.view,
        detailPage: state.detailPage,
        status: 'loading',
        errorMessage: '',
      }));
      this.loadActiveView(selection, state.view, state.detailPage, token);
      return;
    }

    this.loadSummaryAndRestore(state, token);
  }

  private loadSummaryAndRestore(state: WordTypesWordDetailUrlState, token: number): void {
    const selection = wordSelectionOf(state.identity);
    this._panel.set({
      ...INITIAL_PANEL,
      selection,
      kind: 'word',
      selectedRow: state.identity,
      view: state.view,
      detailPage: state.detailPage,
      status: 'loading',
    });

    this.requests.trackSummary(
      this.cache
        .getOrLoad(WordTypesCacheKeys.summary(state.identity), () => this.api.getSummary(state.identity))
        .pipe(
          tap((response) => {
            if (!this.requests.isCurrent(token)) {
              return;
            }

            if (!response.isSuccess || !response.data) {
              this.applySelectionNotFound(state, response.message ?? '');
              return;
            }

            const summary = response.data;
            this._panel.update((panel) => ({
              ...panel,
              summary,
              groupedSummary: null,
              status: 'loading',
            }));
            this.loadActiveView(selection, state.view, state.detailPage, token);
          }),
          catchError((err) => {
            if (!this.requests.isCurrent(token)) {
              return of(undefined);
            }

            if (err instanceof HttpErrorResponse && err.status === 404) {
              this.applySelectionNotFound(state, extractPanelErrorMessage(err, WORD_TYPES_NOT_FOUND_LABEL));
              return of(undefined);
            }

            this.applySelectionError(state, extractPanelErrorMessage(err, WORD_TYPES_ERROR_LABEL));
            return of(undefined);
          }),
        )
        .subscribe(),
    );
  }

  private loadActiveView(
    selection: WordTypeDetailSelection,
    view: WordTypeDetailView,
    detailPage: number,
    token: number,
  ): void {
    this.requests.trackDetail(
      this.viewLoader.loadActiveView(
        { selection, view, detailPage },
        {
          // Unreachable for a word-kind selection (the loader no-ops 'words'
          // there); wired for handler-contract completeness.
          onWords: (response) =>
            this.applyIfCurrent(token, (panel) => ({ ...panel, ...buildWordsPanelUpdate(response) })),
          onAyahs: (response) =>
            this.applyIfCurrent(token, (panel) => ({ ...panel, ...buildAyahsPanelUpdate(response) })),
          onSurahs: (response) =>
            this.applyIfCurrent(token, (panel) => ({ ...panel, ...buildSurahsPanelUpdate(response) })),
          onError: (err) =>
            this.applyIfCurrent(token, (panel) => ({ ...panel, ...buildDetailErrorUpdate(err, WORD_TYPES_ERROR_LABEL) })),
        },
      ),
    );
  }

  private applyIfCurrent(token: number, update: (state: WordTypesDetailState) => WordTypesDetailState): void {
    if (this.requests.isCurrent(token)) {
      this._panel.update(update);
    }
  }

  private applySelectionNotFound(state: WordTypesWordDetailUrlState, message: string): void {
    this._panel.set({
      ...INITIAL_PANEL,
      selection: wordSelectionOf(state.identity),
      kind: 'word',
      selectedRow: state.identity,
      view: state.view,
      detailPage: state.detailPage,
      status: 'notFound',
      errorMessage: message || WORD_TYPES_NOT_FOUND_LABEL,
    });
  }

  private applySelectionError(state: WordTypesWordDetailUrlState, message: string): void {
    this._panel.set({
      ...INITIAL_PANEL,
      selection: wordSelectionOf(state.identity),
      kind: 'word',
      selectedRow: state.identity,
      view: state.view,
      detailPage: state.detailPage,
      status: 'error',
      errorMessage: message || WORD_TYPES_ERROR_LABEL,
    });
  }
}
