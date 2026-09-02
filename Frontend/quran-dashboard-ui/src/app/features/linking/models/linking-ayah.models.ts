import type { QuranVerseKey } from '../../../shared/quran/quran-location';

export interface LinkingAyah {
  verseKey: QuranVerseKey;
  ayahId: number;
  surahNumber: number;
  surahNameArabic: string;
  ayahNumber: number;
  pageNumber: number;
  words: readonly LinkingAyahWord[];
}

export interface LinkingAyahWord {
  renderPosition: number;
  canonicalQuranWordId: number;
  textUthmani: string;
  isAyahMarker: boolean;
  isSourceMatch: boolean;
  isExcludedSourceMatch: boolean;
}
