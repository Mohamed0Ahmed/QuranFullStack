import { Injectable, signal } from '@angular/core';

import { LinkingSourceDescriptor } from '../../linking/models/linking-source.models';

@Injectable()
export class EntityDetailOverlayHeaderStore {
  private readonly _title = signal('');
  private readonly _ayahCount = signal<number | null>(null);
  private readonly _linkingSource = signal<LinkingSourceDescriptor | null>(null);

  readonly title = this._title.asReadonly();
  readonly ayahCount = this._ayahCount.asReadonly();
  readonly linkingSource = this._linkingSource.asReadonly();

  setTitle(title: string): void {
    this._title.set(title);
  }

  setAyahCount(count: number | null): void {
    this._ayahCount.set(count);
  }

  setLinkingSource(source: LinkingSourceDescriptor | null): void {
    this._linkingSource.set(source);
  }

  clear(): void {
    this._title.set('');
    this._ayahCount.set(null);
    this._linkingSource.set(null);
  }
}
