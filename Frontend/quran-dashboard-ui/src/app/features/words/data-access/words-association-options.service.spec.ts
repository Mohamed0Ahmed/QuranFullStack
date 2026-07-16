import { describe, expect, it, vi, beforeEach, afterEach } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { of } from 'rxjs';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { WordTypesApi } from './word-types.api';
import { WordTypesCache, WordTypesCacheKeys } from '../state/word-types-cache';
import { WordTypeTreeDto } from '../models/word-types.models';
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

    let options: readonly { id: string | number; label: string }[] = [];
    optionsService.wordTypeOptions().subscribe((result) => (options = result));

    expect(options).toEqual([
      { id: 'noun-proper', label: 'علم' },
      { id: 'noun-masdar', label: 'مصدر' },
      { id: 'particle-jar', label: 'حرف جر' },
    ]);
  });

  it('fetches GET word-types/tree only once when the Word Types feature already warmed the shared cache', () => {
    const getTree = vi.fn(() => of(treeResponse()));
    const { optionsService, cache } = setup(getTree);

    // Simulate the Word Types explorer visiting first (word-types-explorer.facade.ts's own tree read),
    // which goes through the identical WordTypesCache singleton and WordTypesCacheKeys.tree key.
    cache.getOrLoad(WordTypesCacheKeys.tree, () => getTree()).subscribe();
    expect(getTree).toHaveBeenCalledTimes(1);

    // Now Unique Words asks for its type-picker options: it must hit the shared cache, not the network.
    let options: readonly unknown[] = [];
    optionsService.wordTypeOptions().subscribe((result) => (options = result));

    expect(getTree).toHaveBeenCalledTimes(1);
    expect(options.length).toBe(3);
  });

  it('caches the tree across repeated Unique Words calls in the same browser session', () => {
    const getTree = vi.fn(() => of(treeResponse()));
    const { optionsService } = setup(getTree);

    optionsService.wordTypeOptions().subscribe();
    optionsService.wordTypeOptions().subscribe();
    optionsService.wordTypeOptions().subscribe();

    expect(getTree).toHaveBeenCalledTimes(1);
  });

  it('resolves to an empty option list on a failed tree response without throwing', () => {
    const getTree = vi.fn(() =>
      of({ isSuccess: false, message: 'فشل', data: null } as ApiResponse<WordTypeTreeDto>),
    );
    const { optionsService } = setup(getTree);

    let options: readonly unknown[] | undefined;
    optionsService.wordTypeOptions().subscribe((result) => (options = result));

    expect(options).toEqual([]);
  });
});
