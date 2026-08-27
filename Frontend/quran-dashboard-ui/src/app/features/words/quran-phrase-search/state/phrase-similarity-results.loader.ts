import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';

import { PhraseSimilarityAyahDto } from '../../../../core/api/generated/models/phrase-similarity-ayah-dto';
import { PhraseSimilarityPhraseDto } from '../../../../core/api/generated/models/phrase-similarity-phrase-dto';
import { lastPageNumber } from '../../../../shared/ui/pagination/pagination-range';
import { PhraseSimilarityApi } from '../data-access/phrase-similarity.api';
import {
  PHRASE_SIMILARITY_AYAH_PAGE_SIZE,
  PhraseSimilarityUrlState,
} from '../models/phrase-similarity.models';
import { PhraseRequestFailure, phraseEnvelopeFailure } from './phrase-request-failure';
import { minimumMatchedWords } from './phrase-similarity-threshold';

export interface PhraseSimilarityLoadSuccess {
  readonly kind: 'success';
  readonly activeBuildId: string;
  readonly ayahs: readonly PhraseSimilarityAyahDto[];
  readonly totalAyahCount: number;
  readonly totalOccurrenceCount: number;
  readonly lastPage: number;
  readonly queryPhrase: PhraseSimilarityPhraseDto;
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

  load(route: PhraseSimilarityUrlState): Observable<PhraseSimilarityLoadResult> {
    const minimumMatched = minimumMatchedWords(route.length, route.min);
    return this.api
      .search(
        route.resolution!,
        minimumMatched,
        route.page,
        PHRASE_SIMILARITY_AYAH_PAGE_SIZE,
      )
      .pipe(
        map((response) => {
          if (!response.isSuccess || !response.data) {
            return failureResult(response.errors, response.message);
          }
          if (
            response.data.mode !== route.mode ||
            response.data.wordCount !== route.length ||
            response.data.minimumMatchedWords !== minimumMatched ||
            response.data.pageSize !== PHRASE_SIMILARITY_AYAH_PAGE_SIZE
          ) {
            return invalidResult('نتائج العبارة لا تطابق هوية خيارات الرابط الحالية.');
          }
          return {
            kind: 'success',
            activeBuildId: response.data.activeBuildId,
            ayahs: response.data.items,
            totalAyahCount: response.data.totalAyahCount,
            totalOccurrenceCount: response.data.totalOccurrenceCount,
            lastPage: lastPageNumber(
              PHRASE_SIMILARITY_AYAH_PAGE_SIZE,
              response.data.totalAyahCount,
            ),
            queryPhrase: response.data.query,
          };
        }),
      );
  }
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
