import { ChangeDetectionStrategy, Component, ElementRef, computed, inject, input, output, signal } from '@angular/core';

import { AbwabNode, AbwabOrderScope } from '../../models/abwab.models';
import { ABWAB_LABELS } from '../../models/abwab.labels';
import {
  AbwabTreeRow,
  flattenVisibleAbwabRows,
  resolveAbwabTreeKeyboardIntent,
} from './abwab-tree-keyboard.controller';

export interface AbwabTreeMenuRequest {
  readonly id: number;
  readonly x: number;
  readonly y: number;
}

/**
 * Presentational Abwab tree (plan-slice-b.md T413): `role="tree"` + `treeitem` rows,
 * `aria-level`/`aria-expanded`/`aria-selected`, roving tabindex, and the RTL-mirrored
 * keyboard model from `abwab-tree-keyboard.controller.ts` (pure, unit-tested on its own).
 * Renders flat (one row per visible node, indented by `depth`) rather than nesting
 * `role="group"` per level — `aria-level` already conveys depth to assistive tech, and a
 * flat list keeps roving-tabindex/keyboard wiring in one place; revisit only if an AT
 * regression is found.
 *
 * Rows carry the design contract's two hover actions (`abwab-tree-concept.html:114,436-443`):
 * `＋` (add child) and `⋯` (open the row menu), revealed on hover and on the selected row.
 * They are the contract's *third* add-child path alongside the side panel and the context
 * menu (plan-slice-b.md §6.2), and `⋯` is the only mouse path to the row menu that is not
 * right-click. No relations/protection controls (plan.md §5.1): edit/move/archive stay in
 * the side panel and the menu, which compose this tree rather than the other way around.
 */
@Component({
  selector: 'qd-abwab-tree',
  standalone: true,
  templateUrl: './abwab-tree.component.html',
  styleUrl: './abwab-tree.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AbwabTreeComponent {
  private readonly elementRef = inject<ElementRef<HTMLElement>>(ElementRef);

  readonly roots = input<readonly AbwabNode[]>([]);
  /** Which order space depth-0 rows display/edit. Rows at depth > 0 always use `orderValue`
   * (§5 — `globalOrderValue` is `NULL` past the root) regardless of this input. */
  readonly orderScope = input<AbwabOrderScope>('section');
  readonly ariaLabel = input('');
  readonly selectedId = input<number | null>(null);
  readonly bulkMode = input(false);
  readonly bulkSelectedIds = input<ReadonlySet<number>>(new Set());
  /** Ids a caller (e.g. search auto-expand, T507) forces open, unioned with manual toggles. */
  readonly forceExpandedIds = input<ReadonlySet<number>>(new Set());

  readonly selected = output<number>();
  readonly bulkToggled = output<number>();
  readonly addChildRequested = output<number>();
  /** Carries the anchor point, so the mouse paths (right-click, `⋯`) and the keyboard path
   * (`ContextMenu`/`Shift+F10`, anchored to the focused row) all place the menu themselves
   * instead of the page guessing from a separate `contextmenu` listener. */
  readonly menuRequested = output<AbwabTreeMenuRequest>();
  readonly orderCommitted = output<{ id: number; position: number; scope: AbwabOrderScope }>();
  /** The row's relations chip is a control (Slice D, audit item 13): the page selects the
   * door and opens the relations modal on it, the select-then-act shape every other row
   * path already follows. */
  readonly relationsRequested = output<number>();

  private readonly manuallyExpandedIds = signal<ReadonlySet<number>>(new Set());
  private readonly manualFocusId = signal<number | null>(null);
  protected readonly editingId = signal<number | null>(null);

  private readonly effectiveExpandedIds = computed(
    () => new Set([...this.manuallyExpandedIds(), ...this.forceExpandedIds()]),
  );

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

  protected depthBadge(depth: number): string {
    return ABWAB_LABELS.rowDepthBadge(depth);
  }

  protected onFlagClick(event: Event, id: number): void {
    event.stopPropagation();
    // Inert in bulk mode, like the row actions are hidden there: the row click means "toggle
    // this door's bulk selection", and a control that opened a modal instead would fight it.
    if (this.bulkMode()) {
      return;
    }
    this.manualFocusId.set(id);
    this.relationsRequested.emit(id);
  }

  protected addChildAriaLabel(name: string): string {
    return ABWAB_LABELS.rowAddChildAriaLabel(name);
  }

  protected menuAriaLabel(name: string): string {
    return ABWAB_LABELS.rowMenuAriaLabel(name);
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
    // Select first: the menu acts on the row it opened over, and the page writes
    // `door=<id>` on selection — so every menu path leaves the URL agreeing with the store.
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

  /** Depth > 0 always stays on `orderValue` — `globalOrderValue` is `NULL` there by construction. */
  protected scopeFor(node: AbwabNode): AbwabOrderScope {
    return node.depth === 0 && this.orderScope() === 'global' ? 'global' : 'section';
  }

  protected displayOrder(node: AbwabNode): number {
    return this.scopeFor(node) === 'global' ? (node.globalOrderValue ?? node.orderValue) : node.orderValue;
  }

  protected onOrderClick(event: Event, id: number): void {
    event.stopPropagation();
    this.editingId.set(id);
  }

  protected onOrderKeydown(event: KeyboardEvent, id: number): void {
    event.stopPropagation();
    if (event.key === 'Enter') {
      this.commitOrderEdit(id, event.target);
    } else if (event.key === 'Escape') {
      this.cancelOrderEdit(id);
    }
  }

  /** Enter is the only commit — blur and Escape both abandon the edit. Guarded on the same
   * id as `commitOrderEdit`, so the blur that follows an Enter commit (the input unmounts
   * under the focused element) finds `editingId` already cleared and does nothing. */
  protected cancelOrderEdit(id: number): void {
    if (this.editingId() !== id) {
      return;
    }
    this.editingId.set(null);
  }

  protected commitOrderEdit(id: number, target: EventTarget | null): void {
    if (this.editingId() !== id) {
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
        // Anchor to the focused row rather than the viewport origin — a keyboard user has no
        // pointer position, and a menu pinned at (0,0) is not a usable keyboard path.
        const rect = this.rowElement(intent.id)?.getBoundingClientRect();
        this.openMenuFor(intent.id, rect?.left ?? 0, rect?.bottom ?? 0);
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
  }

  private rowElement(id: number): HTMLElement | null {
    return this.elementRef.nativeElement.querySelector<HTMLElement>(`[data-testid="abwab-tree-row-${id}"]`);
  }

  private focusRow(id: number): void {
    queueMicrotask(() => this.rowElement(id)?.focus());
  }

  private resolveDirection(): 'ltr' | 'rtl' {
    const dirHost = this.elementRef.nativeElement.closest('[dir]');
    return dirHost?.getAttribute('dir') === 'rtl' ? 'rtl' : 'ltr';
  }
}
