import { LinkingWorkspaceItem } from './linking-workspace.models';

export type LinkingWorkspaceCountStatus = 'unresolved' | 'stale' | 'ready';

export interface LinkingWorkspaceSourceRowView {
  item: LinkingWorkspaceItem;
  checked: boolean;
  countStatus: LinkingWorkspaceCountStatus;
  countLabel: string;
}
