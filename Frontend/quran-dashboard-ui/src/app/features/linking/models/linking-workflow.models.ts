import { LinkingAyah } from './linking-ayah.models';
import { MergedLinkingSelection, LinkingSourceIntent } from './linking-merge.models';

export type LinkingSourceLoadStatus = 'idle' | 'loading' | 'success' | 'error' | 'unsupported';

export interface LinkingSourceLoadProgress {
  loaded: number;
  total: number | null;
}

export interface LinkingSourceEditorState {
  sourceKey: string | null;
  capturedConfigurationRevision: number | null;
  sourceLabel: string | null;
  status: LinkingSourceLoadStatus;
  ayahs: readonly LinkingAyah[];
  rawProgress: LinkingSourceLoadProgress;
  universe: readonly string[];
  errorMessage: string | null;
}

export interface LinkingSourceSetOperationResult {
  mergedSelection: MergedLinkingSelection;
  sourceIntents: readonly LinkingSourceIntent[];
  linkingDataRevision: number;
}
