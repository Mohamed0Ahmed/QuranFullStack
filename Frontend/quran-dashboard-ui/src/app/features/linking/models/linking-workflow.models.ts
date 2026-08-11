import { LINKING_LABELS } from './linking.labels';
import { LinkingAyah } from './linking-ayah.models';
import { LinkingSourceDescriptor } from './linking-source.models';
import { MergedLinkingSelection, LinkingSourceIntent } from './linking-merge.models';
import { LinkingOperationMember } from './linking-operation.models';
import { LinkingSelection } from './linking-workspace.models';

export type DirectLinkStep = 'door' | 'ayahs' | 'highlight' | 'review' | 'result';

export function previousDirectLinkStep(step: DirectLinkStep): DirectLinkStep {
  switch (step) {
    case 'ayahs':
      return 'door';
    case 'highlight':
      return 'ayahs';
    case 'review':
      return 'highlight';
    case 'result':
      return 'review';
    default:
      return 'door';
  }
}

export interface DirectLinkResult {
  kind: 'linked';
  message: typeof LINKING_LABELS.success;
}

export type DirectLinkOrigin = 'workspace' | 'source';

export type LinkingSourceLoadStatus = 'idle' | 'loading' | 'success' | 'error' | 'unsupported';

export interface LinkingSourceLoadProgress {
  loaded: number;
  total: number | null;
}

export interface LinkingSourceLoadState {
  status: LinkingSourceLoadStatus;
  ayahs: readonly LinkingAyah[];
  progress: LinkingSourceLoadProgress;
  errorMessage: string | null;
}

export interface LinkingSourceEditorState {
  sourceKey: string | null;
  sourceLabel: string | null;
  capturedConfigurationRevision: number | null;
  status: LinkingSourceLoadStatus;
  ayahs: readonly LinkingAyah[];
  rawProgress: LinkingSourceLoadProgress;
  universe: readonly string[];
  query: string;
  page: number;
  errorMessage: string | null;
}

export interface DirectLinkWorkflowState {
  source: LinkingSourceDescriptor | null;
  sourceKey: string | null;
  origin: DirectLinkOrigin | null;
  step: DirectLinkStep;
  selectedDoorId: number | null;
  doorNotice: string | null;
  selection: LinkingSelection;
  highlightSourceWords: boolean;
  sourceLoad: LinkingSourceLoadState;
  result: DirectLinkResult | null;
}

export interface LinkingTransientOperationState {
  members: readonly LinkingOperationMember[];
  mergedSelection: MergedLinkingSelection | null;
  sourceIntents: readonly LinkingSourceIntent[];
}
