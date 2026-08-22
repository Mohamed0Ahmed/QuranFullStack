import { computed, signal } from '@angular/core';

const STORAGE_PREFIX = 'quran-dashboard.words.table-columns.v1';

export type ExplorerTableColumnMove = 'up' | 'down';

export interface ExplorerTableColumnDefinition {
  readonly key: string;
  readonly label: string;
  readonly track: string;
  readonly locked?: boolean;
  readonly reorderLocked?: boolean;
}

export interface ExplorerTableColumnState extends ExplorerTableColumnDefinition {
  readonly visible: boolean;
}

interface StoredColumnPreferences {
  readonly order?: unknown;
  readonly hidden?: unknown;
}

export class ExplorerTableColumnsController {
  private readonly definitionsByKey: ReadonlyMap<string, ExplorerTableColumnDefinition>;
  private readonly defaultOrder: readonly string[];
  private readonly storageKey: string;
  private readonly orderSignal = signal<readonly string[]>([]);
  private readonly hiddenSignal = signal<ReadonlySet<string>>(new Set());

  readonly columns = computed<readonly ExplorerTableColumnState[]>(() => {
    const hidden = this.hiddenSignal();
    return this.orderSignal().flatMap((key) => {
      const definition = this.definitionsByKey.get(key);
      return definition ? [{ ...definition, visible: !hidden.has(key) }] : [];
    });
  });

  readonly visibleColumns = computed(() => this.columns().filter((column) => column.visible));
  readonly visibleColumnCount = computed(() => this.visibleColumns().length);
  readonly gridTemplate = computed(() => this.visibleColumns().map((column) => column.track).join(' '));

  constructor(tableKey: string, definitions: readonly ExplorerTableColumnDefinition[]) {
    this.storageKey = `${STORAGE_PREFIX}.${tableKey}`;
    this.definitionsByKey = new Map(definitions.map((definition) => [definition.key, definition]));
    this.defaultOrder = definitions.map((definition) => definition.key);
    const restored = this.restore();
    this.orderSignal.set(restored.order);
    this.hiddenSignal.set(restored.hidden);
  }

  isVisible(key: string): boolean {
    return !this.hiddenSignal().has(key) && this.definitionsByKey.has(key);
  }

  setVisible(key: string, visible: boolean): void {
    const definition = this.definitionsByKey.get(key);
    if (!definition || definition.locked) {
      return;
    }
    const next = new Set(this.hiddenSignal());
    visible ? next.delete(key) : next.add(key);
    this.hiddenSignal.set(next);
    this.persist();
  }

  move(key: string, direction: ExplorerTableColumnMove): void {
    if (this.definitionsByKey.get(key)?.reorderLocked) {
      return;
    }
    const current = this.orderSignal();
    const fromIndex = current.indexOf(key);
    const toIndex = direction === 'up' ? fromIndex - 1 : fromIndex + 1;
    this.moveTo(fromIndex, toIndex);
  }

  moveTo(fromIndex: number, toIndex: number): void {
    const current = this.orderSignal();
    if (fromIndex < 0 || toIndex < 0 || fromIndex >= current.length || toIndex >= current.length || fromIndex === toIndex) {
      return;
    }
    if (this.definitionsByKey.get(current[fromIndex])?.reorderLocked || this.definitionsByKey.get(current[toIndex])?.reorderLocked) {
      return;
    }
    const next = [...current];
    const [moved] = next.splice(fromIndex, 1);
    next.splice(toIndex, 0, moved);
    this.orderSignal.set(next);
    this.persist();
  }

  reset(): void {
    this.orderSignal.set(this.defaultOrder);
    this.hiddenSignal.set(new Set());
    try {
      globalThis.localStorage?.removeItem(this.storageKey);
    } catch {
      return;
    }
  }

  private restore(): { order: readonly string[]; hidden: ReadonlySet<string> } {
    try {
      const raw = globalThis.localStorage?.getItem(this.storageKey);
      if (!raw) {
        return { order: this.defaultOrder, hidden: new Set() };
      }
      const parsed = JSON.parse(raw) as StoredColumnPreferences;
      const requestedOrder = Array.isArray(parsed.order)
        ? parsed.order.filter((key): key is string => typeof key === 'string' && this.definitionsByKey.has(key))
        : [];
      const order = [...new Set([...requestedOrder, ...this.defaultOrder])];
      const hidden = new Set(
        Array.isArray(parsed.hidden)
          ? parsed.hidden.filter((key): key is string => {
              const definition = typeof key === 'string' ? this.definitionsByKey.get(key) : undefined;
              return definition !== undefined && !definition.locked;
            })
          : [],
      );
      return { order, hidden };
    } catch {
      return { order: this.defaultOrder, hidden: new Set() };
    }
  }

  private persist(): void {
    try {
      globalThis.localStorage?.setItem(
        this.storageKey,
        JSON.stringify({ order: this.orderSignal(), hidden: [...this.hiddenSignal()] }),
      );
    } catch {
      return;
    }
  }
}
