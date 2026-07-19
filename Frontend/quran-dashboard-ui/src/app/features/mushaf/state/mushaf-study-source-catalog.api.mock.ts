import { vi } from 'vitest';
import { of } from 'rxjs';

import { MushafStudySourceCatalogApi } from '../data-access/mushaf-study-sources.api';
import { MushafSimilarAyahsApi } from '../data-access/mushaf-similar-ayahs.api';
import { MushafAyahMutashabihatApi } from '../data-access/mushaf-ayah-mutashabihat.api';
import { AyahStudyDto, StudySourceCatalogItemDto } from '../models/mushaf.models';

export const ayahStudyDtoMock: AyahStudyDto = {
  ayah: {
    verseKey: '2:25',
    surahNumber: 2,
    surahNameArabic: 'البقرة',
    ayahNumber: 25,
    textUthmani: 'نص تجريبي للآية',
    wordsCount: 5,
    pageFrom: 5,
    pageTo: 5,
    juzNumber: 1,
    hizbNumber: 1,
    rubNumber: 1,
    sajda: null,
  },
  selectedSources: {
    tafsirSource: 'ar-muyassar',
    translationSource: 'en-sahih-international',
    fullI3rabSource: 'muyassar',
  },
  tafsir: null,
  translation: null,
  fullI3rab: null,
  similaritySummary: {
    similarAyahCount: 2,
    mutashabihatGroupCount: 2,
    mutashabihatOccurrenceCount: 3,
  },
};

// Single shared fixture for study-source catalogue metadata: the source keys and display names
// describe real study sources, so they are declared once and imported rather than re-typed per
// spec, where a drifting copy could not be traced back to anything.
export const studySourceCatalogItemMock: StudySourceCatalogItemDto = {
  direction: 'rtl',
  displayNameAr: 'التفسير الميسر',
  displayNameEn: null,
  languageCode: 'ar',
  languageNameAr: 'العربية',
  sourceKey: 'ar-muyassar',
  tafsirKind: 'brief',
  translationType: null,
};

export const mushafStudySourceCatalogApiProvider = {
  provide: MushafStudySourceCatalogApi,
  useValue: {
    getCatalog: vi.fn(() =>
      of({
        isSuccess: true,
        message: 'ok',
        data: { tafsirSources: [], translationSources: [], fullI3rabSources: [] },
      }),
    ),
  },
};

export const mushafSimilarAyahsApiProvider = {
  provide: MushafSimilarAyahsApi,
  useValue: {
    getSimilarAyahs: vi.fn(() =>
      of({
        isSuccess: true,
        message: 'ok',
        data: { verseKey: '2:25', count: 0, items: [] },
      }),
    ),
  },
};

export const mushafAyahMutashabihatApiProvider = {
  provide: MushafAyahMutashabihatApi,
  useValue: {
    getAyahMutashabihat: vi.fn(() =>
      of({
        isSuccess: true,
        message: 'ok',
        data: { verseKey: '2:25', groupCount: 0, groups: [] },
      }),
    ),
  },
};
