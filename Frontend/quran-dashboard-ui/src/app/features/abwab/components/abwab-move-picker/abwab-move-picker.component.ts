import { ChangeDetectionStrategy, Component, computed, effect, input, output, signal, untracked } from '@angular/core';
import { A11yModule } from '@angular/cdk/a11y';

import { AbwabNode } from '../../models/abwab.models';
import { AbwabTreeSectionDto } from '../../../../core/api/generated/models/abwab-tree-section-dto';
import { ABWAB_LABELS } from '../../models/abwab.labels';
import { ModalScrollLockDirective } from '../../../../shared/ui/modal-scroll-lock/modal-scroll-lock.directive';
import { QdTabsComponent } from '../../../../shared/ui/tabs/tabs.component';
import { QdTabDirective } from '../../../../shared/ui/tabs/tab.directive';

export interface AbwabMoveDestination {
  readonly targetParentId: number | null;
  /** Never null: a door always lands in a section, and the backend refuses a root move without one. */
  readonly targetSectionId: number;
}

function subtreeMatches(node: AbwabNode, query: string): boolean {
  return node.name.includes(query) || node.children.some((child) => subtreeMatches(child, query));
}

interface AbwabMovePickerRow {
  readonly node: AbwabNode;
  readonly depth: number;
  readonly hasChildren: boolean;
  readonly isExpanded: boolean;
}

/**
 * One-screen move destination picker (plan-slice-b.md T505, reshaped in ux-slice-m): a persistent
 * section strip sits above the destination list, and picking a section swaps the doors below it in
 * place. The two-stage flow it replaces hid every section behind a «تغيير القسم» control, so the
 * one thing a mover needs to see — which section they are aiming at — was the one thing the modal
 * never showed. There is no «بلا قسم» — every door belongs to a section, so "no section" is not a
 * destination.
 *
 * The strip auto-selects when the answer is already known: the door's own section for a single
 * move, the shared one for a bulk move where every selected door agrees. A bulk selection spanning
 * sections has no such answer, so the strip opens with nothing marked and the destination list is
 * replaced by a prompt until a section is picked — which is also why `confirm` is disabled in that
 * state rather than silently doing nothing. Shared by single and bulk move (the caller decides which
 * write to dispatch on `confirmed`). The destination list is **collapsed on open**: the active
 * section's root doors and nothing else, with branches opened by hand through a chevron that
 * mirrors `abwab-door-picker`'s contract (focusable, `aria-expanded`, leaves keep the element for
 * alignment only), and a search that filters the active section and forces every matching path
 * open. Search-driven expansion is derived, never written into `expandedIds`, so clearing the
 * query returns the list to exactly what the user had opened by hand — it can neither collapse
 * their work nor leave the tree open behind them. It is a flat list indented by depth rather than a nested one — every door at
 * any depth is a valid "nest anywhere" target, so depth is presentation here, not structure. This
 * reverses the earlier "renders everything flat, expanded" reading of plan.md §4's "expandable
 * door tree": a whole section arriving pre-expanded is a wall of rows to scroll past, and every
 * row in it is a destination the user did not ask to see. `excludedIds` is the moved door(s) plus
 * every descendant — the client half of the cycle guard; the server's `409 WouldCycle` remains
 * authoritative.
 *
 * **T402 decision:** `liveRoots` now arrives in the superset's global order, not per-section
 * `orderValue` order (`abwab-tree.builder.ts`). `destinationRows` walks it as given, so a
 * section's destination doors here follow the global order too. Deliberate, not a side
 * effect: this list is a destination picker, not an ordered outline of any one section — the
 * doors are the same regardless of which order they're listed in, and re-sorting to
 * `orderValue` would mean fetching/deriving a second order this component has no other use
 * for. Pinned by `abwab-move-picker.component.spec.ts`.
 */
let nextModalId = 0;

