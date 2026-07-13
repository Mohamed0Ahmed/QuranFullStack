import { Injectable, inject } from '@angular/core';
import { Observable, Subscription } from 'rxjs';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { WordTypesApi } from '../data-access/word-types.api';
import {
  WORD_TYPES_DETAIL_PAGE_SIZE,
  PagedResultDto,
  WordTypeAyahMatchDto,
  WordTypeDetailView,
  WordTypeSurahsResponseDto,
} from '../models/word-types.models';
import {
  WordTypeDetailSelection,
  WordTypeGroupedDetailSelection,
  WordTypeGroupedMemberWordDto,
  WordTypeGroupedRequestParams,
} from '../models/word-types-detail.models';
import { WordTypesCache, WordTypesCacheKeys } from './word-types-cache';

export interface WordTypesDetailViewHandlers {
  readonly onWords: (response: ApiResponse<PagedResultDto<WordTypeGroupedMemberWordDto>>) => void;
  readonly onAyahs: (response: ApiResponse<PagedResultDto<WordTypeAyahMatchDto>>) => void;
  readonly onSurahs: (response: ApiResponse<WordTypeSurahsResponseDto>) => void;
  readonly onError: (err: unknown) => void;
}

export interface WordTypesDetailLoadContext {
  readonly selection: WordTypeDetailSelection;
  readonly view: WordTypeDetailView;
  readonly detailPage: number;
}

@Injectable({ providedIn: 'root' })
export class WordTypesDetailViewLoader {
  private readonly api = inject(WordTypesApi);
  private readonly cache = inject(WordTypesCache);

  loadActiveView(
    context: WordTypesDetailLoadContext,
    handlers: WordTypesDetailViewHandlers,
  ): Subscription | undefined {
    const { selection, view, detailPage } = context;

    switch (view) {
      case 'words':
        // Member words exist only for a grouped selection; a word selection has no words view.
        return selection.kind === 'word'
          ? undefined
          : this.subscribe(this.loadGroupedWords(selection, detailPage), handlers.onWords, handlers.onError);
      case 'ayahs':
        return this.subscribe(this.loadAyahs(selection, detailPage), handlers.onAyahs, handlers.onError);
      case 'surahs':
        return this.subscribe(this.loadSurahs(selection), handlers.onSurahs, handlers.onError);
    }
  }

  private loadGroupedWords(
    selection: WordTypeGroupedDetailSelection,
    page: number,
  ): Observable<ApiResponse<PagedResultDto<WordTypeGroupedMemberWordDto>>> {
    const request = toGroupedRequest(selection);
    return this.cache.getOrLoad(WordTypesCacheKeys.groupedWords(request, page), () =>
      this.api.getGroupedMemberWords(request, page, WORD_TYPES_DETAIL_PAGE_SIZE),
    );
  }

  private loadAyahs(
    selection: WordTypeDetailSelection,
    page: number,
  ): Observable<ApiResponse<PagedResultDto<WordTypeAyahMatchDto>>> {
    if (selection.kind === 'word') {
      return this.cache.getOrLoad(WordTypesCacheKeys.ayahs(selection.identity, page), () =>
        this.api.getAyahMatches(selection.identity, page, WORD_TYPES_DETAIL_PAGE_SIZE),
      );
    }

    const request = toGroupedRequest(selection);
    return this.cache.getOrLoad(WordTypesCacheKeys.groupedAyahs(request, page), () =>
      this.api.getGroupedAyahMatches(request, page, WORD_TYPES_DETAIL_PAGE_SIZE),
    );
  }

  private loadSurahs(selection: WordTypeDetailSelection): Observable<ApiResponse<WordTypeSurahsResponseDto>> {
    if (selection.kind === 'word') {
      return this.cache.getOrLoad(WordTypesCacheKeys.surahs(selection.identity), () =>
        this.api.getSurahs(selection.identity),
      );
    }

    const request = toGroupedRequest(selection);
    return this.cache.getOrLoad(WordTypesCacheKeys.groupedSurahs(request), () => this.api.getGroupedSurahs(request));
  }

  private subscribe<T>(
    source: Observable<ApiResponse<T>>,
    onSuccess: (response: ApiResponse<T>) => void,
    onError: (err: unknown) => void,
  ): Subscription {
    return source.subscribe({
      next: (response) => onSuccess(response),
      error: (err) => onError(err),
    });
  }
}

// Projects a grouped selection into the flat request shape shared by the API client and cache keys.
export function toGroupedRequest(selection: WordTypeGroupedDetailSelection): WordTypeGroupedRequestParams {
  switch (selection.kind) {
    case 'root':
      return { kind: 'root', dimensionId: selection.rootId, ...selection.scope };
    case 'stem':
      return { kind: 'stem', dimensionId: selection.stemId, ...selection.scope };
    case 'lemma':
      return { kind: 'lemma', dimensionId: selection.lemmaId, ...selection.scope };
  }
}
