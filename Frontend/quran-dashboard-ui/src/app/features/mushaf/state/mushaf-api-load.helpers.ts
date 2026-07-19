import { HttpErrorResponse } from '@angular/common/http';
import { Observable, Subscription } from 'rxjs';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { ResourceLoadState } from '../models/mushaf.models';

export function mapApiFailureToLoadState(
  err: unknown,
  options: { notFoundMessage: string; connectionMessage: string },
): ResourceLoadState {
  if (err instanceof HttpErrorResponse) {
    const body = err.error as ApiResponse<unknown> | null | undefined;
    const backendMessage =
      typeof body?.message === 'string' && body.message.length > 0 ? body.message : null;

    // Only a genuine "not found" (404) or malformed-key (400) response means the
    // Quran content itself is absent. Every other 4xx — 401/403 (auth), 429 (rate
    // limit), etc. — is a client/auth failure, not a missing-content signal, and
    // must never be shown to the user as "does not exist".
    if (err.status === 404 || err.status === 400) {
      return {
        isLoading: false,
        isEmpty: true,
        errorMessage: backendMessage ?? options.notFoundMessage,
      };
    }

    return {
      isLoading: false,
      isEmpty: false,
      errorMessage: backendMessage ?? options.connectionMessage,
    };
  }

  return {
    isLoading: false,
    isEmpty: false,
    errorMessage: options.connectionMessage,
  };
}

export function mapApiResponseToLoadState<T>(
  response: ApiResponse<T>,
  options: { emptyMessage: string },
  onSuccess: (data: T) => void,
): ResourceLoadState {
  if (response.isSuccess && response.data != null) {
    onSuccess(response.data);
    return { isLoading: false, isEmpty: false, errorMessage: null };
  }

  return {
    isLoading: false,
    isEmpty: true,
    errorMessage: response.message ?? options.emptyMessage,
  };
}

export function subscribeToApiLoad<T>(
  request$: Observable<ApiResponse<T>>,
  handlers: {
    onSuccess: (data: T) => void;
    onSettled: (loadState: ResourceLoadState) => void;
    emptyMessage: string;
    notFoundMessage: string;
    connectionMessage: string;
  },
): Subscription {
  return request$.subscribe({
    next: (response) => {
      handlers.onSettled(
        mapApiResponseToLoadState(response, { emptyMessage: handlers.emptyMessage }, handlers.onSuccess),
      );
    },
    error: (err) => {
      handlers.onSettled(
        mapApiFailureToLoadState(err, {
          notFoundMessage: handlers.notFoundMessage,
          connectionMessage: handlers.connectionMessage,
        }),
      );
    },
  });
}
