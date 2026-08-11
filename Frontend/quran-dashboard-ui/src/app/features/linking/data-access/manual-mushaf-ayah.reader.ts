import { Injectable, inject } from '@angular/core';
import { Observable, from, map, mergeMap, switchMap, throwError, toArray } from 'rxjs';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { MushafPagesApi } from '../../mushaf/data-access/mushaf-pages.api';
import { MushafAyahStudyApi } from '../../mushaf/data-access/mushaf-ayah-study.api';
import { AyahCoreDto, MushafPageDto } from '../../mushaf/models/mushaf.models';
import { MushafReaderCache, MushafReaderCacheKeys } from '../../mushaf/state/mushaf-reader-cache';
import { LinkingAyah, LinkingAyahWord } from '../models/linking-ayah.models';
import { LinkingManualMushafAyahReference } from '../models/linking-manual-mushaf.models';
import { completeManualMushafAyah } from '../utils/manual-mushaf-ayah-completeness';

const MANUAL_READ_SOURCES = {
  tafsirSource: null,
  translationSource: null,
  fullI3rabSource: null,
} as const;
const PAGE_READ_CONCURRENCY = 3;

@Injectable({ providedIn: 'root' })
export class ManualMushafAyahReader {
  private readonly ayahStudyApi = inject(MushafAyahStudyApi);
  private readonly pagesApi = inject(MushafPagesApi);
  private readonly cache = inject(MushafReaderCache);

  readMetadata(verseKey: string): Observable<LinkingManualMushafAyahReference> {
    return this.readCore(verseKey).pipe(map((ayah) => toManualReference(ayah)));
  }

  readCompleteAyah(verseKey: string): Observable<LinkingAyah> {
    return this.readCore(verseKey).pipe(
      switchMap((core) => this.readPages(core).pipe(map((pages) => toCompleteAyah(core, pages)))),
    );
  }

  readCompleteAyahs(verseKeys: readonly string[]): Observable<readonly LinkingAyah[]> {
    return from(verseKeys).pipe(
      mergeMap((verseKey) => this.readCompleteAyah(verseKey), PAGE_READ_CONCURRENCY),
      toArray(),
    );
  }

  private readCore(verseKey: string): Observable<AyahCoreDto> {
    return this.cache.getOrLoad(MushafReaderCacheKeys.ayahStudy(verseKey, MANUAL_READ_SOURCES), () =>
      this.ayahStudyApi.getAyahStudy(verseKey, MANUAL_READ_SOURCES),
    ).pipe(map((response) => validateCoreResponse(response, verseKey)));
  }

  private readPages(core: AyahCoreDto): Observable<readonly MushafPageDto[]> {
    const pageNumbers = Array.from({ length: core.pageTo - core.pageFrom + 1 }, (_, index) => core.pageFrom + index);
    if (pageNumbers.length === 0 || pageNumbers.some((pageNumber) => pageNumber < 1 || pageNumber > 604)) {
      return throwError(() => new Error('نطاق صفحات آية المصحف غير صالح.'));
    }
    return pageNumbers.length === 1
      ? this.readPage(pageNumbers[0]).pipe(map((page) => [page]))
      : new Observable<number>((subscriber) => {
          pageNumbers.forEach((pageNumber) => subscriber.next(pageNumber));
          subscriber.complete();
        }).pipe(
          mergeMap((pageNumber) => this.readPage(pageNumber), PAGE_READ_CONCURRENCY),
          toArray(),
          map((pages) => pages.sort((left, right) => left.pageNumber - right.pageNumber)),
        );
  }

  private readPage(pageNumber: number): Observable<MushafPageDto> {
    return this.cache.getOrLoad(MushafReaderCacheKeys.page(pageNumber), () => this.pagesApi.getPage(pageNumber)).pipe(
      map((response) => validatePageResponse(response, pageNumber)),
    );
  }
}

function validateCoreResponse(response: ApiResponse<{ ayah: AyahCoreDto }>, verseKey: string): AyahCoreDto {
  const core = response.data?.ayah;
  if (!response.isSuccess || !core || core.verseKey !== verseKey) {
    throw new Error(response.message || 'تعذر تحميل بيانات آية المصحف.');
  }
  return core;
}

function validatePageResponse(response: ApiResponse<MushafPageDto>, pageNumber: number): MushafPageDto {
  const page = response.data;
  if (!response.isSuccess || !page || page.pageNumber !== pageNumber) {
    throw new Error(response.message || 'تعذر تحميل صفحة المصحف كاملة.');
  }
  return page;
}

function toManualReference(ayah: AyahCoreDto): LinkingManualMushafAyahReference {
  return {
    verseKey: ayah.verseKey,
    pageNumber: ayah.pageFrom,
    displayHint: ayah.verseKey,
  };
}

function toCompleteAyah(core: AyahCoreDto, pages: readonly MushafPageDto[]): LinkingAyah {
  const words = pages.flatMap((page) =>
    page.lines.flatMap((line) =>
      line.words
        .filter((word) => word.verseKey === core.verseKey)
        .map((word) => ({ pageNumber: page.pageNumber, lineNumber: line.lineNumber, word })),
    ),
  ).sort((left, right) =>
    left.pageNumber - right.pageNumber || left.lineNumber - right.lineNumber || left.word.lineWordOrder - right.word.lineWordOrder,
  );
  const nonMarkers = words.filter(({ word }) => !word.isAyahMarker);
  if (
    nonMarkers.some(({ word }) => !word.wordLocation || word.verseKey !== core.verseKey) ||
    !hasContiguousWordNumbers(nonMarkers.map(({ word }) => word.wordNumber), core.wordsCount)
  ) {
    throw new Error('تعذر التحقق من تسلسل كلمات آية المصحف.');
  }
  const manualWords: LinkingAyahWord[] = words.map(({ word }) => ({
    renderPosition: word.isAyahMarker ? 0 : word.wordNumber,
    canonicalQuranWordId: null,
    wordLocation: word.wordLocation,
    textUthmani: word.textUthmani,
    isAyahMarker: word.isAyahMarker,
    isSourceMatch: false,
  }));
  return completeManualMushafAyah(
    {
      verseKey: core.verseKey,
      ayahId: null,
      surahNumber: core.surahNumber,
      surahNameArabic: core.surahNameArabic,
      ayahNumber: core.ayahNumber,
      pageNumber: core.pageFrom,
    },
    core.wordsCount,
    manualWords,
  );
}

function hasContiguousWordNumbers(wordNumbers: readonly number[], expectedCount: number): boolean {
  return wordNumbers.length === expectedCount && wordNumbers.every((wordNumber, index) => wordNumber === index + 1);
}
