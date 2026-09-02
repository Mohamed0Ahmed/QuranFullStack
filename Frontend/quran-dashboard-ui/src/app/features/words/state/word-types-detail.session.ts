import { HttpErrorResponse } from '@angular/common/http';
import { DestroyRef, Injectable, inject, signal } from '@angular/core';
import { Subscription } from 'rxjs';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { WordTypesApi } from '../data-access/word-types.api';
import { WORD_TYPES_ERROR_LABEL, WORD_TYPES_NOT_FOUND_LABEL } from '../models/word-types.labels';
import {
  DEFAULT_WORD_TYPES_DETAIL_PAGE,
  DEFAULT_WORD_TYPES_DETAIL_VIEW,
  PagedResultDto,
  WORD_TYPES_DETAIL_PAGE_SIZE,
  WordTypeAyahMatchDto,
  WordTypeRowIdentity,
  WordTypeSummaryDto,
  WordTypeSurahsResponseDto,
} from '../models/word-types.models';
import {
  WordTypeDetailSelection,
  WordTypeDetailScope,
  WordTypeGroupedDetailSelection,
  WordTypeGroupedMemberWordDto,
  WordTypeGroupedRequestParams,
  WordTypeGroupedSummaryDto,
  WordTypesDetailState,
  WordTypesDetailTarget,
} from '../models/word-types-detail.models';
import { WordTypesCache, WordTypesCacheKeys } from './word-types-cache';

const INITIAL_STATE: WordTypesDetailState = {
  status: 'idle',
  selection: null,
  view: DEFAULT_WORD_TYPES_DETAIL_VIEW,
  detailPage: DEFAULT_WORD_TYPES_DETAIL_PAGE,
  summary: null,
  groupedSummary: null,
  words: null,
  ayahs: null,
  surahs: null,
  errorMessage: '',
};

@Injectable()
export class WordTypesDetailSession {
  private readonly api = inject(WordTypesApi);
  private readonly cache = inject(WordTypesCache);
  private readonly _state = signal<WordTypesDetailState>(INITIAL_STATE);
  private summaryRequest?: Subscription;
  private detailRequest?: Subscription;
  private activeTarget: WordTypesDetailTarget | null = null;
  private generation = 0;

  readonly state = this._state.asReadonly();

  constructor() {
    inject(DestroyRef).onDestroy(() => {
      this.cancelRequests();
      this.generation++;
    });
  }

  synchronize(target: WordTypesDetailTarget | null): void {
    const next = target === null ? null : normalizeTarget(target);
    if (targetsEqual(this.activeTarget, next)) {
      return;
    }

    const previousSelection = this.activeTarget?.selection ?? null;
    const generation = this.beginTransition();
    this.activeTarget = next;

    if (next === null) {
      this._state.set(INITIAL_STATE);
      return;
    }

    const current = this._state();
    if (selectionsEqual(previousSelection, next.selection) && hasSummary(current)) {
      this._state.update((state) => ({
        ...state,
        selection: next.selection,
        view: next.view,
        detailPage: next.detailPage,
        status: 'loading',
        errorMessage: '',
      }));
      this.loadDetail(next, generation);
      return;
    }

    const seed = acceptedSeed(next);
    this._state.set({
      ...INITIAL_STATE,
      selection: next.selection,
      view: next.view,
      detailPage: next.detailPage,
      summary: seed?.kind === 'word' ? seed.summary : null,
      groupedSummary: seed?.kind === 'grouped' ? seed.summary : null,
      status: 'loading',
    });

    if (seed !== null) {
      this.loadDetail(next, generation);
      return;
    }

    this.loadSummary(next, generation);
  }

  retry(): void {
    const target = this.activeTarget;
    if (target === null) {
      return;
    }

    const generation = this.beginTransition();
    this._state.update((state) => ({ ...state, status: 'loading', errorMessage: '' }));
    if (hasSummary(this._state())) {
      this.loadDetail(target, generation);
      return;
    }

    this.loadSummary(target, generation);
  }

