import { LinkingSourceDescriptor } from './linking-source.models';

export type LinkingSelection =
  | { mode: 'all-except'; verseKeys: readonly string[] }
  | { mode: 'only'; verseKeys: readonly string[] };

export type LinkingWorkspaceSurface = 'closed' | 'workspace' | 'direct-link';

export interface LinkingWorkspaceItem {
  sourceKey: string;
  source: LinkingSourceDescriptor;
  selection: LinkingSelection;
  resultCount: number | null;
  highlightSourceWords: boolean;
}

export interface LinkingWorkspaceSessionEnvelope {
  version: 1;
  actorSub: string;
  items: readonly LinkingWorkspaceSessionItem[];
}

export interface LinkingWorkspaceSessionItem {
  sourceKey: string;
  source: LinkingSourceDescriptor;
  selection: LinkingSelection;
  resultCount: number | null;
  highlightSourceWords: boolean;
}
