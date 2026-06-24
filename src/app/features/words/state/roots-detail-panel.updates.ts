import { HttpErrorResponse } from '@angular/common/http';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { ROOTS_ERROR_LABEL } from '../models/roots.labels';
import {
  DEFAULT_ROOT_DETAIL_PAGE,
  DEFAULT_ROOT_SURAHS_VIEW,
  DEFAULT_ROOT_VIEW,
  DEFAULT_ROOT_WORD_VIEW,
  PagedResultDto,
  RootAyahMatchDto,
  RootLemmasDto,
  RootMissingSurahsDto,
  RootStemsDto,
  RootSurahsDto,
  RootWordItemDto,
  RootsPanelState,
} from '../models/roots.models';

export function buildAyahsPanelUpdate(
  response: ApiResponse<PagedResultDto<RootAyahMatchDto>>,
): Pick<RootsPanelState, 'ayahs' | 'detailPage' | 'status' | 'errorMessage'> {
  if (!response.isSuccess || !response.data) {
    return {
      ayahs: null,
      status: 'error',
      errorMessage: response.message ?? ROOTS_ERROR_LABEL,
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
  response: ApiResponse<PagedResultDto<RootWordItemDto>>,
): Pick<RootsPanelState, 'words' | 'detailPage' | 'status' | 'errorMessage'> {
  if (!response.isSuccess || !response.data) {
    return {
      words: null,
      status: 'error',
      errorMessage: response.message ?? ROOTS_ERROR_LABEL,
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
  response: ApiResponse<RootSurahsDto>,
): Pick<RootsPanelState, 'mentionedSurahs' | 'status' | 'errorMessage'> {
  if (!response.isSuccess || !response.data) {
    return {
      mentionedSurahs: null,
      status: 'error',
      errorMessage: response.message ?? ROOTS_ERROR_LABEL,
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
  response: ApiResponse<RootMissingSurahsDto>,
): Pick<RootsPanelState, 'missingSurahs' | 'status' | 'errorMessage'> {
  if (!response.isSuccess || !response.data) {
    return {
      missingSurahs: null,
      status: 'error',
      errorMessage: response.message ?? ROOTS_ERROR_LABEL,
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
  response: ApiResponse<RootLemmasDto>,
): Pick<RootsPanelState, 'lemmas' | 'status' | 'errorMessage'> {
  if (!response.isSuccess || !response.data) {
    return {
      lemmas: null,
      status: 'error',
      errorMessage: response.message ?? ROOTS_ERROR_LABEL,
    };
  }

  const data = response.data;
  return {
    lemmas: data,
    status: data.lemmas.length === 0 ? 'empty' : 'success',
    errorMessage: '',
  };
}

export function buildStemsPanelUpdate(
  response: ApiResponse<RootStemsDto>,
): Pick<RootsPanelState, 'stems' | 'status' | 'errorMessage'> {
  if (!response.isSuccess || !response.data) {
    return {
      stems: null,
      status: 'error',
      errorMessage: response.message ?? ROOTS_ERROR_LABEL,
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
): Pick<RootsPanelState, 'status' | 'errorMessage'> {
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

export function restoredRootNotFoundUpdate(
  message: string,
  notFoundLabel: string,
  rootId: number | null,
): RootsPanelState {
  return {
    selectedRootId: rootId,
    summary: null,
    view: DEFAULT_ROOT_VIEW,
    wordView: DEFAULT_ROOT_WORD_VIEW,
    surahView: DEFAULT_ROOT_SURAHS_VIEW,
    detailPage: DEFAULT_ROOT_DETAIL_PAGE,
    ayahs: null,
    words: null,
    mentionedSurahs: null,
    missingSurahs: null,
    lemmas: null,
    stems: null,
    status: 'notFound',
    errorMessage: message || notFoundLabel,
  };
}