@Component({
  selector: 'qd-abwab-move-picker',
  standalone: true,
  imports: [A11yModule, ModalScrollLockDirective, QdTabsComponent, QdTabDirective],
  templateUrl: './abwab-move-picker.component.html',
  styleUrl: './abwab-move-picker.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AbwabMovePickerComponent {
  readonly open = input(false);
  readonly sections = input<readonly AbwabTreeSectionDto[]>([]);
  readonly liveRoots = input<readonly AbwabNode[]>([]);
  readonly excludedIds = input<ReadonlySet<number>>(new Set());
  /** The current sections of the doors being moved — one entry per door, duplicates included. */
  readonly movedSectionIds = input<readonly number[]>([]);
  readonly titleText = input('');

  readonly closed = output<void>();
  readonly confirmed = output<AbwabMoveDestination>();

  private readonly modalId = nextModalId++;
  protected readonly titleId = `abwab-move-picker-title-${this.modalId}`;
  protected readonly destinationsId = `abwab-move-picker-destinations-${this.modalId}`;

  /** Null until a section is picked — one screen needs one signal, so there is no separate stage. */
  protected readonly pickedSectionId = signal<number | null>(null);
  protected readonly pickedParentId = signal<number | null>(null);
  /**
   * Membership means EXPANDED, so the empty set is a collapsed list — the list opens on the
   * section's root doors and every branch below is opened by hand. The moved door's own parent
   * chain is deliberately not seeded: a move is a choice of a new home, and pre-opening the old
   * one puts the answer the user is moving away from at the top of the list.
   */
  private readonly expandedIds = signal<ReadonlySet<number>>(new Set());
  protected readonly searchQuery = signal('');

  protected get descriptionText(): string { return ABWAB_LABELS.movePickerDescription; }
  protected get sectionStripLabel(): string { return ABWAB_LABELS.movePickerSectionStripLabel; }
  protected get searchPlaceholder(): string { return ABWAB_LABELS.movePickerSearchPlaceholder; }
  protected get noMatchesLabel(): string { return ABWAB_LABELS.pickerNoMatches; }
  protected get pickSectionHint(): string { return ABWAB_LABELS.movePickerPickSectionHint; }
  protected get asMainDoorLabel(): string { return ABWAB_LABELS.asMainDoorOption; }
  protected get confirmLabel(): string { return ABWAB_LABELS.moveConfirm; }
  protected get cancelLabel(): string { return ABWAB_LABELS.cancelButton; }

  protected sectionTabId(sectionId: number): string {
    return `abwab-move-picker-section-${this.modalId}-${sectionId}`;
  }

  protected readonly destinationRows = computed<readonly AbwabMovePickerRow[]>(() => {
    const sectionId = this.pickedSectionId();
    if (sectionId === null) {
      return [];
    }
    const excluded = this.excludedIds();
    const expanded = this.expandedIds();
    const query = this.searchQuery().trim();
    const rows: AbwabMovePickerRow[] = [];

    function walk(node: AbwabNode, depth: number): void {
      if (query !== '' && !subtreeMatches(node, query)) {
        return;
      }
      // Children are filtered before they are counted, so a branch whose every child is excluded
      // reads as a leaf: a chevron that opens onto nothing is a worse answer than no chevron.
      // Exclusion is whole-subtree, so filtering per level never orphans a kept descendant.
      const children = node.children.filter((child) => !excluded.has(child.id));
      const hasChildren = children.length > 0;
      // A search forces every matching path open, so a deep match is never hidden behind a
      // collapsed ancestor. This is derived, never written into `expandedIds` — which is exactly
      // what makes clearing the query safe: expansion falls back to the set the user opened by
      // hand, so a search cannot collapse their work and cannot silently leave the tree open.
      const isExpanded = expanded.has(node.id) || (query !== '' && hasChildren);
      rows.push({ node, depth, hasChildren, isExpanded });
      if (isExpanded) {
        for (const child of children) {
          walk(child, depth + 1);
        }
      }
    }

    for (const root of this.liveRoots()) {
      if (root.sectionId === sectionId && !excluded.has(root.id)) {
        walk(root, 0);
      }
    }
    return rows;
  });

  /** Whether the active section offers any door at all once the cycle guard has taken its share. */
  private readonly sectionHasDoors = computed(() => {
    const sectionId = this.pickedSectionId();
    const excluded = this.excludedIds();
    return this.liveRoots().some((root) => root.sectionId === sectionId && !excluded.has(root.id));
  });

  /** A query that reaches nothing is not "this section has no doors" — the doors are there, the
   * query just does not reach them. The `sectionHasDoors` term is what separates the two answers:
   * in a section whose every root the cycle guard excluded, an empty list is the guard's answer,
   * not the query's, and «لا يوجد باب مطابق لبحثك» would be blaming the wrong thing. Same guard the
   * mirrored `abwab-door-picker` applies through its own `nodes()` term. */
  protected readonly searchFoundNothing = computed(
    () => this.searchQuery().trim() !== '' && this.sectionHasDoors() && this.destinationRows().length === 0,
  );

  constructor() {
    // Reset whenever the picker (re)opens for a new selection, then mark the strip's active cell
    // when the selection already answers which section it is — one door, or several that agree.
    //
    // `open()` is the ONLY tracked dependency. `movedSectionIds` is a fresh array on every snapshot
    // rebuild, so tracking it would let a refresh landing mid-pick re-run this reset and throw away a
    // stage-two choice the user had already made. The caller sets the moved ids before it opens the
    // picker, so reading them untracked here still reads the right ones.
    effect(() => {
      if (!this.open()) {
        return;
      }
      untracked(() => {
        const movedSectionIds = this.movedSectionIds();
        this.pickedSectionId.set(null);
        this.pickedParentId.set(null);
        this.expandedIds.set(new Set());
        this.searchQuery.set('');

        const distinct = new Set(movedSectionIds);
        if (distinct.size === 1) {
          this.pickSection([...distinct][0]);
        }
      });
    });
  }

  // Switching sections drops the destination pick with it: a parent door from the section the user
  // just left is not a destination in the one they arrived at. Expansion goes with it for the same
  // reason — the ids belonged to the other section's tree, and the new one opens collapsed. The
  // search query deliberately survives: it is a filter over whichever section is active, so
  // hopping the strip with a query typed is how a user finds a door whose section they have
  // forgotten. Clearing it on every hop would just mean retyping it on every hop.
  protected pickSection(sectionId: number): void {
    this.pickedSectionId.set(sectionId);
    this.pickedParentId.set(null);
    this.expandedIds.set(new Set());
  }

  protected expandAriaLabel(row: AbwabMovePickerRow): string {
    return row.isExpanded
      ? ABWAB_LABELS.relationPickerCollapseAriaLabel(row.node.name)
      : ABWAB_LABELS.relationPickerExpandAriaLabel(row.node.name);
  }

  protected onSearchInput(event: Event): void {
    this.searchQuery.set((event.target as HTMLInputElement).value);
  }

  protected toggleExpanded(event: Event, row: AbwabMovePickerRow): void {
    event.stopPropagation();
    const next = new Set(this.expandedIds());
    if (!next.delete(row.node.id)) {
      next.add(row.node.id);
    }
    this.expandedIds.set(next);
  }

  protected pickAsMain(): void {
    this.pickedParentId.set(null);
  }

  protected pickParent(id: number): void {
    this.pickedParentId.set(id);
  }

  // The template disables confirm while no section is marked, so the null branch is a guard, not a
  // reachable path.
  protected confirm(): void {
    const targetSectionId = this.pickedSectionId();
    if (targetSectionId === null) {
      return;
    }
    this.confirmed.emit({ targetParentId: this.pickedParentId(), targetSectionId });
  }

  protected cancel(): void {
    this.closed.emit();
  }
}
