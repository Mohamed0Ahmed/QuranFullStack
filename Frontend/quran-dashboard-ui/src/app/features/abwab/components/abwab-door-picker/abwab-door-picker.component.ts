import { ChangeDetectionStrategy, Component, ElementRef, computed, input, output, signal, viewChild } from '@angular/core';

import { QdSkeletonRowsComponent } from '../../../../shared/ui/skeleton/skeleton-rows.component';
import { QdStateComponent } from '../../../../shared/ui/state/state.component';
import { AbwabNode } from '../../models/abwab.models';
import { ABWAB_LABELS } from '../../models/abwab.labels';

export type AbwabDoorPickerStatus = 'ready' | 'loading' | 'error' | 'empty';

// Radio grouping is document-scoped by `name`, and emulated encapsulation does not scope an
// attribute. Two pickers sharing a literal name would merge into one group across modals, so the
// name is minted per instance the way these modals already mint their `titleId`.
let nextPickerId = 0;

interface AbwabDoorPickerRow {
  readonly node: AbwabNode;
  readonly depth: number;
  readonly hasChildren: boolean;
  readonly isExpanded: boolean;
  readonly isDisabled: boolean;
}

function subtreeMatches(node: AbwabNode, query: string): boolean {
  return node.name.includes(query) || node.children.some((child) => subtreeMatches(child, query));
}

/**
 * The one searchable, expandable door picker behind «إضافة علاقة» and «نسخ إلى أبواب…»
 * (`docs/TESTING_DEBT.md` row 4's unification trigger, paid by Slice C).
 *
 * Selection is consumer-owned, like `qd-tabs`: the picker renders what `pickedIds` says and
 * emits `toggled`, so the two hosts keep their opposite selection rules — the relations modal
 * single-selects an anchor in bulk mode, the copy modal multi-selects targets — without the
 * picker knowing either. `single` is the one thing the picker cannot infer from that: which
 * *affordance* to render. It changes no selection logic, only checkbox vs radio, because a
 * checkbox promises "pick any number" and anchor-pick mode accepts exactly one.
 *
 * `excludedIds` hides a door but never its subtree: a door may relate to its own ancestor or
 * descendant, so hiding the anchor's children would remove a case the backend allows. Children
 * keep the excluded node's depth so the list has no orphaned indent.
 */
@Component({
  selector: 'qd-abwab-door-picker',
  standalone: true,
  imports: [QdSkeletonRowsComponent, QdStateComponent],
  templateUrl: './abwab-door-picker.component.html',
  styleUrl: './abwab-door-picker.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AbwabDoorPickerComponent {
  readonly nodes = input.required<readonly AbwabNode[]>();
  readonly pickedIds = input.required<readonly number[]>();
  readonly excludedIds = input<readonly number[]>([]);
  readonly disabledIds = input<readonly number[]>([]);
  readonly disabledTag = input('');
  /** Affordance only — see the class doc. The host still owns what a pick does. */
  readonly single = input(false);
  readonly status = input<AbwabDoorPickerStatus>('ready');
  readonly errorMessage = input('');
  /** Required, not optional: "there is nothing to pick" is the fallthrough answer whenever the
   * list is empty for no other stated reason, and a shared picker must not hold one consumer's
   * wording for it. An optional input here bought a silent blank list for whoever forgot it. */
  readonly emptyMessage = input.required<string>();
  readonly searchPlaceholder = input.required<string>();
  readonly testIdPrefix = input.required<string>();

  readonly toggled = output<number>();
  readonly retry = output<void>();

  private readonly searchInput = viewChild<ElementRef<HTMLInputElement>>('searchInput');

  protected readonly searchQuery = signal('');
  private readonly expandedIds = signal<ReadonlySet<number>>(new Set());

  protected get retryLabel(): string { return ABWAB_LABELS.retryButton; }
  protected get loadingLabel(): string { return ABWAB_LABELS.loadingTreeMessage; }
  protected get noMatchesLabel(): string { return ABWAB_LABELS.pickerNoMatches; }

  protected readonly pickerName = `abwab-door-picker-${nextPickerId++}`;

  /** A search that filters every row out is not the host's "there is nothing to pick": the doors
   * are there, the query just does not reach them. The `nodes()` term is what makes that true —
   * a typed query over an genuinely empty tree is still the host's answer, not this one. */
  protected readonly searchFoundNothing = computed(
    () => this.searchQuery().trim() !== '' && this.nodes().length > 0,
  );

  private readonly pickedSet = computed(() => new Set(this.pickedIds()));

  protected readonly rows = computed<readonly AbwabDoorPickerRow[]>(() => {
    const query = this.searchQuery().trim();
    const excluded = new Set(this.excludedIds());
    const disabled = new Set(this.disabledIds());
    const expanded = this.expandedIds();
    const rows: AbwabDoorPickerRow[] = [];

    const walk = (node: AbwabNode, depth: number): void => {
      if (query !== '' && !subtreeMatches(node, query)) {
        return;
      }
      const hasChildren = node.children.length > 0;
      // A search forces every matching path open, so a deep match is never hidden behind a
      // collapsed ancestor the user never touched.
      const isExpanded = expanded.has(node.id) || (query !== '' && hasChildren);
      const isExcluded = excluded.has(node.id);
      if (!isExcluded) {
        rows.push({ node, depth, hasChildren, isExpanded, isDisabled: disabled.has(node.id) });
      }
      if (isExcluded || isExpanded) {
        node.children.forEach((child) => walk(child, isExcluded ? depth : depth + 1));
      }
    };

    this.nodes().forEach((root) => walk(root, 0));
    return rows;
  });

  /** Both hosts open on a list, so their trap's auto-capture lands on a chip or a tab rather than
   * the control the user came to type in; they call this to correct it. */
  focusSearch(): void {
    this.searchInput()?.nativeElement.focus();
  }

  protected isPicked(doorId: number): boolean {
    return this.pickedSet().has(doorId);
  }

  protected expandAriaLabel(row: AbwabDoorPickerRow): string {
    return row.isExpanded
      ? ABWAB_LABELS.relationPickerCollapseAriaLabel(row.node.name)
      : ABWAB_LABELS.relationPickerExpandAriaLabel(row.node.name);
  }

  protected onSearchInput(event: Event): void {
    this.searchQuery.set((event.target as HTMLInputElement).value);
  }

  protected toggleExpanded(event: Event, doorId: number): void {
    event.stopPropagation();
    const next = new Set(this.expandedIds());
    if (!next.delete(doorId)) {
      next.add(doorId);
    }
    this.expandedIds.set(next);
  }

  protected togglePicked(row: AbwabDoorPickerRow): void {
    if (row.isDisabled) {
      return;
    }
    this.toggled.emit(row.node.id);
  }

  /** Radio groups move selection with the arrow keys, and that path fires `change` without the
   * click the row handler listens to — without this, keyboard selection would tick the control
   * while the host's state stayed on the previous door. Emitting twice when a click does happen
   * is harmless: `single` selection is idempotent by definition. */
  protected onRowChange(row: AbwabDoorPickerRow): void {
    if (!this.single()) {
      return;
    }
    this.togglePicked(row);
  }

  /** Closing a modal destroys this instance, so a reopen starts clean on its own. The host still
   * needs this for the path where it stays open and switches subject — a new anchor door must not
   * inherit the previous one's search query and expanded branches. */
  reset(): void {
    this.searchQuery.set('');
    this.expandedIds.set(new Set());
  }
}
