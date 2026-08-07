import { ChangeDetectionStrategy, Component, computed, effect, inject, input, output, signal, viewChild } from '@angular/core';
import { A11yModule } from '@angular/cdk/a11y';

import { ModalScrollLockDirective } from '../../../../shared/ui/modal-scroll-lock/modal-scroll-lock.directive';
import { AbwabDoorFieldsFormComponent } from '../abwab-door-fields-form/abwab-door-fields-form.component';
import { AbwabWriteController, AbwabWriteOutcome } from '../../state/abwab-write.controller';
import { AbwabDoorDto } from '../../../../core/api/generated/models/abwab-door-dto';
import { AbwabTreeSectionDto } from '../../../../core/api/generated/models/abwab-tree-section-dto';
import { AbwabAuthoringFields, EMPTY_AUTHORING_FIELDS } from '../../models/abwab-templates.models';
import { ABWAB_LABELS } from '../../models/abwab.labels';

let nextModalId = 0;

@Component({
  selector: 'qd-abwab-door-modal',
  standalone: true,
  imports: [A11yModule, AbwabDoorFieldsFormComponent, ModalScrollLockDirective],
  templateUrl: './abwab-door-modal.component.html',
  styleUrl: './abwab-door-modal.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AbwabDoorModalComponent {
  private readonly writeController = inject(AbwabWriteController);

  readonly open = input(false);
  readonly door = input<AbwabDoorDto | null>(null);
  readonly parentId = input<number | null>(null);
  readonly parentName = input<string | null>(null);
  readonly activeSectionId = input<number | null>(null);
  readonly sections = input<readonly AbwabTreeSectionDto[]>([]);
  readonly canSave = input(false);

  readonly closed = output<void>();
  readonly saved = output<AbwabDoorDto | null>();

  private readonly fieldsForm = viewChild(AbwabDoorFieldsFormComponent);

  private readonly modalId = nextModalId++;

  protected readonly titleId = `abwab-door-modal-title-${this.modalId}`;

  protected readonly errorMessage = signal<string | null>(null);
  protected readonly confirmingDiscard = signal(false);
  protected readonly chosenSectionId = signal<number | null>(null);
  protected readonly sectionMissing = signal(false);
  protected readonly saveBusy = signal(false);

  protected readonly sectionId = `abwab-door-modal-section-${this.modalId}`;

  protected readonly needsSection = computed(
    () => !this.isEdit && this.parentId() === null && this.activeSectionId() === null,
  );

  protected readonly noSectionsAvailable = computed(
    () => this.needsSection() && this.sections().length === 0,
  );

  protected readonly initialFields = computed<AbwabAuthoringFields>(() => {
    const door = this.door();
    return door === null
      ? EMPTY_AUTHORING_FIELDS
      : {
          name: door.name,
          description: door.description ?? '',
          representativeAyahText: door.representativeAyahText ?? '',
          aliases: door.aliases,
        };
  });

  protected get modalTitle(): string {
    return this.isEdit ? ABWAB_LABELS.editDoorTitle : ABWAB_LABELS.addDoorTitle;
  }
  protected get contextText(): string {
    const door = this.door();
    if (door) {
      return ABWAB_LABELS.contextEdit(door.name);
    }
    const parentName = this.parentName();
    return parentName ? ABWAB_LABELS.contextParent(parentName) : ABWAB_LABELS.contextRoot;
  }
  protected get isEdit(): boolean {
    return this.door() !== null;
  }
  protected get saveLabel(): string { return ABWAB_LABELS.saveButton; }
  protected get cancelLabel(): string { return ABWAB_LABELS.cancelButton; }
  protected get dirtyCloseConfirmMessage(): string { return ABWAB_LABELS.dirtyCloseConfirm; }
  protected get discardChangesLabel(): string { return ABWAB_LABELS.discardChangesButton; }
  protected get keepEditingLabel(): string { return ABWAB_LABELS.keepEditingButton; }
  protected get sectionLabel(): string { return ABWAB_LABELS.doorModalSectionLabel; }
  protected get sectionRequiredError(): string { return ABWAB_LABELS.doorModalSectionRequiredError; }
  protected get noSectionsHint(): string { return ABWAB_LABELS.doorModalNoSectionsHint; }

  protected onSectionChange(value: string): void {
    this.chosenSectionId.set(value === '' ? null : Number(value));
    this.sectionMissing.set(false);
  }

  constructor() {
    effect(() => {
      if (!this.open()) {
        return;
      }
      this.errorMessage.set(null);
      this.confirmingDiscard.set(false);
      this.chosenSectionId.set(null);
      this.sectionMissing.set(false);
      this.saveBusy.set(false);
      setTimeout(() => this.fieldsForm()?.focusFirstField());
    });
  }

  protected onEscape(): void {
    if (this.confirmingDiscard()) {
      this.cancelDiscard();
      return;
    }
    this.requestClose();
  }

  protected requestClose(): void {
    if (this.fieldsForm()?.isDirty() ?? false) {
      this.confirmingDiscard.set(true);
      return;
    }
    this.closed.emit();
  }

  protected confirmDiscard(): void {
    this.confirmingDiscard.set(false);
    this.closed.emit();
  }

  protected cancelDiscard(): void {
    this.confirmingDiscard.set(false);
  }

  protected submit(): void {
    const fields = this.fieldsForm()?.current();
    if (!this.canSave() || !fields || this.saveBusy()) {
      return;
    }
    const name = fields.name.trim();
    if (!name) {
      this.errorMessage.set(ABWAB_LABELS.nameRequiredError);
      return;
    }

    const description = fields.description.trim() || null;
    const representativeAyahText = fields.representativeAyahText.trim() || null;
    const aliases = [...fields.aliases];
    const door = this.door();

    if (door) {
      this.saveBusy.set(true);
      this.writeController
        .updateDoor(door.id, { name, description, representativeAyahText, aliases, version: door.version })
        .subscribe((outcome) => this.handleOutcome(outcome));
      return;
    }

    const parentId = this.parentId();
    const sectionId = this.needsSection() ? this.chosenSectionId() : this.activeSectionId();
    if (this.needsSection() && sectionId === null) {
      this.sectionMissing.set(true);
      return;
    }

    this.saveBusy.set(true);
    this.writeController
      .createDoor({
        name,
        description,
        representativeAyahText,
        aliases,
        parentId,
        sectionId: parentId != null ? null : sectionId,
      })
      .subscribe((outcome) => this.handleOutcome(outcome));
  }

  private handleOutcome(outcome: AbwabWriteOutcome<AbwabDoorDto>): void {
    this.saveBusy.set(false);
    if (outcome.kind === 'success') {
      this.errorMessage.set(null);
      this.saved.emit(outcome.data);
      this.closed.emit();
      return;
    }
    this.errorMessage.set(outcome.message);
  }
}
