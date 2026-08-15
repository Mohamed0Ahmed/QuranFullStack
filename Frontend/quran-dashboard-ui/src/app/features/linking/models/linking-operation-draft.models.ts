import { LinkingManualLinkShape } from './linking-manual-mushaf.models';
import { LinkingSourceDescriptor } from './linking-source.models';

export type LinkingAyahIdSelection =
  | { mode: 'all-except'; ayahIds: readonly number[] }
  | { mode: 'only'; ayahIds: readonly number[] };

export interface LinkingDescriptionDraft {
  ayahId: number;
  orderValue: number;
  body: string;
}

export interface LinkingOperationSourceDraft {
  sourceKey: string;
  sourceId: number | null;
  sourceVersion: number | null;
  linkingDataRevision: number;
  descriptor: LinkingSourceDescriptor;
  label: string;
  selection: LinkingAyahIdSelection;
  selectedWordIdsByAyahId: Readonly<Record<number, readonly number[]>>;
  descriptions: readonly LinkingDescriptionDraft[];
  automaticWordMatchesEnabled: boolean | null;
  manualLinkShape: LinkingManualLinkShape | null;
}

export interface LinkingOperationDraft {
  generation: number;
  linkingDataRevision: number | null;
  doorId: number | null;
  sourceOrder: readonly string[];
  sources: Readonly<Record<string, LinkingOperationSourceDraft>>;
}

export interface LinkingCopyCallbacks {
  readonly acknowledged: () => void;
  readonly stopped: (message: string) => void;
}
