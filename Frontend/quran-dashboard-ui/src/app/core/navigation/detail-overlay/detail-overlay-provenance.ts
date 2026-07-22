import { Location } from '@angular/common';

import { DetailFrame } from './detail-overlay.models';
import { serializeDetailFrame } from './detail-overlay-url-codec';

export interface DetailOverlayProvenance {
  readonly baseSignature: string;
  readonly parentStackHash: string;
  readonly stackHash: string;
  readonly kind: 'push' | 'restore' | 'replace' | 'seed';
}

export const PROVENANCE_STATE_KEY = 'qdDetailNav';

export function hashDetailStack(stack: readonly DetailFrame[]): string {
  return stack.map(serializeDetailFrame).join('|');
}

export function readDetailOverlayProvenance(location: Location): DetailOverlayProvenance | null {
  const historyState = location.getState();
  if (historyState === null || typeof historyState !== 'object') {
    return null;
  }

  const record = (historyState as Record<string, unknown>)[PROVENANCE_STATE_KEY];
  if (record === null || typeof record !== 'object') {
    return null;
  }

  const candidate = record as Partial<DetailOverlayProvenance>;
  return typeof candidate.baseSignature === 'string' &&
    typeof candidate.parentStackHash === 'string' &&
    typeof candidate.stackHash === 'string' &&
    (candidate.kind === 'push' ||
      candidate.kind === 'restore' ||
      candidate.kind === 'replace' ||
      candidate.kind === 'seed')
    ? (candidate as DetailOverlayProvenance)
    : null;
}
