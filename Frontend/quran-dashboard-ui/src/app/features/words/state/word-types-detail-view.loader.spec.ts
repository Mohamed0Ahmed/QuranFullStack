import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { WordTypeGroupedMemberWordDto, WordTypesApi } from '../data-access/word-types.api';
import {
  PagedResultDto,
  WORD_TYPES_DETAIL_PAGE_SIZE,
  WordTypeAyahMatchDto,
  WordTypeSurahsResponseDto,
} from '../models/word-types.models';
import { WordTypeDetailSelection } from '../models/word-types-detail.models';
import { WordTypesCache } from './word-types-cache';
import { WordTypesDetailViewLoader, WordTypesDetailViewHandlers } from './word-types-detail-view.loader';

function okGroupedWords(): ApiResponse<PagedResultDto<WordTypeGroupedMemberWordDto>> {
  return {
    isSuccess: true,
    message: 'تم',
    data: { page: 1, pageSize: WORD_TYPES_DETAIL_PAGE_SIZE, totalCount: 0, items: [] },
  };
}

function okAyahs(): ApiResponse<PagedResultDto<WordTypeAyahMatchDto>> {
  return {
    isSuccess: true,
    message: 'تم',
    data: { page: 1, pageSize: WORD_TYPES_DETAIL_PAGE_SIZE, totalCount: 0, items: [] },
  };
}

function okSurahs(): ApiResponse<WordTypeSurahsResponseDto> {
  return { isSuccess: true, message: 'تم', data: { surahs: [], missingSurahs: [] } };
}

const rootSelection: WordTypeDetailSelection = {
  kind: 'root',
  rootId: 190700,
  scope: { type: 'noun', childCode: null, case: 'all', tense: 'all', voice: 'all' },
};

const wordSelection: WordTypeDetailSelection = {
  kind: 'word',
  identity: { tashkeelWordId: 191001, contextCode: 'INL', case: 'all', tense: 'all', voice: 'all' },
};

const groupedRequest = { kind: 'root', dimensionId: 190700, type: 'noun', childCode: null, case: 'all', tense: 'all', voice: 'all' };

interface ApiMock {
  getAyahMatches: ReturnType<typeof vi.fn>;
  getSurahs: ReturnType<typeof vi.fn>;
  getGroupedMemberWords: ReturnType<typeof vi.fn>;
  getGroupedAyahMatches: ReturnType<typeof vi.fn>;
  getGroupedSurahs: ReturnType<typeof vi.fn>;
}

function setup(apiOverrides: Partial<ApiMock> = {}): { loader: WordTypesDetailViewLoader; api: ApiMock } {
  TestBed.configureTestingModule({
    providers: [
      WordTypesDetailViewLoader,
      WordTypesCache,
      {
        provide: WordTypesApi,
        useValue: {
          getAyahMatches: vi.fn(() => of(okAyahs())),
          getSurahs: vi.fn(() => of(okSurahs())),
          getGroupedMemberWords: vi.fn(() => of(okGroupedWords())),
          getGroupedAyahMatches: vi.fn(() => of(okAyahs())),
          getGroupedSurahs: vi.fn(() => of(okSurahs())),
          ...apiOverrides,
        },
      },
    ],
  });

  const loader = TestBed.inject(WordTypesDetailViewLoader);
  const api = TestBed.inject(WordTypesApi) as unknown as ApiMock;
  return { loader, api };
}

function handlers(): WordTypesDetailViewHandlers & { [K in keyof WordTypesDetailViewHandlers]: ReturnType<typeof vi.fn> } {
  return {
    onWords: vi.fn(),
    onAyahs: vi.fn(),
    onSurahs: vi.fn(),
    onError: vi.fn(),
  } as unknown as WordTypesDetailViewHandlers & { [K in keyof WordTypesDetailViewHandlers]: ReturnType<typeof vi.fn> };
}

describe('WordTypesDetailViewLoader — kind + view dispatch', () => {
  beforeEach(() => getTestBed().resetTestingModule());
  afterEach(() => getTestBed().resetTestingModule());

  it('loads only grouped member words with the requested page', () => {
    const { loader, api } = setup();
    const h = handlers();

    loader.loadActiveView({ selection: rootSelection, view: 'words', detailPage: 3 }, h);

    expect(api.getGroupedMemberWords).toHaveBeenCalledWith(groupedRequest, 3, WORD_TYPES_DETAIL_PAGE_SIZE);
    expect(h.onWords).toHaveBeenCalledTimes(1);
    expect(api.getGroupedAyahMatches).not.toHaveBeenCalled();
    expect(api.getGroupedSurahs).not.toHaveBeenCalled();
  });

  it('dispatches the word or grouped ayahs endpoint by selection kind', () => {
    const { loader, api } = setup();

    loader.loadActiveView({ selection: wordSelection, view: 'ayahs', detailPage: 1 }, handlers());
    expect(api.getAyahMatches).toHaveBeenCalled();
    expect(api.getGroupedAyahMatches).not.toHaveBeenCalled();

    loader.loadActiveView({ selection: rootSelection, view: 'ayahs', detailPage: 1 }, handlers());
    expect(api.getGroupedAyahMatches).toHaveBeenCalled();
  });

  it('dispatches the single-shot surahs endpoint and ignores detailPage', () => {
    const { loader, api } = setup();
    const h = handlers();

    loader.loadActiveView({ selection: rootSelection, view: 'surahs', detailPage: 5 }, h);
    expect(api.getGroupedSurahs).toHaveBeenCalledWith(groupedRequest);
    expect(api.getGroupedSurahs).toHaveBeenCalledTimes(1);
    expect(h.onSurahs).toHaveBeenCalledTimes(1);

    const wordHandlers = handlers();
    loader.loadActiveView({ selection: wordSelection, view: 'surahs', detailPage: 9 }, wordHandlers);
    expect(api.getSurahs).toHaveBeenCalled();
  });

  it('rejects the words view for a word selection', () => {
    const { loader, api } = setup();
    const h = handlers();

    const subscription = loader.loadActiveView({ selection: wordSelection, view: 'words', detailPage: 1 }, h);

    expect(subscription).toBeUndefined();
    expect(api.getGroupedMemberWords).not.toHaveBeenCalled();
    expect(h.onWords).not.toHaveBeenCalled();
  });

  it('uses a separate cache entry per page for words and ayahs but shares the single-shot surahs entry', () => {
    const { loader, api } = setup();

    loader.loadActiveView({ selection: rootSelection, view: 'words', detailPage: 1 }, handlers());
    loader.loadActiveView({ selection: rootSelection, view: 'words', detailPage: 2 }, handlers());
    expect(api.getGroupedMemberWords).toHaveBeenCalledTimes(2);

    loader.loadActiveView({ selection: rootSelection, view: 'ayahs', detailPage: 1 }, handlers());
    loader.loadActiveView({ selection: rootSelection, view: 'ayahs', detailPage: 2 }, handlers());
    expect(api.getGroupedAyahMatches).toHaveBeenCalledTimes(2);

    loader.loadActiveView({ selection: rootSelection, view: 'surahs', detailPage: 1 }, handlers());
    loader.loadActiveView({ selection: rootSelection, view: 'surahs', detailPage: 2 }, handlers());
    expect(api.getGroupedSurahs).toHaveBeenCalledTimes(1);
  });
});
