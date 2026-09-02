import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Observable, map, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { environment } from '../../../../environments/environment';
import { LinkingWorkspaceResponse } from '../../../core/api/generated/models/linking-workspace-response';
import { LinkingWorkspaceInitialConfigurationBody } from '../../../core/api/generated/models/linking-workspace-initial-configuration-body';
import { LinkingWorkspaceSourceResponse } from '../../../core/api/generated/models/linking-workspace-source-response';
import { ApiResponse } from '../../../core/data-access/api-response.model';
import { LinkingSourceLaunch } from '../models/linking-source-launch.models';
import { LinkingSourceDescriptor } from '../models/linking-source.models';
import {
  LinkingSelection,
  LinkingSourceConfiguration,
  LinkingWorkspaceItem,
  LinkingWorkspaceSnapshot,
} from '../models/linking-workspace.models';
import {
  fromLinkingSourceDescriptorBody,
  toLinkingSourceDescriptorBody,
} from '../utils/linking-source-descriptor-body';
import {
  LinkingWorkspaceRepository,
  LinkingWorkspaceStaleVersionError,
} from './linking-workspace.repository';
import { LinkingDataStaleError } from '../models/linking-revision.models';
import { parseQuranVerseKey, type QuranVerseKey } from '../../../shared/quran/quran-location';

