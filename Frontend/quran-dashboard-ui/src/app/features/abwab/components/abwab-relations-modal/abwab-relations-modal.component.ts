import { ChangeDetectionStrategy, Component, computed, effect, input, output, signal, untracked, viewChild } from '@angular/core';
import { A11yModule } from '@angular/cdk/a11y';
import { Observable } from 'rxjs';

import { AbwabDoorPickerComponent } from '../abwab-door-picker/abwab-door-picker.component';
import { QdChipComponent } from '../../../../shared/ui/chip/chip.component';
import { QdTabDirective } from '../../../../shared/ui/tabs/tab.directive';
import { QdTabsComponent } from '../../../../shared/ui/tabs/tabs.component';
import { ModalScrollLockDirective } from '../../../../shared/ui/modal-scroll-lock/modal-scroll-lock.directive';
import { QdStateComponent } from '../../../../shared/ui/state/state.component';
import { AbwabWriteOutcome } from '../../state/abwab-write.controller';
import { AbwabRelationsLoadResult } from '../../state/abwab-relations.controller';
import {
  AbwabNode,
  AbwabRelationDirectionKind,
  AbwabRelationGroupKey,
  AbwabRelationGroupVm,
  AbwabRelationKind,
  AbwabRelationVm,
  groupAbwabRelations,
} from '../../models/abwab.models';
import { AbwabDoorRelationDto } from '../../../../core/api/generated/models/abwab-door-relation-dto';
import { ABWAB_LABELS } from '../../models/abwab.labels';

export interface AbwabRelationTarget {
  readonly id: number;
  readonly name: string;
}

const TYPE_ORDER: readonly AbwabRelationKind[] = ['similarity', 'opposition', 'comprehensiveness'];

const GROUP_LABELS: Readonly<Record<AbwabRelationGroupKey, string>> = {
  similarity: ABWAB_LABELS.relationGroupSimilarity,
  opposition: ABWAB_LABELS.relationGroupOpposition,
  'more-comprehensive': ABWAB_LABELS.relationGroupMoreComprehensive,
  'less-comprehensive': ABWAB_LABELS.relationGroupLessComprehensive,
};

const TYPE_LABELS: Readonly<Record<AbwabRelationKind, string>> = {
  similarity: ABWAB_LABELS.relationTypeSimilarity,
  opposition: ABWAB_LABELS.relationTypeOpposition,
  comprehensiveness: ABWAB_LABELS.relationTypeComprehensiveness,
};

/** The four display groups collapse back onto three dot colors: both شمولية groups are the same
 * relation type seen from the two ends, so they share one marker (contract `:202`). */
const GROUP_DOT_KIND: Readonly<Record<AbwabRelationGroupKey, AbwabRelationKind>> = {
  similarity: 'similarity',
  opposition: 'opposition',
  'more-comprehensive': 'comprehensiveness',
  'less-comprehensive': 'comprehensiveness',
};

let nextModalId = 0;

/**
 * The relations modal (plan §7 T601/T602), implementing `docs/design-preview/abwab-relations-concept.html`:
 * the four display groups, the type segment, the direction pill with its live preview, an
 * expandable/searchable door picker, and one multi-target add. Presentational in the
 * `abwab-sections-modal` sense — the read and the two writes arrive as function inputs, bound by
 * the page to `AbwabRelationsController`, so this component never reaches for the facade.
 *
 * Two modes, one component (§4, bulk entry). `anchorPickMode` inverts which side the picker
 * chooses: normally the anchor is the open door and the picker selects N targets; in bulk mode
 * the N selected doors are the fixed targets and the picker single-selects the anchor. Direction
 * stays anchor-relative in both, so the add call has one shape either way.
 */
