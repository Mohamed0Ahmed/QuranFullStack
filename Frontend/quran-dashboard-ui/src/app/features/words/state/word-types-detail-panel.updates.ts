import { HttpErrorResponse } from '@angular/common/http';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { WORD_TYPES_ERROR_LABEL } from '../models/word-types.labels';
import { WordTypeGroupedMemberWordDto, WordTypesDetailState } from '../models/word-types-detail.models';
import {
  PagedResultDto,
  WordTypeAyahMatchDto,
  WordTypeDetailView,
  WordTypeSurahsResponseDto,
} from '../models/word-types.models';

export function buildWordsPanelUpdate(
  response: ApiResponse<PagedResultDto<WordTypeGroupedMemberWordDto>>,
): Partial<WordTypesDetailState> {
  if (!response.isSuccess || !response.data) {
    return buildDetailFailureUpdate(response.message ?? WORD_TYPES_ERROR_LABEL);
  }

  const page = response.data;
  return {
    words: page,
    status: page.totalCount === 0 ? 'empty' : 'success',
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
  return view === 'words' || view === 'ayahs';
}
