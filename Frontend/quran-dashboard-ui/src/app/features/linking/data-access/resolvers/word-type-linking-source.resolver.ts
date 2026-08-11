import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';

import { WordTypesApi } from '../../../words/data-access/word-types.api';
import {
  WORD_TYPES_DETAIL_PAGE_SIZE,
  WordTypeAyahMatchDto,
} from '../../../words/models/word-types.models';
import { WordTypeGroupedRequestParams } from '../../../words/models/word-types-detail.models';
import { LinkingAyah } from '../../models/linking-ayah.models';
import { LinkingSourceDescriptor } from '../../models/linking-source.models';
import { loadCompletePagedSource } from '../complete-paged-source.loader';

@Injectable({ providedIn: 'root' })
export class WordTypeLinkingSourceResolver {
  private readonly wordTypesApi = inject(WordTypesApi);

  resolve(
    source: Extract<LinkingSourceDescriptor, { kind: 'word-type' }>,
    onProgress: (progress: { loaded: number; total: number }) => void,
  ): Observable<readonly LinkingAyah[]> {
    return loadCompletePagedSource(
      (page) => this.loadPage(source, page),
      onProgress,
    ).pipe(map((matches) => mapWordTypeMatches(matches)));
  }

  private loadPage(
    source: Extract<LinkingSourceDescriptor, { kind: 'word-type' }>,
    page: number,
  ) {
    const { selection } = source;
    if (selection.kind === 'word') {
      return this.wordTypesApi.getAyahMatches(
        {
          tashkeelWordId: selection.tashkeelWordId,
          contextCode: selection.contextCode,
          case: selection.case,
          tense: selection.tense,
          voice: selection.voice,
        },
        page,
        WORD_TYPES_DETAIL_PAGE_SIZE,
      );
    }
    return this.wordTypesApi.getGroupedAyahMatches(
      toGroupedRequest(selection),
      page,
      WORD_TYPES_DETAIL_PAGE_SIZE,
    );
  }
}

function toGroupedRequest(
  selection: Exclude<Extract<LinkingSourceDescriptor, { kind: 'word-type' }>['selection'], { kind: 'word' }>,
): WordTypeGroupedRequestParams {
  const dimensionId = selection.kind === 'root'
    ? selection.rootId
    : selection.kind === 'stem'
      ? selection.stemId
      : selection.lemmaId;
  return { ...selection.scope, kind: selection.kind, dimensionId };
}

function mapWordTypeMatches(matches: readonly WordTypeAyahMatchDto[]): readonly LinkingAyah[] {
  const byVerseKey = new Map<string, LinkingAyah>();
  for (const match of matches) {
    const matchedWordIds = new Set(match.matchedWordIds);
    const mapped: LinkingAyah = {
      verseKey: match.verseKey,
      ayahId: null,
      surahNumber: match.surahNumber,
      surahNameArabic: null,
      ayahNumber: match.ayahNumber,
      pageNumber: match.pageNumber,
      words: match.words.map((word, renderPosition) => ({
        renderPosition,
        canonicalQuranWordId: word.quranWordId,
        wordLocation: null,
        textUthmani: word.textUthmani,
        isAyahMarker: word.isAyahMarker,
        isSourceMatch: matchedWordIds.has(word.quranWordId),
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
