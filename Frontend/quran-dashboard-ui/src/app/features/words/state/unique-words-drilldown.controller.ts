import { Injectable, OnDestroy, computed, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { UniqueWordsApi } from '../data-access/unique-words.api';
import {
  DRILLDOWN_ERROR_LABEL,
  RESTORED_WORD_LOAD_ERROR_LABEL,
  RESTORED_WORD_NOT_FOUND_LABEL,
} from '../models/unique-words.labels';
import {
  DEFAULT_AYAH_PAGE,
  DEFAULT_AYAH_PAGE_SIZE,
  UniqueWordKind,
  UniqueWordListItemDto,
  UniqueWordSummaryDto,
  WordDrilldownState,
  WordDrilldownView,
} from '../models/unique-words.models';
import { toUniqueWordSummary } from '../utils/unique-words-state.helpers';
import {
  buildAyahsDrilldownUpdate,
  buildDrilldownErrorUpdate,
  buildMissingSurahsDrilldownUpdate,
  buildRestoredWordLoadError,
  buildRestoredWordNotFound,
  buildSurahsDrilldownUpdate,
  extractDrilldownMessage,
} from '../utils/unique-words-drilldown.state';
import { DetailRequestLifecycle } from './detail-request-lifecycle';
import { UniqueWordsCache, UniqueWordsCacheKeys } from './unique-words-cache';

export interface UniqueWordsDrilldownUrlState {
  readonly mode: UniqueWordKind;
  readonly wordId: number | null;
  readonly view: WordDrilldownView | null;
  readonly ayahPage: number | null;
}

interface ModalUrlState {
  readonly mode: UniqueWordKind;
  readonly wordId: number;
  readonly view: WordDrilldownView;
  readonly ayahPage: number;
}

const INITIAL_DRILLDOWN: WordDrilldownState = {
  isOpen: false,
  selectedWordId: null,
  view: 'surahs',
  summary: null,
  surahs: null,
  missingSurahs: null,
  ayahs: null,
  ayahPage: DEFAULT_AYAH_PAGE,
  status: 'idle',
  errorMessage: '',
};

@Injectable()
export class UniqueWordsDrilldownController implements OnDestroy {
  private readonly _drilldown = signal<WordDrilldownState>(INITIAL_DRILLDOWN);

  private readonly requests = new DetailRequestLifecycle();
  private activeModalUrlState: ModalUrlState | null = null;

  readonly drilldownState = computed(() => this._drilldown());

  constructor(
    private readonly api: UniqueWordsApi,
    private readonly cache: UniqueWordsCache,
  ) {}

  ngOnDestroy(): void {
    this.cancelPendingWork();
  }

  openDrilldown(word: UniqueWordListItemDto, view: WordDrilldownView): void {
    const token = this.requests.beginTransition();
    this.activeModalUrlState = {
      mode: word.kind,
      wordId: word.id,
      view,
      ayahPage: DEFAULT_AYAH_PAGE,
    };
    this._drilldown.set({
      ...INITIAL_DRILLDOWN,
      isOpen: true,
      selectedWordId: word.id,
      view,
      summary: toUniqueWordSummary(word),
      ayahPage: DEFAULT_AYAH_PAGE,
      status: 'loading',
    });
    this.loadDrilldownView(view, word.kind, word.id, DEFAULT_AYAH_PAGE, token);
  }

  setDrilldownView(view: WordDrilldownView): void {
    const current = this._drilldown();
    if (!current.isOpen || current.selectedWordId === null || current.summary === null) {
      return;
    }

    if (view === current.view) {
      return;
    }

    const nextAyahPage = view === 'ayahs' ? current.ayahPage : DEFAULT_AYAH_PAGE;
    const token = this.requests.beginTransition();
    this.activeModalUrlState = {
      mode: current.summary.kind,
      wordId: current.selectedWordId,
      view,
      ayahPage: nextAyahPage,
    };
    this._drilldown.update((s) => ({
      ...s,
      view,
      status: 'loading',
      errorMessage: '',
      ayahPage: nextAyahPage,
    }));
    this.loadDrilldownView(view, current.summary.kind, current.selectedWordId, nextAyahPage, token);
  }

  setAyahPage(page: number): void {
    const current = this._drilldown();
    if (!current.isOpen || current.selectedWordId === null || current.summary === null || page < 1) {
      return;
    }

    const token = this.requests.beginTransition();
    this.activeModalUrlState = {
      mode: current.summary.kind,
      wordId: current.selectedWordId,
      view: 'ayahs',
      ayahPage: page,
    };
    this._drilldown.update((s) => ({ ...s, ayahPage: page, status: 'loading', errorMessage: '' }));
    this.loadDrilldownView('ayahs', current.summary.kind, current.selectedWordId, page, token);
  }

  closeDrilldown(): void {
    this.cancelPendingWork();
    this._drilldown.set(INITIAL_DRILLDOWN);
  }

  // Dispose in-flight HTTP without touching held state (a request outliving the page must not mutate
  // it offscreen). Clearing activeModalUrlState makes a return to the SAME URL re-drive a real reload.
  cancelPendingWork(): void {
    this.requests.cancelAll();
    this.activeModalUrlState = null;
  }

  applyUrlState(state: UniqueWordsDrilldownUrlState): void {
    if (state.wordId === null) {
      this.closeDrilldown();
      return;
    }

    const nextState: ModalUrlState = {
      mode: state.mode,
      wordId: state.wordId,
      view: state.view ?? 'surahs',
      ayahPage: state.view === 'ayahs' ? state.ayahPage ?? DEFAULT_AYAH_PAGE : DEFAULT_AYAH_PAGE,
    };

    if (this.isSameModalUrlState(this.activeModalUrlState, nextState)) {
      return;
    }

    this.applyIdentity(nextState);
  }

  retryCurrentIdentity(): void {
    const state = this.activeModalUrlState;
    if (state === null) {
      return;
    }

    this.applyIdentity(state);
  }

  // Reuse the held summary only when kind AND id match: selectedWordId alone is ambiguous across
  // modes, so matching on id alone would serve the other mode's word.
  private applyIdentity(nextState: ModalUrlState): void {
    const token = this.requests.beginTransition();
    this.activeModalUrlState = nextState;
    const current = this._drilldown();

    if (
      current.isOpen &&
      current.selectedWordId === nextState.wordId &&
      current.summary !== null &&
      current.summary.kind === nextState.mode
    ) {
      this._drilldown.update((s) => ({
        ...s,
        view: nextState.view,
        ayahPage: nextState.ayahPage,
        status: 'loading',
        errorMessage: '',
      }));
      this.loadDrilldownView(
        nextState.view,
        current.summary.kind,
        nextState.wordId,
        nextState.ayahPage,
        token,
      );
      return;
    }

    this.loadSummaryAndRestore(nextState.mode, nextState, token);
  }

  private loadSummaryAndRestore(mode: UniqueWordKind, nextState: ModalUrlState, token: number): void {
    this._drilldown.set({
      ...INITIAL_DRILLDOWN,
      isOpen: true,
      selectedWordId: nextState.wordId,
      view: nextState.view,
      ayahPage: nextState.ayahPage,
      status: 'loading',
    });

    this.requests.trackSummary(
      this.cache
        .getOrLoad(UniqueWordsCacheKeys.summary(mode, nextState.wordId), () =>
          this.api.getSummary(mode, nextState.wordId),
        )
        .pipe(
          tap((response) => {
            if (!this.requests.isCurrent(token)) {
              return;
            }

            if (!response.isSuccess || !response.data) {
              this.handleRestoredWordNotFound(response.message ?? '');
              return;
            }
            this.openRestoredDrilldown(response.data, nextState, token);
          }),
          catchError((err) => {
            if (!this.requests.isCurrent(token)) {
              return of(undefined);
            }

            if (err instanceof HttpErrorResponse && err.status === 404) {
              this.handleRestoredWordNotFound(this.extractErrorMessage(err, RESTORED_WORD_NOT_FOUND_LABEL));
              return of(undefined);
            }

            this.handleRestoredWordLoadError(this.extractErrorMessage(err, RESTORED_WORD_LOAD_ERROR_LABEL));
            return of(undefined);
          }),
        )
        .subscribe(),
    );
  }

  private openRestoredDrilldown(
    summary: UniqueWordSummaryDto,
    nextState: ModalUrlState,
    token: number,
  ): void {
    this._drilldown.update((s) => ({
      ...s,
      summary,
      view: nextState.view,
      ayahPage: nextState.ayahPage,
      status: 'loading',
    }));
    this.loadDrilldownView(nextState.view, summary.kind, summary.id, nextState.ayahPage, token);
  }

  private handleRestoredWordNotFound(message: string): void {
    this._drilldown.set({ ...INITIAL_DRILLDOWN, ...buildRestoredWordNotFound(message) });
  }

  private handleRestoredWordLoadError(message: string): void {
    this._drilldown.set({ ...INITIAL_DRILLDOWN, ...buildRestoredWordLoadError(message) });
  }

  private isSameModalUrlState(
    current: ModalUrlState | null,
    next: ModalUrlState,
  ): boolean {
    return (
      current !== null &&
      current.mode === next.mode &&
      current.wordId === next.wordId &&
      current.view === next.view &&
      current.ayahPage === next.ayahPage
    );
  }

  private loadDrilldownView(
    view: WordDrilldownView,
    kind: UniqueWordKind,
    wordId: number,
    ayahPage: number,
    token: number,
  ): void {
    if (view === 'surahs') {
      this.trackDrilldownRead(
        this.cache.getOrLoad(UniqueWordsCacheKeys.surahs(kind, wordId), () =>
          this.api.getMentionedSurahs(kind, wordId),
        ),
        token,
        buildSurahsDrilldownUpdate,
      );
      return;
    }

    if (view === 'missing') {
      const heldMissingSurahs = this._drilldown().missingSurahs;
      if (heldMissingSurahs !== null) {
        this.requests.trackDetail(undefined);
        this.applyIfCurrent(token, (s) => ({
          ...s,
          missingSurahs: heldMissingSurahs,
          status: heldMissingSurahs.surahs.length === 0 ? 'empty' : 'success',
          errorMessage: '',
        }));
        return;
      }

      this.trackDrilldownRead(
        this.cache.getOrLoad(UniqueWordsCacheKeys.missing(kind, wordId), () =>
          this.api.getMissingSurahs(kind, wordId),
        ),
        token,
        buildMissingSurahsDrilldownUpdate,
      );
      return;
    }

    this.trackDrilldownRead(
      this.cache.getOrLoad(UniqueWordsCacheKeys.ayahs(kind, wordId, ayahPage), () =>
        this.api.getAyahMatches(kind, wordId, ayahPage, DEFAULT_AYAH_PAGE_SIZE),
      ),
      token,
      buildAyahsDrilldownUpdate,
    );
  }

  private trackDrilldownRead<T>(
    read$: Observable<ApiResponse<T>>,
    token: number,
    buildUpdate: (response: ApiResponse<T>) => Partial<WordDrilldownState>,
  ): void {
    this.requests.trackDetail(
      read$
        .pipe(
          tap((response) => this.applyIfCurrent(token, (s) => ({ ...s, ...buildUpdate(response) }))),
          catchError((err) => {
            this.handleDrilldownError(err, token);
            return of(undefined);
          }),
        )
        .subscribe(),
    );
  }

  private applyIfCurrent(token: number, update: (state: WordDrilldownState) => WordDrilldownState): void {
    if (this.requests.isCurrent(token)) {
      this._drilldown.update(update);
    }
  }

  private handleDrilldownError(err: unknown, token: number): void {
    this.applyIfCurrent(token, (s) => ({ ...s, ...buildDrilldownErrorUpdate(err, DRILLDOWN_ERROR_LABEL) }));
  }

  private extractErrorMessage(err: unknown, fallback: string): string {
    return extractDrilldownMessage(err, fallback);
  }
}
