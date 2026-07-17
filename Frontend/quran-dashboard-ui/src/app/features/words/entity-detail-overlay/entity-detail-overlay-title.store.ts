import { Injectable, signal } from '@angular/core';

/**
 * Component-tree-scoped title channel between the persistent overlay host and
 * the active entity adapter (Feature 029, Change B4). The host provides one
 * instance; the mounted adapter publishes its entity title once its summary
 * loads and clears it on destroy. An empty title means "not loaded yet" — the
 * host then falls back to the generic kind label. Adapters that do not publish
 * (the stubs) simply leave the fallback in place.
 *
 * The same channel carries the entity's ayah count (Feature 030, N6); `null`
 * means "not loaded yet", and the header reserves the count's box either way.
 */
@Injectable()
export class EntityDetailOverlayTitleStore {
  private readonly _title = signal('');
  private readonly _ayahCount = signal<number | null>(null);

  readonly title = this._title.asReadonly();
  readonly ayahCount = this._ayahCount.asReadonly();

  setTitle(title: string): void {
    this._title.set(title);
  }

  setAyahCount(count: number | null): void {
    this._ayahCount.set(count);
  }

  clear(): void {
    this._title.set('');
    this._ayahCount.set(null);
  }
}
