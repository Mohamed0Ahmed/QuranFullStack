import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';

import { RootsApi } from '../../../words/data-access/roots.api';
import { ROOT_DETAIL_PAGE_SIZE, RootAyahMatchDto } from '../../../words/models/roots.models';
import { parseVerseKey } from '../../../words/utils/verse-key';
import { LinkingAyah } from '../../models/linking-ayah.models';
import { LinkingSourceDescriptor } from '../../models/linking-source.models';
import { loadCompletePagedSource } from '../complete-paged-source.loader';

@Injectable({ providedIn: 'root' })
export class RootLinkingSourceResolver {
  private readonly rootsApi = inject(RootsApi);

  resolve(
    source: Extract<LinkingSourceDescriptor, { kind: 'root' }>,
    onProgress: (progress: { loaded: number; total: number }) => void,
  ): Observable<readonly LinkingAyah[]> {
    return loadCompletePagedSource(
      (page) => this.rootsApi.getRootAyahMatches(source.rootId, page, ROOT_DETAIL_PAGE_SIZE),
      onProgress,
    ).pipe(map((matches) => mapRootMatches(matches)));
  }
}

function mapRootMatches(matches: readonly RootAyahMatchDto[]): readonly LinkingAyah[] {
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
        wordLocation: null,
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
