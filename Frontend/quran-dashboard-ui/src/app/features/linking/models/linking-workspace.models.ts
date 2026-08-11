import {
  LinkingManualLinkShape,
  LinkingManualWordLocationsByVerseKey,
} from './linking-manual-mushaf.models';
import { LinkingSourceDescriptor } from './linking-source.models';

export type LinkingSelection =
  | { mode: 'all-except'; verseKeys: readonly string[] }
  | { mode: 'only'; verseKeys: readonly string[] };

export type LinkingAutomaticSourceDescriptor = Exclude<
  LinkingSourceDescriptor,
  { kind: 'manual-mushaf-ayahs' }
>;

export type LinkingSourceConfiguration =
  | {
      kind: 'automatic';
      ayahInclusion: LinkingSelection;
      automaticWordMatchesEnabled: boolean;
    }
  | {
      kind: 'manual';
      ayahInclusion: LinkingSelection;
      wordLocationsByVerseKey: LinkingManualWordLocationsByVerseKey;
      linkShape: LinkingManualLinkShape;
    };

export type LinkingWorkspaceSurface =
  | 'closed'
  | 'workspace'
  | 'source-ayah-editor'
  | 'manual-word-editor'
  | 'linking-flow';

export interface LinkingWorkspaceItem {
  sourceKey: string;
  source: LinkingSourceDescriptor;
  configuration: LinkingSourceConfiguration;
  configurationRevision: number;
  lastResolvedCount: number | null;
  lastResolvedCountIsStale: boolean;
  selection: LinkingSelection;
  highlightSourceWords: boolean;
  resultCount: number | null;
}

export interface LinkingWorkspacePersistedEnvelope {
  version: 2;
  actorSub: string;
  revision: number;
  items: readonly LinkingWorkspacePersistedItem[];
}

export interface LinkingWorkspacePersistedItem {
  sourceKey: string;
  source: LinkingSourceDescriptor;
  configuration: LinkingSourceConfiguration;
  lastResolvedCount: number | null;
}

export interface LinkingRemovedWorkspaceItem {
  item: LinkingWorkspaceItem;
  index: number;
  wasChecked: boolean;
}
