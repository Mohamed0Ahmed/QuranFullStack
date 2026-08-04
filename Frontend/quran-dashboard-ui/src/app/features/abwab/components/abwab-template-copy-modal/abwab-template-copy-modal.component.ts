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
  readonly doorsLoading = input(false);
  readonly doorsError = input<string | null>(null);
  readonly applyTemplate = input.required<
    (targetDoorIds: readonly number[]) => Observable<AbwabWriteOutcome<AbwabDoorDto[] | null>>
  >();

  readonly closed = output<void>();
  readonly retryDoors = output<void>();

  private readonly picker = viewChild(AbwabDoorPickerComponent);

  protected readonly titleId = `abwab-template-copy-modal-title-${nextModalId++}`;
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly pickedIds = signal<ReadonlySet<number>>(new Set());
  protected readonly applyBusy = signal(false);

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

  protected readonly hasElements = computed(() => this.templateNodeCount() > 0);

  protected readonly pickerStatus = computed<AbwabDoorPickerStatus>(() =>
    this.doorsLoading() ? 'loading' : this.doorsError() ? 'error' : this.liveRoots().length === 0 ? 'empty' : 'ready',
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

  protected readonly confirmLabel = computed(() => ABWAB_LABELS.templateCopyConfirmButton(this.pickedIds().size));

  constructor() {
    effect(() => {
      const isOpen = this.open();
      untracked(() => {
        if (!isOpen) {
          return;
        }
        this.resetDraft();
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
    if (targets.length === 0 || this.applyBusy()) {
      return;
    }
    this.applyBusy.set(true);
    this.applyTemplate()(targets).subscribe((outcome) => {
      this.applyBusy.set(false);
      if (outcome.kind !== 'success') {
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
    this.applyBusy.set(false);
    this.pickedIds.set(new Set());
    this.picker()?.reset();
  }
}
