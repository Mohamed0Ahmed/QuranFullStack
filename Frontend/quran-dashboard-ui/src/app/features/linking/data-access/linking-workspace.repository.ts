import { Observable } from 'rxjs';

import { LinkingSourceDescriptor } from '../models/linking-source.models';
import { LinkingWorkspaceSnapshot } from '../models/linking-workspace.models';

export interface LinkingWorkspaceConfigurationRequest {
  sourceVersion: number;
  label: string;
  inclusionMode: 'all-except' | 'only';
  ayahOverrideIds: readonly number[];
  selectedWords: readonly { ayahId: number; quranWordId: number }[];
  automaticWordMatchesEnabled: boolean | null;
  manualLinkShape: 'grouped' | 'independent' | null;
  descriptions: readonly { ayahId: number; orderValue: number; body: string }[];
}

export interface LinkingWorkspaceRepository {
  load(): Observable<LinkingWorkspaceSnapshot>;
  addSource(
    descriptor: LinkingSourceDescriptor,
    workspaceVersion: number | null,
  ): Observable<LinkingWorkspaceSnapshot>;
  removeSource(sourceId: number, workspaceVersion: number | null): Observable<LinkingWorkspaceSnapshot>;
  reorderSources(
    sourceIds: readonly number[],
    workspaceVersion: number | null,
  ): Observable<LinkingWorkspaceSnapshot>;
  replaceConfiguration(
    sourceId: number,
    configuration: LinkingWorkspaceConfigurationRequest,
  ): Observable<LinkingWorkspaceSnapshot>;
  clearSources(workspaceVersion: number | null): Observable<LinkingWorkspaceSnapshot>;
}

export class LinkingWorkspaceStaleVersionError extends Error {
  constructor(message: string) {
    super(message);
  }
}