@Injectable({ providedIn: 'root' })
export class HttpLinkingWorkspaceRepository implements LinkingWorkspaceRepository {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/linking/workspace`;

  load(): Observable<LinkingWorkspaceSnapshot> {
    return this.request(this.http.get<ApiResponse<LinkingWorkspaceResponse>>(this.baseUrl));
  }

  addSource(
    launch: LinkingSourceLaunch,
    workspaceVersion: number | null,
  ): Observable<LinkingWorkspaceSnapshot> {
    return this.request(
      this.http.post<ApiResponse<LinkingWorkspaceResponse>>(`${this.baseUrl}/sources`, {
        descriptor: toLinkingSourceDescriptorBody(launch.source),
        initialConfiguration: toInitialConfigurationBody(launch.initialConfiguration),
        workspaceVersion,
      }),
    );
  }

  removeSource(sourceId: number, workspaceVersion: number | null): Observable<LinkingWorkspaceSnapshot> {
    return this.request(
      this.http.delete<ApiResponse<LinkingWorkspaceResponse>>(`${this.baseUrl}/sources/${sourceId}`, {
        params: versionParams(workspaceVersion),
      }),
    );
  }

  updateSourceTypes(
    sourceId: number,
    typeCodes: readonly string[],
    sourceVersion: number,
    workspaceVersion: number | null,
  ): Observable<LinkingWorkspaceSnapshot> {
    return this.request(
      this.http.patch<ApiResponse<LinkingWorkspaceResponse>>(
        `${this.baseUrl}/sources/${sourceId}/types`,
        {
          typeCodes: [...typeCodes],
          sourceVersion,
          workspaceVersion,
        },
      ),
    );
  }

  reorderSources(
    sourceIds: readonly number[],
    workspaceVersion: number | null,
  ): Observable<LinkingWorkspaceSnapshot> {
    return this.request(
      this.http.put<ApiResponse<LinkingWorkspaceResponse>>(`${this.baseUrl}/sources/order`, {
        sourceIds: [...sourceIds],
        workspaceVersion,
      }),
    );
  }

  clearSources(workspaceVersion: number | null): Observable<LinkingWorkspaceSnapshot> {
    return this.request(
      this.http.delete<ApiResponse<LinkingWorkspaceResponse>>(`${this.baseUrl}/sources`, {
        params: versionParams(workspaceVersion),
      }),
    );
  }

  private request(
    response$: Observable<ApiResponse<LinkingWorkspaceResponse>>,
  ): Observable<LinkingWorkspaceSnapshot> {
    return response$.pipe(
      map((response) => toSnapshot(response)),
      catchError((error: unknown) => throwError(() => toWorkspaceError(error))),
    );
  }
}

function toInitialConfigurationBody(
  configuration: LinkingSourceLaunch['initialConfiguration'],
): LinkingWorkspaceInitialConfigurationBody | null {
  if (configuration === null) {
    return null;
  }
  return {
    inclusionMode: configuration.inclusionMode === 'all-except' ? 'all_except' : 'only',
    ayahOverrides: [...configuration.ayahOverrideIds],
    selectedWords: configuration.selectedWords.map((word) => ({ ...word })),
    automaticWordMatchesEnabled: configuration.automaticWordMatchesEnabled,
    manualLinkShape: configuration.manualLinkShape,
    descriptions: [],
  };
}

function versionParams(workspaceVersion: number | null): HttpParams {
  return workspaceVersion === null
    ? new HttpParams()
    : new HttpParams().set('workspaceVersion', workspaceVersion);
}

function toWorkspaceError(error: unknown): Error {
  if (error instanceof HttpErrorResponse) {
    const response = error.error as ApiResponse<{ code?: string }> | null;
    const message = response?.message;
    if (error.status === 409 && response?.data?.code === 'LINKING_DATA_STALE') {
      return new LinkingDataStaleError(message || 'تغيّرت بيانات الربط؛ أعد تحميل المصدر.');
    }
    if (error.status === 409) {
      return new LinkingWorkspaceStaleVersionError(message || 'تغيّرت مساحة الربط في مكان آخر.');
    }
    return new Error(message || 'تعذر حفظ مساحة الربط.');
  }
  return error instanceof Error ? error : new Error('تعذر حفظ مساحة الربط.');
}

function toSnapshot(response: ApiResponse<LinkingWorkspaceResponse>): LinkingWorkspaceSnapshot {
  const workspace = response.data;
  if (!response.isSuccess || !workspace) {
    throw new Error(response.message || 'تعذر تحميل مساحة الربط.');
  }
  const items = workspace.sources
    .slice()
    .sort((left, right) => left.orderValue - right.orderValue)
    .map(toWorkspaceItem)
    .filter((item): item is LinkingWorkspaceItem => item !== null);
  return { workspaceVersion: workspace.workspaceVersion, items };
}

function toWorkspaceItem(source: LinkingWorkspaceSourceResponse): LinkingWorkspaceItem | null {
  const manualAyahs = orderedManualAyahs(source);
  if (manualAyahs === null) {
    return null;
  }
  const descriptor = fromLinkingSourceDescriptorBody(
    source.descriptor,
    manualAyahs.map((ayah) => ayah.verseKey),
  );
  if (descriptor === null) {
    return null;
  }
  const ayahIdByVerseKey = Object.fromEntries(
    manualAyahs.map((ayah) => [ayah.verseKey, ayah.ayahId]),
  );
  const configuration = toConfiguration(source, descriptor, ayahIdByVerseKey);
  if (configuration === null) {
    return null;
  }
  return {
    sourceKey: source.sourceIdentity,
    sourceId: source.id,
    sourceVersion: source.sourceVersion,
    source: descriptor,
    configuration,
    configurationRevision: 0,
    linkingDataRevision: null,
    ayahOverrideIds: source.ayahOverrides,
    selectedWordIdsByAyahId: toWordIdsByAyahId(source),
    ayahIdByVerseKey,
    lastResolvedCount: source.lastResolvedCount,
  };
}

function toWordIdsByAyahId(
  source: LinkingWorkspaceSourceResponse,
): Readonly<Record<number, readonly number[]>> {
  const grouped: Record<number, number[]> = {};
  for (const selectedWord of source.selectedWords) {
    if (selectedWord.ayahId === null || selectedWord.quranWordId === null) {
      continue;
    }
    grouped[selectedWord.ayahId] = [...(grouped[selectedWord.ayahId] ?? []), selectedWord.quranWordId];
  }
  return Object.fromEntries(
    Object.entries(grouped).map(([ayahId, wordIds]) => [
      ayahId,
      [...new Set(wordIds)].sort((left, right) => left - right),
    ]),
  );
}

function toConfiguration(
  source: LinkingWorkspaceSourceResponse,
  descriptor: LinkingSourceDescriptor,
  ayahIdByVerseKey: Readonly<Record<string, number>>,
): LinkingSourceConfiguration | null {
  const ayahInclusion = toInclusion(source, ayahIdByVerseKey);
  if (ayahInclusion === null) {
    return null;
  }
  if (descriptor.kind === 'manual-mushaf-ayahs') {
    return {
      kind: 'manual',
      ayahInclusion,
      quranWordIdsByVerseKey: toWordIdsByVerseKey(source, ayahIdByVerseKey),
      linkShape: source.manualLinkShape === 'grouped' ? 'grouped' : 'independent',
    };
  }
  return {
    kind: 'automatic',
    ayahInclusion,
    automaticWordMatchesEnabled: source.automaticWordMatchesEnabled !== false,
  };
}

function toInclusion(
  source: LinkingWorkspaceSourceResponse,
  ayahIdByVerseKey: Readonly<Record<string, number>>,
): LinkingSelection | null {
  const mode = fromWireInclusionMode(source.inclusionMode);
  if (mode === null) {
    return null;
  }
  const verseKeyByAyahId = canonicalVerseKeyByAyahId(ayahIdByVerseKey);
  const verseKeys = source.ayahOverrides
    .map((ayahId) => verseKeyByAyahId.get(ayahId))
    .filter((verseKey): verseKey is QuranVerseKey => verseKey !== undefined);
  return { mode, verseKeys };
}

function toWordIdsByVerseKey(
  source: LinkingWorkspaceSourceResponse,
  ayahIdByVerseKey: Readonly<Record<string, number>>,
): Readonly<Record<string, readonly number[]>> {
  const verseKeyByAyahId = canonicalVerseKeyByAyahId(ayahIdByVerseKey);
  const wordIdsByVerseKey: Record<string, number[]> = {};
  for (const word of source.selectedWords) {
    const verseKey = word.ayahId === null ? undefined : verseKeyByAyahId.get(word.ayahId);
    if (verseKey === undefined || word.quranWordId === null) {
      continue;
    }
    (wordIdsByVerseKey[verseKey] ??= []).push(word.quranWordId);
  }
  for (const rawVerseKey of Object.keys(wordIdsByVerseKey)) {
    const parsed = parseQuranVerseKey(rawVerseKey);
    if (!parsed) {
      continue;
    }
    wordIdsByVerseKey[parsed.key] = [...new Set(wordIdsByVerseKey[parsed.key])].sort(
      (left, right) => left - right,
    );
  }
  return wordIdsByVerseKey;
}

function canonicalVerseKeyByAyahId(
  ayahIdByVerseKey: Readonly<Record<string, number>>,
): ReadonlyMap<number, QuranVerseKey> {
  return new Map(
    Object.entries(ayahIdByVerseKey).flatMap(([verseKey, ayahId]) => {
      const parsed = parseQuranVerseKey(verseKey);
      return parsed ? [[ayahId, parsed.key] as const] : [];
    }),
  );
}

function orderedManualAyahs(
  source: LinkingWorkspaceSourceResponse,
): readonly { ayahId: number; verseKey: QuranVerseKey }[] | null {
  const ordered = source.manualAyahs
    .slice()
    .sort((left, right) => left.orderValue - right.orderValue)
    .map((ayah) => {
      const parsed = parseQuranVerseKey(ayah.verseKey);
      return parsed && parsed.key === ayah.verseKey
        ? { ayahId: ayah.ayahId, verseKey: parsed.key }
        : null;
    });
  return ordered.some((ayah) => ayah === null)
    ? null
    : ordered.filter((ayah): ayah is NonNullable<typeof ayah> => ayah !== null);
}

function fromWireInclusionMode(mode: string): 'all-except' | 'only' | null {
  if (mode === 'all_except') {
    return 'all-except';
  }
  return mode === 'only' ? 'only' : null;
}
