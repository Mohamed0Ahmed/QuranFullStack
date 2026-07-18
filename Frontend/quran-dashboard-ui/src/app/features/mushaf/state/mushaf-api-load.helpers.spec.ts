import { HttpErrorResponse } from '@angular/common/http';
import { of, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import {
  mapApiFailureToLoadState,
  mapApiResponseToLoadState,
  subscribeToApiLoad,
} from './mushaf-api-load.helpers';

const notFoundMessage = 'الآية غير موجودة.';
const connectionMessage = 'تعذّر الاتصال بالخادم.';
const emptyMessage = 'تعذّر تحميل دراسة الآية.';

describe('mapApiFailureToLoadState', () => {
  it('maps a 404 to isEmpty:true with the backend not-found message', () => {
    const err = new HttpErrorResponse({
      status: 404,
      error: { isSuccess: false, message: 'المورد غير موجود', errors: [] },
    });

    expect(mapApiFailureToLoadState(err, { notFoundMessage, connectionMessage })).toEqual({
      isLoading: false,
      isEmpty: true,
      errorMessage: 'المورد غير موجود',
    });
  });

  it('maps a 404 with no backend message to isEmpty:true with the fallback not-found message', () => {
    const err = new HttpErrorResponse({ status: 404, error: null });

    expect(mapApiFailureToLoadState(err, { notFoundMessage, connectionMessage })).toEqual({
      isLoading: false,
      isEmpty: true,
      errorMessage: notFoundMessage,
    });
  });

  it('maps a 400 (malformed key) to isEmpty:true with the not-found message', () => {
    const err = new HttpErrorResponse({ status: 400, error: null });

    expect(mapApiFailureToLoadState(err, { notFoundMessage, connectionMessage })).toEqual({
      isLoading: false,
      isEmpty: true,
      errorMessage: notFoundMessage,
    });
  });

  it.each([401, 403, 429])(
    'maps a %d to an error state, never the not-found message',
    (status) => {
      const err = new HttpErrorResponse({ status, error: null });

      const result = mapApiFailureToLoadState(err, { notFoundMessage, connectionMessage });

      expect(result).toEqual({
        isLoading: false,
        isEmpty: false,
        errorMessage: connectionMessage,
      });
      expect(result.errorMessage).not.toBe(notFoundMessage);
    },
  );

  it('preserves a backend message on a 401/403/429 error instead of the connection fallback', () => {
    const err = new HttpErrorResponse({
      status: 401,
      error: { isSuccess: false, message: 'يجب تسجيل الدخول', errors: [] },
    });

    expect(mapApiFailureToLoadState(err, { notFoundMessage, connectionMessage })).toEqual({
      isLoading: false,
      isEmpty: false,
      errorMessage: 'يجب تسجيل الدخول',
    });
  });

  it('maps a 500 to an unchanged error state', () => {
    const err = new HttpErrorResponse({ status: 500, error: null });

    expect(mapApiFailureToLoadState(err, { notFoundMessage, connectionMessage })).toEqual({
      isLoading: false,
      isEmpty: false,
      errorMessage: connectionMessage,
    });
  });

  it('maps a network error (non-HttpErrorResponse) to an unchanged error state', () => {
    const err = new Error('network down');

    expect(mapApiFailureToLoadState(err, { notFoundMessage, connectionMessage })).toEqual({
      isLoading: false,
      isEmpty: false,
      errorMessage: connectionMessage,
    });
  });
});

describe('mapApiResponseToLoadState', () => {
  it('calls onSuccess and reports a non-empty, non-error state when data is present', () => {
    const onSuccess = vi.fn();
    const response: ApiResponse<{ id: number }> = {
      isSuccess: true,
      message: null,
      data: { id: 1 },
    };

    const result = mapApiResponseToLoadState(response, { emptyMessage }, onSuccess);

    expect(onSuccess).toHaveBeenCalledWith({ id: 1 });
    expect(result).toEqual({ isLoading: false, isEmpty: false, errorMessage: null });
  });

  it('reports isEmpty:true with the backend message when the backend reports failure', () => {
    const onSuccess = vi.fn();
    const response: ApiResponse<{ id: number }> = {
      isSuccess: false,
      message: 'لا توجد بيانات',
      data: null,
    };

    const result = mapApiResponseToLoadState(response, { emptyMessage }, onSuccess);

    expect(onSuccess).not.toHaveBeenCalled();
    expect(result).toEqual({ isLoading: false, isEmpty: true, errorMessage: 'لا توجد بيانات' });
  });

  it('falls back to emptyMessage when isSuccess but data is missing', () => {
    const onSuccess = vi.fn();
    const response: ApiResponse<{ id: number }> = { isSuccess: true, message: null, data: null };

    const result = mapApiResponseToLoadState(response, { emptyMessage }, onSuccess);

    expect(onSuccess).not.toHaveBeenCalled();
    expect(result).toEqual({ isLoading: false, isEmpty: true, errorMessage: emptyMessage });
  });
});

describe('subscribeToApiLoad', () => {
  it('settles with the success load state and forwards data to onSuccess', () => {
    const onSuccess = vi.fn();
    const onSettled = vi.fn();
    const response: ApiResponse<{ id: number }> = { isSuccess: true, message: null, data: { id: 7 } };

    subscribeToApiLoad(of(response), {
      onSuccess,
      onSettled,
      emptyMessage,
      notFoundMessage,
      connectionMessage,
    });

    expect(onSuccess).toHaveBeenCalledWith({ id: 7 });
    expect(onSettled).toHaveBeenCalledWith({ isLoading: false, isEmpty: false, errorMessage: null });
  });

  it('settles with a mapped error state (never isEmpty for a 429) when the request errors', () => {
    const onSuccess = vi.fn();
    const onSettled = vi.fn();
    const err = new HttpErrorResponse({ status: 429, error: null });

    subscribeToApiLoad(throwError(() => err), {
      onSuccess,
      onSettled,
      emptyMessage,
      notFoundMessage,
      connectionMessage,
    });

    expect(onSuccess).not.toHaveBeenCalled();
    expect(onSettled).toHaveBeenCalledWith({
      isLoading: false,
      isEmpty: false,
      errorMessage: connectionMessage,
    });
  });
});
