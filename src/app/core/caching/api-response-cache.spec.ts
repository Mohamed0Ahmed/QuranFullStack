import { describe, expect, it, vi } from 'vitest';
import { firstValueFrom, Observable, of, Subject } from 'rxjs';

import { ApiResponse } from '../data-access/api-response.model';
import { ApiResponseCache } from './api-response-cache';

function success<T>(data: T): ApiResponse<T> {
  return { isSuccess: true, message: 'تم', data };
}

describe('ApiResponseCache', () => {
  it('reuses a successful cached response for later reads', async () => {
    const cache = new ApiResponseCache();
    const loader = vi.fn(() => of(success({ value: 1 })));

    await firstValueFrom(cache.getOrLoad('key', loader));
    const cached = await firstValueFrom(cache.getOrLoad('key', loader));

    expect(loader).toHaveBeenCalledTimes(1);
    expect(cached.data).toEqual({ value: 1 });
  });

  it('deduplicates concurrent in-flight reads for the same key', () => {
    const cache = new ApiResponseCache();
    const response$ = new Subject<ApiResponse<number>>();
    const loader = vi.fn(() => response$.asObservable());
    const values: number[] = [];

    cache.getOrLoad('key', loader).subscribe((response) => values.push(response.data ?? 0));
    cache.getOrLoad('key', loader).subscribe((response) => values.push(response.data ?? 0));
    response$.next(success(7));
    response$.complete();

    expect(loader).toHaveBeenCalledTimes(1);
    expect(values).toEqual([7, 7]);
  });

  it('keeps an in-flight read shared when one subscriber unsubscribes', () => {
    const cache = new ApiResponseCache();
    const response$ = new Subject<ApiResponse<number>>();
    const loader = vi.fn(() => response$.asObservable());
    const values: number[] = [];

    const first = cache
      .getOrLoad('key', loader)
      .subscribe((response) => values.push(response.data ?? 0));
    cache
      .getOrLoad('key', loader)
      .subscribe((response) => values.push(response.data ?? 0));
    first.unsubscribe();
    cache
      .getOrLoad('key', loader)
      .subscribe((response) => values.push(response.data ?? 0));
    response$.next(success(9));
    response$.complete();

    expect(loader).toHaveBeenCalledTimes(1);
    expect(values).toEqual([9, 9]);
  });

  it('cancels in-flight reads when all subscribers unsubscribe before completion', () => {
    const cache = new ApiResponseCache();
    const unsubscribeSpy = vi.fn();
    const loader = vi.fn(() => new Observable<ApiResponse<number>>(() => unsubscribeSpy));

    const subscription = cache.getOrLoad('key', loader).subscribe();
    subscription.unsubscribe();
    cache.getOrLoad('key', loader).subscribe().unsubscribe();

    expect(loader).toHaveBeenCalledTimes(2);
    expect(unsubscribeSpy).toHaveBeenCalledTimes(2);
  });

  it('evicts the least recently used entry when the cache grows past 48 entries', async () => {
    const cache = new ApiResponseCache();
    const loader = vi.fn(() => of(success(1)));

    for (let index = 1; index <= 48; index += 1) {
      cache.store(`key-${index}`, success(index));
    }

    await firstValueFrom(cache.getOrLoad('key-1', loader));
    cache.store('key-49', success(49));

    expect(loader).not.toHaveBeenCalled();
    expect(cache.peek<number>('key-1')).toBe(1);
    expect(cache.peek<number>('key-2')).toBeNull();
    expect(cache.peek<number>('key-49')).toBe(49);
  });

  it('refreshes recency when storing an existing key', () => {
    const cache = new ApiResponseCache();

    for (let index = 1; index <= 48; index += 1) {
      cache.store(`key-${index}`, success(index));
    }

    cache.store('key-1', success(100));
    cache.store('key-49', success(49));

    expect(cache.peek<number>('key-1')).toBe(100);
    expect(cache.peek<number>('key-2')).toBeNull();
    expect(cache.peek<number>('key-49')).toBe(49);
  });

  it('does not store failed or null-data loader responses', async () => {
    const cache = new ApiResponseCache();
    const loader = vi
      .fn()
      .mockReturnValueOnce(of({ isSuccess: false, message: 'فشل', data: null } as ApiResponse<null>))
      .mockReturnValueOnce(of({ isSuccess: true, message: 'تم', data: null } as ApiResponse<null>))
      .mockReturnValueOnce(of(success('fresh')));

    await firstValueFrom(cache.getOrLoad('key', loader));
    await firstValueFrom(cache.getOrLoad('key', loader));
    const response = await firstValueFrom(cache.getOrLoad('key', loader));

    expect(loader).toHaveBeenCalledTimes(3);
    expect(response.data).toBe('fresh');
  });
});
