import { Injectable } from '@angular/core';

import { ApiResponseCache } from '../../../core/caching/api-response-cache';
import { AyahStudySourceParams } from '../data-access/mushaf-ayah-study.api';
import type { QuranVerseKey, QuranWordLocation } from '../../../shared/quran/quran-location';

export const MushafReaderCacheKeys = {
  page(pageNumber: number): string {
    return `mushaf:page:${pageNumber}`;
  },

  ayahStudy(verseKey: QuranVerseKey, sources: AyahStudySourceParams): string {
    const taf = sources.tafsirSource ?? 'none';
    const tr = sources.translationSource ?? 'none';
    const i3rab = sources.fullI3rabSource ?? 'none';
    return `mushaf:ayah-study:${verseKey}:taf:${taf}:tr:${tr}:i3rab:${i3rab}`;
  },

  wordAnalysis(wordLocation: QuranWordLocation): string {
    return `mushaf:word-analysis:${wordLocation}`;
  },

  similarAyahs(verseKey: QuranVerseKey): string {
    return `mushaf:similar-ayahs:${verseKey}`;
  },

  ayahMutashabihat(verseKey: QuranVerseKey): string {
    return `mushaf:mutashabihat:${verseKey}`;
  },
} as const;

@Injectable({ providedIn: 'root' })
export class MushafReaderCache extends ApiResponseCache {}
