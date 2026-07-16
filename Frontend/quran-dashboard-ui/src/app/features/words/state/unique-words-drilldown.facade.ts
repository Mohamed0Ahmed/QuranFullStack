import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Subscription, of } from 'rxjs';
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
  PagedResultDto,
  UniqueWordAyahMatchDto,
  UniqueWordKind,
  UniqueWordListItemDto,
  UniqueWordMissingSurahsDto,
  UniqueWordSummaryDto,
  UniqueWordSurahsDto,
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
import { UniqueWordsCache, UniqueWordsCacheKeys } from './unique-words-cache';

/**
 * Identity of the drilldown modal as expressed by the URL. `mode` is part of the identity:
 * simple and tashkeel are separate word spaces, so the same `wordId` denotes a different word
 * in each and a held summary from one mode must never be reused for the other.
 */
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

@Injectable({ providedIn: 'root' })
export class UniqueWordsDrilldownFacade {
  private readonly api = inject(UniqueWordsApi);
  private readonly cache = inject(UniqueWordsCache);

  private readonly _drilldown = signal<WordDrilldownState>(INITIAL_DRILLDOWN);

  private drilldownSub?: Subscription;
  private summarySub?: Subscription;

  private activeModalUrlState: ModalUrlState | null = null;

  readonly drilldownState = computed(() => this._drilldown());

