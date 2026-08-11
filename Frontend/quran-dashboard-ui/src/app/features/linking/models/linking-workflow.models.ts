import { LINKING_LABELS } from './linking.labels';

export type DirectLinkStep = 'door' | 'ayahs' | 'highlight' | 'review' | 'result';

export interface DirectLinkResult {
  kind: 'success';
  message: typeof LINKING_LABELS.success;
}
