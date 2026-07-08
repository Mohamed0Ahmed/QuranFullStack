import { HttpErrorResponse } from '@angular/common/http';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { WORD_TYPES_ERROR_LABEL, WORD_TYPES_NOT_FOUND_LABEL } from '../models/word-types.labels';
import {
  PagedResultDto,
  WordTypeAyahMatchDto,
  WordTypeDetailView,
  WordTypeSummaryDto,
  WordTypeSurahsResponseDto,
  WordTypesDetailState,
  WordTypesLoadStatus,
} from '../models/word-types.models';

export function buildSummaryPanelUpdate(
  response: ApiResponse<WordTypeSummaryDto>,
): Partial<WordTypesDetailState> {
  if (!response.isSuccess || !response.data) {
    return {
      status: 'notFound',
      summary: null,
      errorMessage: response.message ?? WORD_TYPES_NOT_FOUND_LABEL,
    };
  }

  return {
    summary: response.data,
    status: 'loading',
    errorMessage: '',
  };
}

export function buildAyahsPanelUpdate(
  response: ApiResponse<PagedResultDto<WordTypeAyahMatchDto>>,
): Partial<WordTypesDetailState> {
  if (!response.isSuccess || !response.data) {
    return buildDetailFailureUpdate(response.message ?? WORD_TYPES_ERROR_LABEL);
  }

  const page = response.data;
  return {
    ayahs: page,
    status: page.totalCount === 0 ? 'empty' : 'success',
    errorMessage: '',
  };
}

export function buildSurahsPanelUpdate(
  response: ApiResponse<WordTypeSurahsResponseDto>,
): Partial<WordTypesDetailState> {
  if (!response.isSuccess || !response.data) {
    return buildDetailFailureUpdate(response.message ?? WORD_TYPES_ERROR_LABEL);
  }

  const payload = response.data;
  return {
    surahs: payload,
    status: payload.surahs.length === 0 && payload.missingSurahs.length === 0 ? 'empty' : 'success',
    errorMessage: '',
  };
}

export function buildDetailErrorUpdate(err: unknown, fallback: string): Partial<WordTypesDetailState> {
  return buildDetailFailureUpdate(extractPanelErrorMessage(err, fallback));
}

export function restoredRowNotFoundUpdate(
  message: string,
  fallback: string,
  selectedRow: WordTypesDetailState['selectedRow'],
): WordTypesDetailState {
  return {
    status: 'notFound',
    selectedRow,
    view: 'ayahs',
    summary: null,
    ayahs: null,
    surahs: null,
    detailPage: 1,
    location: null,
    errorMessage: message || fallback,
  };
}

function buildDetailFailureUpdate(message: string): Partial<WordTypesDetailState> {
  return {
    status: 'error',
    errorMessage: message || WORD_TYPES_ERROR_LABEL,
  };
}

export function extractPanelErrorMessage(err: unknown, fallback: string): string {
  if (err instanceof HttpErrorResponse) {
    const body = err.error as ApiResponse<unknown> | null;
    if (body?.message) {
      return body.message;
    }
  }

  return fallback;
}

export function isPaginatedWordTypeView(view: WordTypeDetailView): boolean {
  return view === 'ayahs';
}