  private beginTransition(): number {
    this.cancelRequests();
    return ++this.generation;
  }

  private cancelRequests(): void {
    this.summaryRequest?.unsubscribe();
    this.detailRequest?.unsubscribe();
    this.summaryRequest = undefined;
    this.detailRequest = undefined;
  }

  private loadSummary(target: WordTypesDetailTarget, generation: number): void {
    const selection = target.selection;
    if (selection.kind === 'word') {
      this.summaryRequest = this.cache
        .getOrLoad(WordTypesCacheKeys.summary(selection.identity), () =>
          this.api.getSummary(selection.identity),
        )
        .subscribe({
          next: (response) => this.acceptWordSummary(response, target, generation),
          error: (error) => this.applySummaryTransportFailure(error, target, generation),
        });
      return;
    }

    const request = toGroupedRequest(selection);
    this.summaryRequest = this.cache
      .getOrLoad(WordTypesCacheKeys.groupedSummary(request), () =>
        this.api.getGroupedSummary(request),
      )
      .subscribe({
        next: (response) => this.acceptGroupedSummary(response, target, generation),
        error: (error) => this.applySummaryTransportFailure(error, target, generation),
      });
  }

  private acceptWordSummary(
    response: ApiResponse<WordTypeSummaryDto>,
    target: WordTypesDetailTarget,
    generation: number,
  ): void {
    if (!this.isCurrent(generation)) {
      return;
    }
    if (!response.isSuccess || response.data == null) {
      this.applySummaryFailure(target, 'notFound', response.message ?? WORD_TYPES_NOT_FOUND_LABEL);
      return;
    }

    const summary = response.data;
    this._state.update((state) => ({ ...state, summary, groupedSummary: null }));
    this.loadDetail(target, generation);
  }

  private acceptGroupedSummary(
    response: ApiResponse<WordTypeGroupedSummaryDto>,
    target: WordTypesDetailTarget,
    generation: number,
  ): void {
    if (!this.isCurrent(generation)) {
      return;
    }
    if (!response.isSuccess || response.data == null) {
      this.applySummaryFailure(target, 'notFound', response.message ?? WORD_TYPES_NOT_FOUND_LABEL);
      return;
    }

    const summary = response.data;
    this._state.update((state) => ({ ...state, summary: null, groupedSummary: summary }));
    this.loadDetail(target, generation);
  }

  private applySummaryTransportFailure(
    error: unknown,
    target: WordTypesDetailTarget,
    generation: number,
  ): void {
    if (!this.isCurrent(generation)) {
      return;
    }

    const notFound = error instanceof HttpErrorResponse && error.status === 404;
    this.applySummaryFailure(
      target,
      notFound ? 'notFound' : 'error',
      errorMessage(error, notFound ? WORD_TYPES_NOT_FOUND_LABEL : WORD_TYPES_ERROR_LABEL),
    );
  }

  private applySummaryFailure(
    target: WordTypesDetailTarget,
    status: 'notFound' | 'error',
    message: string,
  ): void {
    this._state.set({
      ...INITIAL_STATE,
      selection: target.selection,
      view: target.view,
      detailPage: target.detailPage,
      status,
      errorMessage:
        message || (status === 'notFound' ? WORD_TYPES_NOT_FOUND_LABEL : WORD_TYPES_ERROR_LABEL),
    });
  }

