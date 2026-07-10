import { Injectable, inject } from '@angular/core';
import { Observable, Subscription } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { StemsApi } from '../data-access/stems.api';
import {
  PagedResultDto,
  STEM_DETAIL_PAGE_SIZE,
  StemAyahMatchDto,
  StemLemmasDto,
  StemMissingSurahsDto,
  StemSurahView,
  StemSurahsDto,
  StemView,
  StemWordItemDto,
  StemWordView,
} from '../models/stems.models';
import { StemsCache, StemsCacheKeys } from './stems-cache';

export interface StemsDetailViewHandlers {
  readonly onAyahs: (response: ApiResponse<PagedResultDto<StemAyahMatchDto>>) => void;
  readonly onWords: (response: ApiResponse<PagedResultDto<StemWordItemDto>>) => void;
  readonly onMentionedSurahs: (response: ApiResponse<StemSurahsDto>) => void;
  readonly onMissingSurahs: (response: ApiResponse<StemMissingSurahsDto>) => void;
  readonly onLemmas: (response: ApiResponse<StemLemmasDto>) => void;
  readonly onError: (err: unknown) => void;
}

export interface StemsDetailLoadContext {
  readonly stemId: number;
  readonly view: StemView;
  readonly wordView: StemWordView;
  readonly surahView: StemSurahView;
  readonly ayahTypeCode: string | null;
  readonly detailPage: number;
  readonly cachedMissingSurahs: StemMissingSurahsDto | null;
}

/**
 * Stems Explorer detail view loader (Feature 016). Sibling of
 * `RootsDetailViewLoader`. Active-view loading is wired but is only invoked from
 * the story phases once the stem catalogue and summary reads exist (US2+).
 * Phase 2 ships the typed skeleton so the facade can compile.
 */
@Injectable({ providedIn: 'root' })
export class StemsDetailViewLoader {
  private readonly api = inject(StemsApi);
  private readonly cache = inject(StemsCache);

  loadActiveView(
    context: StemsDetailLoadContext,
    handlers: StemsDetailViewHandlers,
  ): Subscription | undefined {
    switch (context.view) {
      case 'ayahs':
        return this.subscribe(
          this.cache.getOrLoad(
            StemsCacheKeys.ayahs(
              context.stemId,
              context.detailPage,
              STEM_DETAIL_PAGE_SIZE,
              context.ayahTypeCode,
            ),
            () =>
              this.api.getStemAyahMatches(
                context.stemId,
                context.detailPage,
                STEM_DETAIL_PAGE_SIZE,
                context.ayahTypeCode,
              ),
          ),
          handlers.onAyahs,
          handlers.onError,
        );
      case 'words':
        return this.subscribe(
          this.cache.getOrLoad(
            StemsCacheKeys.words(context.stemId, context.wordView, context.detailPage),
            () =>
              this.api.getStemWords(
                context.stemId,
                context.wordView,
                context.detailPage,
                STEM_DETAIL_PAGE_SIZE,
              ),
          ),
          handlers.onWords,
          handlers.onError,
        );
      case 'surahs':
        return context.surahView === 'missing'
          ? this.loadMissingSurahs(context.stemId, context.cachedMissingSurahs, handlers)
          : this.subscribe(
              this.cache.getOrLoad(StemsCacheKeys.surahs(context.stemId), () =>
                this.api.getStemMentionedSurahs(context.stemId),
              ),
              handlers.onMentionedSurahs,
              handlers.onError,
            );
      case 'lemmas':
        return this.subscribe(
          this.cache.getOrLoad(StemsCacheKeys.lemmas(context.stemId), () =>
            this.api.getStemLemmas(context.stemId),
          ),
          handlers.onLemmas,
          handlers.onError,
        );
    }
  }

  private loadMissingSurahs(
    stemId: number,
    cachedMissingSurahs: StemMissingSurahsDto | null,
    handlers: StemsDetailViewHandlers,
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
      this.cache.getOrLoad(StemsCacheKeys.missing(stemId), () =>
        this.api.getStemMissingSurahs(stemId),
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
