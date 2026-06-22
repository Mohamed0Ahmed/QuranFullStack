import { describe, expect, it, vi, beforeEach, afterEach } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, ParamMap, convertToParamMap } from '@angular/router';
import { BehaviorSubject, of, throwError } from 'rxjs';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { UniqueWordsApi } from '../data-access/unique-words.api';
import { UniqueWordsFacade } from './unique-words.facade';
import { EMPTY_LIST_LABEL } from '../models/unique-words.labels';
import {
  DEFAULT_LIST_PAGE,
  DEFAULT_LIST_PAGE_SIZE,
  UniqueWordListItemDto,
} from '../models/unique-words.models';

/** Source-safe synthetic placeholder — not Quranic text. */
function item(id: number, overrides: Partial<UniqueWordListItemDto> = {}): UniqueWordListItemDto {
  return {
    id,
    kind: 'tashkeel',
    displayTextUthmani: `كلمة-تجريبية-${id}`,
    occurrencesCount: 1,
    ayahsCount: 1,
    surahsCount: 1,
    missingSurahsCount: 113,
    firstVerseKey: '1:1',
    firstLocation: '1:1:1',
    ...overrides,
  };
}

function okResponse(items: UniqueWordListItemDto[], totalCount: number): ApiResponse<{ page: number; pageSize: number; totalCount: number; items: UniqueWordListItemDto[] }> {
  return {
    isSuccess: true,
    message: 'تم',
    data: { page: 1, pageSize: DEFAULT_LIST_PAGE_SIZE, totalCount, items },
  };
}

function setup(getList = vi.fn()) {
  TestBed.configureTestingModule({
    providers: [UniqueWordsFacade, { provide: UniqueWordsApi, useValue: { getList } }],
  });
  return TestBed.inject(UniqueWordsFacade);
}

describe('UniqueWordsFacade list state', () => {
  beforeEach(() => {
    getTestBed().resetTestingModule();
  });

  afterEach(() => {
    getTestBed().resetTestingModule();
  });

  it('transitions through loading then success with items and totalCount', () => {
    const getList = vi.fn(() => of(okResponse([item(1), item(2)], 2)));
    const facade = setup(getList);

    expect(facade.status()).toBe('idle');
    facade.loadList();
    expect(facade.status()).toBe('success');
    expect(facade.items()).toHaveLength(2);
    expect(facade.totalCount()).toBe(2);

    const state = facade.listState();
    expect(state.pageSize).toBe(DEFAULT_LIST_PAGE_SIZE);
    expect(state.page).toBe(DEFAULT_LIST_PAGE);
    expect(state.mode).toBe('tashkeel');
    expect(state.sort).toBe('mushaf-order');
  });

  it('maps a zero-total-count success into the empty status', () => {
    const getList = vi.fn(() => of(okResponse([], 0)));
    const facade = setup(getList);

    facade.loadList();

    expect(facade.status()).toBe('empty');
    expect(facade.items()).toHaveLength(0);
    expect(facade.totalCount()).toBe(0);
  });

  it('maps a backend isSuccess:false failure into the error status with message', () => {
    const getList = vi.fn(() =>
      of({ isSuccess: false, message: 'نوع الكلمات غير صالح', data: null } as ApiResponse<never>),
    );
    const facade = setup(getList);

    facade.loadList();

    expect(facade.status()).toBe('error');
    expect(facade.errorMessage()).toBe('نوع الكلمات غير صالح');
  });

  it('falls back to the empty-list label when a failure carries no message', () => {
    const getList = vi.fn(() =>
      of({ isSuccess: false, message: null, data: null } as ApiResponse<never>),
    );
    const facade = setup(getList);

    facade.loadList();

    expect(facade.status()).toBe('error');
    expect(facade.errorMessage()).toBe(EMPTY_LIST_LABEL);
  });

  it('maps a transport error (HttpErrorResponse) into the error status', () => {
    const getList = vi.fn(() =>
      throwError(() => new HttpErrorResponse({ status: 500, error: { message: 'خطأ في الخادم' } })),
    );
    const facade = setup(getList);

    facade.loadList();

    expect(facade.status()).toBe('error');
    expect(facade.errorMessage()).toBe('خطأ في الخادم');
  });

  it('uses a connection-failure message for transport errors without a body message', () => {
    const getList = vi.fn(() => throwError(() => new HttpErrorResponse({ status: 0 })));
    const facade = setup(getList);

    facade.loadList();

    expect(facade.status()).toBe('error');
    expect(facade.errorMessage().length).toBeGreaterThan(0);
  });

  it('reloads with the new mode and resets to page 1 on setMode', () => {
    const getList = vi.fn(() => of(okResponse([item(1)], 1)));
    const facade = setup(getList);
    facade.loadList();
    expect(getList).toHaveBeenLastCalledWith('tashkeel', '', 'mushaf-order', 1, DEFAULT_LIST_PAGE_SIZE);

    facade.setMode('simple');

    expect(getList).toHaveBeenLastCalledWith('simple', '', 'mushaf-order', 1, DEFAULT_LIST_PAGE_SIZE);
  });

  it('reloads with the new search and resets to page 1 on setSearch', () => {
    const getList = vi.fn(() => of(okResponse([item(1)], 1)));
    const facade = setup(getList);
    facade.loadList();

    facade.setSearch('اسم');

    expect(getList).toHaveBeenLastCalledWith('tashkeel', 'اسم', 'mushaf-order', 1, DEFAULT_LIST_PAGE_SIZE);
  });

  it('reloads with the new sort and resets to page 1 on setSort', () => {
    const getList = vi.fn(() => of(okResponse([item(1)], 1)));
    const facade = setup(getList);
    facade.loadList();

    facade.setSort('occurrences');

    expect(getList).toHaveBeenLastCalledWith('tashkeel', '', 'occurrences', 1, DEFAULT_LIST_PAGE_SIZE);
  });

  it('reloads with the requested page on setPage', () => {
    const getList = vi.fn(() => of(okResponse([item(1)], 100)));
    const facade = setup(getList);
    facade.loadList();

    facade.setPage(3);

    expect(getList).toHaveBeenLastCalledWith('tashkeel', '', 'mushaf-order', 3, DEFAULT_LIST_PAGE_SIZE);
  });
});

