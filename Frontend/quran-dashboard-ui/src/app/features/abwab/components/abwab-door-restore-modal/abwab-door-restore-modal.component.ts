import { ChangeDetectionStrategy, Component, computed, effect, inject, input, output, signal, untracked } from '@angular/core';

import { ConfirmDialogComponent } from '../../../../shared/ui/confirm-dialog/confirm-dialog.component';
import { QdStateComponent } from '../../../../shared/ui/state/state.component';
import { AbwabTreeSectionDto } from '../../../../core/api/generated/models/abwab-tree-section-dto';
import { AbwabNode } from '../../models/abwab.models';
import { ABWAB_LABELS } from '../../models/abwab.labels';
import { AbwabWriteController } from '../../state/abwab-write.controller';

@Component({
  selector: 'qd-abwab-door-restore-modal',
  standalone: true,
  imports: [ConfirmDialogComponent, QdStateComponent],
  templateUrl: './abwab-door-restore-modal.component.html',
  styleUrl: './abwab-door-restore-modal.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AbwabDoorRestoreModalComponent {
  private readonly writeController = inject(AbwabWriteController);

  readonly door = input<AbwabNode | null>(null);
  readonly sections = input<readonly AbwabTreeSectionDto[]>([]);
  readonly ancestors = input<readonly AbwabNode[]>([]);
  readonly canRestoreDoor = input(false);

  readonly closed = output<void>();
  readonly restored = output<void>();

  protected readonly chosenSectionId = signal<number | null>(null);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly busy = signal(false);
  protected readonly sectionTouched = signal(false);

  protected readonly selectId = 'abwab-door-restore-modal-section';

  protected readonly open = computed(() => this.door() !== null);

  protected readonly needsDestination = computed(() => {
    const door = this.door();
    return door !== null && door.parentId === null;
  });

  protected readonly destinationRequired = computed(
    () => this.needsDestination() && (this.door()?.sectionRetired ?? false),
  );

  protected readonly noSectionsAvailable = computed(
    () => this.destinationRequired() && this.sections().length === 0,
  );

  protected readonly confirmDisabled = computed(
    () => !this.canRestoreDoor() || (this.destinationRequired() && this.chosenSectionId() === null),
  );

  protected readonly sectionInvalid = computed(() => this.sectionTouched() && this.confirmDisabled());

  protected readonly pathText = computed(() =>
    this.ancestors().map((ancestor) => ancestor.name).join('، '),
  );

  protected get title(): string { return ABWAB_LABELS.restoreModalTitle; }
  protected get confirmLabel(): string { return ABWAB_LABELS.restoreModalConfirm; }
  protected get cancelLabel(): string { return ABWAB_LABELS.restoreModalCancel; }
  protected get sectionLabel(): string { return ABWAB_LABELS.restoreModalSectionLabel; }
  protected get retiredHint(): string { return ABWAB_LABELS.restoreModalRetiredHint; }
  protected get noSectionsHint(): string { return ABWAB_LABELS.restoreModalNoSectionsHint; }
  protected get childHint(): string { return ABWAB_LABELS.restoreModalChildHint; }
  protected get permissionHint(): string { return ABWAB_LABELS.restorePermissionHint; }

  private readonly doorSubjectId = computed(() => this.door()?.id ?? null);

  constructor() {
    effect(() => {
      this.doorSubjectId();
      const door = untracked(() => this.door());
      untracked(() => {
        this.errorMessage.set(null);
        this.busy.set(false);
        this.sectionTouched.set(false);
        this.chosenSectionId.set(
          door !== null && door.parentId === null && !door.sectionRetired ? door.sectionId : null,
        );
      });
    });
  }

  protected onSectionChange(value: string): void {
    this.sectionTouched.set(true);
    this.chosenSectionId.set(value === '' ? null : Number(value));
  }

  protected confirm(): void {
    const door = this.door();
    if (!this.canRestoreDoor() || !door || this.confirmDisabled()) {
      return;
    }

    const chosen = this.chosenSectionId();
    this.busy.set(true);
    this.errorMessage.set(null);
    this.writeController
      .restoreDoor(door.id, {
        ...(this.needsDestination() && chosen !== null && chosen !== door.sectionId
          ? { sectionId: chosen }
          : {}),
        version: door.version,
      })
      .subscribe((outcome) => {
        this.busy.set(false);
        if (outcome.kind === 'success') {
          this.restored.emit();
          this.closed.emit();
          return;
        }
        this.errorMessage.set(outcome.message);
      });
  }

  protected cancel(): void {
    this.closed.emit();
  }
}
