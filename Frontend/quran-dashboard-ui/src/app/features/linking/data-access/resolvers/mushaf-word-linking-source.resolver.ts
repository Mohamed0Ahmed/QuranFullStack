import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';

import { MushafPagesApi } from '../../../mushaf/data-access/mushaf-pages.api';
import { LinkingAyah } from '../../models/linking-ayah.models';
import { LinkingSourceDescriptor } from '../../models/linking-source.models';

@Injectable({ providedIn: 'root' })
export class MushafWordLinkingSourceResolver {
  private readonly pagesApi = inject(MushafPagesApi);

  resolve(
    source: Extract<LinkingSourceDescriptor, { kind: 'mushaf-word' }>,
    onProgress: (progress: { loaded: number; total: number }) => void,
  ): Observable<readonly LinkingAyah[]> {
    return this.pagesApi.getPage(source.pageNumber).pipe(
      map((response) => {
        if (!response.isSuccess || !response.data || response.data.pageNumber !== source.pageNumber) {
          throw new Error('تعذر تحميل صفحة المصحف الخاصة بالكلمة المحددة.');
        }
        const ayah = mapMushafWordAyah(response.data.lines.flatMap((line) => line.words), source);
        onProgress({ loaded: 1, total: 1 });
        return [ayah];
      }),
    );
  }
}

function mapMushafWordAyah(
  words: readonly {
    isAyahMarker: boolean;
    textUthmani: string;
    verseKey: string;
    wordLocation: string;
  }[],
  source: Extract<LinkingSourceDescriptor, { kind: 'mushaf-word' }>,
): LinkingAyah {
  const ayahWords = words.filter((word) => word.verseKey === source.verseKey);
  if (!ayahWords.some((word) => word.wordLocation === source.wordLocation)) {
    throw new Error('لم تعد الكلمة المحددة موجودة في صفحة المصحف.');
  }

  const [surahNumber, ayahNumber] = source.verseKey.split(':').map(Number);
  if (!Number.isSafeInteger(surahNumber) || !Number.isSafeInteger(ayahNumber)) {
    throw new Error('تعذر تحديد الآية الخاصة بالكلمة المحددة.');
  }

  return {
    verseKey: source.verseKey,
    ayahId: null,
    surahNumber,
    surahNameArabic: null,
    ayahNumber,
    pageNumber: source.pageNumber,
    words: ayahWords.map((word, renderPosition) => ({
      renderPosition,
      canonicalQuranWordId: word.wordLocation === source.wordLocation ? source.quranWordId : null,
      textUthmani: word.textUthmani,
      isAyahMarker: word.isAyahMarker,
      isSourceMatch: word.wordLocation === source.wordLocation,
    })),
  };
}
