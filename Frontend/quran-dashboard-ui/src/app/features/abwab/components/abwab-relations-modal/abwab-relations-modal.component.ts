import { ChangeDetectionStrategy, Component, computed, effect, input, output, signal, untracked, viewChild } from '@angular/core';
import { A11yModule } from '@angular/cdk/a11y';
import { Observable } from 'rxjs';

import { AbwabDoorPickerComponent } from '../abwab-door-picker/abwab-door-picker.component';
import { QdChipComponent } from '../../../../shared/ui/chip/chip.component';
import { QdTabDirective } from '../../../../shared/ui/tabs/tab.directive';
import { QdTabsComponent } from '../../../../shared/ui/tabs/tabs.component';
import { ConfirmDialogComponent } from '../../../../shared/ui/confirm-dialog/confirm-dialog.component';
import { ModalScrollLockDirective } from '../../../../shared/ui/modal-scroll-lock/modal-scroll-lock.directive';
import { QdSkeletonRowsComponent } from '../../../../shared/ui/skeleton/skeleton-rows.component';
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
  abwabRelationGroupKey,
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

@Component({
  selector: 'qd-abwab-relations-modal',
  standalone: true,
  imports: [
    A11yModule,
    AbwabDoorPickerComponent,
    ConfirmDialogComponent,
    ModalScrollLockDirective,
    QdChipComponent,
    QdSkeletonRowsComponent,
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
  readonly anchorRelationCount = input(0);
  readonly anchorPickMode = input(false);
  readonly bulkTargets = input<readonly AbwabRelationTarget[]>([]);
  readonly liveRoots = input<readonly AbwabNode[]>([]);
  readonly loadRelations = input.required<(doorId: number) => Observable<AbwabRelationsLoadResult>>();
  readonly refetchRelations = input.required<(doorId: number) => Observable<AbwabRelationsLoadResult>>();
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
  readonly revealRequested = output<number>();

  private readonly picker = viewChild(AbwabDoorPickerComponent);

  protected readonly relations = signal<readonly AbwabRelationVm[]>([]);
  protected readonly status = signal<'loading' | 'ready' | 'error'>('ready');
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly type = signal<AbwabRelationKind>('similarity');
  protected readonly direction = signal<AbwabRelationDirectionKind>('anchor-more');
  protected readonly pickedIds = signal<ReadonlySet<number>>(new Set());

  protected readonly pendingDelete = signal<AbwabRelationVm | null>(null);
  protected readonly deleteBusy = signal(false);
  protected readonly deleteError = signal<string | null>(null);

  protected readonly typeOptions = TYPE_ORDER;
  protected readonly titleId = `abwab-relations-modal-title-${nextModalId++}`;

  protected get addTitle(): string { return ABWAB_LABELS.relationAddTitle; }
  protected get descriptionText(): string { return ABWAB_LABELS.relationsModalDescription; }
  protected get emptyText(): string { return ABWAB_LABELS.relationsEmpty; }
  protected get loadingLabel(): string { return ABWAB_LABELS.relationsLoading; }
  protected get retryLabel(): string { return ABWAB_LABELS.retryButton; }
  protected get directionLabel(): string { return ABWAB_LABELS.relationDirectionLabel; }
  protected get typeTabsAriaLabel(): string { return ABWAB_LABELS.relationTypeTabsAriaLabel; }
  protected get alreadyLinkedLabel(): string { return ABWAB_LABELS.relationAlreadyLinked; }
  protected get pickerEmptyLabel(): string { return ABWAB_LABELS.relationPickerEmptyDoors; }
  protected get noneSelectedLabel(): string { return ABWAB_LABELS.relationNoneSelected; }
  protected get bulkAnchorHint(): string { return ABWAB_LABELS.relationsBulkAnchorHint; }
  protected get closeLabel(): string { return ABWAB_LABELS.relationsCloseButton; }
  protected get deleteConfirmTitle(): string { return ABWAB_LABELS.relationDeleteConfirmTitle; }
  protected get deleteConfirmLabel(): string { return ABWAB_LABELS.deleteConfirmButton; }
  protected get cancelLabel(): string { return ABWAB_LABELS.cancelButton; }
  protected get deleteConfirmSides(): string { return ABWAB_LABELS.relationDeleteConfirmSides; }

  protected deleteConfirmBody(relation: AbwabRelationVm): string {
    return ABWAB_LABELS.relationDeleteConfirmBody(
      this.anchorDoorName(),
      relation.otherDoorName,
      abwabRelationGroupKey(relation),
    );
  }

  protected deleteAriaLabel(doorName: string): string { return ABWAB_LABELS.relationDeleteAriaLabel(doorName); }
  protected revealAriaLabel(doorName: string): string { return ABWAB_LABELS.relationRevealAriaLabel(doorName); }

  protected requestReveal(otherDoorId: number): void {
    this.revealRequested.emit(otherDoorId);
  }

  protected readonly searchPlaceholder = computed(() =>
    this.anchorPickMode() ? ABWAB_LABELS.relationsBulkAnchorPlaceholder : ABWAB_LABELS.relationPickerPlaceholder,
  );

  protected readonly excludedTagLabel = computed(() =>
    this.anchorPickMode() ? ABWAB_LABELS.pickerExcludedTargetTag : ABWAB_LABELS.pickerExcludedAnchorTag,
  );

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

  private readonly excludedIds = computed<ReadonlySet<number>>(() => {
    if (this.anchorPickMode()) {
      return new Set(this.bulkTargets().map((target) => target.id));
    }
    const anchorId = this.anchorDoorId();
    return anchorId === null ? new Set<number>() : new Set([anchorId]);
  });

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
        this.status.set('ready');
        // The instance outlives a close (only the template is torn down), so a confirm still
        // pending when the modal closed would render again over the next door's list.
        this.pendingDelete.set(null);
        this.deleteBusy.set(false);
        this.deleteError.set(null);
        // Every read below is untracked on purpose: the count is snapshot-derived, so tracking it
        // would re-run this whole reset — draft picks included — the moment a write refreshes the
        // tree. The anchor id and `open` are the only legitimate triggers.
        if (!this.anchorPickMode() && anchorId !== null && this.anchorRelationCount() > 0) {
          // A zero-count door is answered from the snapshot: the request is skipped, not issued and
          // then hidden behind an empty state that is only true once it returns.
          this.loadWithSkeleton(anchorId);
        }
        // Both modes open on a list, so the trap's capture would stop at the first chip or tab
        // were the picker's search not marked `cdkFocusInitial`. With that mark the trap lands
        // here itself, and this call normally re-focuses what is already focused. It stays as the
        // jsdom path — auto-capture cannot fire there — and as the guard for a capture that
        // resolves before the picker renders.
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
      this.refetchAfterWrite(anchorId);
    });
  }

  protected remove(relation: AbwabRelationVm): void {
    this.pendingDelete.set(relation);
    this.deleteError.set(null);
  }

  protected cancelRemove(): void {
    if (this.deleteBusy()) {
      return;
    }
    this.pendingDelete.set(null);
    this.deleteError.set(null);
  }

  protected confirmRemove(): void {
    const relation = this.pendingDelete();
    if (relation === null || this.deleteBusy()) {
      return;
    }
    const anchorId = this.anchorDoorId();
    this.deleteBusy.set(true);
    this.deleteError.set(null);
    this.deleteRelation()(relation.id).subscribe((outcome) => {
      this.deleteBusy.set(false);
      if (outcome.kind !== 'success') {
        this.deleteError.set(outcome.message);
        return;
      }
      this.pendingDelete.set(null);
      this.errorMessage.set(null);
      if (anchorId !== null) {
        this.refetchAfterWrite(anchorId);
      }
    });
  }

  protected close(): void {
    this.closed.emit();
  }

  protected retryLoad(): void {
    const anchorId = this.anchorDoorId();
    if (anchorId === null) {
      return;
    }
    this.errorMessage.set(null);
    this.loadWithSkeleton(anchorId);
  }

  private loadWithSkeleton(anchorId: number): void {
    this.status.set('loading');
    this.consume(this.loadRelations()(anchorId));
  }

  private refetchAfterWrite(anchorId: number): void {
    this.consume(this.refetchRelations()(anchorId));
  }

  private consume(load$: Observable<AbwabRelationsLoadResult>): void {
    load$.subscribe((result) => {
      if (result.kind === 'success') {
        this.relations.set(result.relations);
        // Clearing here is what un-sticks an error the user has already recovered from: before
        // this, one failed load kept its message on screen for the life of the open modal.
        this.errorMessage.set(null);
        this.status.set('ready');
      } else {
        this.errorMessage.set(result.message);
        this.status.set('error');
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
