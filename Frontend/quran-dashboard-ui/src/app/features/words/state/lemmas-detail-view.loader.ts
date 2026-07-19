import { Injectable, inject } from '@angular/core';
import { Observable, Subscription } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { LemmasApi } from '../data-access/lemmas.api';
import {
  LEMMA_DETAIL_PAGE_SIZE,
  LemmaAyahMatchDto,
  LemmaMissingSurahsDto,
  LemmaStemsDto,
  LemmaSurahView,
  LemmaSurahsDto,
  LemmaView,
  LemmaWordItemDto,
  LemmaWordView,
  PagedResultDto,
} from '../models/lemmas.models';
import { LemmasCache, LemmasCacheKeys } from './lemmas-cache';

export interface LemmasDetailViewHandlers {
  readonly onAyahs: (response: ApiResponse<PagedResultDto<LemmaAyahMatchDto>>) => void;
  readonly onWords: (response: ApiResponse<PagedResultDto<LemmaWordItemDto>>) => void;
  readonly onMentionedSurahs: (response: ApiResponse<LemmaSurahsDto>) => void;
  readonly onMissingSurahs: (response: ApiResponse<LemmaMissingSurahsDto>) => void;
  readonly onStems: (response: ApiResponse<LemmaStemsDto>) => void;
  readonly onError: (err: unknown) => void;
}

export interface LemmasDetailLoadContext {
  readonly lemmaId: number;
  readonly view: LemmaView;
  readonly wordView: LemmaWordView;
  readonly surahView: LemmaSurahView;
  readonly ayahTypeCode: string | null;
  readonly detailPage: number;
  readonly cachedMissingSurahs: LemmaMissingSurahsDto | null;
}

@Injectable({ providedIn: 'root' })
export class LemmasDetailViewLoader {
  private readonly api = inject(LemmasApi);
  private readonly cache = inject(LemmasCache);

  loadActiveView(
    context: LemmasDetailLoadContext,
    handlers: LemmasDetailViewHandlers,
  ): Subscription | undefined {
    switch (context.view) {
      case 'ayahs':
        return this.subscribe(
          this.cache.getOrLoad(
            LemmasCacheKeys.ayahs(
              context.lemmaId,
              context.detailPage,
              LEMMA_DETAIL_PAGE_SIZE,
              context.ayahTypeCode,
            ),
            () =>
              this.api.getLemmaAyahMatches(
                context.lemmaId,
                context.detailPage,
                LEMMA_DETAIL_PAGE_SIZE,
                context.ayahTypeCode,
              ),
          ),
          handlers.onAyahs,
          handlers.onError,
        );
      case 'words':
        return this.subscribe(
          this.cache.getOrLoad(
            LemmasCacheKeys.words(context.lemmaId, context.wordView, context.detailPage),
            () =>
              this.api.getLemmaWords(
                context.lemmaId,
                context.wordView,
                context.detailPage,
                LEMMA_DETAIL_PAGE_SIZE,
              ),
          ),
          handlers.onWords,
          handlers.onError,
        );
      case 'surahs':
        return context.surahView === 'missing'
          ? this.loadMissingSurahs(context.lemmaId, context.cachedMissingSurahs, handlers)
          : this.subscribe(
              this.cache.getOrLoad(LemmasCacheKeys.surahs(context.lemmaId), () =>
                this.api.getLemmaMentionedSurahs(context.lemmaId),
              ),
              handlers.onMentionedSurahs,
              handlers.onError,
            );
      case 'stems':
        return this.subscribe(
          this.cache.getOrLoad(LemmasCacheKeys.stems(context.lemmaId), () =>
            this.api.getLemmaStems(context.lemmaId),
          ),
          handlers.onStems,
          handlers.onError,
        );
    }
  }

  private loadMissingSurahs(
    lemmaId: number,
    cachedMissingSurahs: LemmaMissingSurahsDto | null,
    handlers: LemmasDetailViewHandlers,
  ): Subscription | undefined {
    if (cachedMissingSurahs !== null) {
      handlers.onMissingSurahs({
        isSuccess: true,
        data: cachedMissingSurahs,
        message: null,
        errors: null,
      });
      return undefined;
    }

    return this.subscribe(
      this.cache.getOrLoad(LemmasCacheKeys.missing(lemmaId), () =>
        this.api.getLemmaMissingSurahs(lemmaId),
      ),
      handlers.onMissingSurahs,
      handlers.onError,
    );
  }

  private subscribe<T>(
    request$: Observable<ApiResponse<T>>,
    onSuccess: (response: ApiResponse<T>) => void,
    onError: (err: unknown) => void,
  ): Subscription {
    return request$
      .pipe(
        tap((response) => onSuccess(response)),
        catchError((err) => {
          onError(err);
          return [];
        }),
      )
      .subscribe();
  }
}
