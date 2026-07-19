import { describe, expect, it, vi, beforeEach, afterEach } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { HttpErrorResponse, provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { of, throwError } from 'rxjs';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { PagedResultDto } from '../../../core/data-access/paged-result.model';
import { WordTypesApi } from './word-types.api';
import { RootsApi } from './roots.api';
import { LemmasApi } from './lemmas.api';
import { WordTypesCache, WordTypesCacheKeys } from '../state/word-types-cache';
import { WordTypeTreeDto } from '../models/word-types.models';
import { RootListItemDto } from '../models/roots.models';
import { LemmaListItemDto } from '../models/lemmas.models';
import { AssociationOptionsResult } from '../state/words-association-filters';
import { WordsAssociationOptionsService } from './words-association-options.service';

function treeResponse(): ApiResponse<WordTypeTreeDto> {
  return {
    isSuccess: true,
    message: 'تم',
    data: {
      mainTypes: [
        {
          code: 'noun',
          count: 10,
          label: { ar: 'اسم' },
          secondaryFilter: { kind: 'case', options: [], voiceOptions: [] },
          children: [
            { code: 'noun-proper', childCode: 'proper', count: 4, label: { ar: 'علم' } },
            { code: 'noun-masdar', childCode: 'masdar', count: 6, label: { ar: 'مصدر' } },
          ],
        },
        {
          code: 'particle',
          count: 3,
          label: { ar: 'حرف' },
          secondaryFilter: { kind: 'none', options: [], voiceOptions: [] },
          children: [{ code: 'particle-jar', childCode: 'jar', count: 3, label: { ar: 'حرف جر' } }],
        },
        {
          code: 'verb',
          count: 5,
          label: { ar: 'فعل' },
          secondaryFilter: { kind: 'tense+voice', options: [], voiceOptions: [] },
          children: [{ code: 'verb-past', childCode: 'past', count: 5, label: { ar: 'ماضٍ' } }],
        },
      ],
    },
  };
}

function setup(getTree: ReturnType<typeof vi.fn>): {
  optionsService: WordsAssociationOptionsService;
  cache: WordTypesCache;
} {
  TestBed.configureTestingModule({
    providers: [
      provideHttpClient(),
      provideHttpClientTesting(),
      WordsAssociationOptionsService,
      { provide: WordTypesApi, useValue: { getTree } },
    ],
  });

  return {
    optionsService: TestBed.inject(WordsAssociationOptionsService),
    cache: TestBed.inject(WordTypesCache),
  };
}

describe('WordsAssociationOptionsService.wordTypeOptions (shared word-types tree cache, perf finding F2)', () => {
  beforeEach(() => {
    getTestBed().resetTestingModule();
  });

  afterEach(() => {
    getTestBed().resetTestingModule();
  });

  it('shapes noun/particle POS-leaf children into association options (unchanged option shape)', () => {
    const getTree = vi.fn(() => of(treeResponse()));
    const { optionsService } = setup(getTree);

    let result: AssociationOptionsResult | undefined;
    optionsService.wordTypeOptions().subscribe((value) => (result = value));

    expect(result).toEqual({
      status: 'success',
      options: [
        { id: 'noun-proper', label: 'علم' },
        { id: 'noun-masdar', label: 'مصدر' },
        { id: 'particle-jar', label: 'حرف جر' },
      ],
    });
  });

  it('fetches GET word-types/tree only once when the Word Types feature already warmed the shared cache', () => {
    const getTree = vi.fn(() => of(treeResponse()));
    const { optionsService, cache } = setup(getTree);

    // Simulate the Word Types explorer visiting first (word-types-explorer.facade.ts's own tree read),
    // which goes through the identical WordTypesCache singleton and WordTypesCacheKeys.tree key.
    cache.getOrLoad(WordTypesCacheKeys.tree, () => getTree()).subscribe();
    expect(getTree).toHaveBeenCalledTimes(1);

    // Now Unique Words asks for its type-picker options: it must hit the shared cache, not the network.
    let result: AssociationOptionsResult | undefined;
    optionsService.wordTypeOptions().subscribe((value) => (result = value));

    expect(getTree).toHaveBeenCalledTimes(1);
    expect(result?.status).toBe('success');
    expect(result?.status === 'success' && result.options.length).toBe(3);
  });

  it('caches the tree across repeated Unique Words calls in the same browser session', () => {
    const getTree = vi.fn(() => of(treeResponse()));
    const { optionsService } = setup(getTree);

    optionsService.wordTypeOptions().subscribe();
    optionsService.wordTypeOptions().subscribe();
    optionsService.wordTypeOptions().subscribe();

    expect(getTree).toHaveBeenCalledTimes(1);
  });

  // M32/M43 + M74: a backend failure response must be distinguishable from a genuine empty list.
  it('yields an error result (not a silent empty list) on a backend isSuccess:false tree response', () => {
    const getTree = vi.fn(() =>
      of({ isSuccess: false, message: 'فشل', data: null } as ApiResponse<WordTypeTreeDto>),
    );
    const { optionsService } = setup(getTree);

    let result: AssociationOptionsResult | undefined;
    optionsService.wordTypeOptions().subscribe((value) => (result = value));

    expect(result).toEqual({ status: 'error' });
  });

  it('yields an error result (not a silent empty list) on a transport error, without throwing', () => {
    const getTree = vi.fn(() =>
      throwError(() => new HttpErrorResponse({ status: 0 })),
    );
    const { optionsService } = setup(getTree);

    let result: AssociationOptionsResult | undefined;
    let threw = false;
    optionsService.wordTypeOptions().subscribe({
      next: (value) => (result = value),
      error: () => (threw = true),
    });

    expect(threw).toBe(false);
    expect(result).toEqual({ status: 'error' });
  });
});

function rootsListResponse(): ApiResponse<PagedResultDto<RootListItemDto>> {
  return {
    isSuccess: true,
    message: 'تم',
    data: {
      page: 1,
      pageSize: 30,
      totalCount: 1,
      items: [
        {
          id: 12,
          rootText: 'ر ح م',
          occurrencesCount: 5,
          ayahsCount: 5,
          surahsCount: 3,
          simpleWordsCount: 3,
          tashkeelWordsCount: 2,
          lemmasCount: 2,
          stemsCount: 2,
        },
      ],
    },
  };
}

function setupRoots(getRootsList: ReturnType<typeof vi.fn>): { optionsService: WordsAssociationOptionsService } {
  TestBed.configureTestingModule({
    providers: [
      provideHttpClient(),
      provideHttpClientTesting(),
      WordsAssociationOptionsService,
      { provide: RootsApi, useValue: { getRootsList } },
    ],
  });

  return { optionsService: TestBed.inject(WordsAssociationOptionsService) };
}

describe('WordsAssociationOptionsService.searchRoots (M32/M43 + M74: error must not collapse into empty)', () => {
  beforeEach(() => {
    getTestBed().resetTestingModule();
  });

  afterEach(() => {
    getTestBed().resetTestingModule();
  });

  it('yields a success result with the mapped options on a genuine match', () => {
    const getRootsList = vi.fn(() => of(rootsListResponse()));
    const { optionsService } = setupRoots(getRootsList);

    let result: AssociationOptionsResult | undefined;
    optionsService.searchRoots('رحم').subscribe((value) => (result = value));

    expect(result).toEqual({ status: 'success', options: [{ id: 12, label: 'ر ح م' }] });
  });

  it('yields a success result with an empty option list on a genuine zero-match response', () => {
    const getRootsList = vi.fn(() =>
      of({ isSuccess: true, message: 'تم', data: { page: 1, pageSize: 30, totalCount: 0, items: [] } } as ApiResponse<
        PagedResultDto<RootListItemDto>
      >),
    );
    const { optionsService } = setupRoots(getRootsList);

    let result: AssociationOptionsResult | undefined;
    optionsService.searchRoots('غير موجود').subscribe((value) => (result = value));

    expect(result).toEqual({ status: 'success', options: [] });
  });

  it('yields an error result (not an empty list) on a backend isSuccess:false response', () => {
    const getRootsList = vi.fn(() =>
      of({ isSuccess: false, message: 'حدث خطأ', data: null } as ApiResponse<PagedResultDto<RootListItemDto>>),
    );
    const { optionsService } = setupRoots(getRootsList);

    let result: AssociationOptionsResult | undefined;
    optionsService.searchRoots('رحم').subscribe((value) => (result = value));

    expect(result).toEqual({ status: 'error' });
  });

  it('yields an error result (not an empty list) on a transport error, without throwing', () => {
    const getRootsList = vi.fn(() =>
      throwError(() => new HttpErrorResponse({ status: 500, error: { message: 'خطأ في الخادم' } })),
    );
    const { optionsService } = setupRoots(getRootsList);

    let result: AssociationOptionsResult | undefined;
    let threw = false;
    optionsService.searchRoots('رحم').subscribe({
      next: (value) => (result = value),
      error: () => (threw = true),
    });

    expect(threw).toBe(false);
    expect(result).toEqual({ status: 'error' });
  });
});

function lemmasListResponse(): ApiResponse<PagedResultDto<LemmaListItemDto>> {
  return {
    isSuccess: true,
    message: 'تم',
    data: {
      page: 1,
      pageSize: 30,
      totalCount: 1,
      items: [
        {
          id: 44,
          lemmaText: 'رَحِمَ',
          occurrencesCount: 4,
          ayahsCount: 4,
          surahsCount: 2,
          simpleWordsCount: 2,
          tashkeelWordsCount: 2,
          stemsCount: 1,
          rootId: 12,
          rootText: 'ر ح م',
        },
      ],
    },
  };
}

function setupLemmas(getLemmasList: ReturnType<typeof vi.fn>): { optionsService: WordsAssociationOptionsService } {
  TestBed.configureTestingModule({
    providers: [
      provideHttpClient(),
      provideHttpClientTesting(),
      WordsAssociationOptionsService,
      { provide: LemmasApi, useValue: { getLemmasList } },
    ],
  });

  return { optionsService: TestBed.inject(WordsAssociationOptionsService) };
}

describe('WordsAssociationOptionsService.searchLemmas (M32/M43 + M74: error must not collapse into empty)', () => {
  beforeEach(() => {
    getTestBed().resetTestingModule();
  });

  afterEach(() => {
    getTestBed().resetTestingModule();
  });

  it('yields a success result with the mapped options on a genuine match', () => {
    const getLemmasList = vi.fn(() => of(lemmasListResponse()));
    const { optionsService } = setupLemmas(getLemmasList);

    let result: AssociationOptionsResult | undefined;
    optionsService.searchLemmas('رحم').subscribe((value) => (result = value));

    expect(result).toEqual({ status: 'success', options: [{ id: 44, label: 'رَحِمَ' }] });
  });

  it('yields an error result (not an empty list) on a backend isSuccess:false response', () => {
    const getLemmasList = vi.fn(() =>
      of({ isSuccess: false, message: 'حدث خطأ', data: null } as ApiResponse<PagedResultDto<LemmaListItemDto>>),
    );
    const { optionsService } = setupLemmas(getLemmasList);

    let result: AssociationOptionsResult | undefined;
    optionsService.searchLemmas('رحم').subscribe((value) => (result = value));

    expect(result).toEqual({ status: 'error' });
  });

  it('yields an error result (not an empty list) on a transport error, without throwing', () => {
    const getLemmasList = vi.fn(() =>
      throwError(() => new HttpErrorResponse({ status: 500, error: { message: 'خطأ في الخادم' } })),
    );
    const { optionsService } = setupLemmas(getLemmasList);

    let result: AssociationOptionsResult | undefined;
    let threw = false;
    optionsService.searchLemmas('رحم').subscribe({
      next: (value) => (result = value),
      error: () => (threw = true),
    });

    expect(threw).toBe(false);
    expect(result).toEqual({ status: 'error' });
  });
});
