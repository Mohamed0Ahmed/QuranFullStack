import { ChangeDetectionStrategy, Component, computed, effect, input, output, signal, untracked } from '@angular/core';
import { Observable } from 'rxjs';

import { ModalScrollLockDirective } from '../../../../shared/ui/modal-scroll-lock/modal-scroll-lock.directive';
import { QdSkeletonRowsComponent } from '../../../../shared/ui/skeleton/skeleton-rows.component';
import { AbwabWriteOutcome } from '../../state/abwab-write.controller';
import { AbwabNode } from '../../models/abwab.models';
import { ABWAB_LABELS } from '../../models/abwab.labels';
import { AbwabDoorDto } from '../../../../core/api/generated/models/abwab-door-dto';

interface AbwabCopyPickerRow {
  readonly node: AbwabNode;
  readonly depth: number;
  readonly hasChildren: boolean;
  readonly isExpanded: boolean;
}

let nextModalId = 0;

function subtreeMatches(node: AbwabNode, query: string): boolean {
  return node.name.includes(query) || node.children.some((child) => subtreeMatches(child, query));
}

/**
 * «نسخ إلى أبواب…» — the multi-target picker for applying a template
 * (`abwab-templates-concept.html:142-160`). The preview states the whole contract before the
 * write: what each target gains, that a copy can never be a root door, and — the sentence the
 * mockup does not have — that the copies are independent of the template from birth (plan §5.6).
 *
 * The picker lists live doors only and offers no root-level option, which is what makes the
 * route's empty-targets `400` unreachable through the UI; the refusal exists so the route is not
 * a hole, not because a control leads to it.
 *
 * The picker logic is deliberately duplicated from `abwab-relations-modal.component.ts:167-197`
 * rather than extracted: that component has **no spec at all** (`docs/TESTING_DEBT.md` row 4), so
 * unifying them under a no-new-tests posture would mean refactoring untested code to save ~30
 * lines. The unification trigger is row 4's own — when the relations modal next changes shape and
 * gets its specs, both pickers become one.
 */
