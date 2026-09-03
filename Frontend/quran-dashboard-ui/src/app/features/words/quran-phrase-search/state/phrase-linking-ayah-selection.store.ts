import { Injectable, computed, signal } from '@angular/core';

export type PhraseLinkingAyahSelectionMode = 'only' | 'all-except';

export interface PhraseLinkingAyahSelectionSnapshot {
  readonly resultSetKey: string;
  readonly mode: PhraseLinkingAyahSelectionMode;
  readonly ayahIds: readonly number[];
  readonly selectedCount: number;
  readonly totalAyahCount: number;
  readonly revision: number;
}

@Injectable()
export class PhraseLinkingAyahSelectionStore {
  private readonly _resultSetKey = signal('');
  private readonly _mode = signal<PhraseLinkingAyahSelectionMode>('only');
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

  synchronizePopulation(resultSetKey: string, totalAyahCount: number): void {
    const safeTotal = safePopulationTotal(totalAyahCount);
    if (this._resultSetKey() !== resultSetKey) {
      this._resultSetKey.set(resultSetKey);
      this._mode.set('only');
      this._overrides.set(new Set());
      this._totalAyahCount.set(safeTotal);
      this.bumpRevision();
      return;
    }
    if (safeTotal === this._totalAyahCount()) {
      return;
    }
    this._totalAyahCount.set(safeTotal);
    if (
      (safeTotal === 0 && this._mode() === 'all-except') ||
      this._overrides().size > safeTotal
    ) {
      this._mode.set('only');
      this._overrides.set(new Set());
    }
    this.bumpRevision();
  }

  isSelected(ayahId: number): boolean {
    if (!isPositiveSafeInteger(ayahId) || this._totalAyahCount() === 0) {
      return false;
    }
    const overridden = this._overrides().has(ayahId);
    return this._mode() === 'only' ? overridden : !overridden;
  }

  setSelected(ayahId: number, selected: boolean): void {
    if (
      !isPositiveSafeInteger(ayahId) ||
      this._totalAyahCount() === 0 ||
      this.isSelected(ayahId) === selected
    ) {
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

  snapshot(): PhraseLinkingAyahSelectionSnapshot {
    return {
      resultSetKey: this._resultSetKey(),
      mode: this._mode(),
      ayahIds: [...this._overrides()].sort((left, right) => left - right),
      selectedCount: this.selectedCount(),
      totalAyahCount: this._totalAyahCount(),
      revision: this._revision(),
    };
  }

  private bumpRevision(): void {
    this._revision.update((revision) => revision + 1);
  }
}

function safePopulationTotal(value: number): number {
  return Number.isSafeInteger(value) && value > 0 ? value : 0;
}

function isPositiveSafeInteger(value: number): boolean {
  return Number.isSafeInteger(value) && value > 0;
}
