import { Location } from '@angular/common';

import { DetailFrame } from './detail-overlay.models';
import { serializeDetailFrame } from './detail-overlay-url-codec';

/**
 * History-state provenance for app-created overlay navigations. Dialog Back
 * calls browser Back only when this record proves the immediately previous
 * entry is the parent card; anything else (shared URL, unowned base
 * navigation) uses the deterministic replace fallback.
 *
 * `baseSignature` is the base THIS entry sits on. It is re-stamped by an owned
 * base replacement (ayah continuity, B7) and compared against the live base at
 * Back time, so it means "no unowned base navigation has happened since this
 * record was written" — not "the overlay may never change base".
 */
export interface DetailOverlayProvenance {
  readonly baseSignature: string;
  readonly parentStackHash: string;
  readonly stackHash: string;
  readonly kind: 'push' | 'restore' | 'replace' | 'seed';
}

export const PROVENANCE_STATE_KEY = 'qdDetailNav';

const CHAIN_LEDGER_KEY = `${PROVENANCE_STATE_KEY}:chain`;

/** Bounds the per-tab ledger; a stack is capped at eight frames, pushes are not. */
const MAX_REMEMBERED_CHAIN_URLS = 32;

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

/**
 * The URLs of the overlay history chain this tab is currently inside.
 *
 * Seed idempotence cannot be decided from the URL alone: the same shared URL
 * can appear at two unrelated history entries, and only one of them has its
 * prefix chain below it. It also cannot be decided from `history.state` alone,
 * because the router rewrites that during a reload's initial navigation while
 * the prefix entries themselves survive.
 *
 * The ledger therefore tracks the LIVE chain only. Entries are remembered while
 * we own them or seed them, and the whole ledger is dropped the moment the app
 * leaves the chain — so a later visit to the same URL from unrelated history
 * finds no ledger, cannot prove parent adjacency, and is re-seeded instead of
 * being granted fabricated `seed` provenance.
 *
 * `sessionStorage` is per-tab and survives a reload, which is exactly the
 * lifetime a history chain has. It can be unavailable (private modes, blocked
 * storage); every access fails closed to "not part of a chain", which re-seeds.
 * Re-seeding is always safe — it can only duplicate entries, never send Back to
 * unrelated history.
 */
export class SeededChainLedger {
  /** True when `url` is an entry of the chain this tab is currently inside. */
  has(url: string): boolean {
    return this.read().includes(url);
  }

  /** Records `url` as part of the live chain. */
  remember(url: string): void {
    const urls = this.read();
    if (urls.includes(url)) {
      return;
    }

    this.write([...urls, url].slice(-MAX_REMEMBERED_CHAIN_URLS));
  }

  /** Replaces the live chain with exactly `urls` (a fresh seeding). */
  reset(urls: readonly string[]): void {
    this.write([...urls].slice(-MAX_REMEMBERED_CHAIN_URLS));
  }

  /** Drops the live chain: the app has left it, so nothing below is provable. */
  clear(): void {
    try {
      sessionStorage.removeItem(CHAIN_LEDGER_KEY);
    } catch {
      // Session storage unavailable: the ledger is already effectively empty.
    }
  }

  private read(): readonly string[] {
    try {
      const raw = sessionStorage.getItem(CHAIN_LEDGER_KEY);
      if (raw === null) {
        return [];
      }

      const parsed: unknown = JSON.parse(raw);
      return Array.isArray(parsed) ? parsed.filter((url): url is string => typeof url === 'string') : [];
    } catch {
      return [];
    }
  }

  private write(urls: readonly string[]): void {
    try {
      sessionStorage.setItem(CHAIN_LEDGER_KEY, JSON.stringify(urls));
    } catch {
      // Session storage unavailable: seeding stays correct, only reload dedup is lost.
    }
  }
}
