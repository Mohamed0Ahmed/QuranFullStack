import { LinkingWorkspaceItem } from '../models/linking-workspace.models';

export interface LinkingWorkspaceLoadResult {
  items: readonly LinkingWorkspaceItem[];
  invalidPayload: boolean;
}

export interface LinkingWorkspaceRepository {
  load(actorSub: string): Promise<LinkingWorkspaceLoadResult>;
  save(actorSub: string, revision: number, items: readonly LinkingWorkspaceItem[]): Promise<void>;
  invalidateActiveActor(actorSub: string): Promise<void>;
}
