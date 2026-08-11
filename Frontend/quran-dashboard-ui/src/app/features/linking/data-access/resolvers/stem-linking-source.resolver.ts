import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';

import { StemsApi } from '../../../words/data-access/stems.api';
import { STEM_DETAIL_PAGE_SIZE, StemAyahMatchDto } from '../../../words/models/stems.models';
import { parseVerseKey } from '../../../words/utils/verse-key';
import { LinkingAyah } from '../../models/linking-ayah.models';
import { LinkingSourceDescriptor } from '../../models/linking-source.models';
import { loadCompletePagedSource } from '../complete-paged-source.loader';

@Injectable({ providedIn: 'root' })
export class StemLinkingSourceResolver {
  private readonly stemsApi = inject(StemsApi);

  resolve(
    source: Extract<LinkingSourceDescriptor, { kind: 'stem' }>,
    onProgress: (progress: { loaded: number; total: number }) => void,
  ): Observable<readonly LinkingAyah[]> {
    return loadCompletePagedSource(
      (page) =>
        this.stemsApi.getStemAyahMatches(
          source.stemId,
          page,
          STEM_DETAIL_PAGE_SIZE,
          source.typeCode,
        ),
      onProgress,
    ).pipe(map((matches) => mapStemMatches(matches)));
  }
}

function mapStemMatches(matches: readonly StemAyahMatchDto[]): readonly LinkingAyah[] {
  const byVerseKey = new Map<string, LinkingAyah>();
  for (const match of matches) {
    const { surahNumber, ayahNumber } = parseVerseKey(match.verseKey);
    const mapped: LinkingAyah = {
      verseKey: match.verseKey,
      ayahId: match.ayahId,
      surahNumber,
      surahNameArabic: match.surahNameArabic,
      ayahNumber,
      pageNumber: match.pageNumber,
      words: match.words.map((word, renderPosition) => ({
        renderPosition,
        canonicalQuranWordId: null,
        textUthmani: word.textUthmani,
        isAyahMarker: false,
        isSourceMatch: word.isMatched,
      })),
    };
    const existing = byVerseKey.get(mapped.verseKey);
    if (existing && !sameAyah(existing, mapped)) {
      throw new Error('تعارضت بيانات الآية المكررة في نتائج المصدر.');
    }
    if (!existing) {
      byVerseKey.set(mapped.verseKey, mapped);
    }
  }
  return [...byVerseKey.values()];
}

function sameAyah(left: LinkingAyah, right: LinkingAyah): boolean {
  return JSON.stringify(left) === JSON.stringify(right);
}
