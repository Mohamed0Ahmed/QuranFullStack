import { ChangeDetectionStrategy, Component, computed, effect, inject, input, output, signal, viewChild } from '@angular/core';
import { A11yModule } from '@angular/cdk/a11y';

import { ModalScrollLockDirective } from '../../../../shared/ui/modal-scroll-lock/modal-scroll-lock.directive';
import { AbwabDoorFieldsFormComponent } from '../abwab-door-fields-form/abwab-door-fields-form.component';
import { AbwabWriteController, AbwabWriteOutcome } from '../../state/abwab-write.controller';
import { AbwabDoorDto } from '../../../../core/api/generated/models/abwab-door-dto';
import { AbwabAuthoringFields, EMPTY_AUTHORING_FIELDS } from '../../models/abwab-templates.models';
import { ABWAB_LABELS } from '../../models/abwab.labels';

let nextModalId = 0;

/**
 * Add/edit door modal (plan-slice-b.md T414). Composes `.qd-modal`/`.qd-modal-backdrop`
 * and `qdModalScrollLock` rather than hand-rolling a dialog, and renders the four authoring
 * fields through the shared `qd-abwab-door-fields-form` — this shell keeps the framing, the
 * tracking box, the dirty guard's confirm strip, and the write dispatch.
 *
 * Create-under-a-parent nulls `sectionId` here (M10) even though `AbwabApi.createDoor` already
 * strips the key at the wire level (T405/M33) — defense in depth at the layer that decides
 * *whether* a section applies, not just how it is serialized. It stays in this shell: the shared
 * form has no concept of a section.
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

  readonly closed = output<void>();
  readonly saved = output<AbwabDoorDto>();

  private readonly fieldsForm = viewChild(AbwabDoorFieldsFormComponent);

  protected readonly titleId = `abwab-door-modal-title-${nextModalId++}`;

  protected readonly errorMessage = signal<string | null>(null);
  protected readonly confirmingDiscard = signal(false);

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
  protected get trackingHeading(): string { return ABWAB_LABELS.trackingDataHeading; }
  protected get trackingAddedByLabel(): string { return ABWAB_LABELS.trackingAddedByLabel; }
  protected get trackingAddedByPlaceholder(): string { return ABWAB_LABELS.trackingAddedByPlaceholder; }
  protected get trackingApprovedLabel(): string { return ABWAB_LABELS.trackingApprovedLabel; }
  protected get trackingApprovedPlaceholder(): string { return ABWAB_LABELS.trackingApprovedPlaceholder; }
  protected get trackingArchiveLabel(): string { return ABWAB_LABELS.trackingArchiveLabel; }
  protected get trackingArchiveActiveValue(): string { return ABWAB_LABELS.trackingArchiveActiveValue; }

  constructor() {
    // The form resets itself from `initialFields`; this clears only what the shell owns. Reopening
    // must not surface the previous attempt's error or a half-answered discard prompt.
    effect(() => {
      if (!this.open()) {
        return;
      }
      this.errorMessage.set(null);
      this.confirmingDiscard.set(false);
      // The trap auto-captures the first tabbable element during the render that follows this
      // effect, and that is the error box, not a field. Queuing the real target as a task lands
      // it after the capture rather than before it.
      setTimeout(() => this.fieldsForm()?.focusFirstField());
    });
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
    // The form and this button live under the same `@if (open())`, so the form is present
    // whenever the button is clickable.
    const fields = this.fieldsForm()?.current();
    if (!fields) {
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
