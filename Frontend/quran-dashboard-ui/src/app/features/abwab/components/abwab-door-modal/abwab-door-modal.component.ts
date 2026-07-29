import { ChangeDetectionStrategy, Component, effect, inject, input, output, signal, untracked } from '@angular/core';

import { QdChipComponent } from '../../../../shared/ui/chip/chip.component';
import { ModalScrollLockDirective } from '../../../../shared/ui/modal-scroll-lock/modal-scroll-lock.directive';
import { AbwabWriteController, AbwabWriteOutcome } from '../../state/abwab-write.controller';
import { AbwabDoorDto } from '../../../../core/api/generated/models/abwab-door-dto';
import { ABWAB_LABELS } from '../../models/abwab.labels';

let nextModalId = 0;

/**
 * Add/edit door modal (plan-slice-b.md T414). Composes `.qd-modal`/`.qd-modal-backdrop`
 * and `qdModalScrollLock` rather than hand-rolling a dialog. Create-under-a-parent nulls
 * `sectionId` here (M10) even though `AbwabApi.createDoor` already strips the key at the
 * wire level (T405/M33) — defense in depth at the layer that decides *whether* a section
 * applies, not just how it is serialized.
 *
 * Tracking-data box: `AbwabDoorDto` carries no audit-seed columns on the wire (verified
 * against the generated model + `openapi/swagger.json` — no `createdAt`/`createdBy`/
 * `approvedAt`/`approvedBy`). Rather than inventing a date the mock hardcodes, this box
 * shows only what is honestly derivable: the mock's own "not available yet" placeholder
 * copy for added-by/approved-by (plan.md §3 — no audit system in this slice), and the
 * archive status (always "نشط" — edit is only reachable for live doors, §4.5).
 */
@Component({
  selector: 'qd-abwab-door-modal',
  standalone: true,
  imports: [QdChipComponent, ModalScrollLockDirective],
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

  readonly closed = output<void>();
  readonly saved = output<AbwabDoorDto>();

  protected readonly titleId = `abwab-door-modal-title-${nextModalId++}`;

  protected readonly name = signal('');
  protected readonly description = signal('');
  protected readonly ayah = signal('');
  protected readonly aliases = signal<string[]>([]);
  protected readonly aliasDraft = signal('');
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly confirmingDiscard = signal(false);

  private initialSnapshot = '';

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
  protected get nameLabel(): string { return ABWAB_LABELS.nameFieldLabel; }
  protected get descriptionLabel(): string { return ABWAB_LABELS.descriptionFieldLabel; }
  protected get descriptionPlaceholder(): string { return ABWAB_LABELS.descriptionPlaceholder; }
  protected get ayahLabel(): string { return ABWAB_LABELS.ayahFieldLabel; }
  protected get ayahPlaceholder(): string { return ABWAB_LABELS.ayahPlaceholder; }
  protected get ayahHint(): string { return ABWAB_LABELS.ayahHint; }
  protected get aliasLabel(): string { return ABWAB_LABELS.aliasFieldLabel; }
  protected get aliasPlaceholder(): string { return ABWAB_LABELS.aliasPlaceholder; }
  protected get saveLabel(): string { return ABWAB_LABELS.saveButton; }
  protected get cancelLabel(): string { return ABWAB_LABELS.cancelButton; }
  protected get dirtyCloseConfirmMessage(): string { return ABWAB_LABELS.dirtyCloseConfirm; }
  protected get discardChangesLabel(): string { return ABWAB_LABELS.discardChangesButton; }
  protected get keepEditingLabel(): string { return ABWAB_LABELS.keepEditingButton; }
  protected get trackingHeading(): string { return ABWAB_LABELS.trackingDataHeading; }
  protected get trackingAddedByLabel(): string { return ABWAB_LABELS.trackingAddedByLabel; }
  protected get trackingAddedByPlaceholder(): string { return ABWAB_LABELS.trackingAddedByPlaceholder; }
  protected get trackingApprovedLabel(): string { return ABWAB_LABELS.trackingApprovedLabel; }
  protected get trackingApprovedPlaceholder(): string { return ABWAB_LABELS.trackingApprovedPlaceholder; }
  protected get trackingArchiveLabel(): string { return ABWAB_LABELS.trackingArchiveLabel; }
  protected get trackingArchiveActiveValue(): string { return ABWAB_LABELS.trackingArchiveActiveValue; }

  protected removeAliasLabel(alias: string): string {
    return ABWAB_LABELS.removeAliasAriaLabel(alias);
  }

  constructor() {
    // `untracked` is load-bearing: resetFormFromInputs() both writes and (via
    // snapshotKey) reads the form signals, so without it those reads register as
    // effect dependencies and every keystroke re-triggers the reset, wiping the
    // user's input back to the door's original value.
    effect(() => {
      const isOpen = this.open();
      if (isOpen) {
        untracked(() => this.resetFormFromInputs());
      }
    });
  }

  private resetFormFromInputs(): void {
    const door = this.door();
    this.name.set(door?.name ?? '');
    this.description.set(door?.description ?? '');
    this.ayah.set(door?.representativeAyahText ?? '');
    this.aliases.set(door ? [...door.aliases] : []);
    this.aliasDraft.set('');
    this.errorMessage.set(null);
    this.confirmingDiscard.set(false);
    this.initialSnapshot = this.snapshotKey();
  }

  private snapshotKey(): string {
    return JSON.stringify({
      name: this.name(),
      description: this.description(),
      ayah: this.ayah(),
      aliases: this.aliases(),
    });
  }

  private get isDirty(): boolean {
    return this.snapshotKey() !== this.initialSnapshot;
  }

  protected onNameInput(event: Event): void {
    this.name.set((event.target as HTMLInputElement).value);
  }
  protected onDescriptionInput(event: Event): void {
    this.description.set((event.target as HTMLTextAreaElement).value);
  }
  protected onAyahInput(event: Event): void {
    this.ayah.set((event.target as HTMLInputElement).value);
  }
  protected onAliasDraftInput(event: Event): void {
    this.aliasDraft.set((event.target as HTMLInputElement).value);
  }

  protected onAliasEnter(event: Event): void {
    event.preventDefault();
    const value = this.aliasDraft().trim();
    if (!value) {
      return;
    }
    this.aliases.update((current) => [...current, value]);
    this.aliasDraft.set('');
  }

  protected removeAliasAt(index: number): void {
    this.aliases.update((current) => current.filter((_, i) => i !== index));
  }

  protected requestClose(): void {
    if (this.isDirty) {
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
    const name = this.name().trim();
    if (!name) {
      this.errorMessage.set(ABWAB_LABELS.nameRequiredError);
      return;
    }

    const description = this.description().trim() || null;
    const representativeAyahText = this.ayah().trim() || null;
    const aliases = this.aliases();
    const door = this.door();

    if (door) {
      this.writeController
        .updateDoor(door.id, { name, description, representativeAyahText, aliases, version: door.version })
        .subscribe((outcome) => this.handleOutcome(outcome));
      return;
    }

    const parentId = this.parentId();
    this.writeController
      .createDoor({
        name,
        description,
        representativeAyahText,
        aliases,
        parentId,
        sectionId: parentId != null ? null : this.activeSectionId(),
      })
      .subscribe((outcome) => this.handleOutcome(outcome));
  }

  private handleOutcome(outcome: AbwabWriteOutcome<AbwabDoorDto>): void {
    if (outcome.kind === 'success') {
      this.errorMessage.set(null);
      this.saved.emit(outcome.data);
      this.closed.emit();
      return;
    }
    this.errorMessage.set(outcome.message);
  }
}
