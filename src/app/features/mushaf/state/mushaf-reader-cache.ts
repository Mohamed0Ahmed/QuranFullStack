import { Injectable } from '@angular/core';

import { ApiResponseCache } from '../../../core/caching/api-response-cache';
import { AyahStudySourceParams } from '../data-access/mushaf-ayah-study.api';

export const MushafReaderCacheKeys = {
  page(pageNumber: number): string {
    return `mushaf:page:${pageNumber}`;
  },

  ayahStudy(verseKey: string, sources: AyahStudySourceParams): string {
    const taf = sources.tafsirSource ?? 'none';
    const tr = sources.translationSource ?? 'none';
    const i3rab = sources.fullI3rabSource ?? 'none';
    return `mushaf:ayah-study:${verseKey}:taf:${taf}:tr:${tr}:i3rab:${i3rab}`;
  },

  wordAnalysis(wordLocation: string): string {
    return `mushaf:word-analysis:${wordLocation}`;
  },

  similarAyahs(verseKey: string): string {
    return `mushaf:similar-ayahs:${verseKey}`;
  },

  ayahMutashabihat(verseKey: string): string {
    return `mushaf:mutashabihat:${verseKey}`;
  },
} as const;

@Injectable({ providedIn: 'root' })
export class MushafReaderCache extends ApiResponseCache {}