describe('UniqueWordsFacade route binding', () => {
  function fakeRoute(
    paramMap: BehaviorSubject<ParamMap>,
    queryParamMap: BehaviorSubject<ParamMap>,
  ): ActivatedRoute {
    return { paramMap, queryParamMap } as unknown as ActivatedRoute;
  }

  it('reads the mode from the :mode path segment and query state on bind', () => {
    const getList = vi.fn(() => of(okResponse([item(1)], 1)));
    const facade = setup(getList);

    const paramMap = new BehaviorSubject<ParamMap>(convertToParamMap({ mode: 'simple' }));
    const queryParamMap = new BehaviorSubject<ParamMap>(
      convertToParamMap({ search: 'اسم', sort: 'occurrences', page: '2' }),
    );
    facade.bindToRoute(fakeRoute(paramMap, queryParamMap));

    expect(facade.mode()).toBe('simple');
    expect(getList).toHaveBeenLastCalledWith('simple', 'اسم', 'occurrences', 2, DEFAULT_LIST_PAGE_SIZE);
  });

  it('reloads with the new mode when only the :mode path segment changes', () => {
    // Regression: a pure mode switch (query params unchanged) must reload. The
    // mode lives in the path segment, so reload cannot depend on a query-param
    // change emitting.
    const getList = vi.fn(() => of(okResponse([item(1)], 1)));
    const facade = setup(getList);

    const paramMap = new BehaviorSubject<ParamMap>(convertToParamMap({ mode: 'tashkeel' }));
    const queryParamMap = new BehaviorSubject<ParamMap>(convertToParamMap({}));
    facade.bindToRoute(fakeRoute(paramMap, queryParamMap));
    expect(getList).toHaveBeenLastCalledWith('tashkeel', '', 'mushaf-order', 1, DEFAULT_LIST_PAGE_SIZE);

    paramMap.next(convertToParamMap({ mode: 'simple' }));

    expect(facade.mode()).toBe('simple');
    expect(getList).toHaveBeenLastCalledWith('simple', '', 'mushaf-order', 1, DEFAULT_LIST_PAGE_SIZE);

    facade.unbindFromRoute();
  });

  it('normalizes an unknown route mode to the tashkeel default', () => {
    const getList = vi.fn(() => of(okResponse([item(1)], 1)));
    const facade = setup(getList);

    const paramMap = new BehaviorSubject<ParamMap>(convertToParamMap({ mode: 'bogus' }));
    const queryParamMap = new BehaviorSubject<ParamMap>(convertToParamMap({}));
    facade.bindToRoute(fakeRoute(paramMap, queryParamMap));

    expect(facade.mode()).toBe('tashkeel');
    expect(getList).toHaveBeenLastCalledWith('tashkeel', '', 'mushaf-order', 1, DEFAULT_LIST_PAGE_SIZE);

    facade.unbindFromRoute();
  });
});
