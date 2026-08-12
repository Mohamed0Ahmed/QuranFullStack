import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { MushafAyahStudyApi } from '../../mushaf/data-access/mushaf-ayah-study.api';
import { AyahCoreDto } from '../../mushaf/models/mushaf.models';
import { MushafReaderCache, MushafReaderCacheKeys } from '../../mushaf/state/mushaf-reader-cache';
import { LinkingManualMushafAyahReference } from '../models/linking-manual-mushaf.models';

const METADATA_READ_SOURCES = {
  tafsirSource: null,
  translationSource: null,
  fullI3rabSource: null,
} as const;

@Injectable({ providedIn: 'root' })
export class LinkingManualAyahMetadataReader {
  private readonly ayahStudyApi = inject(MushafAyahStudyApi);
  private readonly cache = inject(MushafReaderCache);

  readMetadata(verseKey: string): Observable<LinkingManualMushafAyahReference> {
    return this.cache
      .getOrLoad(MushafReaderCacheKeys.ayahStudy(verseKey, METADATA_READ_SOURCES), () =>
        this.ayahStudyApi.getAyahStudy(verseKey, METADATA_READ_SOURCES),
      )
      .pipe(map((response) => toManualReference(validateCoreResponse(response, verseKey))));
  }
}

function validateCoreResponse(response: ApiResponse<{ ayah: AyahCoreDto }>, verseKey: string): AyahCoreDto {
  const core = response.data?.ayah;
  if (!response.isSuccess || !core || core.verseKey !== verseKey) {
    throw new Error(response.message || 'تعذر تحميل بيانات آية المصحف.');
  }
  return core;
}

function toManualReference(ayah: AyahCoreDto): LinkingManualMushafAyahReference {
  return {
    verseKey: ayah.verseKey,
    pageNumber: ayah.pageFrom,
    displayHint: ayah.verseKey,
  };
}
