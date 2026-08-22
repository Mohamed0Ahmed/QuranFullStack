import { WritableSignal, signal } from '@angular/core';

import { AbwabNode, AbwabOrderScope } from '../../models/abwab.models';
import { ABWAB_LABELS } from '../../models/abwab.labels';

export interface AbwabTreeOrderCommit {
  readonly id: number;
  readonly position: number;
  readonly scope: AbwabOrderScope;
}

export class AbwabTreeOrderController {
  readonly editingId: WritableSignal<number | null> = signal(null);

  constructor(
    private readonly host: HTMLElement,
    private readonly canReorder: () => boolean,
    private readonly nodesById: () => ReadonlyMap<number, AbwabNode>,
    private readonly orderScope: () => AbwabOrderScope,
    private readonly committed: (commit: AbwabTreeOrderCommit) => void,
  ) {}

  ariaLabel(node: AbwabNode): string {
    return ABWAB_LABELS.rowOrderEditAriaLabel(node.name, this.displayOrder(node));
  }

  displayOrder(node: AbwabNode): number {
    return this.scopeFor(node) === 'global' ? (node.globalOrderValue ?? node.orderValue) : node.orderValue;
  }

  start(event: Event, id: number): void {
    event.stopPropagation();
    if (!this.canReorder()) {
      return;
    }
    this.editingId.set(id);
    setTimeout(() => this.orderInput(id)?.focus());
  }

  onKeydown(event: KeyboardEvent, id: number): void {
    event.stopPropagation();
    if (!this.canReorder()) {
      this.cancel(id);
      return;
    }
    if (event.key === 'Enter') {
      this.commit(id, event.target);
      this.focusOrderChip(id);
    } else if (event.key === 'Escape') {
      this.cancel(id);
      this.focusOrderChip(id);
    }
  }

  cancel(id: number): void {
    if (this.editingId() === id) {
      this.editingId.set(null);
    }
  }

  private commit(id: number, target: EventTarget | null): void {
    if (!this.canReorder() || this.editingId() !== id) {
      return;
    }
    this.editingId.set(null);
    const input = target as HTMLInputElement | null;
    const value = input ? Number(input.value) : Number.NaN;
    const node = this.nodesById().get(id);
    if (node && Number.isInteger(value) && value >= 1) {
      this.committed({ id, position: value, scope: this.scopeFor(node) });
    }
  }

  private scopeFor(node: AbwabNode): AbwabOrderScope {
    return node.depth === 0 && this.orderScope() === 'global' ? 'global' : 'section';
  }

  private orderInput(id: number): HTMLInputElement | null {
    return this.host.querySelector<HTMLInputElement>(`[data-testid="abwab-tree-order-input-${id}"]`);
  }

  private focusOrderChip(id: number): void {
    setTimeout(() => this.host.querySelector<HTMLElement>(`[data-testid="abwab-tree-order-${id}"]`)?.focus());
  }
}
