import { HttpErrorResponse } from '@angular/common/http';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { LEMMAS_ERROR_LABEL } from '../models/lemmas.labels';
import {
  DEFAULT_LEMMA_DETAIL_PAGE,
  DEFAULT_LEMMA_SURAHS_VIEW,
  DEFAULT_LEMMA_VIEW,
  DEFAULT_LEMMA_WORD_VIEW,
  LemmaAyahMatchDto,
  LemmaMissingSurahsDto,
  LemmaStemsDto,
  LemmaSurahsDto,
  LemmaWordItemDto,
  LemmasPanelState,
  PagedResultDto,
} from '../models/lemmas.models';

export function buildAyahsPanelUpdate(
  response: ApiResponse<PagedResultDto<LemmaAyahMatchDto>>,
): Pick<LemmasPanelState, 'ayahs' | 'detailPage' | 'status' | 'errorMessage'> {
  if (!response.isSuccess || !response.data) {
    return {
      ayahs: null,
      status: 'error',
      errorMessage: response.message ?? LEMMAS_ERROR_LABEL,
      detailPage: 1,
    };
  }

  const data = response.data;
  return {
    ayahs: data,
    detailPage: data.page,
    status: data.totalCount === 0 ? 'empty' : 'success',
    errorMessage: '',
  };
}

export function buildWordsPanelUpdate(
  response: ApiResponse<PagedResultDto<LemmaWordItemDto>>,
): Pick<LemmasPanelState, 'words' | 'detailPage' | 'status' | 'errorMessage'> {
  if (!response.isSuccess || !response.data) {
    return {
      words: null,
      status: 'error',
      errorMessage: response.message ?? LEMMAS_ERROR_LABEL,
      detailPage: 1,
    };
  }

  const data = response.data;
  return {
    words: data,
    detailPage: data.page,
    status: data.totalCount === 0 ? 'empty' : 'success',
    errorMessage: '',
  };
}

export function buildMentionedSurahsPanelUpdate(
  response: ApiResponse<LemmaSurahsDto>,
): Pick<LemmasPanelState, 'mentionedSurahs' | 'status' | 'errorMessage'> {
  if (!response.isSuccess || !response.data) {
    return {
      mentionedSurahs: null,
      status: 'error',
      errorMessage: response.message ?? LEMMAS_ERROR_LABEL,
    };
  }

  const data = response.data;
  return {
    mentionedSurahs: data,
    status: data.surahs.length === 0 ? 'empty' : 'success',
    errorMessage: '',
  };
}

export function buildMissingSurahsPanelUpdate(
  response: ApiResponse<LemmaMissingSurahsDto>,
): Pick<LemmasPanelState, 'missingSurahs' | 'status' | 'errorMessage'> {
  if (!response.isSuccess || !response.data) {
    return {
      missingSurahs: null,
      status: 'error',
      errorMessage: response.message ?? LEMMAS_ERROR_LABEL,
    };
  }

  const data = response.data;
  return {
    missingSurahs: data,
    status: data.surahs.length === 0 ? 'empty' : 'success',
    errorMessage: '',
  };
}

export function buildStemsPanelUpdate(
  response: ApiResponse<LemmaStemsDto>,
): Pick<LemmasPanelState, 'stems' | 'status' | 'errorMessage'> {
  if (!response.isSuccess || !response.data) {
    return {
      stems: null,
      status: 'error',
      errorMessage: response.message ?? LEMMAS_ERROR_LABEL,
    };
  }

  const data = response.data;
  return {
    stems: data,
    status: data.stems.length === 0 ? 'empty' : 'success',
    errorMessage: '',
  };
}

export function buildDetailErrorUpdate(
  err: unknown,
  fallback: string,
): Pick<LemmasPanelState, 'status' | 'errorMessage'> {
  return {
    status: 'error',
    errorMessage: extractPanelErrorMessage(err, fallback),
  };
}

export function extractPanelErrorMessage(err: unknown, fallback: string): string {
  if (err instanceof HttpErrorResponse) {
    const body = err.error as ApiResponse<unknown> | null | undefined;
    return typeof body?.message === 'string' && body.message.length > 0 ? body.message : fallback;
  }

  return fallback;
}

export function restoredLemmaNotFoundUpdate(
  message: string,
  notFoundLabel: string,
  lemmaId: number | null,
): LemmasPanelState {
  return {
    selectedLemmaId: lemmaId,
    summary: null,
    view: DEFAULT_LEMMA_VIEW,
    wordView: DEFAULT_LEMMA_WORD_VIEW,
    surahView: DEFAULT_LEMMA_SURAHS_VIEW,
    ayahTypeCode: null,
    detailPage: DEFAULT_LEMMA_DETAIL_PAGE,
    ayahs: null,
    words: null,
    mentionedSurahs: null,
    missingSurahs: null,
    stems: null,
    status: 'notFound',
    errorMessage: message || notFoundLabel,
  };
}
