import { Injectable, computed, signal } from '@angular/core';

export type PhraseContextAyahSelectionMode = 'only' | 'all-except';

export interface PhraseContextAyahSelectionSnapshot {
  readonly resultSetKey: string;
  readonly mode: PhraseContextAyahSelectionMode;
  readonly ayahIds: readonly number[];
  readonly selectedCount: number;
  readonly totalAyahCount: number;
  readonly revision: number;
}

@Injectable()
export class PhraseContextAyahSelectionStore {
  private readonly _resultSetKey = signal('');
  private readonly _mode = signal<PhraseContextAyahSelectionMode>('only');
  private readonly _overrides = signal<ReadonlySet<number>>(new Set());
  private readonly _totalAyahCount = signal(0);
  private readonly _revision = signal(0);

  readonly resultSetKey = this._resultSetKey.asReadonly();
  readonly mode = this._mode.asReadonly();
  readonly overrides = this._overrides.asReadonly();
  readonly totalAyahCount = this._totalAyahCount.asReadonly();
  readonly revision = this._revision.asReadonly();
  readonly selectedCount = computed(() =>
    this._mode() === 'only'
      ? this._overrides().size
      : Math.max(this._totalAyahCount() - this._overrides().size, 0),
  );
  readonly allSelected = computed(
    () => this._totalAyahCount() > 0 && this.selectedCount() === this._totalAyahCount(),
  );
  readonly partiallySelected = computed(
    () => this.selectedCount() > 0 && this.selectedCount() < this._totalAyahCount(),
  );

  synchronizeResultSet(resultSetKey: string): void {
    if (this._resultSetKey() === resultSetKey) {
      return;
    }
    this._resultSetKey.set(resultSetKey);
    this.resetSelection();
  }

  setTotalAyahCount(totalAyahCount: number): void {
    const safeTotal = Number.isSafeInteger(totalAyahCount) && totalAyahCount > 0
      ? totalAyahCount
      : 0;
    if (safeTotal === this._totalAyahCount()) {
      return;
    }
    this._totalAyahCount.set(safeTotal);
    if (
      (safeTotal === 0 && this._mode() === 'all-except') ||
      this._overrides().size > safeTotal
    ) {
      this.clearAll();
      return;
    }
    this.bumpRevision();
  }

  isSelected(ayahId: number): boolean {
    const overridden = this._overrides().has(ayahId);
    return this._mode() === 'only' ? overridden : !overridden;
  }

  setSelected(ayahId: number, selected: boolean): void {
    if (!isPositiveSafeInteger(ayahId) || this.isSelected(ayahId) === selected) {
      return;
    }
    const overrides = new Set(this._overrides());
    if (this._mode() === 'only') {
      selected ? overrides.add(ayahId) : overrides.delete(ayahId);
    } else {
      selected ? overrides.delete(ayahId) : overrides.add(ayahId);
    }
    this._overrides.set(overrides);
    this.bumpRevision();
  }

  selectAll(): void {
    if (this._totalAyahCount() === 0 || this.allSelected()) {
      return;
    }
    this._mode.set('all-except');
    this._overrides.set(new Set());
    this.bumpRevision();
  }

  clearAll(): void {
    if (this._mode() === 'only' && this._overrides().size === 0) {
      return;
    }
    this._mode.set('only');
    this._overrides.set(new Set());
    this.bumpRevision();
  }

  snapshot(): PhraseContextAyahSelectionSnapshot {
    return {
      resultSetKey: this._resultSetKey(),
      mode: this._mode(),
      ayahIds: [...this._overrides()].sort((left, right) => left - right),
      selectedCount: this.selectedCount(),
      totalAyahCount: this._totalAyahCount(),
      revision: this._revision(),
    };
  }

  private resetSelection(): void {
    this._mode.set('only');
    this._overrides.set(new Set());
    this._totalAyahCount.set(0);
    this.bumpRevision();
  }

  private bumpRevision(): void {
    this._revision.update((revision) => revision + 1);
  }
}

function isPositiveSafeInteger(value: number): boolean {
  return Number.isSafeInteger(value) && value > 0;
}
