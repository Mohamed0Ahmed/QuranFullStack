import { HttpErrorResponse } from '@angular/common/http';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { STEMS_ERROR_LABEL } from '../models/stems.labels';
import {
  DEFAULT_STEM_DETAIL_PAGE,
  DEFAULT_STEM_SURAHS_VIEW,
  DEFAULT_STEM_VIEW,
  DEFAULT_STEM_WORD_VIEW,
  PagedResultDto,
  StemAyahMatchDto,
  StemLemmasDto,
  StemMissingSurahsDto,
  StemSurahsDto,
  StemWordItemDto,
  StemsPanelState,
} from '../models/stems.models';

export function buildAyahsPanelUpdate(
  response: ApiResponse<PagedResultDto<StemAyahMatchDto>>,
): Pick<StemsPanelState, 'ayahs' | 'detailPage' | 'status' | 'errorMessage'> {
  if (!response.isSuccess || !response.data) {
    return {
      ayahs: null,
      status: 'error',
      errorMessage: response.message ?? STEMS_ERROR_LABEL,
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
  response: ApiResponse<PagedResultDto<StemWordItemDto>>,
): Pick<StemsPanelState, 'words' | 'detailPage' | 'status' | 'errorMessage'> {
  if (!response.isSuccess || !response.data) {
    return {
      words: null,
      status: 'error',
      errorMessage: response.message ?? STEMS_ERROR_LABEL,
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
  response: ApiResponse<StemSurahsDto>,
): Pick<StemsPanelState, 'mentionedSurahs' | 'status' | 'errorMessage'> {
  if (!response.isSuccess || !response.data) {
    return {
      mentionedSurahs: null,
      status: 'error',
      errorMessage: response.message ?? STEMS_ERROR_LABEL,
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
  response: ApiResponse<StemMissingSurahsDto>,
): Pick<StemsPanelState, 'missingSurahs' | 'status' | 'errorMessage'> {
  if (!response.isSuccess || !response.data) {
    return {
      missingSurahs: null,
      status: 'error',
      errorMessage: response.message ?? STEMS_ERROR_LABEL,
    };
  }

  const data = response.data;
  return {
    missingSurahs: data,
    status: data.surahs.length === 0 ? 'empty' : 'success',
    errorMessage: '',
  };
}

export function buildLemmasPanelUpdate(
  response: ApiResponse<StemLemmasDto>,
): Pick<StemsPanelState, 'lemmas' | 'status' | 'errorMessage'> {
  if (!response.isSuccess || !response.data) {
    return {
      lemmas: null,
      status: 'error',
      errorMessage: response.message ?? STEMS_ERROR_LABEL,
    };
  }

  const data = response.data;
  return {
    lemmas: data,
    status: data.lemmas.length === 0 ? 'empty' : 'success',
    errorMessage: '',
  };
}

export function buildDetailErrorUpdate(
  err: unknown,
  fallback: string,
): Pick<StemsPanelState, 'status' | 'errorMessage'> {
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

export function restoredStemNotFoundUpdate(
  message: string,
  notFoundLabel: string,
  stemId: number | null,
): StemsPanelState {
  return {
    selectedStemId: stemId,
    summary: null,
    view: DEFAULT_STEM_VIEW,
    wordView: DEFAULT_STEM_WORD_VIEW,
    surahView: DEFAULT_STEM_SURAHS_VIEW,
    detailPage: DEFAULT_STEM_DETAIL_PAGE,
    ayahs: null,
    words: null,
    mentionedSurahs: null,
    missingSurahs: null,
    lemmas: null,
    status: 'notFound',
    errorMessage: message || notFoundLabel,
  };
}
