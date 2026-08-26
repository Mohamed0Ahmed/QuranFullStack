import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';

import { PhraseSimilarityGroupDto } from '../../../../core/api/generated/models/phrase-similarity-group-dto';
import { PhraseSimilarityMatchDto } from '../../../../core/api/generated/models/phrase-similarity-match-dto';
import { lastPageNumber } from '../../../../shared/ui/pagination/pagination-range';
import { PhraseSimilarityApi } from '../data-access/phrase-similarity.api';
import {
  PHRASE_SIMILARITY_PAGE_SIZE,
  PhraseSimilarityUrlState,
} from '../models/phrase-similarity.models';
import { PhraseRequestFailure, phraseEnvelopeFailure } from './phrase-request-failure';
import { minimumMatchedWords } from './phrase-similarity-threshold';

export interface PhraseSimilarityLoadSuccess {
  readonly kind: 'success';
  readonly activeBuildId: string;
  readonly groups: readonly PhraseSimilarityGroupDto[];
  readonly matches: readonly PhraseSimilarityMatchDto[];
  readonly totalCount: number;
  readonly lastPage: number;
}

export interface PhraseSimilarityLoadFailure {
  readonly kind: 'failure';
  readonly failure: PhraseRequestFailure;
}

export type PhraseSimilarityLoadResult =
  | PhraseSimilarityLoadSuccess
  | PhraseSimilarityLoadFailure;

@Injectable()
export class PhraseSimilarityResultsLoader {
  private readonly api = inject(PhraseSimilarityApi);

  loadManual(route: PhraseSimilarityUrlState): Observable<PhraseSimilarityLoadResult> {
    const minimumMatched = minimumMatchedWords(route.length, route.min);
    return this.api
      .search(route.resolution!, minimumMatched, route.page, PHRASE_SIMILARITY_PAGE_SIZE)
      .pipe(
        map((response) => {
          if (!response.isSuccess || !response.data) {
            return failureResult(response.errors, response.message);
          }
          if (
            response.data.mode !== route.mode ||
            response.data.wordCount !== route.length ||
            response.data.minimumMatchedWords !== minimumMatched
          ) {
            return invalidResult('نتائج العبارة لا تطابق هوية خيارات الرابط الحالية.');
          }
          return successResult(
            response.data.activeBuildId,
            [],
            response.data.items,
            response.data.totalCount,
          );
        }),
      );
  }

  loadGroups(route: PhraseSimilarityUrlState): Observable<PhraseSimilarityLoadResult> {
    return this.api
      .getGroups(
        route.mode,
        route.length,
        route.min,
        route.page,
        PHRASE_SIMILARITY_PAGE_SIZE,
      )
      .pipe(
        map((response) => {
          if (!response.isSuccess || !response.data) {
            return failureResult(response.errors, response.message);
          }
          if (
            response.data.mode !== route.mode ||
            response.data.wordCount !== route.length ||
            response.data.threshold !== route.min
          ) {
            return invalidResult('نتائج المجموعات لا تطابق خيارات الرابط الحالية.');
          }
          return successResult(
            response.data.activeBuildId,
            response.data.items,
            [],
            response.data.totalCount,
          );
        }),
      );
  }

  loadMatches(
    route: PhraseSimilarityUrlState,
    anchor: PhraseSimilarityGroupDto,
  ): Observable<PhraseSimilarityLoadResult> {
    return this.api
      .getMatches(
        route.build!,
        anchor.anchor.variantId,
        route.min,
        route.page,
        PHRASE_SIMILARITY_PAGE_SIZE,
      )
      .pipe(
        map((response) => {
          if (!response.isSuccess || !response.data) {
            return failureResult(response.errors, response.message);
          }
          if (
            response.data.anchor.variantId !== anchor.anchor.variantId ||
            response.data.threshold !== route.min
          ) {
            return invalidResult('تعذر استعادة المجموعة المحددة بهذه الخيارات.');
          }
          return successResult(
            response.data.activeBuildId,
            [],
            response.data.items,
            response.data.totalCount,
          );
        }),
      );
  }
}

function successResult(
  activeBuildId: string,
  groups: readonly PhraseSimilarityGroupDto[],
  matches: readonly PhraseSimilarityMatchDto[],
  totalCount: number,
): PhraseSimilarityLoadSuccess {
  return {
    kind: 'success',
    activeBuildId,
    groups,
    matches,
    totalCount,
    lastPage: lastPageNumber(PHRASE_SIMILARITY_PAGE_SIZE, totalCount),
  };
}

function failureResult(
  errors: readonly string[] | null,
  message: string | null,
): PhraseSimilarityLoadFailure {
  return { kind: 'failure', failure: phraseEnvelopeFailure(errors, message) };
}

function invalidResult(message: string): PhraseSimilarityLoadFailure {
  return { kind: 'failure', failure: { status: 'invalid', message } };
}
