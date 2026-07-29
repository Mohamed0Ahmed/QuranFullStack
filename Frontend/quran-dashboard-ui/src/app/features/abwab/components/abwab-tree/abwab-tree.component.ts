import { ChangeDetectionStrategy, Component, ElementRef, computed, inject, input, output, signal } from '@angular/core';

import { AbwabNode } from '../../models/abwab.models';
import {
  AbwabTreeRow,
  flattenVisibleAbwabRows,
  resolveAbwabTreeKeyboardIntent,
} from './abwab-tree-keyboard.controller';

/**
 * Presentational Abwab tree (plan-slice-b.md T413): `role="tree"` + `treeitem` rows,
 * `aria-level`/`aria-expanded`/`aria-selected`, roving tabindex, and the RTL-mirrored
 * keyboard model from `abwab-tree-keyboard.controller.ts` (pure, unit-tested on its own).
 * Renders flat (one row per visible node, indented by `depth`) rather than nesting
 * `role="group"` per level — `aria-level` already conveys depth to assistive tech, and a
 * flat list keeps roving-tabindex/keyboard wiring in one place; revisit only if an AT
 * regression is found. No relations/protection controls (plan.md §5.1) and no inline
 * row-action buttons here — edit/add-child/move/archive live in the side panel and the
 * row context menu (`abwab-page-overlays.controller.ts`), which compose this tree
 * rather than the other way around.
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
  readonly ariaLabel = input('');
  readonly selectedId = input<number | null>(null);
  readonly bulkMode = input(false);
  readonly bulkSelectedIds = input<ReadonlySet<number>>(new Set());
  /** Ids a caller (e.g. search auto-expand, T507) forces open, unioned with manual toggles. */
  readonly forceExpandedIds = input<ReadonlySet<number>>(new Set());

  readonly selected = output<number>();
  readonly bulkToggled = output<number>();
  readonly menuRequested = output<number>();
  readonly orderCommitted = output<{ id: number; position: number }>();

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

  protected onRowContextMenu(event: Event, id: number): void {
    event.preventDefault();
    if (this.bulkMode()) {
      return;
    }
    this.manualFocusId.set(id);
    this.selected.emit(id);
    this.menuRequested.emit(id);
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

  protected onOrderClick(event: Event, id: number): void {
    event.stopPropagation();
    this.editingId.set(id);
  }

  protected onOrderKeydown(event: KeyboardEvent, id: number): void {
    event.stopPropagation();
    if (event.key === 'Enter') {
      this.commitOrderEdit(id, event.target);
    } else if (event.key === 'Escape') {
      this.editingId.set(null);
    }
  }

  protected commitOrderEdit(id: number, target: EventTarget | null): void {
    if (this.editingId() !== id) {
      return;
    }
    this.editingId.set(null);
    const input = target as HTMLInputElement | null;
    const value = input ? Number(input.value) : Number.NaN;
    if (Number.isInteger(value) && value >= 1) {
      this.orderCommitted.emit({ id, position: value });
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
      case 'openMenu':
        event.preventDefault();
        this.menuRequested.emit(intent.id);
        break;
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

  private focusRow(id: number): void {
    queueMicrotask(() => {
      this.elementRef.nativeElement
        .querySelector<HTMLElement>(`[data-testid="abwab-tree-row-${id}"]`)
        ?.focus();
    });
  }

  private resolveDirection(): 'ltr' | 'rtl' {
    const dirHost = this.elementRef.nativeElement.closest('[dir]');
    return dirHost?.getAttribute('dir') === 'rtl' ? 'rtl' : 'ltr';
  }
}
