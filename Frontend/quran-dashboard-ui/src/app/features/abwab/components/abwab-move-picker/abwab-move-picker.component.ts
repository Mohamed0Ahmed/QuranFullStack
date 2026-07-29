import { ChangeDetectionStrategy, Component, computed, effect, input, output, signal, untracked } from '@angular/core';

import { AbwabNode } from '../../models/abwab.models';
import { AbwabTreeSectionDto } from '../../../../core/api/generated/models/abwab-tree-section-dto';
import { ABWAB_LABELS } from '../../models/abwab.labels';

export interface AbwabMoveDestination {
  readonly targetParentId: number | null;
  readonly targetSectionId: number | null;
}

interface AbwabMovePickerRow {
  readonly node: AbwabNode;
  readonly depth: number;
}

/**
 * Two-stage move destination picker (plan-slice-b.md T505): stage one picks a section
 * (including «بلا قسم»), stage two picks a destination door inside it or «كباب رئيسي»
 * scoped to that section. Shared by single and bulk move (the caller decides which
 * write to dispatch on `confirmed`). The destination list renders flat, indented by
 * depth, rather than a collapsible tree — every door at any depth is already a valid
 * "nest anywhere" target, so there is nothing a collapse/expand toggle would add here
 * (deviation from plan.md §4's "expandable door tree" wording, recorded in the phase
 * completion note). `excludedIds` is the moved door(s) plus every descendant — the
 * client half of the cycle guard; the server's `409 WouldCycle` remains authoritative.
 */
@Component({
  selector: 'qd-abwab-move-picker',
  standalone: true,
  templateUrl: './abwab-move-picker.component.html',
  styleUrl: './abwab-move-picker.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AbwabMovePickerComponent {
  readonly open = input(false);
  readonly sections = input<readonly AbwabTreeSectionDto[]>([]);
  readonly liveRoots = input<readonly AbwabNode[]>([]);
  readonly excludedIds = input<ReadonlySet<number>>(new Set());
  readonly titleText = input('');

  readonly closed = output<void>();
  readonly confirmed = output<AbwabMoveDestination>();

  protected readonly stage = signal<'section' | 'destination'>('section');
  private readonly pickedSectionId = signal<number | null>(null);
  private readonly sectionPicked = signal(false);
  protected readonly pickedParentId = signal<number | null>(null);

  protected get descriptionText(): string { return ABWAB_LABELS.movePickerDescription; }
  protected get noSectionLabel(): string { return ABWAB_LABELS.noSectionOption; }
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
    // Reset to stage one whenever the picker (re)opens for a new selection.
    effect(() => {
      if (this.open()) {
        untracked(() => {
          this.stage.set('section');
          this.sectionPicked.set(false);
          this.pickedSectionId.set(null);
          this.pickedParentId.set(null);
        });
      }
    });
  }

  protected pickSection(sectionId: number | null): void {
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

  protected confirm(): void {
    this.confirmed.emit({ targetParentId: this.pickedParentId(), targetSectionId: this.pickedSectionId() });
  }

  protected cancel(): void {
    this.closed.emit();
  }
}
