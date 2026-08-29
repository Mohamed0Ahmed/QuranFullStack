import { Observable } from 'rxjs';

import { LinkingSourceLaunch } from '../models/linking-source-launch.models';
import { LinkingWorkspaceSnapshot } from '../models/linking-workspace.models';
export { LinkingDataStaleError } from '../models/linking-revision.models';

export interface LinkingWorkspaceRepository {
  load(): Observable<LinkingWorkspaceSnapshot>;
  addSource(
    launch: LinkingSourceLaunch,
    workspaceVersion: number | null,
  ): Observable<LinkingWorkspaceSnapshot>;
  removeSource(sourceId: number, workspaceVersion: number | null): Observable<LinkingWorkspaceSnapshot>;
  updateSourceTypes(
    sourceId: number,
    typeCodes: readonly string[],
    sourceVersion: number,
    workspaceVersion: number | null,
  ): Observable<LinkingWorkspaceSnapshot>;
  reorderSources(
    sourceIds: readonly number[],
    workspaceVersion: number | null,
  ): Observable<LinkingWorkspaceSnapshot>;
  clearSources(workspaceVersion: number | null): Observable<LinkingWorkspaceSnapshot>;
}

export class LinkingWorkspaceStaleVersionError extends Error {
  constructor(message: string) {
    super(message);
  }
}