  openDrilldown(word: UniqueWordListItemDto, view: WordDrilldownView): void {
    const summary = toUniqueWordSummary(word);
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
      summary,
      ayahPage: DEFAULT_AYAH_PAGE,
      status: 'loading',
    });
    this.loadDrilldownView(view, word.kind, word.id, DEFAULT_AYAH_PAGE);
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
    this.loadDrilldownView(view, current.summary.kind, current.selectedWordId, this._drilldown().ayahPage);
  }

  setAyahPage(page: number): void {
    const current = this._drilldown();
    if (!current.isOpen || current.selectedWordId === null || current.summary === null || page < 1) {
      return;
    }

    this.activeModalUrlState = {
      mode: current.summary.kind,
      wordId: current.selectedWordId,
      view: 'ayahs',
      ayahPage: page,
    };
    this._drilldown.update((s) => ({ ...s, ayahPage: page, status: 'loading', errorMessage: '' }));
    this.loadDrilldownView('ayahs', current.summary.kind, current.selectedWordId, page);
  }

  closeDrilldown(): void {
    this.cancelPendingWork();
    this._drilldown.set(INITIAL_DRILLDOWN);
  }

  /**
   * Disposes any in-flight summary/detail HTTP subscription without touching the currently-held
   * drilldown state (perf finding F3). Called on page/facade unbind (component destroy or
   * navigation away) so a request that outlives the page can no longer mutate this root-singleton's
   * state offscreen. `activeModalUrlState` is cleared so that returning to the SAME URL is never
   * short-circuited by the "unchanged selection" fast path — it always re-drives a real reload
   * (which itself may resolve from the detail cache, preserving that behavior) instead of leaving
   * the state stuck mid-load.
   */
  cancelPendingWork(): void {
    this.summarySub?.unsubscribe();
    this.summarySub = undefined;
    this.drilldownSub?.unsubscribe();
    this.drilldownSub = undefined;
    this.activeModalUrlState = null;
  }

  restoreFromUrl(
    mode: UniqueWordKind,
    wordId: number | null,
    view: WordDrilldownView | null,
    ayahPage: number | null,
  ): void {
    if (wordId === null) {
      this.closeDrilldown();
      return;
    }

    const nextState: ModalUrlState = {
      mode,
      wordId,
      view: view ?? 'surahs',
      ayahPage: view === 'ayahs' ? ayahPage ?? DEFAULT_AYAH_PAGE : DEFAULT_AYAH_PAGE,
    };

    if (this.isSameModalUrlState(this.activeModalUrlState, nextState)) {
      return;
    }

    this.activeModalUrlState = nextState;
    this.restoreOrUpdateModal(nextState);
  }

  /**
   * Reuses the held summary only when it describes the very word the URL now asks for. The
   * summary's own `kind` must match the requested mode: `selectedWordId` alone is ambiguous
   * across modes, so matching on it would serve the previous mode's word and details.
   */
  private restoreOrUpdateModal(nextState: ModalUrlState): void {
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
      );
      return;
    }

    this.loadSummaryAndRestore(nextState.mode, nextState);
  }

  private loadSummaryAndRestore(mode: UniqueWordKind, nextState: ModalUrlState): void {
    this.summarySub?.unsubscribe();
    this._drilldown.set({
      ...INITIAL_DRILLDOWN,
      isOpen: true,
      selectedWordId: nextState.wordId,
      view: nextState.view,
      ayahPage: nextState.ayahPage,
      status: 'loading',
    });

    this.summarySub = this.cache
      .getOrLoad(UniqueWordsCacheKeys.summary(mode, nextState.wordId), () =>
        this.api.getSummary(mode, nextState.wordId),
      )
      .pipe(
        tap((response) => {
          if (!response.isSuccess || !response.data) {
            this.handleRestoredWordNotFound(response.message ?? '');
            return;
          }
          this.openRestoredDrilldown(response.data, nextState);
        }),
        catchError((err) => {
          if (err instanceof HttpErrorResponse && err.status === 404) {
            const message = this.extractErrorMessage(err, RESTORED_WORD_NOT_FOUND_LABEL);
            this.handleRestoredWordNotFound(message);
            return of(undefined);
          }

          const message = this.extractErrorMessage(err, RESTORED_WORD_LOAD_ERROR_LABEL);
          this.handleRestoredWordLoadError(message);
          return of(undefined);
        }),
      )
      .subscribe();
  }

  private openRestoredDrilldown(
    summary: UniqueWordSummaryDto,
    nextState: ModalUrlState,
  ): void {
    this._drilldown.update((s) => ({
      ...s,
      summary,
      view: nextState.view,
      ayahPage: nextState.ayahPage,
      status: 'loading',
    }));
    this.loadDrilldownView(nextState.view, summary.kind, summary.id, nextState.ayahPage);
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
  ): void {
    this.drilldownSub?.unsubscribe();

    if (view === 'surahs') {
      this.drilldownSub = this.cache
        .getOrLoad(UniqueWordsCacheKeys.surahs(kind, wordId), () =>
          this.api.getMentionedSurahs(kind, wordId),
        )
        .pipe(
          tap((response) => this.handleSurahsResponse(response)),
          catchError((err) => {
            this.handleDrilldownError(err);
            return of(undefined);
          }),
        )
        .subscribe();
      return;
    }

    if (view === 'missing') {
      const current = this._drilldown();
      if (current.missingSurahs !== null) {
        const missingSurahs = current.missingSurahs;
        this._drilldown.update((s) => ({
          ...s,
          missingSurahs,
          status: missingSurahs.surahs.length === 0 ? 'empty' : 'success',
          errorMessage: '',
        }));
        return;
      }

      this.drilldownSub = this.cache
        .getOrLoad(UniqueWordsCacheKeys.missing(kind, wordId), () =>
          this.api.getMissingSurahs(kind, wordId),
        )
        .pipe(
          tap((response) => this.handleMissingSurahsResponse(response)),
          catchError((err) => {
            this.handleDrilldownError(err);
            return of(undefined);
          }),
        )
        .subscribe();
      return;
    }

    this.drilldownSub = this.cache
      .getOrLoad(UniqueWordsCacheKeys.ayahs(kind, wordId, ayahPage), () =>
        this.api.getAyahMatches(kind, wordId, ayahPage, DEFAULT_AYAH_PAGE_SIZE),
      )
      .pipe(
        tap((response) => this.handleAyahsResponse(response)),
        catchError((err) => {
          this.handleDrilldownError(err);
          return of(undefined);
        }),
      )
      .subscribe();
  }

  private handleSurahsResponse(response: ApiResponse<UniqueWordSurahsDto>): void {
    this._drilldown.update((s) => ({ ...s, ...buildSurahsDrilldownUpdate(response) }));
  }

  private handleMissingSurahsResponse(response: ApiResponse<UniqueWordMissingSurahsDto>): void {
    this._drilldown.update((s) => ({ ...s, ...buildMissingSurahsDrilldownUpdate(response) }));
  }

  private handleAyahsResponse(response: ApiResponse<PagedResultDto<UniqueWordAyahMatchDto>>): void {
    this._drilldown.update((s) => ({ ...s, ...buildAyahsDrilldownUpdate(response) }));
  }

  private handleDrilldownError(err: unknown): void {
    this._drilldown.update((s) => ({ ...s, ...buildDrilldownErrorUpdate(err, DRILLDOWN_ERROR_LABEL) }));
  }

  private extractErrorMessage(err: unknown, fallback: string): string {
    return extractDrilldownMessage(err, fallback);
  }
}