@Component({
  selector: 'qd-abwab-relations-modal',
  standalone: true,
  imports: [
    A11yModule,
    AbwabDoorPickerComponent,
    ModalScrollLockDirective,
    QdChipComponent,
    QdStateComponent,
    QdTabDirective,
    QdTabsComponent,
  ],
  templateUrl: './abwab-relations-modal.component.html',
  styleUrl: './abwab-relations-modal.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AbwabRelationsModalComponent {
  readonly open = input(false);
  readonly anchorDoorId = input<number | null>(null);
  readonly anchorDoorName = input('');
  readonly anchorPickMode = input(false);
  readonly bulkTargets = input<readonly AbwabRelationTarget[]>([]);
  readonly liveRoots = input<readonly AbwabNode[]>([]);
  readonly loadRelations = input.required<(doorId: number) => Observable<AbwabRelationsLoadResult>>();
  readonly addRelations = input.required<
    (
      anchorDoorId: number,
      kind: AbwabRelationKind,
      direction: AbwabRelationDirectionKind | null,
      targetDoorIds: readonly number[],
    ) => Observable<AbwabWriteOutcome<AbwabDoorRelationDto[]>>
  >();
  readonly deleteRelation = input.required<(relationId: number) => Observable<AbwabWriteOutcome<unknown>>>();

  readonly closed = output<void>();

  private readonly picker = viewChild(AbwabDoorPickerComponent);

  protected readonly relations = signal<readonly AbwabRelationVm[]>([]);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly type = signal<AbwabRelationKind>('similarity');
  protected readonly direction = signal<AbwabRelationDirectionKind>('anchor-more');
  protected readonly pickedIds = signal<ReadonlySet<number>>(new Set());

  protected readonly typeOptions = TYPE_ORDER;
  protected readonly titleId = `abwab-relations-modal-title-${nextModalId++}`;

  protected get addTitle(): string { return ABWAB_LABELS.relationAddTitle; }
  protected get descriptionText(): string { return ABWAB_LABELS.relationsModalDescription; }
  protected get emptyText(): string { return ABWAB_LABELS.relationsEmpty; }
  protected get directionLabel(): string { return ABWAB_LABELS.relationDirectionLabel; }
  protected get typeTabsAriaLabel(): string { return ABWAB_LABELS.relationTypeTabsAriaLabel; }
  protected get alreadyLinkedLabel(): string { return ABWAB_LABELS.relationAlreadyLinked; }
  protected get pickerEmptyLabel(): string { return ABWAB_LABELS.relationPickerEmptyDoors; }
  protected get noneSelectedLabel(): string { return ABWAB_LABELS.relationNoneSelected; }
  protected get bulkAnchorHint(): string { return ABWAB_LABELS.relationsBulkAnchorHint; }
  protected get closeLabel(): string { return ABWAB_LABELS.relationsCloseButton; }

  protected deleteAriaLabel(doorName: string): string { return ABWAB_LABELS.relationDeleteAriaLabel(doorName); }

  /** Door mode's placeholder invites several doors; anchor-pick mode takes exactly one, and the
   * picker's radio affordance should not be the only place that says so. */
  protected readonly searchPlaceholder = computed(() =>
    this.anchorPickMode() ? ABWAB_LABELS.relationsBulkAnchorPlaceholder : ABWAB_LABELS.relationPickerPlaceholder,
  );

  /** The pill names whichever side the picker chooses, and the two modes choose opposite sides:
   * door mode picks the targets, anchor-pick mode picks the anchor. One pair of strings would
   * therefore be right in one mode and inverted in the other. */
  protected readonly anchorMoreLabel = computed(() =>
    this.anchorPickMode() ? ABWAB_LABELS.relationsBulkDirectionAnchorMore : ABWAB_LABELS.relationDirectionAnchorMore,
  );

  protected readonly anchorLessLabel = computed(() =>
    this.anchorPickMode() ? ABWAB_LABELS.relationsBulkDirectionAnchorLess : ABWAB_LABELS.relationDirectionAnchorLess,
  );

  protected typeLabel(kind: AbwabRelationKind): string { return TYPE_LABELS[kind]; }

  protected groupLabel(key: AbwabRelationGroupKey): string { return GROUP_LABELS[key]; }
  protected groupDotKind(key: AbwabRelationGroupKey): AbwabRelationKind { return GROUP_DOT_KIND[key]; }

  protected readonly titleText = computed(() =>
    this.anchorPickMode()
      ? ABWAB_LABELS.relationsBulkTitle(this.bulkTargets().length)
      : ABWAB_LABELS.relationsModalTitle(this.anchorDoorName()),
  );

  protected readonly groups = computed<readonly AbwabRelationGroupVm[]>(() => groupAbwabRelations(this.relations()));

  private readonly nodesById = computed(() => {
    const byId = new Map<number, AbwabNode>();
    const walk = (node: AbwabNode): void => {
      byId.set(node.id, node);
      node.children.forEach(walk);
    };
    this.liveRoots().forEach(walk);
    return byId;
  });

  /** Never pickable: the anchor itself in door mode (contract `:221`), and every fixed target in
   * anchor-pick mode — an anchor drawn from the bulk set would be a self-relation. */
  private readonly excludedIds = computed<ReadonlySet<number>>(() => {
    if (this.anchorPickMode()) {
      return new Set(this.bulkTargets().map((target) => target.id));
    }
    const anchorId = this.anchorDoorId();
    return anchorId === null ? new Set<number>() : new Set([anchorId]);
  });

  /** "Already linked" is per (pair, type) with no direction term (§5.2), so switching the type
   * segment re-computes which rows are blocked. Empty in anchor-pick mode: the flag would have to
   * mean "all N pairs already exist", a rule the user cannot see (T602). */
  private readonly linkedIds = computed<ReadonlySet<number>>(() => {
    if (this.anchorPickMode()) {
      return new Set<number>();
    }
    const kind = this.type();
    return new Set(this.relations().filter((relation) => relation.kind === kind).map((relation) => relation.otherDoorId));
  });

  protected readonly pickedIdList = computed(() => Array.from(this.pickedIds()));
  protected readonly excludedIdList = computed(() => Array.from(this.excludedIds()));
  protected readonly linkedIdList = computed(() => Array.from(this.linkedIds()));

  protected readonly pickedNames = computed(() => {
    const byId = this.nodesById();
    return Array.from(this.pickedIds(), (id) => byId.get(id)?.name ?? String(id));
  });

  /** Door mode counts the picked targets; anchor-pick mode counts the fixed bulk targets, which
   * is how many relations the one call actually creates once an anchor exists. */
  protected readonly addCount = computed(() => {
    if (!this.anchorPickMode()) {
      return this.pickedIds().size;
    }
    return this.pickedIds().size === 1 ? this.bulkTargets().length : 0;
  });

  protected readonly addButtonLabel = computed(() => ABWAB_LABELS.relationAddButton(this.addCount()));

  protected readonly selectedSummary = computed(() => ABWAB_LABELS.relationSelectedSummary(this.pickedNames()));

  protected readonly directionRowVisible = computed(() => this.type() === 'comprehensiveness');

  protected readonly directionPreview = computed(() => {
    const anchorIsMore = this.direction() === 'anchor-more';
    if (!this.anchorPickMode()) {
      return ABWAB_LABELS.relationDirectionPreview(this.anchorDoorName(), anchorIsMore);
    }
    const anchorName = this.pickedNames()[0];
    return anchorName === undefined
      ? this.bulkAnchorHint
      : ABWAB_LABELS.relationsBulkDirectionPreview(this.bulkTargets().length, anchorName, anchorIsMore);
  });

  constructor() {
    effect(() => {
      const isOpen = this.open();
      const anchorId = this.anchorDoorId();
      untracked(() => {
        if (!isOpen) {
          return;
        }
        this.resetDraft();
        this.relations.set([]);
        this.errorMessage.set(null);
        if (!this.anchorPickMode() && anchorId !== null) {
          this.reload(anchorId);
        }
        // Both modes open on a list, so the trap's auto-capture would stop at the first chip or
        // tab. Queued rather than called inline: that capture runs during the render that
        // follows this effect and would overwrite a synchronous focus here.
        setTimeout(() => this.picker()?.focusSearch());
      });
    });
  }

  protected pickType(kind: AbwabRelationKind): void {
    if (kind === this.type()) {
      return;
    }
    this.type.set(kind);
    // Picks are cleared with the type (contract `:268`): "already linked" is per type, so a
    // carried-over pick could be one the new type blocks.
    this.pickedIds.set(new Set());
  }

  protected pickDirection(value: AbwabRelationDirectionKind): void {
    this.direction.set(value);
  }

  /** Anchor-pick mode selects exactly one door — the picker renders what it is told and leaves
   * which-selection-rule-applies to its host. Selecting, not toggling: the control is a radio
   * there, and a radio group has no "click the selected one to clear it" gesture to mirror. */
  protected togglePicked(doorId: number): void {
    if (this.anchorPickMode()) {
      this.pickedIds.set(new Set([doorId]));
      return;
    }
    const next = new Set(this.pickedIds());
    if (!next.delete(doorId)) {
      next.add(doorId);
    }
    this.pickedIds.set(next);
  }

  protected add(): void {
    const picked = Array.from(this.pickedIds());
    const anchorPick = this.anchorPickMode();
    const anchorId = anchorPick ? (picked[0] ?? null) : this.anchorDoorId();
    const targetIds = anchorPick ? this.bulkTargets().map((target) => target.id) : picked;
    if (anchorId === null || targetIds.length === 0) {
      return;
    }
    const direction = this.type() === 'comprehensiveness' ? this.direction() : null;
    this.addRelations()(anchorId, this.type(), direction, targetIds).subscribe((outcome) => {
      if (outcome.kind !== 'success') {
        this.errorMessage.set(outcome.message);
        return;
      }
      this.errorMessage.set(null);
      this.pickedIds.set(new Set());
      // Anchor-pick mode shows no groups, so there is nothing here that could confirm the write;
      // closing hands the user back to the tree, where the flags did move.
      if (anchorPick) {
        this.closed.emit();
        return;
      }
      this.reload(anchorId);
    });
  }

  protected remove(relationId: number): void {
    const anchorId = this.anchorDoorId();
    this.deleteRelation()(relationId).subscribe((outcome) => {
      if (outcome.kind !== 'success') {
        this.errorMessage.set(outcome.message);
        return;
      }
      this.errorMessage.set(null);
      if (anchorId !== null) {
        this.reload(anchorId);
      }
    });
  }

  protected close(): void {
    this.closed.emit();
  }

  private reload(anchorId: number): void {
    this.loadRelations()(anchorId).subscribe((result) => {
      if (result.kind === 'success') {
        this.relations.set(result.relations);
      } else {
        this.errorMessage.set(result.message);
      }
    });
  }

  private resetDraft(): void {
    this.type.set('similarity');
    this.direction.set('anchor-more');
    this.pickedIds.set(new Set());
    this.picker()?.reset();
  }
}
