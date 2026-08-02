import { ChangeDetectionStrategy, Component, computed, effect, input, output, signal, untracked } from '@angular/core';
import { A11yModule } from '@angular/cdk/a11y';

import { AbwabNode } from '../../models/abwab.models';
import { AbwabTreeSectionDto } from '../../../../core/api/generated/models/abwab-tree-section-dto';
import { ABWAB_LABELS } from '../../models/abwab.labels';
import { ModalScrollLockDirective } from '../../../../shared/ui/modal-scroll-lock/modal-scroll-lock.directive';

export interface AbwabMoveDestination {
  readonly targetParentId: number | null;
  /** Never null: a door always lands in a section, and the backend refuses a root move without one. */
  readonly targetSectionId: number;
}

interface AbwabMovePickerRow {
  readonly node: AbwabNode;
  readonly depth: number;
}

/**
 * Two-stage move destination picker (plan-slice-b.md T505): stage one picks a section, stage two
 * picks a destination door inside it or «كباب رئيسي» scoped to that section. There is no
 * «بلا قسم» — every door belongs to a section, so "no section" is not a destination.
 *
 * Stage one auto-selects when the answer is already known: the door's own section for a single
 * move, the shared one for a bulk move where every selected door agrees. A bulk selection spanning
 * sections has no such answer and is asked. The auto-selection is a starting point, not a
 * commitment — stage two can always go back. Shared by single and bulk move (the caller decides which
 * write to dispatch on `confirmed`). The destination list renders flat, indented by
 * depth, rather than a collapsible tree — every door at any depth is already a valid
 * "nest anywhere" target, so there is nothing a collapse/expand toggle would add here
 * (deviation from plan.md §4's "expandable door tree" wording, recorded in the phase
 * completion note). `excludedIds` is the moved door(s) plus every descendant — the
 * client half of the cycle guard; the server's `409 WouldCycle` remains authoritative.
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
  imports: [A11yModule, ModalScrollLockDirective],
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

  protected readonly titleId = `abwab-move-picker-title-${nextModalId++}`;

  protected readonly stage = signal<'section' | 'destination'>('section');
  private readonly pickedSectionId = signal<number | null>(null);
  private readonly sectionPicked = signal(false);
  protected readonly pickedParentId = signal<number | null>(null);

  protected get descriptionText(): string { return ABWAB_LABELS.movePickerDescription; }
  protected get changeSectionLabel(): string { return ABWAB_LABELS.movePickerChangeSection; }
  protected get asMainDoorLabel(): string { return ABWAB_LABELS.asMainDoorOption; }
  protected get confirmLabel(): string { return ABWAB_LABELS.moveConfirm; }
  protected get cancelLabel(): string { return ABWAB_LABELS.cancelButton; }

  protected readonly destinationRows = computed<readonly AbwabMovePickerRow[]>(() => {
    if (!this.sectionPicked()) {
      return [];
    }
    const sectionId = this.pickedSectionId();
    const excluded = this.excludedIds();
    const rows: AbwabMovePickerRow[] = [];

    function walk(node: AbwabNode, depth: number): void {
      if (excluded.has(node.id)) {
        return;
      }
      rows.push({ node, depth });
      for (const child of node.children) {
        walk(child, depth + 1);
      }
    }

    for (const root of this.liveRoots()) {
      if (root.sectionId === sectionId) {
        walk(root, 0);
      }
    }
    return rows;
  });

  constructor() {
    // Reset whenever the picker (re)opens for a new selection, then skip stage one when the
    // selection already answers it — one door, or several that agree.
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
        this.stage.set('section');
        this.sectionPicked.set(false);
        this.pickedSectionId.set(null);
        this.pickedParentId.set(null);

        const distinct = new Set(movedSectionIds);
        if (distinct.size === 1) {
          this.pickSection([...distinct][0]);
        }
      });
    });
  }

  protected backToSections(): void {
    this.stage.set('section');
  }

  protected pickSection(sectionId: number): void {
    this.pickedSectionId.set(sectionId);
    this.sectionPicked.set(true);
    this.pickedParentId.set(null);
    this.stage.set('destination');
  }

  protected pickAsMain(): void {
    this.pickedParentId.set(null);
  }

  protected pickParent(id: number): void {
    this.pickedParentId.set(id);
  }

  // Stage two is only reachable once a section is picked, so the section is never null here.
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
