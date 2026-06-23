import { HttpErrorResponse } from '@angular/common/http';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import {
  DRILLDOWN_ERROR_LABEL,
  RESTORED_WORD_LOAD_ERROR_LABEL,
  RESTORED_WORD_NOT_FOUND_LABEL,
} from '../models/unique-words.labels';
import {
  PagedResultDto,
  UniqueWordAyahMatchDto,
  UniqueWordMissingSurahsDto,
  UniqueWordSurahsDto,
  WordDrilldownState,
} from '../models/unique-words.models';

export function buildSurahsDrilldownUpdate(
  response: ApiResponse<UniqueWordSurahsDto>,
): Partial<WordDrilldownState> {
  if (!response.isSuccess || !response.data) {
    return { status: 'error', errorMessage: response.message ?? DRILLDOWN_ERROR_LABEL };
  }

  return {
    surahs: response.data,
    status: response.data.surahs.length === 0 ? 'empty' : 'success',
    errorMessage: '',
  };
}

export function buildMissingSurahsDrilldownUpdate(
  response: ApiResponse<UniqueWordMissingSurahsDto>,
): Partial<WordDrilldownState> {
  if (!response.isSuccess || !response.data) {
    return { status: 'error', errorMessage: response.message ?? DRILLDOWN_ERROR_LABEL };
  }

  return {
    missingSurahs: response.data,
    status: response.data.surahs.length === 0 ? 'empty' : 'success',
    errorMessage: '',
  };
}

export function buildAyahsDrilldownUpdate(
  response: ApiResponse<PagedResultDto<UniqueWordAyahMatchDto>>,
): Partial<WordDrilldownState> {
  if (!response.isSuccess || !response.data) {
    return { status: 'error', errorMessage: response.message ?? DRILLDOWN_ERROR_LABEL };
  }

  return {
    ayahs: response.data,
    ayahPage: response.data.page,
    status: response.data.totalCount === 0 ? 'empty' : 'success',
    errorMessage: '',
  };
}

export function buildDrilldownErrorUpdate(err: unknown, fallback: string): Partial<WordDrilldownState> {
  return { status: 'error', errorMessage: extractDrilldownMessage(err, fallback) };
}

export function extractDrilldownMessage(err: unknown, fallback: string): string {
  if (err instanceof HttpErrorResponse) {
    const body = err.error as ApiResponse<unknown> | null | undefined;
    return typeof body?.message === 'string' && body.message.length > 0 ? body.message : fallback;
  }

  return fallback;
}

export function buildRestoredWordLoadError(message: string): Partial<WordDrilldownState> {
  return {
    isOpen: false,
    status: 'error',
    errorMessage: message || RESTORED_WORD_LOAD_ERROR_LABEL,
  };
}

export function buildRestoredWordNotFound(message: string): Partial<WordDrilldownState> {
  return {
    isOpen: false,
    status: 'notFound',
    errorMessage: message || RESTORED_WORD_NOT_FOUND_LABEL,
  };
}
