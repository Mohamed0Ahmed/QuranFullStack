import { Location } from '@angular/common';

import { DetailFrame } from './detail-overlay.models';
import { serializeDetailFrame } from './detail-overlay-url-codec';

// Dialog Back uses browser Back only when this record proves the previous entry is the
// parent card; anything else falls back to a deterministic replace. `baseSignature` is the
// base THIS entry sits on, re-stamped by an owned base replacement (ayah continuity) and
// compared against the live base at Back time — it means "no unowned base navigation since".
export interface DetailOverlayProvenance {
  readonly baseSignature: string;
  readonly parentStackHash: string;
  readonly stackHash: string;
  readonly kind: 'push' | 'restore' | 'replace' | 'seed';
}

export const PROVENANCE_STATE_KEY = 'qdDetailNav';

/** Stable identity of a stack: the frames it holds, in order. */
export function hashDetailStack(stack: readonly DetailFrame[]): string {
  return stack.map(serializeDetailFrame).join('|');
}

/** Reads this history entry's provenance record, or null when we do not own it. */
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
