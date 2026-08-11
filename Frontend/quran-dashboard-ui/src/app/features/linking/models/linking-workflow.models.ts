import { LINKING_LABELS } from './linking.labels';
import { LinkingAyah } from './linking-ayah.models';
import { LinkingSourceDescriptor } from './linking-source.models';

export type DirectLinkStep = 'door' | 'ayahs' | 'highlight' | 'review' | 'result';

export interface DirectLinkResult {
  kind: 'success';
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

export interface DirectLinkWorkflowState {
  source: LinkingSourceDescriptor | null;
  sourceKey: string | null;
  origin: DirectLinkOrigin | null;
  step: DirectLinkStep;
  selectedDoorId: number | null;
  doorNotice: string | null;
  sourceLoad: LinkingSourceLoadState;
  result: DirectLinkResult | null;
}
