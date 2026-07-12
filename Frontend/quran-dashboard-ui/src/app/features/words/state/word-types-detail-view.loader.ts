import { Injectable, inject } from '@angular/core';
import { Subscription } from 'rxjs';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { WordTypesApi } from '../data-access/word-types.api';
import {
  WORD_TYPES_DETAIL_PAGE_SIZE,
  PagedResultDto,
  WordTypeAyahMatchDto,
  WordTypeDetailView,
  WordTypeRowIdentity,
  WordTypeSurahsResponseDto,
} from '../models/word-types.models';
import { WordTypesCache, WordTypesCacheKeys } from './word-types-cache';

export interface WordTypesDetailViewHandlers {
  readonly onAyahs: (response: ApiResponse<PagedResultDto<WordTypeAyahMatchDto>>) => void;
  readonly onSurahs: (response: ApiResponse<WordTypeSurahsResponseDto>) => void;
  readonly onError: (err: unknown) => void;
}

export interface WordTypesDetailLoadContext {
  readonly identity: WordTypeRowIdentity;
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
    switch (context.view) {
      // Grouped member-word loading is introduced with the kind-aware loader in Task 7.
      case 'words':
        return undefined;
      case 'ayahs':
        return this.subscribe(
          this.cache.getOrLoad(
            WordTypesCacheKeys.ayahs(context.identity, context.detailPage),
            () =>
              this.api.getAyahMatches(
                context.identity,
                context.detailPage,
                WORD_TYPES_DETAIL_PAGE_SIZE,
              ),
          ),
          handlers.onAyahs,
          handlers.onError,
        );
      case 'surahs':
        return this.subscribe(
          this.cache.getOrLoad(WordTypesCacheKeys.surahs(context.identity), () =>
            this.api.getSurahs(context.identity),
          ),
          handlers.onSurahs,
          handlers.onError,
        );
    }
  }

  private subscribe<T>(
    source: ReturnType<WordTypesCache['getOrLoad']>,
    onSuccess: (response: ApiResponse<T>) => void,
    onError: (err: unknown) => void,
  ): Subscription {
    return source.subscribe({
      next: (response) => onSuccess(response as ApiResponse<T>),
      error: (err) => onError(err),
    });
  }
}
