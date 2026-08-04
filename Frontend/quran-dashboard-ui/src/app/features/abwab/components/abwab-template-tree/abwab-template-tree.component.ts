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
  readonly quickAddRequested = output<string>();

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

  protected orderEditAriaLabel(node: AbwabTemplateNodeVm): string {
    return ABWAB_LABELS.templateNodeOrderEditAriaLabel(node.name, node.orderValue);
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

  protected onRowContextMenu(event: MouseEvent, nodeId: number): void {
    event.preventDefault();
    this.menuRequested.emit({ nodeId, x: event.clientX, y: event.clientY });
  }

  protected onRowKeydown(event: KeyboardEvent, nodeId: number): void {
    if (event.key !== 'ContextMenu' && !(event.key === 'F10' && event.shiftKey)) {
      return;
    }
    event.preventDefault();
    const rect = this.rowElement(nodeId)?.getBoundingClientRect();
    const anchorX = this.resolveDirection() === 'rtl' ? rect?.right : rect?.left;
    this.menuRequested.emit({ nodeId, x: anchorX ?? 0, y: rect?.bottom ?? 0 });
  }

  private rowElement(nodeId: number): HTMLElement | null {
    return this.elementRef.nativeElement.querySelector<HTMLElement>(
      `[data-testid="abwab-template-tree-row-${nodeId}"]`,
    );
  }

  private resolveDirection(): 'ltr' | 'rtl' {
    const dirHost = this.elementRef.nativeElement.closest('[dir]');
    return dirHost?.getAttribute('dir') === 'rtl' ? 'rtl' : 'ltr';
  }

  protected onOrderClick(node: AbwabTemplateNodeVm): void {
    if (node.parentNodeId === null) {
      return;
    }
    this.editingOrderId.set(node.id);
    setTimeout(() => this.orderInput(node.id)?.focus());
  }

  private orderInput(nodeId: number): HTMLInputElement | null {
    return this.elementRef.nativeElement.querySelector<HTMLInputElement>(
      `[data-testid="abwab-template-tree-order-input-${nodeId}"]`,
    );
  }

  protected onOrderKeydown(event: KeyboardEvent, nodeId: number): void {
    if (event.key === 'Enter') {
      this.commitOrderEdit(nodeId, event.target);
      this.focusOrderChip(nodeId);
    } else if (event.key === 'Escape') {
      this.cancelOrderEdit(nodeId);
      this.focusOrderChip(nodeId);
    }
  }

  private focusOrderChip(nodeId: number): void {
    setTimeout(() =>
      this.elementRef.nativeElement
        .querySelector<HTMLElement>(`[data-testid="abwab-template-tree-order-${nodeId}"]`)
        ?.focus(),
    );
  }

  protected cancelOrderEdit(nodeId: number): void {
    if (this.editingOrderId() !== nodeId) {
      return;
    }
    this.editingOrderId.set(null);
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
