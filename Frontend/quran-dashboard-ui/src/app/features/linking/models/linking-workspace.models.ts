import {
  LinkingManualLinkShape,
  LinkingManualWordIdsByVerseKey,
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
      quranWordIdsByVerseKey: LinkingManualWordIdsByVerseKey;
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
  sourceId: number | null;
  sourceVersion: number | null;
  source: LinkingSourceDescriptor;
  configuration: LinkingSourceConfiguration;
  configurationRevision: number;
  ayahOverrideIds: readonly number[];
  ayahIdByVerseKey: Readonly<Record<string, number>>;
  lastResolvedCount: number | null;
}

export interface LinkingWorkspaceSnapshot {
  workspaceVersion: number | null;
  items: readonly LinkingWorkspaceItem[];
}

export interface LinkingRemovedWorkspaceItem {
  item: LinkingWorkspaceItem;
  index: number;
  wasChecked: boolean;
}