  private loadDetail(target: WordTypesDetailTarget, generation: number): void {
    const selection = target.selection;
    switch (target.view) {
      case 'words': {
        if (selection.kind === 'word') {
          return;
        }
        const request = toGroupedRequest(selection);
        this.detailRequest = this.cache
          .getOrLoad(WordTypesCacheKeys.groupedWords(request, target.detailPage), () =>
            this.api.getGroupedMemberWords(request, target.detailPage, WORD_TYPES_DETAIL_PAGE_SIZE),
          )
          .subscribe({
            next: (response) => this.acceptWords(response, generation),
            error: (error) => this.applyDetailTransportFailure(error, generation),
          });
        return;
      }
      case 'ayahs': {
        const source =
          selection.kind === 'word'
            ? this.cache.getOrLoad(
                WordTypesCacheKeys.ayahs(selection.identity, target.detailPage),
                () =>
                  this.api.getAyahMatches(
                    selection.identity,
                    target.detailPage,
                    WORD_TYPES_DETAIL_PAGE_SIZE,
                  ),
              )
            : this.loadGroupedAyahs(selection, target.detailPage);
        this.detailRequest = source.subscribe({
          next: (response) => this.acceptAyahs(response, generation),
          error: (error) => this.applyDetailTransportFailure(error, generation),
        });
        return;
      }
      case 'surahs': {
        const source =
          selection.kind === 'word'
            ? this.cache.getOrLoad(WordTypesCacheKeys.surahs(selection.identity), () =>
                this.api.getSurahs(selection.identity),
              )
            : this.loadGroupedSurahs(selection);
        this.detailRequest = source.subscribe({
          next: (response) => this.acceptSurahs(response, generation),
          error: (error) => this.applyDetailTransportFailure(error, generation),
        });
      }
    }
  }

  private loadGroupedAyahs(selection: WordTypeGroupedDetailSelection, page: number) {
    const request = toGroupedRequest(selection);
    return this.cache.getOrLoad(WordTypesCacheKeys.groupedAyahs(request, page), () =>
      this.api.getGroupedAyahMatches(request, page, WORD_TYPES_DETAIL_PAGE_SIZE),
    );
  }

  private loadGroupedSurahs(selection: WordTypeGroupedDetailSelection) {
    const request = toGroupedRequest(selection);
    return this.cache.getOrLoad(WordTypesCacheKeys.groupedSurahs(request), () =>
      this.api.getGroupedSurahs(request),
    );
  }

  private acceptWords(
    response: ApiResponse<PagedResultDto<WordTypeGroupedMemberWordDto>>,
    generation: number,
  ): void {
    if (!this.isCurrent(generation)) {
      return;
    }
    if (!response.isSuccess || response.data == null) {
      this.applyDetailFailure(response.message ?? WORD_TYPES_ERROR_LABEL);
      return;
    }
    const page = response.data;
    this._state.update((state) => ({
      ...state,
      words: page,
      status: page.totalCount === 0 ? 'empty' : 'success',
      errorMessage: '',
    }));
  }

  private acceptAyahs(
    response: ApiResponse<PagedResultDto<WordTypeAyahMatchDto>>,
    generation: number,
  ): void {
    if (!this.isCurrent(generation)) {
      return;
    }
    if (!response.isSuccess || response.data == null) {
      this.applyDetailFailure(response.message ?? WORD_TYPES_ERROR_LABEL);
      return;
    }
    const page = response.data;
    this._state.update((state) => ({
      ...state,
      ayahs: page,
      status: page.totalCount === 0 ? 'empty' : 'success',
      errorMessage: '',
    }));
  }

  private acceptSurahs(response: ApiResponse<WordTypeSurahsResponseDto>, generation: number): void {
    if (!this.isCurrent(generation)) {
      return;
    }
    if (!response.isSuccess || response.data == null) {
      this.applyDetailFailure(response.message ?? WORD_TYPES_ERROR_LABEL);
      return;
    }
    const payload = response.data;
    this._state.update((state) => ({
      ...state,
      surahs: payload,
      status:
        payload.surahs.length === 0 && payload.missingSurahs.length === 0 ? 'empty' : 'success',
      errorMessage: '',
    }));
  }

  private applyDetailTransportFailure(error: unknown, generation: number): void {
    if (this.isCurrent(generation)) {
      this.applyDetailFailure(errorMessage(error, WORD_TYPES_ERROR_LABEL));
    }
  }

