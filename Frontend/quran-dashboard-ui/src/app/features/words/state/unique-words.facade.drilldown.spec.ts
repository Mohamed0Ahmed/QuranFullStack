import { describe, expect, it, vi, beforeEach, afterEach } from 'vitest';
import { getTestBed, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { UniqueWordsApi } from '../data-access/unique-words.api';
import { UniqueWordsFacade } from './unique-words.facade';
import {
  DEFAULT_AYAH_PAGE_SIZE,
  UniqueWordAyahMatchDto,
  UniqueWordListItemDto,
  UniqueWordMissingSurahsDto,
  UniqueWordSurahsDto,
} from '../models/unique-words.models';
import { DRILLDOWN_EMPTY_AYAHS_LABEL, DRILLDOWN_ERROR_LABEL } from '../models/unique-words.labels';

function word(id: number, overrides: Partial<UniqueWordListItemDto> = {}): UniqueWordListItemDto {
  return {
    id,
    kind: 'tashkeel',
    displayText: `كلمة-تجريبية-${id}`,
    occurrencesCount: 3,
    ayahsCount: 2,
    surahsCount: 2,
    missingSurahsCount: 112,
    primaryWordTypeCode: null,
    primaryWordTypeBroadArabicLabel: null,
    rootId: null,
    rootText: null,
    ...overrides,
  };
}

function setup(api: Partial<UniqueWordsApi> = {}) {
  TestBed.configureTestingModule({
    providers: [
      UniqueWordsFacade,
      {
        provide: UniqueWordsApi,
        useValue: {
          getList: vi.fn(),
          getMentionedSurahs: vi.fn(),
          getMissingSurahs: vi.fn(),
          getAyahMatches: vi.fn(),
          ...api,
        },
      },
    ],
  });
  return TestBed.inject(UniqueWordsFacade);
}

describe('UniqueWordsFacade drill-down', () => {
  beforeEach(() => {
    getTestBed().resetTestingModule();
  });

  afterEach(() => {
    getTestBed().resetTestingModule();
  });

  it('opens modal view and loads mentioned surahs', () => {
    const getMentionedSurahs = vi.fn(() =>
      of({
        isSuccess: true,
        message: 'تم',
        data: {
          surahs: [{ surahNumber: 1, nameArabic: 'سورة-تجريبية', occurrencesInSurah: 2 }],
        },
      } as ApiResponse<UniqueWordSurahsDto>),
    );
    const facade = setup({ getMentionedSurahs });

    facade.openDrilldown(word(1), 'surahs');

    expect(facade.drilldownState().isOpen).toBe(true);
    expect(facade.drilldownState().view).toBe('surahs');
    expect(facade.drilldownState().status).toBe('success');
    expect(facade.drilldownState().surahs?.surahs).toHaveLength(1);
    expect(getMentionedSurahs).toHaveBeenCalledWith('tashkeel', 1);
  });

  it('reuses a cached drill-down response when the same word is reopened', () => {
    const getMentionedSurahs = vi.fn(() =>
      of({
        isSuccess: true,
        message: 'تم',
        data: {
          surahs: [{ surahNumber: 1, nameArabic: 'سورة-تجريبية', occurrencesInSurah: 2 }],
        },
      } as ApiResponse<UniqueWordSurahsDto>),
    );
    const facade = setup({ getMentionedSurahs });

    facade.openDrilldown(word(1), 'surahs');
    facade.closeDrilldown();
    facade.openDrilldown(word(1), 'surahs');

    expect(getMentionedSurahs).toHaveBeenCalledTimes(1);
    expect(facade.drilldownState().surahs?.surahs).toHaveLength(1);
  });

  it('preserves list state when modal opens and closes', () => {
    const facade = setup({
      getList: vi.fn(() =>
        of({
          isSuccess: true,
          message: 'تم',
          data: { page: 1, pageSize: 1000, totalCount: 1, items: [word(1)] },
        }),
      ),
      getMentionedSurahs: vi.fn(() =>
        of({
          isSuccess: true,
          message: 'تم',
          data: { surahs: [] },
        }),
      ),
    });

    facade.loadList();
    facade.openDrilldown(word(1), 'surahs');
    facade.closeDrilldown();

    expect(facade.listState().items).toHaveLength(1);
    expect(facade.drilldownState().isOpen).toBe(false);
  });

  it('maps drill-down transport errors', () => {
    const facade = setup({
      getMissingSurahs: vi.fn(() => throwError(() => new Error('network'))),
    });

    facade.openDrilldown(word(1), 'missing');

    expect(facade.drilldownState().status).toBe('error');
    expect(facade.drilldownState().errorMessage.length).toBeGreaterThan(0);
  });

  it('maps backend isSuccess:false for each drill-down view', () => {
    const backendMessage = 'فشل من الخادم';
    const getMentionedSurahs = vi.fn(() =>
      of({ isSuccess: false, message: backendMessage, data: null } as ApiResponse<UniqueWordSurahsDto>),
    );
    const getAyahMatches = vi.fn(() =>
      of({
        isSuccess: false,
        message: backendMessage,
        data: null,
      } as ApiResponse<{ page: number; pageSize: number; totalCount: number; items: UniqueWordAyahMatchDto[] }>),
    );
    const getMissingSurahs = vi.fn(() =>
      of({ isSuccess: false, message: backendMessage, data: null } as ApiResponse<UniqueWordMissingSurahsDto>),
    );
    const facade = setup({ getMentionedSurahs, getMissingSurahs, getAyahMatches });

    facade.openDrilldown(word(1), 'surahs');
    expect(facade.drilldownState().status).toBe('error');
    expect(facade.drilldownState().errorMessage).toBe(backendMessage);

    facade.openDrilldown(word(1), 'missing');
    expect(facade.drilldownState().status).toBe('error');
    expect(facade.drilldownState().errorMessage).toBe(backendMessage);

    facade.openDrilldown(word(1), 'ayahs');
    expect(facade.drilldownState().status).toBe('error');
    expect(facade.drilldownState().errorMessage).toBe(backendMessage);
  });

  it('maps empty surahs, missing surahs, and ayah payloads to empty status', () => {
    const getMentionedSurahs = vi
      .fn()
      .mockReturnValueOnce(
        of({
          isSuccess: true,
          message: 'تم',
          data: { surahs: [] },
        } as ApiResponse<UniqueWordSurahsDto>),
      )
      .mockReturnValueOnce(
        of({
          isSuccess: true,
          message: 'تم',
          data: {
            surahs: Array.from({ length: 114 }, (_, index) => ({
              surahNumber: index + 1,
              nameArabic: `سورة-${index + 1}`,
              occurrencesInSurah: 1,
            })),
          },
        } as ApiResponse<UniqueWordSurahsDto>),
      );
    const getMissingSurahs = vi.fn(() =>
      of({
        isSuccess: true,
        message: 'تم',
        data: {
          surahs: [],
        },
      } as ApiResponse<UniqueWordMissingSurahsDto>),
    );
    const getAyahMatches = vi.fn(() =>
      of({
        isSuccess: true,
        message: 'تم',
        data: { page: 1, pageSize: DEFAULT_AYAH_PAGE_SIZE, totalCount: 0, items: [] },
      }),
    );
    const facade = setup({ getMentionedSurahs, getMissingSurahs, getAyahMatches });

    facade.openDrilldown(word(1), 'surahs');
    expect(facade.drilldownState().status).toBe('empty');

    facade.openDrilldown(word(1, { missingSurahsCount: 0 }), 'missing');
    expect(facade.drilldownState().status).toBe('empty');

    facade.openDrilldown(word(1, { ayahsCount: 0 }), 'ayahs');
    expect(facade.drilldownState().status).toBe('empty');
  });

  it('falls back to drill-down error label when backend failure has no message', () => {
    const facade = setup({
      getMentionedSurahs: vi.fn(() =>
        of({ isSuccess: false, message: null, data: null } as ApiResponse<UniqueWordSurahsDto>),
      ),
    });

    facade.openDrilldown(word(1), 'surahs');

    expect(facade.drilldownState().status).toBe('error');
    expect(facade.drilldownState().errorMessage).toBe(DRILLDOWN_ERROR_LABEL);
  });

  it('switches between all three drill-down views', () => {
    const getMentionedSurahs = vi.fn(() =>
      of({
        isSuccess: true,
        message: 'تم',
        data: {
          surahs: [{ surahNumber: 1, nameArabic: 'سورة-تجريبية', occurrencesInSurah: 1 }],
        },
      } as ApiResponse<UniqueWordSurahsDto>),
    );
    const getAyahMatches = vi.fn(() =>
      of({
        isSuccess: true,
        message: 'تم',
        data: {
          page: 1,
          pageSize: DEFAULT_AYAH_PAGE_SIZE,
          totalCount: 1,
          items: [
            {
              ayahId: 11,
              verseKey: '1:1',
              surahNameArabic: 'سورة-تجريبية',
              ayahNumber: 1,
              pageNumber: 1,
              matchedQuranWordIds: [1001],
              words: [{ quranWordId: 1001, textUthmani: 'كلمة-تجريبية', isAyahMarker: false }],
            },
          ],
        },
      }),
    );
    const getMissingSurahs = vi.fn(() =>
      of({
        isSuccess: true,
        message: 'تم',
        data: {
          surahs: Array.from({ length: 113 }, (_, index) => ({
            surahNumber: index + 2,
            nameArabic: `سورة-${index + 2}`,
          })),
        },
      } as ApiResponse<UniqueWordMissingSurahsDto>),
    );
    const facade = setup({ getMentionedSurahs, getMissingSurahs, getAyahMatches });

    facade.openDrilldown(word(1), 'surahs');
    expect(facade.drilldownState().view).toBe('surahs');
    expect(facade.drilldownState().status).toBe('success');

    facade.setDrilldownView('missing');
    expect(facade.drilldownState().view).toBe('missing');
    expect(getMentionedSurahs).toHaveBeenCalledTimes(1);
    expect(getMissingSurahs).toHaveBeenCalledTimes(1);
    expect(facade.drilldownState().missingSurahs?.surahs).toHaveLength(113);
    expect(facade.drilldownState().status).toBe('success');

    facade.setDrilldownView('ayahs');
    expect(facade.drilldownState().view).toBe('ayahs');
    expect(getAyahMatches).toHaveBeenCalledWith('tashkeel', 1, 1, DEFAULT_AYAH_PAGE_SIZE);
    expect(facade.drilldownState().status).toBe('success');
  });

  it('reloads ayah page when ayah pagination changes', () => {
    const getAyahMatches = vi.fn(() =>
      of({
        isSuccess: true,
        message: 'تم',
        data: { page: 2, pageSize: DEFAULT_AYAH_PAGE_SIZE, totalCount: 3, items: [] },
      }),
    );
    const facade = setup({ getAyahMatches });

    facade.openDrilldown(word(1), 'ayahs');
    facade.setAyahPage(2);

    expect(getAyahMatches).toHaveBeenLastCalledWith('tashkeel', 1, 2, DEFAULT_AYAH_PAGE_SIZE);
    expect(facade.drilldownState().ayahPage).toBe(2);
  });

  it('shows empty ayah label context when ayah list is empty', () => {
    const facade = setup({
      getAyahMatches: vi.fn(() =>
        of({
          isSuccess: true,
          message: 'تم',
          data: { page: 1, pageSize: DEFAULT_AYAH_PAGE_SIZE, totalCount: 0, items: [] },
        }),
      ),
    });

    facade.openDrilldown(word(1, { ayahsCount: 0 }), 'ayahs');

    expect(facade.drilldownState().status).toBe('empty');
    expect(DRILLDOWN_EMPTY_AYAHS_LABEL.length).toBeGreaterThan(0);
  });
});
