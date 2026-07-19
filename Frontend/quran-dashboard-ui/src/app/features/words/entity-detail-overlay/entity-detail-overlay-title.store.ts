import { Injectable, signal } from '@angular/core';

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
