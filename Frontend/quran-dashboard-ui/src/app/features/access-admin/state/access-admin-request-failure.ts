import { HttpErrorResponse } from '@angular/common/http';

import { ApiResponse } from '../../../core/data-access/api-response.model';

export const ACCESS_ADMIN_LOAD_ERROR = 'تعذر تحميل بيانات إدارة الوصول.';

export function failureMessage(error: unknown, fallback: string): string {
  if (!(error instanceof HttpErrorResponse) || typeof error.error !== 'object' || error.error === null) {
    return fallback;
  }
  const response = error.error as Partial<ApiResponse<unknown>>;
  return typeof response.message === 'string' && response.message.trim() ? response.message : fallback;
}
