import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';

import { ApiResponse } from '../../../../core/data-access/api-response.model';
import { PhraseLoadStatus } from '../models/phrase-repetitions.models';

export interface PhraseRequestFailure {
  readonly status: Extract<
    PhraseLoadStatus,
    'invalid' | 'error' | 'rate-limited' | 'stale' | 'unavailable'
  >;
  readonly message: string;
}

const DEFAULT_ERROR_MESSAGE = 'تعذر تحميل بيانات البحث الآن. حاول مرة أخرى.';
const RATE_LIMIT_FALLBACK_MESSAGE = 'عدد كبير من الطلبات. حاول مرة أخرى بعد قليل.';

export function phraseRequestFailure(error: unknown): PhraseRequestFailure {
  if (!(error instanceof HttpErrorResponse)) {
    return { status: 'error', message: DEFAULT_ERROR_MESSAGE };
  }

  const body = isApiResponse(error.error) ? error.error : null;
  const codes = body?.errors ?? [];
  const message = body?.message || DEFAULT_ERROR_MESSAGE;

  if (error.status === HttpStatusCode.TooManyRequests) {
    const retryAfterSeconds = parseRetryAfterSeconds(error.headers.get('Retry-After'));
    return {
      status: 'rate-limited',
      message:
        retryAfterSeconds === null
          ? body?.message || RATE_LIMIT_FALLBACK_MESSAGE
          : `عدد كبير من الطلبات. حاول مرة أخرى بعد ${retryAfterSeconds} ثانية.`,
    };
  }
  if (
    error.status === HttpStatusCode.ServiceUnavailable ||
    codes.includes('phrase_index_unavailable')
  ) {
    return { status: 'unavailable', message };
  }
  if (error.status === HttpStatusCode.Conflict || codes.includes('phrase_index_changed')) {
    return { status: 'stale', message };
  }
  if (
    error.status === HttpStatusCode.BadRequest ||
    error.status === HttpStatusCode.NotFound
  ) {
    return { status: 'invalid', message };
  }
  return { status: 'error', message };
}

export function phraseEnvelopeFailure(
  errors: readonly string[] | null,
  message: string | null,
): PhraseRequestFailure {
  const codes = errors ?? [];
  if (codes.includes('phrase_index_unavailable')) {
    return {
      status: 'unavailable',
      message: message || 'فهرس البحث غير متاح الآن. أعد المحاولة بعد اكتمال بنائه.',
    };
  }
  if (codes.includes('phrase_index_changed')) {
    return { status: 'stale', message: message || 'تغير فهرس البحث، أعد اختيار النتيجة' };
  }
  if (codes.some((code) => code.endsWith('_invalid'))) {
    return { status: 'invalid', message: message || 'معطيات البحث غير صالحة.' };
  }
  return { status: 'error', message: message || DEFAULT_ERROR_MESSAGE };
}

function isApiResponse(value: unknown): value is ApiResponse<unknown> {
  return typeof value === 'object' && value !== null && 'isSuccess' in value;
}

function parseRetryAfterSeconds(value: string | null): number | null {
  const normalized = value?.trim();
  if (!normalized || !/^\d+$/.test(normalized)) {
    return null;
  }

  const seconds = Number(normalized);
  return Number.isSafeInteger(seconds) && seconds > 0 ? seconds : null;
}