@Component({
  selector: 'qd-abwab-template-copy-modal',
  standalone: true,
  imports: [ModalScrollLockDirective, QdSkeletonRowsComponent],
  templateUrl: './abwab-template-copy-modal.component.html',
  styleUrl: './abwab-template-copy-modal.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AbwabTemplateCopyModalComponent {
  readonly open = input(false);
  readonly templateName = input('');
  readonly templateNodeCount = input(0);
  readonly liveRoots = input<readonly AbwabNode[]>([]);
  /** The doors snapshot is fetched as this modal opens, so an empty picker means "still
   * loading" until it resolves. Saying «لا توجد أبواب حية» there would be a positive false
   * statement, not a flash of nothing — and it is exactly what direct URL entry to the
   * workshop would hit, where no snapshot was ever loaded. */
  readonly doorsLoading = input(false);
  readonly doorsError = input<string | null>(null);
  readonly applyTemplate = input.required<
    (targetDoorIds: readonly number[]) => Observable<AbwabWriteOutcome<AbwabDoorDto[] | null>>
  >();

  readonly closed = output<void>();

  protected readonly titleId = `abwab-template-copy-modal-title-${nextModalId++}`;
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly searchQuery = signal('');
  protected readonly pickedIds = signal<ReadonlySet<number>>(new Set());
  private readonly expandedIds = signal<ReadonlySet<number>>(new Set());

  protected get descriptionText(): string { return ABWAB_LABELS.templateCopyDescription; }
  protected get previewNoRootText(): string { return ABWAB_LABELS.templateCopyPreviewNoRoot; }
  protected get previewDetachedText(): string { return ABWAB_LABELS.templateCopyPreviewDetached; }
  protected get searchPlaceholder(): string { return ABWAB_LABELS.templateCopySearchPlaceholder; }
  protected get noneSelectedLabel(): string { return ABWAB_LABELS.templateCopyNoneSelected; }
  protected get emptyDoorsLabel(): string { return ABWAB_LABELS.templateCopyEmptyDoors; }
  protected get loadingDoorsLabel(): string { return ABWAB_LABELS.loadingTreeMessage; }
  protected get cancelLabel(): string { return ABWAB_LABELS.cancelButton; }

  protected readonly titleText = computed(() => ABWAB_LABELS.templateCopyTitle(this.templateName()));

  protected readonly previewText = computed(() =>
    ABWAB_LABELS.templateCopyPreview(this.templateName(), this.templateNodeCount()),
  );

  private readonly nodesById = computed(() => {
    const byId = new Map<number, AbwabNode>();
    const walk = (node: AbwabNode): void => {
      byId.set(node.id, node);
      node.children.forEach(walk);
    };
    this.liveRoots().forEach(walk);
    return byId;
  });

  protected readonly pickerRows = computed<readonly AbwabCopyPickerRow[]>(() => {
    const query = this.searchQuery().trim();
    const expanded = this.expandedIds();
    const rows: AbwabCopyPickerRow[] = [];

    const walk = (node: AbwabNode, depth: number): void => {
      if (query !== '' && !subtreeMatches(node, query)) {
        return;
      }
      const hasChildren = node.children.length > 0;
      // A search forces every matching path open, so a deep match is never hidden behind a
      // collapsed ancestor the user never touched.
      const isExpanded = expanded.has(node.id) || (query !== '' && hasChildren);
      rows.push({ node, depth, hasChildren, isExpanded });
      if (isExpanded) {
        node.children.forEach((child) => walk(child, depth + 1));
      }
    };

    this.liveRoots().forEach((root) => walk(root, 0));
    return rows;
  });

  protected readonly pickedNames = computed(() => {
    const byId = this.nodesById();
    return Array.from(this.pickedIds(), (id) => byId.get(id)?.name ?? String(id));
  });

  protected readonly selectedSummary = computed(() => ABWAB_LABELS.templateCopySelectedSummary(this.pickedNames()));

  /** Always the number of targets, never a union: selecting a door and its own descendant
   * produces two independent copies. This is the deliberate opposite of bulk-archive's union
   * count, where archiving an ancestor already claims its descendants (plan §6.1). */
  protected readonly confirmLabel = computed(() => ABWAB_LABELS.templateCopyConfirmButton(this.pickedIds().size));

  protected expandAriaLabel(row: AbwabCopyPickerRow): string {
    return row.isExpanded
      ? ABWAB_LABELS.templateCopyCollapseAriaLabel(row.node.name)
      : ABWAB_LABELS.templateCopyExpandAriaLabel(row.node.name);
  }

  constructor() {
    effect(() => {
      const isOpen = this.open();
      untracked(() => {
        if (isOpen) {
          this.resetDraft();
        }
      });
    });
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

  protected togglePicked(row: AbwabCopyPickerRow): void {
    const next = new Set(this.pickedIds());
    if (!next.delete(row.node.id)) {
      next.add(row.node.id);
    }
    this.pickedIds.set(next);
  }

  protected isPicked(doorId: number): boolean {
    return this.pickedIds().has(doorId);
  }

  protected confirm(): void {
    const targets = Array.from(this.pickedIds());
    if (targets.length === 0) {
      return;
    }
    this.applyTemplate()(targets).subscribe((outcome) => {
      if (outcome.kind !== 'success') {
        // The apply is all-or-nothing, so nothing was created and every pick is still a valid
        // retry once the collision is resolved — the bulk-conflict precedent: the selection is
        // preserved rather than cleared.
        this.errorMessage.set(outcome.message);
        return;
      }
      this.errorMessage.set(null);
      this.closed.emit();
    });
  }

  protected close(): void {
    this.closed.emit();
  }

  private resetDraft(): void {
    this.errorMessage.set(null);
    this.searchQuery.set('');
    this.pickedIds.set(new Set());
    this.expandedIds.set(new Set());
  }
}
