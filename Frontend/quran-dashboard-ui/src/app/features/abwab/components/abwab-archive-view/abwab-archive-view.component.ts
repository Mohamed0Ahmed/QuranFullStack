import { ChangeDetectionStrategy, Component, computed, input, output, signal, viewChild } from '@angular/core';

import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { QdHierarchyKeyboardDirective } from '../../../../shared/ui/hierarchy/hierarchy-keyboard.directive';

import { AbwabNode } from '../../models/abwab.models';
import {
  AbwabTreeRow,
  flattenVisibleAbwabRows,
  isNativeButtonActivation,
  resolveAbwabTreeKeyboardIntent,
} from '../abwab-tree/abwab-tree-keyboard.controller';
import { ABWAB_LABELS } from '../../models/abwab.labels';

@Component({
  selector: 'qd-abwab-archive-view',
  standalone: true,
  imports: [QdActionDirective, QdHierarchyKeyboardDirective],
  templateUrl: './abwab-archive-view.component.html',
  styleUrl: './abwab-archive-view.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AbwabArchiveViewComponent {
  private readonly hierarchy = viewChild.required(QdHierarchyKeyboardDirective);

  readonly roots = input<readonly AbwabNode[]>([]);
  readonly ariaLabel = input('');
  readonly canRestoreDoor = input(false);
  readonly matchedIds = input<ReadonlySet<number>>(new Set());
  readonly searchExpandedIds = input<ReadonlySet<number>>(new Set());

  readonly restoreRequested = output<number>();
  readonly inclusionsRequested = output<number>();

  private readonly expandedIds = signal<ReadonlySet<number>>(new Set());
  private readonly manualFocusId = signal<number | null>(null);

  protected get restoreLabel(): string { return ABWAB_LABELS.restoreButton; }
  protected get restoreParentFirstHint(): string { return ABWAB_LABELS.restoreParentFirstHint; }
  protected get restorePermissionHint(): string { return ABWAB_LABELS.restorePermissionHint; }
  protected get inclusionsLabel(): string { return ABWAB_LABELS.archivedInclusionsButton; }

  protected readonly nodesById = computed(() => {
    const map = new Map<number, AbwabNode>();
    const walk = (node: AbwabNode): void => {
      map.set(node.id, node);
      node.children.forEach(walk);
    };
    this.roots().forEach(walk);
    return map;
  });

  protected readonly visibleRows = computed<AbwabTreeRow[]>(() => {
    const searchExpandedIds = this.searchExpandedIds();
    const expandedIds = searchExpandedIds.size === 0
      ? this.expandedIds()
      : new Set([...this.expandedIds(), ...searchExpandedIds]);
    return flattenVisibleAbwabRows(this.roots(), expandedIds);
  });

  protected readonly rovingId = computed(() => {
    const rows = this.visibleRows();
    if (rows.length === 0) {
      return null;
    }
    const manual = this.manualFocusId();
    if (manual !== null && rows.some((row) => row.id === manual)) {
      return manual;
    }
    return rows[0].id;
  });

  protected expandAriaLabel(row: AbwabTreeRow, node: AbwabNode): string {
    return row.isExpanded
      ? ABWAB_LABELS.relationPickerCollapseAriaLabel(node.name)
      : ABWAB_LABELS.relationPickerExpandAriaLabel(node.name);
  }

  protected onChevronClick(event: Event, row: AbwabTreeRow): void {
    event.stopPropagation();
    if (!row.hasChildren) {
      return;
    }
    this.setExpanded(row.id, !this.expandedIds().has(row.id));
  }

  protected onRestoreClick(id: number): void {
    const node = this.nodesById().get(id);
    if (!this.canRestoreDoor() || !node || node.depth > 0) {
      return;
    }
    this.restoreRequested.emit(id);
  }

  protected onInclusionsClick(id: number): void {
    if (this.nodesById().has(id)) {
      this.inclusionsRequested.emit(id);
    }
  }

  protected inclusionsAriaLabel(node: AbwabNode): string {
    return ABWAB_LABELS.archivedInclusionsAriaLabel(
      node.name,
      node.inclusionSourceCount,
      node.inclusionConsumerCount,
    );
  }

  protected inclusionCount(node: AbwabNode): number {
    return node.inclusionSourceCount + node.inclusionConsumerCount;
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
      bulkMode: false,
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
        this.requestRestoreIfAllowed(intent.id);
        break;
      case 'openMenu':
        event.preventDefault();
        this.onInclusionsClick(intent.id);
        break;
      default:
        break;
    }
  }

  private requestRestoreIfAllowed(id: number): void {
    const node = this.nodesById().get(id);
    if (!this.canRestoreDoor() || !node || node.depth > 0) {
      return;
    }
    this.restoreRequested.emit(id);
  }

  protected restoreDisabled(node: AbwabNode): boolean {
    return node.depth > 0 || !this.canRestoreDoor();
  }

  protected restorePermissionHintId(id: number): string {
    return `abwab-archive-restore-permission-${id}`;
  }

  private setExpanded(id: number, expanded: boolean): void {
    this.expandedIds.update((current) => {
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
    this.hierarchy().focusRow(id);
  }

  private resolveDirection(): 'ltr' | 'rtl' {
    return this.hierarchy().direction();
  }
}
