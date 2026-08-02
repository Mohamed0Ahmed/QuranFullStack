import { ChangeDetectionStrategy, Component, computed, effect, input, output, signal, untracked, viewChild } from '@angular/core';
import { A11yModule } from '@angular/cdk/a11y';
import { Observable } from 'rxjs';

import { AbwabDoorPickerComponent, AbwabDoorPickerStatus } from '../abwab-door-picker/abwab-door-picker.component';
import { ModalScrollLockDirective } from '../../../../shared/ui/modal-scroll-lock/modal-scroll-lock.directive';
import { QdStateComponent } from '../../../../shared/ui/state/state.component';
import { AbwabWriteOutcome } from '../../state/abwab-write.controller';
import { AbwabNode } from '../../models/abwab.models';
import { ABWAB_LABELS } from '../../models/abwab.labels';
import { AbwabDoorDto } from '../../../../core/api/generated/models/abwab-door-dto';

let nextModalId = 0;

/**
 * «نسخ إلى أبواب…» — the multi-target picker for applying a template
 * (`abwab-templates-concept.html:142-160`, superseded by the ux-slice-g reversal below). The
 * preview states the whole contract before the write: each target gains the template's
 * elements (never the root itself, since ux-slice-g), that a copy can never be a root door, and
 * — the sentence the mockup does not have — that the copies are independent of the template
 * from birth (plan §5.6).
 *
 * The picker lists live doors only and offers no root-level option, which is what makes the
 * route's empty-targets `400` unreachable through the UI; the refusal exists so the route is not
 * a hole, not because a control leads to it. The writer's empty-template `400` is the same kind
 * of guarantee — this modal's `hasElements` affordance only makes it legible before the write; it
 * is a courtesy, not the check (§4.2-11).
 */
@Component({
  selector: 'qd-abwab-template-copy-modal',
  standalone: true,
  imports: [A11yModule, AbwabDoorPickerComponent, ModalScrollLockDirective, QdStateComponent],
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
  /** Abwab today offers no recovery from a failed doors fetch at all; this is the retry the
   * parent page's `AbwabSnapshotFacade` already supports via `load()`. */
  readonly retryDoors = output<void>();

  private readonly picker = viewChild(AbwabDoorPickerComponent);

  protected readonly titleId = `abwab-template-copy-modal-title-${nextModalId++}`;
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly pickedIds = signal<ReadonlySet<number>>(new Set());

  protected get descriptionText(): string { return ABWAB_LABELS.templateCopyDescription; }
  protected get emptyTemplateText(): string { return ABWAB_LABELS.templateCopyEmptyTemplate; }
  protected get previewNoRootText(): string { return ABWAB_LABELS.templateCopyPreviewNoRoot; }
  protected get previewDetachedText(): string { return ABWAB_LABELS.templateCopyPreviewDetached; }
  protected get searchPlaceholder(): string { return ABWAB_LABELS.templateCopySearchPlaceholder; }
  protected get noneSelectedLabel(): string { return ABWAB_LABELS.templateCopyNoneSelected; }
  protected get emptyDoorsLabel(): string { return ABWAB_LABELS.templateCopyEmptyDoors; }
  protected get loadingDoorsLabel(): string { return ABWAB_LABELS.loadingTreeMessage; }
  protected get cancelLabel(): string { return ABWAB_LABELS.cancelButton; }
  protected get retryLabel(): string { return ABWAB_LABELS.retryButton; }

  protected readonly titleText = computed(() => ABWAB_LABELS.templateCopyTitle(this.templateName()));

  protected readonly previewText = computed(() =>
    ABWAB_LABELS.templateCopyPreview(this.templateName(), this.templateNodeCount()),
  );

  /** A courtesy, not the guarantee: the writer's empty-template `400` is authoritative. This only
   * keeps a stale list from showing a disabled-looking confirm that would otherwise still promise
   * copies a template that has since lost its last child cannot produce (§4.2-11). */
  protected readonly hasElements = computed(() => this.templateNodeCount() > 0);

  /** The picker's own empty/loading/error block only renders when it has no rows, so mapping the
   * two snapshot inputs onto the status is exhaustive: whatever is not loading or failed is the
   * "no live doors" answer. */
  protected readonly pickerStatus = computed<AbwabDoorPickerStatus>(() =>
    this.doorsLoading() ? 'loading' : this.doorsError() ? 'error' : 'empty',
  );

  protected readonly pickedIdList = computed(() => Array.from(this.pickedIds()));

  private readonly nodesById = computed(() => {
    const byId = new Map<number, AbwabNode>();
    const walk = (node: AbwabNode): void => {
      byId.set(node.id, node);
      node.children.forEach(walk);
    };
    this.liveRoots().forEach(walk);
    return byId;
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

  constructor() {
    effect(() => {
      const isOpen = this.open();
      untracked(() => {
        if (!isOpen) {
          return;
        }
        this.resetDraft();
        // The trap now captures straight onto the picker's search (`cdkFocusInitial`), instead of
        // the first control above the list, so this call normally re-focuses what is already
        // focused. It stays as the jsdom path — auto-capture cannot fire there — and as the guard
        // for a capture that resolves before the picker renders.
        setTimeout(() => this.picker()?.focusSearch());
      });
    });
  }

  protected togglePicked(doorId: number): void {
    const next = new Set(this.pickedIds());
    if (!next.delete(doorId)) {
      next.add(doorId);
    }
    this.pickedIds.set(next);
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
    this.pickedIds.set(new Set());
    this.picker()?.reset();
  }
}
