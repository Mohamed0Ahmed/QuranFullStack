import { ChangeDetectionStrategy, Component, computed, ElementRef, inject, input, output, signal } from '@angular/core';

import { AbwabTemplateNodeVm } from '../../models/abwab-templates.models';
import { ABWAB_LABELS } from '../../models/abwab.labels';

export interface AbwabTemplateNodeMenuRequest {
  readonly nodeId: number;
  readonly x: number;
  readonly y: number;
}

interface AbwabTemplateTreeRow {
  readonly node: AbwabTemplateNodeVm;
  readonly hasChildren: boolean;
  readonly isExpanded: boolean;
}

/**
 * The template editor's tree: the same tree *language* as the doors tree — chevron
 * expand/collapse at any depth, an order chip, the root marked `◆` with a bold name, and the
 * hover-revealed `＋` (add child) and `⋯` (row menu) actions — but not the same component.
 * `AbwabTreeComponent` is typed on `AbwabNode` and carries selection, bulk mode, roving
 * tabindex, and URL concerns this page has none of, plus a spec suite pinned to that behavior.
 *
 * It renders a list rather than `role="tree"`, deliberately: the doors tree earns that role with
 * a full RTL-mirrored keyboard model, and claiming the role without the arrow-key model would
 * promise a navigation contract this component does not implement. Every row is reachable by Tab
 * through its own controls, and `aria-level` still conveys depth. ux-slice-g adds `ContextMenu` /
 * `Shift+F10` as a third path to the same row menu (alongside `⋯` and right-click): the row `<div>`
 * catches the key as it bubbles from whichever of the row's own controls has focus, so no row
 * becomes a tab stop and this reasoning still holds.
 *
 * Presentational — every action is an output; the only injected dependency is `ElementRef`, read
 * to anchor the keyboard menu path at the focused row's own bounding rect.
 */
@Component({
  selector: 'qd-abwab-template-tree',
  standalone: true,
  templateUrl: './abwab-template-tree.component.html',
  styleUrl: './abwab-template-tree.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AbwabTemplateTreeComponent {
  private readonly elementRef = inject<ElementRef<HTMLElement>>(ElementRef);

  readonly root = input<AbwabTemplateNodeVm | null>(null);
  readonly ariaLabel = input('');

  readonly addChildRequested = output<number>();
  readonly menuRequested = output<AbwabTemplateNodeMenuRequest>();
  readonly orderCommitted = output<{ nodeId: number; position: number }>();
  /** The inline «إضافة عنصر…» row: a name-only child of the root, then editable through the full
   * authoring modal like every other node. */
  readonly quickAddRequested = output<string>();

  /** Collapsed, not expanded: the contract renders every node open (`renderNode`'s `node open`),
   * so the set starts empty and only a deliberate collapse puts an id in it. */
  private readonly collapsedIds = signal<ReadonlySet<number>>(new Set());
  protected readonly editingOrderId = signal<number | null>(null);
  protected readonly quickAddDraft = signal('');

  protected readonly rows = computed<readonly AbwabTemplateTreeRow[]>(() => {
    const collapsed = this.collapsedIds();
    const rows: AbwabTemplateTreeRow[] = [];

    const walk = (node: AbwabTemplateNodeVm): void => {
      const hasChildren = node.children.length > 0;
      const isExpanded = !collapsed.has(node.id);
      rows.push({ node, hasChildren, isExpanded });
      if (isExpanded) {
        node.children.forEach(walk);
      }
    };

    const root = this.root();
    if (root) {
      walk(root);
    }
    return rows;
  });

  protected get addChildPlaceholder(): string { return ABWAB_LABELS.templateAddChildPlaceholder; }

  protected addChildAriaLabel(name: string): string {
    return ABWAB_LABELS.templateAddChildAriaLabel(name);
  }

  protected menuAriaLabel(name: string): string {
    return ABWAB_LABELS.templateNodeMenuAriaLabel(name);
  }

  protected expandAriaLabel(row: AbwabTemplateTreeRow): string {
    return row.isExpanded
      ? ABWAB_LABELS.templateNodeCollapseAriaLabel(row.node.name)
      : ABWAB_LABELS.templateNodeExpandAriaLabel(row.node.name);
  }

  protected toggleExpanded(nodeId: number): void {
    const next = new Set(this.collapsedIds());
    if (!next.delete(nodeId)) {
      next.add(nodeId);
    }
    this.collapsedIds.set(next);
  }

  protected onAddChildClick(nodeId: number): void {
    this.addChildRequested.emit(nodeId);
  }

  protected onMoreClick(event: MouseEvent, nodeId: number): void {
    this.menuRequested.emit({ nodeId, x: event.clientX, y: event.clientY });
  }

  /** No bulk-mode guard here, unlike the doors tree's equivalent: the workshop tree has no bulk
   * mode, so importing the guard would be a branch that can never be taken. */
  protected onRowContextMenu(event: MouseEvent, nodeId: number): void {
    event.preventDefault();
    this.menuRequested.emit({ nodeId, x: event.clientX, y: event.clientY });
  }

  /** Bubbles from whichever of the row's own controls (chevron / ＋ / ⋯) has focus — there is no
   * roving-tabindex model here, so the row itself is what catches the key. Anchored at the row's
   * own bounding rect, falling back to (0, 0) only if the row is missing — a menu pinned at the
   * viewport origin is not a usable keyboard path (the doors tree's own reason, carried over). */
  protected onRowKeydown(event: KeyboardEvent, nodeId: number): void {
    if (event.key !== 'ContextMenu' && !(event.key === 'F10' && event.shiftKey)) {
      return;
    }
    event.preventDefault();
    const rect = this.rowElement(nodeId)?.getBoundingClientRect();
    this.menuRequested.emit({ nodeId, x: rect?.left ?? 0, y: rect?.bottom ?? 0 });
  }

  private rowElement(nodeId: number): HTMLElement | null {
    return this.elementRef.nativeElement.querySelector<HTMLElement>(
      `[data-testid="abwab-template-tree-row-${nodeId}"]`,
    );
  }

  /** The root has no siblings, so its chip is the `◆` marker and is not an order editor. */
  protected onOrderClick(node: AbwabTemplateNodeVm): void {
    if (node.parentNodeId === null) {
      return;
    }
    this.editingOrderId.set(node.id);
  }

  protected onOrderKeydown(event: KeyboardEvent, nodeId: number): void {
    if (event.key === 'Enter') {
      this.commitOrderEdit(nodeId, event.target);
    } else if (event.key === 'Escape') {
      this.editingOrderId.set(null);
    }
  }

  protected commitOrderEdit(nodeId: number, target: EventTarget | null): void {
    if (this.editingOrderId() !== nodeId) {
      return;
    }
    this.editingOrderId.set(null);
    const input = target as HTMLInputElement | null;
    const value = input ? Number(input.value) : Number.NaN;
    if (Number.isInteger(value) && value >= 1) {
      this.orderCommitted.emit({ nodeId, position: value });
    }
  }

  protected onQuickAddInput(event: Event): void {
    this.quickAddDraft.set((event.target as HTMLInputElement).value);
  }

  protected onQuickAddEnter(event: Event): void {
    event.preventDefault();
    const name = this.quickAddDraft().trim();
    if (!name) {
      return;
    }
    this.quickAddDraft.set('');
    this.quickAddRequested.emit(name);
  }
}
