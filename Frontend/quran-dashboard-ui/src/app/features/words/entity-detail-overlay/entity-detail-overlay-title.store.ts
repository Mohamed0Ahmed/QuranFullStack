import { Injectable, signal } from '@angular/core';

/**
 * Component-tree-scoped title channel between the persistent overlay host and
 * the active entity adapter (Feature 029, Change B4). The host provides one
 * instance; the mounted adapter publishes its entity title once its summary
 * loads and clears it on destroy. An empty title means "not loaded yet" — the
 * host then falls back to the generic kind label. Adapters that do not publish
 * (the stubs) simply leave the fallback in place.
 */
@Injectable()
export class EntityDetailOverlayTitleStore {
  private readonly _title = signal('');

  readonly title = this._title.asReadonly();

  setTitle(title: string): void {
    this._title.set(title);
  }

  clear(): void {
    this._title.set('');
  }
}
