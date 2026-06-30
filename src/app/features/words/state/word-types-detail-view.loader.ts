import { Injectable, inject } from '@angular/core';
import { Subscription } from 'rxjs';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { MushafWordAnalysisApi } from '../../mushaf/data-access/mushaf-word-analysis.api';
import { WordAnalysisDto } from '../../mushaf/models/mushaf.models';
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
  readonly onAnalysis: (response: ApiResponse<WordAnalysisDto>) => void;
  readonly onError: (err: unknown) => void;
}

export interface WordTypesDetailLoadContext {
  readonly identity: WordTypeRowIdentity;
  readonly view: WordTypeDetailView;
  readonly detailPage: number;
  readonly location: string | null;
}

@Injectable({ providedIn: 'root' })
export class WordTypesDetailViewLoader {
  private readonly api = inject(WordTypesApi);
  private readonly analysisApi = inject(MushafWordAnalysisApi);
  private readonly cache = inject(WordTypesCache);

  loadActiveView(
    context: WordTypesDetailLoadContext,
    handlers: WordTypesDetailViewHandlers,
  ): Subscription | undefined {
    switch (context.view) {
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
      case 'analysis':
        if (!context.location) {
          return undefined;
        }

        return this.subscribe(
          this.cache.getOrLoad(WordTypesCacheKeys.analysis(context.location), () =>
            this.analysisApi.getWordAnalysis(context.location!),
          ),
          handlers.onAnalysis,
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
