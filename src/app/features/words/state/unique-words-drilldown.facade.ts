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
  UniqueWordSummaryDto,
  UniqueWordSurahsDto,
  WordDrilldownState,
  WordDrilldownView,
} from '../models/unique-words.models';
import { buildMissingSurahsPayload } from '../utils/unique-words-surahs';
import { toUniqueWordSummary } from '../utils/unique-words-state.helpers';
import {
  buildAyahsDrilldownUpdate,
  buildDrilldownErrorUpdate,
  buildRestoredWordLoadError,
  buildRestoredWordNotFound,
  buildSurahsDrilldownUpdate,
  extractDrilldownMessage,
} from '../utils/unique-words-drilldown.state';

interface ModalUrlState {
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

/**
 * Owns the selected-word drill-down/modal slice: open/close, view switching,
 * ayah pagination, and URL restore (US3 + the modal half of US4). Split out of
 * {@link UniqueWordsFacade} so the list facade stays focused on list loading and
 * paging; {@link UniqueWordsFacade} delegates the drill-down surface to this
 * service and feeds it URL changes via {@link restoreFromUrl}.
 */
@Injectable({ providedIn: 'root' })
export class UniqueWordsDrilldownFacade {
  private readonly api = inject(UniqueWordsApi);

  private readonly _drilldown = signal<WordDrilldownState>(INITIAL_DRILLDOWN);

  private drilldownSub?: Subscription;
  private summarySub?: Subscription;

  /**
   * Modal state currently reflected by the URL or an in-app action. This tracks
   * the full modal tuple, not just the word ID, so browser back/forward can
   * restore same-word `view` and `ap` changes.
   */
  private activeModalUrlState: ModalUrlState | null = null;

  readonly drilldownState = computed(() => this._drilldown());

  openDrilldown(word: UniqueWordListItemDto, view: WordDrilldownView): void {
    const summary = toUniqueWordSummary(word);
    this.activeModalUrlState = {
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
      wordId: current.selectedWordId,
      view: 'ayahs',
      ayahPage: page,
    };
    this._drilldown.update((s) => ({ ...s, ayahPage: page, status: 'loading', errorMessage: '' }));
    this.loadDrilldownView('ayahs', current.summary.kind, current.selectedWordId, page);
  }

  closeDrilldown(): void {
    this.summarySub?.unsubscribe();
    this.summarySub = undefined;
    this.drilldownSub?.unsubscribe();
    this.drilldownSub = undefined;
    this.activeModalUrlState = null;
    this._drilldown.set(INITIAL_DRILLDOWN);
  }

  /**
   * Reconciles the drill-down/modal slice with the `word`/`view`/`ap` URL state.
   * `mode` is the active list kind, used only to fetch the summary for a freshly
   * restored word. A null `wordId` closes the modal.
   */
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
      wordId,
      view: view ?? 'surahs',
      ayahPage: view === 'ayahs' ? ayahPage ?? DEFAULT_AYAH_PAGE : DEFAULT_AYAH_PAGE,
    };

    if (this.isSameModalUrlState(this.activeModalUrlState, nextState)) {
      return;
    }

    this.activeModalUrlState = nextState;
    this.restoreOrUpdateModal(mode, nextState);
  }

  private restoreOrUpdateModal(mode: UniqueWordKind, nextState: ModalUrlState): void {
    const current = this._drilldown();
    if (
      current.isOpen &&
      current.selectedWordId === nextState.wordId &&
      current.summary !== null
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

    this.loadSummaryAndRestore(mode, nextState);
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

    this.summarySub = this.api
      .getSummary(mode, nextState.wordId)
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
    // Controlled not-found: keep the modal surface closed, surface a not-found
    // status, and keep the list fully usable. `activeModalUrlState` stays set to
    // the attempted state so a lingering bad `word` param is not re-fetched on
    // later list-only navigation. The page renders a controlled Arabic message;
    // no Quranic text is invented.
    this._drilldown.set({ ...INITIAL_DRILLDOWN, ...buildRestoredWordNotFound(message) });
  }

  private handleRestoredWordLoadError(message: string): void {
    // Mirror not-found handling: keep `activeModalUrlState` so the failed
    // restore is not re-attempted on every later list-only navigation.
    this._drilldown.set({ ...INITIAL_DRILLDOWN, ...buildRestoredWordLoadError(message) });
  }

  private isSameModalUrlState(
    current: ModalUrlState | null,
    next: ModalUrlState,
  ): boolean {
    return (
      current !== null &&
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
      this.drilldownSub = this.api
        .getMentionedSurahs(kind, wordId)
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

      if (current.surahs !== null) {
        const surahs = current.surahs;
        this._drilldown.update((s) => ({
          ...s,
          missingSurahs: buildMissingSurahsPayload(surahs),
          status: surahs.surahs.length === 0 ? 'empty' : 'success',
          errorMessage: '',
        }));
        return;
      }

      this.drilldownSub = this.api
        .getMentionedSurahs(kind, wordId)
        .pipe(
          tap((response) => {
            if (!response.isSuccess || !response.data) {
              this._drilldown.update((s) => ({
                ...s,
                status: 'error',
                errorMessage: response.message ?? DRILLDOWN_ERROR_LABEL,
              }));
              return;
            }

            const missingSurahs = buildMissingSurahsPayload(response.data);

            this._drilldown.update((s) => ({
              ...s,
              missingSurahs,
              status: missingSurahs.surahs.length === 0 ? 'empty' : 'success',
              errorMessage: '',
            }));
          }),
          catchError((err) => {
            this.handleDrilldownError(err);
            return of(undefined);
          }),
        )
        .subscribe();
      return;
    }

    this.drilldownSub = this.api
      .getAyahMatches(kind, wordId, ayahPage, DEFAULT_AYAH_PAGE_SIZE)
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
