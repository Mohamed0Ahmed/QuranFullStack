import { ChangeDetectionStrategy, Component, ElementRef, computed, inject, input, output, signal } from '@angular/core';

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
  templateUrl: './abwab-archive-view.component.html',
  styleUrl: './abwab-archive-view.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AbwabArchiveViewComponent {
  private readonly elementRef = inject<ElementRef<HTMLElement>>(ElementRef);

  readonly roots = input<readonly AbwabNode[]>([]);
  readonly ariaLabel = input('');
  readonly canRestoreDoor = input(false);

  readonly restoreRequested = output<number>();

  private readonly expandedIds = signal<ReadonlySet<number>>(new Set());
  private readonly manualFocusId = signal<number | null>(null);

  protected get restoreLabel(): string { return ABWAB_LABELS.restoreButton; }
  protected get restoreParentFirstHint(): string { return ABWAB_LABELS.restoreParentFirstHint; }
  protected get restorePermissionHint(): string { return ABWAB_LABELS.restorePermissionHint; }

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
    flattenVisibleAbwabRows(this.roots(), this.expandedIds()),
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
    queueMicrotask(() => {
      this.elementRef.nativeElement
        .querySelector<HTMLElement>(`[data-testid="abwab-archive-row-${id}"]`)
        ?.focus();
    });
  }

  private resolveDirection(): 'ltr' | 'rtl' {
    const dirHost = this.elementRef.nativeElement.closest('[dir]');
    return dirHost?.getAttribute('dir') === 'rtl' ? 'rtl' : 'ltr';
  }
}
