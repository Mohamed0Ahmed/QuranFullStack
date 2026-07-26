import { HttpErrorResponse } from '@angular/common/http';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { AbwabConflictError, isAbwabConflictCode } from './abwab-conflict';

export const ABWAB_UNEXPECTED_ERROR = 'حدث خطأ غير متوقع.';

export async function unwrapAbwabResponse<T>(request: Promise<ApiResponse<T>>): Promise<T> {
  let response: ApiResponse<T>;
  try {
    response = await request;
  } catch (error) {
    throw toAbwabDomainError(error);
  }
  if (!response.isSuccess) {
    throw new Error(response.message ?? ABWAB_UNEXPECTED_ERROR);
  }
  return response.data as T;
}

export function toAbwabDomainError(error: unknown): Error {
  if (error instanceof HttpErrorResponse && error.status === 409) {
    const code = readFirstErrorCode(error.error);
    if (code && isAbwabConflictCode(code)) {
      return new AbwabConflictError(code);
    }
  }
  if (error instanceof HttpErrorResponse) {
    const body = error.error as Partial<ApiResponse<unknown>> | undefined;
    return new Error(body?.message ?? ABWAB_UNEXPECTED_ERROR);
  }
  return error instanceof Error ? error : new Error(ABWAB_UNEXPECTED_ERROR);
}

function readFirstErrorCode(body: unknown): string | null {
  if (typeof body !== 'object' || body === null) {
    return null;
  }
  const errors = (body as Partial<ApiResponse<unknown>>).errors;
  return Array.isArray(errors) && typeof errors[0] === 'string' ? errors[0] : null;
}
