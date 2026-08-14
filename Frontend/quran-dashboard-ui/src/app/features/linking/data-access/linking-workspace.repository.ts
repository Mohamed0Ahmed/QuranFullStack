import { Observable } from 'rxjs';

import { LinkingSourceDescriptor } from '../models/linking-source.models';
import { LinkingWorkspaceSnapshot } from '../models/linking-workspace.models';
export { LinkingDataStaleError } from '../models/linking-revision.models';

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
  clearSources(workspaceVersion: number | null): Observable<LinkingWorkspaceSnapshot>;
}

export class LinkingWorkspaceStaleVersionError extends Error {
  constructor(message: string) {
    super(message);
  }
}
