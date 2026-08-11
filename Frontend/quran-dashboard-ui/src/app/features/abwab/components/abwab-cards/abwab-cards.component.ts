import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { AbwabNode, AbwabOrderScope } from '../../models/abwab.models';
import { ABWAB_LABELS } from '../../models/abwab.labels';
import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { QdEmptyStateComponent } from '../../../../shared/ui/empty-state/empty-state.component';

export interface AbwabCardsCrumb {
  readonly id: number | null;
  readonly name: string;
}

@Component({
  selector: 'qd-abwab-cards',
  standalone: true,
  imports: [QdActionDirective, QdEmptyStateComponent],
  templateUrl: './abwab-cards.component.html',
  styleUrl: './abwab-cards.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AbwabCardsComponent {
  readonly roots = input<readonly AbwabNode[]>([]);
  readonly byId = input<ReadonlyMap<number, AbwabNode>>(new Map());
  readonly cardId = input<number | null>(null);
  readonly orderScope = input<AbwabOrderScope>('section');
  readonly selectedId = input<number | null>(null);
  readonly bulkMode = input(false);
  readonly bulkSelectedIds = input<ReadonlySet<number>>(new Set());
  readonly isFiltering = input(false);
  readonly visibleIds = input<ReadonlySet<number>>(new Set());

  readonly selected = output<number>();
  readonly bulkToggled = output<number>();
  readonly drilled = output<number>();
  readonly crumbSelected = output<number | null>();

  protected get allDoorsCrumbLabel(): string {
    return ABWAB_LABELS.allDoorsTab;
  }

  private readonly path = computed<readonly AbwabNode[]>(() => {
    const id = this.cardId();
    if (id === null) {
      return [];
    }
    const leaf = this.byId().get(id);
    if (!leaf || leaf.isArchived) {
      return [];
    }
    const chain: AbwabNode[] = [];
    let current: AbwabNode | undefined = leaf;
    while (current) {
      chain.unshift(current);
      current = current.parentId !== null ? this.byId().get(current.parentId) : undefined;
    }
    return chain;
  });

  protected readonly crumbs = computed<readonly AbwabCardsCrumb[]>(() =>
    this.path().map((node) => ({ id: node.id, name: node.name })),
  );

  private readonly level = computed<readonly AbwabNode[]>(() => {
    const path = this.path();
    return path.length > 0 ? path[path.length - 1].children : this.roots();
  });

  protected readonly visibleLevel = computed<readonly AbwabNode[]>(() => {
    const level = this.level();
    if (!this.isFiltering()) {
      return level;
    }
    const visibleIds = this.visibleIds();
    return level.filter((node) => visibleIds.has(node.id));
  });

  protected readonly emptyStateMessage = computed<string>(() =>
    this.isFiltering() && this.level().length > 0
      ? ABWAB_LABELS.noSearchMatchesMessage
      : ABWAB_LABELS.emptyTreeMessage,
  );

  protected childCountAriaLabel(count: number): string {
    return ABWAB_LABELS.rowChildCountAriaLabel(count);
  }

  protected displayOrder(node: AbwabNode): number {
    const isTopLevel = this.cardId() === null;
    return isTopLevel && this.orderScope() === 'global' ? (node.globalOrderValue ?? node.orderValue) : node.orderValue;
  }

  protected onCardClick(node: AbwabNode): void {
    if (this.bulkMode()) {
      this.bulkToggled.emit(node.id);
      return;
    }
    this.selected.emit(node.id);
    if (node.children.length > 0) {
      this.drilled.emit(node.id);
    }
  }

  protected onCheckboxClick(event: Event, id: number): void {
    event.stopPropagation();
    this.bulkToggled.emit(id);
  }

  protected onCrumbClick(id: number | null): void {
    this.crumbSelected.emit(id);
  }
}