  private applyDetailFailure(message: string): void {
    this._state.update((state) => ({
      ...state,
      status: 'error',
      errorMessage: message || WORD_TYPES_ERROR_LABEL,
    }));
  }

  private isCurrent(generation: number): boolean {
    return generation === this.generation;
  }
}

function normalizeTarget(target: WordTypesDetailTarget): WordTypesDetailTarget {
  const view =
    target.selection.kind === 'word' && target.view === 'words'
      ? DEFAULT_WORD_TYPES_DETAIL_VIEW
      : target.view;
  const detailPage =
    view === 'surahs' || !Number.isSafeInteger(target.detailPage) || target.detailPage < 1
      ? DEFAULT_WORD_TYPES_DETAIL_PAGE
      : target.detailPage;
  return { ...target, view, detailPage };
}

function hasSummary(state: WordTypesDetailState): boolean {
  return state.summary !== null || state.groupedSummary !== null;
}

function acceptedSeed(
  target: WordTypesDetailTarget,
):
  | { readonly kind: 'word'; readonly summary: WordTypeSummaryDto }
  | { readonly kind: 'grouped'; readonly summary: WordTypeGroupedSummaryDto }
  | null {
  const seed = target.seed;
  if (seed === undefined || !selectionsEqual(seed.selection, target.selection)) {
    return null;
  }
  if (seed.kind === 'word' && target.selection.kind === 'word') {
    return identitiesEqual(seed.summary, target.selection.identity)
      ? { kind: 'word', summary: seed.summary }
      : null;
  }
  if (seed.kind === 'grouped' && target.selection.kind !== 'word') {
    return seed.summary.kind === target.selection.kind &&
      seed.summary.dimensionId === dimensionId(target.selection)
      ? { kind: 'grouped', summary: seed.summary }
      : null;
  }
  return null;
}

function targetsEqual(
  current: WordTypesDetailTarget | null,
  next: WordTypesDetailTarget | null,
): boolean {
  if (current === null || next === null) {
    return current === next;
  }
  return (
    selectionsEqual(current.selection, next.selection) &&
    current.view === next.view &&
    current.detailPage === next.detailPage
  );
}

function selectionsEqual(
  current: WordTypeDetailSelection | null,
  next: WordTypeDetailSelection,
): boolean {
  if (current === null || current.kind !== next.kind) {
    return false;
  }
  if (current.kind === 'word' && next.kind === 'word') {
    return identitiesEqual(current.identity, next.identity);
  }
  if (current.kind !== 'word' && next.kind !== 'word') {
    return dimensionId(current) === dimensionId(next) && scopesEqual(current.scope, next.scope);
  }
  return false;
}

function identitiesEqual(current: WordTypeRowIdentity, next: WordTypeRowIdentity): boolean {
  return (
    current.tashkeelWordId === next.tashkeelWordId &&
    current.contextCode === next.contextCode &&
    current.case === next.case &&
    current.tense === next.tense &&
    current.voice === next.voice
  );
}

function scopesEqual(current: WordTypeDetailScope, next: WordTypeDetailScope): boolean {
  return (
    current.type === next.type &&
    current.childCode === next.childCode &&
    current.case === next.case &&
    current.tense === next.tense &&
    current.voice === next.voice
  );
}

function dimensionId(selection: WordTypeGroupedDetailSelection): number {
  switch (selection.kind) {
    case 'root':
      return selection.rootId;
    case 'stem':
      return selection.stemId;
    case 'lemma':
      return selection.lemmaId;
  }
}

function toGroupedRequest(selection: WordTypeGroupedDetailSelection): WordTypeGroupedRequestParams {
  return { kind: selection.kind, dimensionId: dimensionId(selection), ...selection.scope };
}

function errorMessage(error: unknown, fallback: string): string {
  if (error instanceof HttpErrorResponse) {
    const body = error.error as ApiResponse<unknown> | null;
    if (body?.message) {
      return body.message;
    }
  }
  return fallback;
}
