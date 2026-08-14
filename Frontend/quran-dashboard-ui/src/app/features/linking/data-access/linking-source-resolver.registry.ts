import { Injectable, inject } from '@angular/core';
import { Observable, map, tap } from 'rxjs';

import { LinkingResolvedAyahDto } from '../../../core/api/generated/models/linking-resolved-ayah-dto';
import { LinkingResolvedSourceDto } from '../../../core/api/generated/models/linking-resolved-source-dto';
import { ApiResponse } from '../../../core/data-access/api-response.model';
import { LinkingAyah } from '../models/linking-ayah.models';
import { LinkingSourceDescriptor } from '../models/linking-source.models';
import { LinkingResolvedSourceRevision } from '../models/linking-revision.models';
import { LinkingSourceCache, LinkingSourceCacheKeys } from '../state/linking-source.cache';
import { linkingSourceKey } from '../utils/linking-source-key';
import { LinkingSourceResolutionApi } from './linking-source-resolution.api';
import type { LinkingSourceResolveProgress } from './linking-source-resolver';

@Injectable({ providedIn: 'root' })
export class LinkingSourceResolverRegistry {
  private readonly api = inject(LinkingSourceResolutionApi);
  private readonly cache = inject(LinkingSourceCache);

  resolve(
    source: LinkingSourceDescriptor,
    onProgress: (progress: LinkingSourceResolveProgress) => void,
  ): Observable<readonly LinkingAyah[]> {
    return this.resolveRevisioned(source, onProgress).pipe(map((resolved) => resolved.ayahs));
  }

  resolveRevisioned(
    source: LinkingSourceDescriptor,
    onProgress: (progress: LinkingSourceResolveProgress) => void,
  ): Observable<LinkingResolvedSourceRevision> {
    const sourceIdentity = linkingSourceKey(source);
    return this.cache
      .getOrLoadSource(sourceIdentity, () =>
        this.api
          .resolveSource(source)
          .pipe(tap((response) => validateResolvedSource(response, sourceIdentity))),
      )
      .pipe(
        map((response) => {
          const resolved = validateResolvedSource(response, sourceIdentity);
          const ayahs = toLinkingAyahs(resolved);
          onProgress({ loaded: ayahs.length, total: ayahs.length });
          return { ayahs, linkingDataRevision: resolved.linkingDataRevision };
        }),
      );
  }
}

function validateResolvedSource(
  response: ApiResponse<LinkingResolvedSourceDto>,
  sourceIdentity: string,
): LinkingResolvedSourceDto {
  const resolved = response.data;
  if (!response.isSuccess || !resolved) {
    throw new Error(response.message || 'تعذر تحميل نتائج المصدر كاملة.');
  }
  if (resolved.sourceIdentity !== sourceIdentity) {
    throw new Error('هوية المصدر المعادة لا تطابق المصدر المطلوب.');
  }
  if (resolved.totalAyahCount !== resolved.ayahs.length) {
    throw new Error('بيانات نتائج المصدر غير مكتملة.');
  }
  if (resolved.ayahs.some(hasRepeatedQuranWordId)) {
    throw new Error('تكررت معرّفات كلمات القرآن في نتائج المصدر.');
  }
  return resolved;
}

function hasRepeatedQuranWordId(ayah: LinkingResolvedAyahDto): boolean {
  return new Set(ayah.words.map((word) => word.quranWordId)).size !== ayah.words.length;
}

function toLinkingAyahs(resolved: LinkingResolvedSourceDto): readonly LinkingAyah[] {
  return resolved.ayahs.map((ayah) => {
    const matchedQuranWordIds = new Set(ayah.matchedQuranWordIds);
    return {
      verseKey: ayah.verseKey,
      ayahId: ayah.ayahId,
      surahNumber: ayah.surahNumber,
      surahNameArabic: ayah.surahNameArabic,
      ayahNumber: ayah.ayahNumber,
      pageNumber: ayah.pageFrom,
      words: ayah.words.map((word) => ({
        renderPosition: word.wordNumber,
        canonicalQuranWordId: word.quranWordId,
        textUthmani: word.textUthmani,
        isAyahMarker: word.isAyahMarker,
        isSourceMatch: matchedQuranWordIds.has(word.quranWordId),
      })),
    };
  });
}
