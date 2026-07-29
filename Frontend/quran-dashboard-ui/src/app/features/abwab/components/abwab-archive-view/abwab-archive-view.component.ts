import { ChangeDetectionStrategy, Component, ElementRef, computed, inject, input, output, signal } from '@angular/core';

import { AbwabNode } from '../../models/abwab.models';
import {
  AbwabTreeRow,
  flattenVisibleAbwabRows,
  resolveAbwabTreeKeyboardIntent,
} from '../abwab-tree/abwab-tree-keyboard.controller';
import { ABWAB_LABELS } from '../../models/abwab.labels';

/**
 * The archived-doors hierarchy (plan-slice-b.md T508). Restore is the **only** action
 * (plan.md §5.1/§4.5) — no edit/move/reorder/add-child/bulk anywhere here, so this is a
 * deliberately separate, smaller component rather than a projection slot bolted onto
 * `AbwabTreeComponent` (whose doc already states it carries no inline row-action
 * buttons). It reuses the tree's pure row-flattening and keyboard-intent helpers for
 * expand/collapse and roving tabindex (M20's "keyboard focus, within the archive tree"
 * cell) — the `select`/`toggleBulk`/`openMenu` intents that helper can also produce are
 * simply ignored here, since this view has no selection or bulk concept of its own.
 *
 * **A-live vs A-arch is read straight off the builder's partition**, never re-derived:
 * `buildAbwabTreeSnapshot` gives every archive root `depth = 0` exactly when its own
 * parent is live or absent (`abwab-tree.builder.ts`'s archive-root filter) — so `depth
 * === 0` ⇒ restorable, `depth > 0` ⇒ its parent is archived ⇒ restore is disabled with
 * «استرجع الأب أولًا» (M21). No child-count badge is rendered: every archived door's
 * live-child count is always 0 (archiving a subtree archives all of it), so the badge
 * would print a meaningless "0" on every branch.
 */
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

  readonly restoreRequested = output<number>();

  private readonly expandedIds = signal<ReadonlySet<number>>(new Set());
  private readonly manualFocusId = signal<number | null>(null);

  protected get restoreLabel(): string { return ABWAB_LABELS.restoreButton; }
  protected get restoreParentFirstHint(): string { return ABWAB_LABELS.restoreParentFirstHint; }

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

  protected onChevronClick(event: Event, row: AbwabTreeRow): void {
    event.stopPropagation();
    if (!row.hasChildren) {
      return;
    }
    this.setExpanded(row.id, !this.expandedIds().has(row.id));
  }

  protected onRestoreClick(id: number): void {
    this.restoreRequested.emit(id);
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
      // 'select' / 'toggleBulk' / 'openMenu' do not apply: the archive view has no
      // selection or bulk concept, and restore is a direct per-row button, not a menu.
      default:
        break;
    }
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
