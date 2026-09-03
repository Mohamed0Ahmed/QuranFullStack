import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

import { environment } from '../../../../environments/environment';
import type { MushafPageResponse as MushafPageWireDto } from '../../../core/api/generated/models';
import { ApiResponse } from '../../../core/data-access/api-response.model';
import { MushafLineDto, MushafPageDto, MushafWordDto, PageMarkerDto } from '../models/mushaf.models';
import {
  parseQuranVerseKey,
  parseQuranWordLocation,
  quranVerseKeyFromWordLocation,
} from '../../../shared/quran/quran-location';

@Injectable({ providedIn: 'root' })
export class MushafPagesApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  getPage(pageNumber: number): Observable<ApiResponse<MushafPageDto>> {
    return this.http
      .get<ApiResponse<MushafPageWireDto>>(`${this.baseUrl}/api/mushaf/pages/${pageNumber}`)
      .pipe(map(attestMushafPageResponse));
  }
}

function attestMushafPageResponse(response: ApiResponse<MushafPageWireDto>): ApiResponse<MushafPageDto> {
  if (!response.isSuccess || response.data == null) {
    return { ...response, data: null };
  }

  const lines = response.data.lines.map(attestLine);
  const markers = response.data.markers.map(attestMarker);
  if (lines.some((line) => line === null) || markers.some((marker) => marker === null)) {
    return {
      isSuccess: false,
      message: 'بيانات مواضع المصحف غير صالحة',
      data: null,
    };
  }

  return {
    ...response,
    data: {
      ...response.data,
      lines: lines.filter((line): line is MushafLineDto => line !== null),
      markers: markers.filter((marker): marker is PageMarkerDto => marker !== null),
    },
  };
}

function attestLine(line: MushafPageWireDto['lines'][number]): MushafLineDto | null {
  if (line.lineType !== 'ayah' && line.lineType !== 'surah_name' && line.lineType !== 'basmallah') {
    return null;
  }

  const words = line.words.map(attestWord);
  if (words.some((word) => word === null)) {
    return null;
  }

  return {
    ...line,
    lineType: line.lineType,
    words: words.filter((word): word is MushafWordDto => word !== null),
  };
}

function attestWord(word: MushafPageWireDto['lines'][number]['words'][number]): MushafWordDto | null {
  const verse = parseQuranVerseKey(word.verseKey);
  const location = parseQuranWordLocation(word.wordLocation);
  if (!verse || !location || verse.key !== quranVerseKeyFromWordLocation(location.location)) {
    return null;
  }

  return { ...word, verseKey: verse.key, wordLocation: location.location };
}

function attestMarker(marker: MushafPageWireDto['markers'][number]): PageMarkerDto | null {
  if (!isMarkerType(marker.markerType)) {
    return null;
  }
  const verse = parseQuranVerseKey(marker.verseKey);
  const location = parseQuranWordLocation(marker.wordLocation);
  if (!verse || !location || verse.key !== quranVerseKeyFromWordLocation(location.location)) {
    return null;
  }

  return {
    ...marker,
    markerType: marker.markerType,
    verseKey: verse.key,
    wordLocation: location.location,
  };
}

function isMarkerType(value: string): value is PageMarkerDto['markerType'] {
  return value === 'juz' || value === 'hizb' || value === 'rub' || value === 'sajda';
}
