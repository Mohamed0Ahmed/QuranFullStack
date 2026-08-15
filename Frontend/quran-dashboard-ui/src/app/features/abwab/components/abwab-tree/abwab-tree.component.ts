import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
  untracked,
  viewChild,
} from '@angular/core';

import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { QdHierarchyKeyboardDirective } from '../../../../shared/ui/hierarchy/hierarchy-keyboard.directive';

import { AbwabNode, AbwabOrderScope } from '../../models/abwab.models';
import { ABWAB_LABELS } from '../../models/abwab.labels';
import { AbwabDoorLinksPanelComponent } from '../abwab-door-links-panel/abwab-door-links-panel.component';
import {
  AbwabTreeRow,
  flattenVisibleAbwabRows,
  isNativeButtonActivation,
  resolveAbwabTreeKeyboardIntent,
} from './abwab-tree-keyboard.controller';

export interface AbwabTreeMenuRequest {
  readonly id: number;
  readonly x: number;
  readonly y: number;
}

@Component({
  selector: 'qd-abwab-tree',
  standalone: true,
  imports: [
    AbwabDoorLinksPanelComponent,
    QdActionDirective,
    QdHierarchyKeyboardDirective,
  ],
  templateUrl: './abwab-tree.component.html',
  styleUrl: './abwab-tree.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AbwabTreeComponent {
  private readonly elementRef = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly hierarchy = viewChild.required(QdHierarchyKeyboardDirective);

  readonly roots = input<readonly AbwabNode[]>([]);
  readonly orderScope = input<AbwabOrderScope>('section');
  readonly ariaLabel = input('');
  readonly selectedId = input<number | null>(null);
  readonly bulkMode = input(false);
  readonly bulkSelectedIds = input<ReadonlySet<number>>(new Set());
  readonly expandSeedIds = input<ReadonlySet<number>>(new Set());
  readonly searchExpandedIds = input<ReadonlySet<number>>(new Set());
  readonly matchedIds = input<ReadonlySet<number>>(new Set());
  readonly revealedId = input<number | null>(null);
  readonly canCreateDoor = input(false);
  readonly canReorderDoor = input(false);
  readonly openLinksDoorId = input<number | null>(null);

  readonly selected = output<number>();
  readonly bulkToggled = output<number>();
  readonly addChildRequested = output<number>();
  readonly menuRequested = output<AbwabTreeMenuRequest>();
  readonly orderCommitted = output<{ id: number; position: number; scope: AbwabOrderScope }>();
  readonly relationsRequested = output<number>();
  readonly linksToggled = output<number>();

  private readonly manuallyExpandedIds = signal<ReadonlySet<number>>(new Set());
  private readonly manualFocusId = signal<number | null>(null);
  protected readonly editingId = signal<number | null>(null);

  constructor() {
    effect(() => {
      const seed = this.expandSeedIds();
      if (seed.size === 0) {
        return;
      }
      untracked(() => this.manuallyExpandedIds.update((current) => new Set([...current, ...seed])));
    });
  }

  private readonly effectiveExpandedIds = computed<ReadonlySet<number>>(() => {
    const manual = this.manuallyExpandedIds();
    const search = this.searchExpandedIds();
    return search.size === 0 ? manual : new Set([...manual, ...search]);
  });

  protected readonly nodesById = computed(() => {
    const map = new Map<number, AbwabNode>();
    const walk = (node: AbwabNode): void => {
      map.set(node.id, node);
      node.children.forEach(walk);
    };
    this.roots().forEach(walk);
    return map;
  });

  protected readonly visibleRows = computed<AbwabTreeRow[]>(() =>
    flattenVisibleAbwabRows(this.roots(), this.effectiveExpandedIds()),
  );

  protected readonly rovingId = computed(() => {
    const rows = this.visibleRows();
    if (rows.length === 0) {
      return null;
    }
    const manual = this.manualFocusId();
    if (manual !== null && rows.some((row) => row.id === manual)) {
      return manual;
    }
    const selected = this.selectedId();
    if (selected !== null && rows.some((row) => row.id === selected)) {
      return selected;
    }
    return rows[0].id;
  });

  protected onRowClick(id: number): void {
    if (this.editingId() !== null) {
      return;
    }
    if (this.bulkMode()) {
      this.bulkToggled.emit(id);
      return;
    }
    this.manualFocusId.set(id);
    this.selected.emit(id);
  }

  protected onCheckboxClick(event: Event, id: number): void {
    event.stopPropagation();
    this.bulkToggled.emit(id);
  }

  protected get relationsFlagLabel(): string {
    return ABWAB_LABELS.relationsFlagLabel;
  }

  protected relationsAriaLabel(name: string, count: number): string {
    return ABWAB_LABELS.rowRelationsAriaLabel(name, count);
  }

  protected childCountAriaLabel(count: number): string {
    return ABWAB_LABELS.rowChildCountAriaLabel(count);
  }

  protected descendantCountAriaLabel(count: number): string {
    return ABWAB_LABELS.rowDescendantCountAriaLabel(count);
  }

  protected depthAriaLabel(depth: number): string {
    return ABWAB_LABELS.rowDepthAriaLabel(depth);
  }

  protected get headerDirectLabel(): string { return ABWAB_LABELS.rowHeaderDirect; }
  protected get headerLinksLabel(): string { return ABWAB_LABELS.rowHeaderLinks; }
  protected get headerTotalLabel(): string { return ABWAB_LABELS.rowHeaderTotal; }
  protected get headerDepthLabel(): string { return ABWAB_LABELS.rowHeaderDepth; }

  protected depthBadge(depth: number): string {
    return ABWAB_LABELS.rowDepthBadge(depth);
  }

  protected onFlagClick(event: Event, id: number): void {
    event.stopPropagation();
    if (this.bulkMode()) {
      this.bulkToggled.emit(id);
      return;
    }
    this.manualFocusId.set(id);
    this.relationsRequested.emit(id);
  }

  protected onLinksClick(event: Event, id: number): void {
    event.stopPropagation();
    this.linksToggled.emit(id);
  }

  protected linksAriaLabel(node: AbwabNode): string {
    return ABWAB_LABELS.rowLinksAriaLabel(node.name, node.linkCount);
  }

  protected addChildAriaLabel(name: string): string {
    return ABWAB_LABELS.rowAddChildAriaLabel(name);
  }

  protected menuAriaLabel(name: string): string {
    return ABWAB_LABELS.rowMenuAriaLabel(name);
  }

  protected expandAriaLabel(row: AbwabTreeRow, node: AbwabNode): string {
    return row.isExpanded
      ? ABWAB_LABELS.relationPickerCollapseAriaLabel(node.name)
      : ABWAB_LABELS.relationPickerExpandAriaLabel(node.name);
  }

  protected orderEditAriaLabel(node: AbwabNode): string {
    return ABWAB_LABELS.rowOrderEditAriaLabel(node.name, this.displayOrder(node));
  }

  protected onRowContextMenu(event: MouseEvent, id: number): void {
    event.preventDefault();
    if (this.bulkMode()) {
      return;
    }
    this.openMenuFor(id, event.clientX, event.clientY);
  }

  protected onAddChildClick(event: Event, id: number): void {
    event.stopPropagation();
    if (!this.canCreateDoor()) {
      return;
    }
    this.manualFocusId.set(id);
    this.selected.emit(id);
    this.addChildRequested.emit(id);
  }

  protected onMoreClick(event: MouseEvent, id: number): void {
    event.stopPropagation();
    this.openMenuFor(id, event.clientX, event.clientY);
  }

  private openMenuFor(id: number, x: number, y: number): void {
    this.manualFocusId.set(id);
    this.selected.emit(id);
    this.menuRequested.emit({ id, x, y });
  }

  protected onRowDblClick(row: AbwabTreeRow): void {
    if (!row.hasChildren) {
      return;
    }
    this.toggleExpanded(row.id);
  }

  protected onChevronClick(event: Event, row: AbwabTreeRow): void {
    event.stopPropagation();
    if (!row.hasChildren) {
      return;
    }
    this.toggleExpanded(row.id);
  }

  protected scopeFor(node: AbwabNode): AbwabOrderScope {
    return node.depth === 0 && this.orderScope() === 'global' ? 'global' : 'section';
  }

  protected displayOrder(node: AbwabNode): number {
    return this.scopeFor(node) === 'global' ? (node.globalOrderValue ?? node.orderValue) : node.orderValue;
  }

  protected onOrderClick(event: Event, id: number): void {
    event.stopPropagation();
    if (!this.canReorderDoor()) {
      return;
    }
    this.editingId.set(id);
    setTimeout(() => this.orderInput(id)?.focus());
  }

  private orderInput(id: number): HTMLInputElement | null {
    return this.elementRef.nativeElement.querySelector<HTMLInputElement>(
      `[data-testid="abwab-tree-order-input-${id}"]`,
    );
  }

  private focusOrderChip(id: number): void {
    setTimeout(() =>
      this.elementRef.nativeElement
        .querySelector<HTMLElement>(`[data-testid="abwab-tree-order-${id}"]`)
        ?.focus(),
    );
  }

  protected onOrderKeydown(event: KeyboardEvent, id: number): void {
    event.stopPropagation();
    if (!this.canReorderDoor()) {
      this.cancelOrderEdit(id);
      return;
    }
    if (event.key === 'Enter') {
      this.commitOrderEdit(id, event.target);
      this.focusOrderChip(id);
    } else if (event.key === 'Escape') {
      this.cancelOrderEdit(id);
      this.focusOrderChip(id);
    }
  }

  protected cancelOrderEdit(id: number): void {
    if (this.editingId() !== id) {
      return;
    }
    this.editingId.set(null);
  }

  protected commitOrderEdit(id: number, target: EventTarget | null): void {
    if (!this.canReorderDoor() || this.editingId() !== id) {
      return;
    }
    this.editingId.set(null);
    const input = target as HTMLInputElement | null;
    const value = input ? Number(input.value) : Number.NaN;
    const node = this.nodesById().get(id);
    if (node && Number.isInteger(value) && value >= 1) {
      this.orderCommitted.emit({ id, position: value, scope: this.scopeFor(node) });
    }
  }

  protected onKeydown(event: KeyboardEvent): void {
    if (event.target !== event.currentTarget && isNativeButtonActivation(event.key)) {
      return;
    }
    const focusedId = this.rovingId();
    if (focusedId === null) {
      return;
    }

    const intent = resolveAbwabTreeKeyboardIntent({
      key: event.key,
      visibleRows: this.visibleRows(),
      focusedId,
      direction: this.resolveDirection(),
      bulkMode: this.bulkMode(),
      shiftKey: event.shiftKey,
    });

    switch (intent.type) {
      case 'focus':
        event.preventDefault();
        this.manualFocusId.set(intent.id);
        this.focusRow(intent.id);
        break;
      case 'expand':
        event.preventDefault();
        this.setExpanded(intent.id, true);
        break;
      case 'collapse':
        event.preventDefault();
        this.setExpanded(intent.id, false);
        break;
      case 'select':
        event.preventDefault();
        this.selected.emit(intent.id);
        break;
      case 'toggleBulk':
        event.preventDefault();
        this.bulkToggled.emit(intent.id);
        break;
      case 'openMenu': {
        event.preventDefault();
        const rect = this.rowElement(intent.id)?.getBoundingClientRect();
        const anchorX = this.resolveDirection() === 'rtl' ? rect?.right : rect?.left;
        this.openMenuFor(intent.id, anchorX ?? 0, rect?.bottom ?? 0);
        break;
      }
      case 'none':
        break;
    }
  }

  private toggleExpanded(id: number): void {
    const isExpanded = this.effectiveExpandedIds().has(id);
    this.setExpanded(id, !isExpanded);
  }

  private setExpanded(id: number, expanded: boolean): void {
    this.manuallyExpandedIds.update((current) => {
      const next = new Set(current);
      if (expanded) {
        next.add(id);
      } else {
        next.delete(id);
      }
      return next;
    });
    const openLinksDoorId = this.openLinksDoorId();
    if (!expanded && openLinksDoorId !== null && !this.visibleRows().some((row) => row.id === openLinksDoorId)) {
      this.linksToggled.emit(openLinksDoorId);
    }
  }

  private rowElement(id: number): HTMLElement | null {
    return this.hierarchy().rowElement(id);
  }

  private focusRow(id: number): void {
    this.hierarchy().focusRow(id);
  }

  private resolveDirection(): 'ltr' | 'rtl' {
    return this.hierarchy().direction();
  }
}
