import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { WordTypeGroupedMemberWordDto } from '../models/word-types-detail.models';
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
  scope: { type: 'inl', childCode: null, case: 'all', tense: 'all', voice: 'all' },
};

const groupedCacheIsolationCases: readonly {
  label: string;
  selection: WordTypeDetailSelection;
  endpoint: string;
  childCode: string | null;
}[] = [
  {
    label: 'kind',
    selection: {
      kind: 'stem',
      stemId: 190600,
      scope: rootSelection.scope,
    },
    endpoint: '/stems/190600/words',
    childCode: null,
  },
  {
    label: 'dimension identity',
    selection: {
      kind: 'root',
      rootId: 190701,
      scope: rootSelection.scope,
    },
    endpoint: '/roots/190701/words',
    childCode: null,
  },
  {
    label: 'grammatical scope',
    selection: {
      ...rootSelection,
      scope: { ...rootSelection.scope, childCode: 'PN' },
    },
    endpoint: '/roots/190700/words',
    childCode: 'PN',
  },
];

function setup(): { loader: WordTypesDetailViewLoader; http: HttpTestingController } {
  TestBed.configureTestingModule({
    providers: [
      WordTypesDetailViewLoader,
      WordTypesCache,
      provideHttpClient(),
      provideHttpClientTesting(),
    ],
  });

  const loader = TestBed.inject(WordTypesDetailViewLoader);
  const http = TestBed.inject(HttpTestingController);
  return { loader, http };
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
  afterEach(() => {
    TestBed.inject(HttpTestingController, null)?.verify({ ignoreCancelled: true });
    getTestBed().resetTestingModule();
  });

  it('loads only grouped member words with the requested page', () => {
    const { loader, http } = setup();
    const h = handlers();

    loader.loadActiveView({ selection: rootSelection, view: 'words', detailPage: 3 }, h);

    const request = http.expectOne((candidate) => candidate.url.endsWith('/api/words/word-types/table/roots/190700/words'));
    expect(request.request.params.get('type')).toBe('noun');
    expect(request.request.params.get('page')).toBe('3');
    expect(request.request.params.get('pageSize')).toBe(String(WORD_TYPES_DETAIL_PAGE_SIZE));
    request.flush(okGroupedWords());

    expect(h.onWords).toHaveBeenCalledTimes(1);
  });

  it('dispatches the word or grouped ayahs endpoint by selection kind', () => {
    const { loader, http } = setup();

    const wordHandlers = handlers();
    loader.loadActiveView({ selection: wordSelection, view: 'ayahs', detailPage: 1 }, wordHandlers);
    const wordRequest = http.expectOne((candidate) => candidate.url.endsWith('/api/words/word-types/words/191001/ayahs'));
    wordRequest.flush(okAyahs());
    expect(wordHandlers.onAyahs).toHaveBeenCalledTimes(1);

    const groupedHandlers = handlers();
    loader.loadActiveView({ selection: rootSelection, view: 'ayahs', detailPage: 1 }, groupedHandlers);
    const groupedRequest = http.expectOne((candidate) => candidate.url.endsWith('/api/words/word-types/table/roots/190700/ayahs'));
    groupedRequest.flush(okAyahs());
    expect(groupedHandlers.onAyahs).toHaveBeenCalledTimes(1);
  });

  it('dispatches the single-shot surahs endpoint and ignores detailPage', () => {
    const { loader, http } = setup();
    const h = handlers();

    loader.loadActiveView({ selection: rootSelection, view: 'surahs', detailPage: 5 }, h);
    const groupedRequest = http.expectOne((candidate) => candidate.url.endsWith('/api/words/word-types/table/roots/190700/surahs'));
    expect(groupedRequest.request.params.has('page')).toBe(false);
    groupedRequest.flush(okSurahs());
    expect(h.onSurahs).toHaveBeenCalledTimes(1);

    const wordHandlers = handlers();
    loader.loadActiveView({ selection: wordSelection, view: 'surahs', detailPage: 9 }, wordHandlers);
    const wordRequest = http.expectOne((candidate) => candidate.url.endsWith('/api/words/word-types/words/191001/surahs'));
    expect(wordRequest.request.params.has('page')).toBe(false);
    wordRequest.flush(okSurahs());
    expect(wordHandlers.onSurahs).toHaveBeenCalledTimes(1);
  });

  it('rejects the words view for a word selection', () => {
    const { loader, http } = setup();
    const h = handlers();

    const subscription = loader.loadActiveView({ selection: wordSelection, view: 'words', detailPage: 1 }, h);

    expect(subscription).toBeUndefined();
    http.expectNone(() => true);
    expect(h.onWords).not.toHaveBeenCalled();
  });

  it('reuses identical grouped reads, isolates paged views by page, and shares single-shot surahs', () => {
    const { loader, http } = setup();

    const firstWordsHandlers = handlers();
    loader.loadActiveView({ selection: rootSelection, view: 'words', detailPage: 1 }, firstWordsHandlers);
    http.expectOne((candidate) => candidate.url.endsWith('/roots/190700/words')).flush(okGroupedWords());
    loader.loadActiveView({ selection: rootSelection, view: 'words', detailPage: 1 }, handlers());
    http.expectNone((candidate) => candidate.url.endsWith('/roots/190700/words'));
    loader.loadActiveView({ selection: rootSelection, view: 'words', detailPage: 2 }, handlers());
    const secondWordsPage = http.expectOne((candidate) => candidate.url.endsWith('/roots/190700/words'));
    expect(secondWordsPage.request.params.get('page')).toBe('2');
    secondWordsPage.flush(okGroupedWords());

    loader.loadActiveView({ selection: rootSelection, view: 'ayahs', detailPage: 1 }, handlers());
    http.expectOne((candidate) => candidate.url.endsWith('/roots/190700/ayahs')).flush(okAyahs());
    loader.loadActiveView({ selection: rootSelection, view: 'ayahs', detailPage: 1 }, handlers());
    http.expectNone((candidate) => candidate.url.endsWith('/roots/190700/ayahs'));
    loader.loadActiveView({ selection: rootSelection, view: 'ayahs', detailPage: 2 }, handlers());
    const secondAyahsPage = http.expectOne((candidate) => candidate.url.endsWith('/roots/190700/ayahs'));
    expect(secondAyahsPage.request.params.get('page')).toBe('2');
    secondAyahsPage.flush(okAyahs());

    loader.loadActiveView({ selection: rootSelection, view: 'surahs', detailPage: 1 }, handlers());
    http.expectOne((candidate) => candidate.url.endsWith('/roots/190700/surahs')).flush(okSurahs());
    loader.loadActiveView({ selection: rootSelection, view: 'surahs', detailPage: 2 }, handlers());
    http.expectNone((candidate) => candidate.url.endsWith('/roots/190700/surahs'));
  });

  it.each(groupedCacheIsolationCases)('does not reuse grouped words across a changed $label', ({ selection, endpoint, childCode }) => {
    const { loader, http } = setup();

    loader.loadActiveView({ selection: rootSelection, view: 'words', detailPage: 1 }, handlers());
    http.expectOne((candidate) => candidate.url.endsWith('/roots/190700/words')).flush(okGroupedWords());

    loader.loadActiveView({ selection, view: 'words', detailPage: 1 }, handlers());
    const isolatedRequest = http.expectOne((candidate) => candidate.url.endsWith(endpoint));
    expect(isolatedRequest.request.params.get('childCode')).toBe(childCode);
    isolatedRequest.flush(okGroupedWords());
  });
});
