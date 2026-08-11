import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';

import { UniqueWordsApi } from '../../../words/data-access/unique-words.api';
import { DEFAULT_AYAH_PAGE_SIZE } from '../../../words/models/unique-words.models';
import { LinkingAyah } from '../../models/linking-ayah.models';
import { LinkingSourceDescriptor } from '../../models/linking-source.models';
import { loadCompletePagedSource } from '../complete-paged-source.loader';

@Injectable({ providedIn: 'root' })
export class UniqueWordLinkingSourceResolver {
  private readonly uniqueWordsApi = inject(UniqueWordsApi);

  resolve(
    source: Extract<LinkingSourceDescriptor, { kind: 'unique-word' }>,
    onProgress: (progress: { loaded: number; total: number }) => void,
  ): Observable<readonly LinkingAyah[]> {
    return loadCompletePagedSource(
      (page) => this.uniqueWordsApi.getAyahMatches(source.mode, source.wordId, page, DEFAULT_AYAH_PAGE_SIZE),
      onProgress,
    ).pipe(map((matches) => mapUniqueWordMatches(matches)));
  }
}

function mapUniqueWordMatches(
  matches: readonly {
    ayahId: number;
    ayahNumber: number;
    matchedQuranWordIds: readonly number[];
    pageNumber: number;
    surahNameArabic: string;
    verseKey: string;
    words: readonly { isAyahMarker: boolean; quranWordId: number | null; textUthmani: string }[];
  }[],
): readonly LinkingAyah[] {
  const byVerseKey = new Map<string, LinkingAyah>();
  for (const match of matches) {
    const mapped: LinkingAyah = {
      verseKey: match.verseKey,
      ayahId: match.ayahId,
      surahNumber: Number(match.verseKey.split(':')[0]) || null,
      surahNameArabic: match.surahNameArabic,
      ayahNumber: match.ayahNumber,
      pageNumber: match.pageNumber,
      words: match.words.map((word, renderPosition) => ({
        renderPosition,
        canonicalQuranWordId: word.quranWordId,
        textUthmani: word.textUthmani,
        isAyahMarker: word.isAyahMarker,
        isSourceMatch: word.quranWordId !== null && match.matchedQuranWordIds.includes(word.quranWordId),
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
