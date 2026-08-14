import { Observable } from 'rxjs';

import { LinkingWorkspaceDeltaBody } from '../../../core/api/generated/models/linking-workspace-delta-body';
import { LinkingWorkspaceDeltaResponse } from '../../../core/api/generated/models/linking-workspace-delta-response';

export abstract class LinkingWorkspaceConfigurationRepository {
  abstract applyDelta(
    sourceId: number,
    delta: LinkingWorkspaceDeltaBody,
  ): Observable<LinkingWorkspaceDeltaResponse>;
}

export class LinkingWorkspaceSourceStaleError extends Error {}
