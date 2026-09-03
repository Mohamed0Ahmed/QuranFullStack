import { MushafPagesApi } from '../data-access/mushaf-pages.api';
import { MushafPageViewModel } from '../models/mushaf.models';
import { MushafReaderCache, MushafReaderCacheKeys } from './mushaf-reader-cache';
import type { QuranWordLocation } from '../../../shared/quran/quran-location';

export function prefetchAdjacentMushafPages(
  previousPageNumber: number | null,
  nextPageNumber: number | null,
  readerCache: MushafReaderCache,
  pagesApi: MushafPagesApi,
): void {
  if (previousPageNumber !== null) {
    readerCache.prefetch(MushafReaderCacheKeys.page(previousPageNumber), () =>
      pagesApi.getPage(previousPageNumber),
    );
  }

  if (nextPageNumber !== null) {
    readerCache.prefetch(MushafReaderCacheKeys.page(nextPageNumber), () =>
      pagesApi.getPage(nextPageNumber),
    );
  }
}

export function isAyahMarkerOnMushafPage(
  page: MushafPageViewModel | null,
  wordLocation: QuranWordLocation,
): boolean {
  if (!page) {
    return false;
  }

  for (const line of page.lines) {
    const word = line.words.find((candidate) => candidate.wordLocation === wordLocation);
    if (word) {
      return word.isAyahMarker;
    }
  }

  return false;
}
