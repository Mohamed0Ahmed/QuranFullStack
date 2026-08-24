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

export interface LinkingManualAyahMetadata {
  readonly reference: LinkingManualMushafAyahReference;
  readonly surahNameArabic: string;
  readonly ayahNumber: number;
  readonly textUthmani: string;
  readonly ayahMarkerText: string;
}

@Injectable({ providedIn: 'root' })
export class LinkingManualAyahMetadataReader {
  private readonly ayahStudyApi = inject(MushafAyahStudyApi);
  private readonly cache = inject(MushafReaderCache);

  readMetadata(verseKey: string): Observable<LinkingManualAyahMetadata> {
    return this.cache
      .getOrLoad(MushafReaderCacheKeys.ayahStudy(verseKey, METADATA_READ_SOURCES), () =>
        this.ayahStudyApi.getAyahStudy(verseKey, METADATA_READ_SOURCES),
      )
      .pipe(map((response) => toManualMetadata(validateCoreResponse(response, verseKey))));
  }
}

function validateCoreResponse(response: ApiResponse<{ ayah: AyahCoreDto }>, verseKey: string): AyahCoreDto {
  const core = response.data?.ayah;
  if (!response.isSuccess || !core || core.verseKey !== verseKey) {
    throw new Error(response.message || 'تعذر تحميل بيانات آية المصحف.');
  }
  return core;
}

function toManualMetadata(ayah: AyahCoreDto): LinkingManualAyahMetadata {
  const display = splitAyahMarker(ayah.textUthmani, ayah.ayahNumber);
  return {
    reference: {
      verseKey: ayah.verseKey,
      pageNumber: ayah.pageFrom,
      displayHint: ayah.verseKey,
    },
    surahNameArabic: ayah.surahNameArabic,
    ayahNumber: ayah.ayahNumber,
    textUthmani: display.text,
    ayahMarkerText: display.marker,
  };
}

function splitAyahMarker(textUthmani: string, ayahNumber: number): { text: string; marker: string } {
  const match = /\s+([0-9٠-٩۰-۹]+)\s*$/u.exec(textUthmani);
  const marker = match?.[1] ?? toArabicIndicDigits(ayahNumber);
  if (match === null || readLocalizedNumber(marker) !== ayahNumber) {
    return { text: textUthmani, marker: toArabicIndicDigits(ayahNumber) };
  }
  return { text: textUthmani.slice(0, match.index).trimEnd(), marker };
}

function readLocalizedNumber(value: string): number {
  const normalized = value
    .replace(/[٠-٩]/gu, (digit) => String(digit.charCodeAt(0) - 0x0660))
    .replace(/[۰-۹]/gu, (digit) => String(digit.charCodeAt(0) - 0x06f0));
  return Number(normalized);
}

function toArabicIndicDigits(value: number): string {
  return String(value).replace(/[0-9]/gu, (digit) => String.fromCharCode(0x0660 + Number(digit)));
}
