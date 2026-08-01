import { ChangeDetectionStrategy, Component, effect, input, output, signal, viewChild } from '@angular/core';
import { A11yModule } from '@angular/cdk/a11y';
import { Observable } from 'rxjs';

import { ModalScrollLockDirective } from '../../../../shared/ui/modal-scroll-lock/modal-scroll-lock.directive';
import { AbwabDoorFieldsFormComponent } from '../abwab-door-fields-form/abwab-door-fields-form.component';
import { AbwabWriteOutcome } from '../../state/abwab-write.controller';
import { AbwabAuthoringFields, EMPTY_AUTHORING_FIELDS } from '../../models/abwab-templates.models';
import { ABWAB_LABELS } from '../../models/abwab.labels';

let nextModalId = 0;

/**
 * The template node's authoring shell — the same four fields as a door, through the same form
 * component, because the user's locked requirement is the same modal rather than a parallel one.
 * Presentational in the `abwab-sections-modal` sense: the submit arrives as a function input,
 * bound by the workshop page to `AbwabTemplatesController`, so this component never reaches for
 * a controller itself.
 *
 * No tracking-data box: a template node has no archive status and no audit seed to show, which
 * is why the door modal's box stays in *its* shell rather than becoming a flag on the form.
 */
@Component({
  selector: 'qd-abwab-template-node-modal',
  standalone: true,
  imports: [A11yModule, AbwabDoorFieldsFormComponent, ModalScrollLockDirective],
  templateUrl: './abwab-template-node-modal.component.html',
  styleUrl: './abwab-template-node-modal.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AbwabTemplateNodeModalComponent {
  readonly open = input(false);
  /** `null` when adding; the node's current field values when editing. */
  readonly fields = input<AbwabAuthoringFields | null>(null);
  readonly isEdit = input(false);
  /** The parent node's name when adding a child, the edited node's name when editing, and `null`
   * for the template root — whose context line says so instead. */
  readonly contextName = input<string | null>(null);
  readonly isRoot = input(false);
  readonly submitNode = input.required<(fields: AbwabAuthoringFields) => Observable<AbwabWriteOutcome<unknown>>>();

  readonly closed = output<void>();

  private readonly fieldsForm = viewChild(AbwabDoorFieldsFormComponent);

  protected readonly titleId = `abwab-template-node-modal-title-${nextModalId++}`;
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly confirmingDiscard = signal(false);

  protected get initialFields(): AbwabAuthoringFields {
    return this.fields() ?? EMPTY_AUTHORING_FIELDS;
  }

  protected get modalTitle(): string {
    return this.isEdit() ? ABWAB_LABELS.editTemplateNodeTitle : ABWAB_LABELS.addTemplateNodeTitle;
  }

  protected get contextText(): string {
    if (this.isRoot()) {
      return ABWAB_LABELS.templateNodeContextRoot;
    }
    const name = this.contextName() ?? '';
    return this.isEdit()
      ? ABWAB_LABELS.templateNodeContextEdit(name)
      : ABWAB_LABELS.templateNodeContextParent(name);
  }

  protected get saveLabel(): string { return ABWAB_LABELS.saveButton; }
  protected get cancelLabel(): string { return ABWAB_LABELS.cancelButton; }
  protected get dirtyCloseConfirmMessage(): string { return ABWAB_LABELS.dirtyCloseConfirm; }
  protected get discardChangesLabel(): string { return ABWAB_LABELS.discardChangesButton; }
  protected get keepEditingLabel(): string { return ABWAB_LABELS.keepEditingButton; }

  constructor() {
    effect(() => {
      if (!this.open()) {
        return;
      }
      this.errorMessage.set(null);
      this.confirmingDiscard.set(false);
      // The trap now captures straight onto this field (`cdkFocusInitial` in the fields form), so
      // this call normally re-focuses what is already focused and fires no second focus event.
      // It stays as the jsdom path — auto-capture cannot fire there — and as the guard for a
      // capture that resolves before the field renders.
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
    const fields = this.fieldsForm()?.current();
    if (!fields) {
      return;
    }
    if (!fields.name.trim()) {
      this.errorMessage.set(ABWAB_LABELS.nameRequiredError);
      return;
    }

    this.submitNode()(fields).subscribe((outcome) => {
      if (outcome.kind !== 'success') {
        this.errorMessage.set(outcome.message);
        return;
      }
      // The controller refreshes the template on success, so there is nothing to hand back.
      this.errorMessage.set(null);
      this.closed.emit();
    });
  }
}
