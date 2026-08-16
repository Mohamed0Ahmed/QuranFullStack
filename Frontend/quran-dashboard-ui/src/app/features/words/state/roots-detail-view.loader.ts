import { Injectable, inject } from '@angular/core';
import { Observable, Subscription } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { RootsApi } from '../data-access/roots.api';
import {
  PagedResultDto,
  ROOT_DETAIL_PAGE_SIZE,
  RootAyahMatchDto,
  RootLemmasDto,
  RootMissingSurahsDto,
  RootStemsDto,
  RootSurahView,
  RootSurahsDto,
  RootView,
  RootWordItemDto,
  RootWordView,
} from '../models/roots.models';
import { RootsCache, RootsCacheKeys } from './roots-cache';

export interface RootsDetailViewHandlers {
  readonly onAyahs: (response: ApiResponse<PagedResultDto<RootAyahMatchDto>>) => void;
  readonly onWords: (response: ApiResponse<PagedResultDto<RootWordItemDto>>) => void;
  readonly onMentionedSurahs: (response: ApiResponse<RootSurahsDto>) => void;
  readonly onMissingSurahs: (response: ApiResponse<RootMissingSurahsDto>) => void;
  readonly onLemmas: (response: ApiResponse<RootLemmasDto>) => void;
  readonly onStems: (response: ApiResponse<RootStemsDto>) => void;
  readonly onError: (err: unknown) => void;
}

export interface RootsDetailLoadContext {
  readonly rootId: number;
  readonly view: RootView;
  readonly wordView: RootWordView;
  readonly surahView: RootSurahView;
  readonly ayahTypeCode: string | null;
  readonly detailPage: number;
  readonly cachedMissingSurahs: RootMissingSurahsDto | null;
}

@Injectable({ providedIn: 'root' })
export class RootsDetailViewLoader {
  private readonly api = inject(RootsApi);
  private readonly cache = inject(RootsCache);

  loadActiveView(context: RootsDetailLoadContext, handlers: RootsDetailViewHandlers): Subscription | undefined {
    switch (context.view) {
      case 'ayahs':
        return this.subscribe(
          this.cache.getOrLoad(RootsCacheKeys.ayahs(context.rootId, context.detailPage, context.ayahTypeCode), () =>
            this.api.getRootAyahMatches(
              context.rootId,
              context.detailPage,
              ROOT_DETAIL_PAGE_SIZE,
              context.ayahTypeCode,
            ),
          ),
          handlers.onAyahs,
          handlers.onError,
        );
      case 'words':
        return this.subscribe(
          this.cache.getOrLoad(
            RootsCacheKeys.words(context.rootId, context.wordView, context.detailPage),
            () =>
              this.api.getRootWords(
                context.rootId,
                context.wordView,
                context.detailPage,
                ROOT_DETAIL_PAGE_SIZE,
              ),
          ),
          handlers.onWords,
          handlers.onError,
        );
      case 'surahs':
        return context.surahView === 'missing'
          ? this.loadMissingSurahs(context.rootId, context.cachedMissingSurahs, handlers)
          : this.subscribe(
              this.cache.getOrLoad(RootsCacheKeys.surahs(context.rootId), () =>
                this.api.getRootMentionedSurahs(context.rootId),
              ),
              handlers.onMentionedSurahs,
              handlers.onError,
            );
      case 'lemmas':
        return this.subscribe(
          this.cache.getOrLoad(RootsCacheKeys.lemmas(context.rootId), () =>
            this.api.getRootLemmas(context.rootId),
          ),
          handlers.onLemmas,
          handlers.onError,
        );
      case 'stems':
        return this.subscribe(
          this.cache.getOrLoad(RootsCacheKeys.stems(context.rootId), () =>
            this.api.getRootStems(context.rootId),
          ),
          handlers.onStems,
          handlers.onError,
        );
    }
  }

  private loadMissingSurahs(
    rootId: number,
    cachedMissingSurahs: RootMissingSurahsDto | null,
    handlers: RootsDetailViewHandlers,
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
      this.cache.getOrLoad(RootsCacheKeys.missing(rootId), () => this.api.getRootMissingSurahs(rootId)),
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
