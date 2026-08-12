import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { LinkingAyahPreflightDto } from '../../../core/api/generated/models/linking-ayah-preflight-dto';
import { LinkingPreflightResultDto } from '../../../core/api/generated/models/linking-preflight-result-dto';
import { LinkingSourcePreflightDto } from '../../../core/api/generated/models/linking-source-preflight-dto';
import { ApiResponse } from '../../../core/data-access/api-response.model';
import { LinkingSourceIntent } from '../models/linking-merge.models';
import {
  LinkingAyahClassification,
  LinkingAyahPreflight,
  LinkingPreflightResult,
  LinkingSourceClassification,
  LinkingSourcePreflight,
} from '../models/linking-preflight.models';
import { toPreflightSourceBodies } from './linking-operation-request';

const SOURCE_CLASSIFICATIONS: readonly LinkingSourceClassification[] = [
  'NEW_SOURCE',
  'UNCHANGED',
  'UPDATE',
  'INVALID',
];
const AYAH_CLASSIFICATIONS: readonly LinkingAyahClassification[] = [
  'NEW_AYAH',
  'OVERLAP_OTHER_SOURCE',
  'UNCHANGED',
  'UPDATE',
  'REMOVE',
  'INVALID',
];

@Injectable({ providedIn: 'root' })
export class LinkingPreflightApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  preflight(
    doorId: number,
    sourceIntents: readonly LinkingSourceIntent[],
  ): Observable<LinkingPreflightResult> {
    return this.http
      .post<ApiResponse<LinkingPreflightResultDto>>(`${this.baseUrl}/api/linking/operations/preflight`, {
        doorId,
        sources: toPreflightSourceBodies(sourceIntents),
      })
      .pipe(map((response) => toPreflightResult(response)));
  }
}

function toPreflightResult(response: ApiResponse<LinkingPreflightResultDto>): LinkingPreflightResult {
  const result = response.data;
  if (!response.isSuccess || !result) {
    throw new Error(response.message || 'تعذر تحضير مراجعة الربط.');
  }
  return {
    doorId: result.doorId,
    doorName: result.doorName,
    isNoOp: result.isNoOp,
    isBlocked: result.isBlocked,
    preflightToken: result.preflightToken,
    totals: result.totals,
    sources: result.sources.map(toSourcePreflight),
  };
}

function toSourcePreflight(source: LinkingSourcePreflightDto): LinkingSourcePreflight {
  return {
    sourceIdentity: source.sourceIdentity,
    label: source.label,
    sourceKind: source.sourceKind,
    contributionMode: source.contributionMode,
    classification: toSourceClassification(source.classification),
    automaticWordMatchesEnabled: source.automaticWordMatchesEnabled,
    existingContributionId: source.existingContributionId,
    existingContributionVersion: source.existingContributionVersion,
    counts: source.counts,
    ayahs: source.ayahs.map(toAyahPreflight),
  };
}

function toAyahPreflight(ayah: LinkingAyahPreflightDto): LinkingAyahPreflight {
  return {
    ayahId: ayah.ayahId,
    verseKey: ayah.verseKey,
    surahNumber: ayah.surahNumber,
    ayahNumber: ayah.ayahNumber,
    classification: toAyahClassification(ayah.classification),
    overlappingSources: ayah.overlappingSources,
    wordChanges: ayah.wordChanges,
    descriptionChanges: ayah.descriptionChanges,
    invalidReason: ayah.invalidReason,
  };
}

function toSourceClassification(classification: string): LinkingSourceClassification {
  const known = SOURCE_CLASSIFICATIONS.find((candidate) => candidate === classification);
  if (known === undefined) {
    throw new Error('تصنيف مصدر غير معروف في نتيجة المراجعة.');
  }
  return known;
}

function toAyahClassification(classification: string): LinkingAyahClassification {
  const known = AYAH_CLASSIFICATIONS.find((candidate) => candidate === classification);
  if (known === undefined) {
    throw new Error('تصنيف آية غير معروف في نتيجة المراجعة.');
  }
  return known;
}
