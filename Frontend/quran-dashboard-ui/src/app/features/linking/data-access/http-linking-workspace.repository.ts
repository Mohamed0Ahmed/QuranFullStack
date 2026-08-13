import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Observable, map, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { environment } from '../../../../environments/environment';
import { LinkingWorkspaceResponse } from '../../../core/api/generated/models/linking-workspace-response';
import { LinkingWorkspaceSourceResponse } from '../../../core/api/generated/models/linking-workspace-source-response';
import { ApiResponse } from '../../../core/data-access/api-response.model';
import { LinkingManualMushafAyahReference } from '../models/linking-manual-mushaf.models';
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
  LinkingWorkspaceConfigurationRequest,
  LinkingWorkspaceRepository,
  LinkingWorkspaceStaleVersionError,
} from './linking-workspace.repository';

@Injectable({ providedIn: 'root' })
export class HttpLinkingWorkspaceRepository implements LinkingWorkspaceRepository {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/linking/workspace`;

  load(): Observable<LinkingWorkspaceSnapshot> {
    return this.request(this.http.get<ApiResponse<LinkingWorkspaceResponse>>(this.baseUrl));
  }

  addSource(
    descriptor: LinkingSourceDescriptor,
    workspaceVersion: number | null,
  ): Observable<LinkingWorkspaceSnapshot> {
    return this.request(
      this.http.post<ApiResponse<LinkingWorkspaceResponse>>(`${this.baseUrl}/sources`, {
        descriptor: toLinkingSourceDescriptorBody(descriptor),
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

  replaceConfiguration(
    sourceId: number,
    configuration: LinkingWorkspaceConfigurationRequest,
  ): Observable<LinkingWorkspaceSnapshot> {
    return this.request(
      this.http.put<ApiResponse<LinkingWorkspaceResponse>>(
        `${this.baseUrl}/sources/${sourceId}/configuration`,
        {
          sourceVersion: configuration.sourceVersion,
          label: configuration.label,
          inclusionMode: toWireInclusionMode(configuration.inclusionMode),
          ayahOverrides: [...configuration.ayahOverrideIds],
          selectedWords: configuration.selectedWords.map((word) => ({ ...word })),
          automaticWordMatchesEnabled: configuration.automaticWordMatchesEnabled,
          manualLinkShape: configuration.manualLinkShape,
          descriptions: configuration.descriptions.map((description) => ({ ...description })),
        },
      ),
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

function versionParams(workspaceVersion: number | null): HttpParams {
  return workspaceVersion === null
    ? new HttpParams()
    : new HttpParams().set('workspaceVersion', workspaceVersion);
}

function toWorkspaceError(error: unknown): Error {
  if (error instanceof HttpErrorResponse) {
    const message = (error.error as ApiResponse<unknown> | null)?.message;
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
  const descriptor = fromLinkingSourceDescriptorBody(
    source.descriptor,
    manualAyahs.map(toManualReference),
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
    ayahOverrideIds: source.ayahOverrides,
    ayahIdByVerseKey,
    descriptions: source.descriptions.map((description) => ({
      ayahId: description.ayahId,
      orderValue: description.orderValue,
      body: description.body,
    })),
    lastResolvedCount: source.lastResolvedCount,
  };
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
  const verseKeyByAyahId = new Map(
    Object.entries(ayahIdByVerseKey).map(([verseKey, ayahId]) => [ayahId, verseKey]),
  );
  const verseKeys = source.ayahOverrides
    .map((ayahId) => verseKeyByAyahId.get(ayahId))
    .filter((verseKey): verseKey is string => verseKey !== undefined);
  return { mode, verseKeys };
}

function toWordIdsByVerseKey(
  source: LinkingWorkspaceSourceResponse,
  ayahIdByVerseKey: Readonly<Record<string, number>>,
): Readonly<Record<string, readonly number[]>> {
  const verseKeyByAyahId = new Map(
    Object.entries(ayahIdByVerseKey).map(([verseKey, ayahId]) => [ayahId, verseKey]),
  );
  const wordIdsByVerseKey: Record<string, number[]> = {};
  for (const word of source.selectedWords) {
    const verseKey = word.ayahId === null ? undefined : verseKeyByAyahId.get(word.ayahId);
    if (verseKey === undefined || word.quranWordId === null) {
      continue;
    }
    (wordIdsByVerseKey[verseKey] ??= []).push(word.quranWordId);
  }
  for (const verseKey of Object.keys(wordIdsByVerseKey)) {
    wordIdsByVerseKey[verseKey] = [...new Set(wordIdsByVerseKey[verseKey])].sort(
      (left, right) => left - right,
    );
  }
  return wordIdsByVerseKey;
}

function orderedManualAyahs(
  source: LinkingWorkspaceSourceResponse,
): readonly { ayahId: number; verseKey: string; pageHint: number | null }[] {
  return source.manualAyahs
    .slice()
    .sort((left, right) => left.orderValue - right.orderValue)
    .map((ayah) => ({ ayahId: ayah.ayahId, verseKey: ayah.verseKey, pageHint: ayah.pageHint }));
}

function toManualReference(ayah: {
  verseKey: string;
  pageHint: number | null;
}): LinkingManualMushafAyahReference {
  return { verseKey: ayah.verseKey, pageNumber: ayah.pageHint, displayHint: ayah.verseKey };
}

function toWireInclusionMode(mode: 'all-except' | 'only'): string {
  return mode === 'all-except' ? 'all_except' : 'only';
}

function fromWireInclusionMode(mode: string): 'all-except' | 'only' | null {
  if (mode === 'all_except') {
    return 'all-except';
  }
  return mode === 'only' ? 'only' : null;
}
