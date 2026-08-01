import { ChangeDetectionStrategy, Component, computed, effect, input, output, signal } from '@angular/core';
import { A11yModule } from '@angular/cdk/a11y';
import { Observable } from 'rxjs';

import { AbwabTreeSectionDto } from '../../../../core/api/generated/models/abwab-tree-section-dto';
import { AbwabSectionDto } from '../../../../core/api/generated/models/abwab-section-dto';
import { AbwabWriteOutcome } from '../../state/abwab-write.controller';
import { ABWAB_LABELS } from '../../models/abwab.labels';
import { QdStateComponent } from '../../../../shared/ui/state/state.component';
import { ModalScrollLockDirective } from '../../../../shared/ui/modal-scroll-lock/modal-scroll-lock.directive';

let nextModalId = 0;

/**
 * List / add / rename / reorder / delete-empty for sections (plan-slice-b.md T510).
 * Presentational: the write functions are inputs (bound by the page shell to
 * `AbwabSectionsController`) rather than an injected service, so this component's own
 * spec exercises the M27/M28 outcomes without standing up the controller/facade chain.
 * Rename always reads the section's row from the live `sections` input at submit time
 * (never a value captured when edit mode opened), since `version` goes stale the moment
 * any other write touches this scope.
 *
 * The dirty guard is this modal's one behavior change in Slice C: a rename draft or a typed
 * section name is unsaved work, and closing used to discard it silently. It follows the door
 * modal's trio rather than inventing a second confirmation shape.
 */
@Component({
  selector: 'qd-abwab-sections-modal',
  standalone: true,
  imports: [A11yModule, QdStateComponent, ModalScrollLockDirective],
  templateUrl: './abwab-sections-modal.component.html',
  styleUrl: './abwab-sections-modal.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AbwabSectionsModalComponent {
  readonly open = input(false);
  readonly sections = input<readonly AbwabTreeSectionDto[]>([]);
  readonly createSection = input.required<(name: string) => Observable<AbwabWriteOutcome<AbwabSectionDto>>>();
  readonly renameSection = input.required<
    (id: number, name: string, version: number) => Observable<AbwabWriteOutcome<AbwabSectionDto>>
  >();
  readonly deleteSection = input.required<(id: number) => Observable<AbwabWriteOutcome<unknown>>>();
  readonly reorderSection = input.required<
    (id: number, position: number, version: number) => Observable<AbwabWriteOutcome<AbwabSectionDto>>
  >();

  readonly closed = output<void>();

  protected readonly titleId = `abwab-sections-modal-title-${nextModalId++}`;

  protected readonly newSectionName = signal('');
  protected readonly editingId = signal<number | null>(null);
  protected readonly editingName = signal('');
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly confirmingDiscard = signal(false);

  /** Unsaved work is a typed section name or a rename draft that differs from the saved name;
   * an opened rename the user has not altered is not a change to discard. */
  private readonly isDirty = computed(() => {
    if (this.newSectionName().trim() !== '') {
      return true;
    }
    const editingId = this.editingId();
    if (editingId === null) {
      return false;
    }
    const saved = this.sections().find((section) => section.id === editingId);
    return saved !== undefined && this.editingName() !== saved.name;
  });

  protected get title(): string { return ABWAB_LABELS.sectionsModalTitle; }
  protected get nameLabel(): string { return ABWAB_LABELS.sectionNameLabel; }
  protected get addLabel(): string { return ABWAB_LABELS.addSectionButton; }
  protected get renameLabel(): string { return ABWAB_LABELS.renameSectionButton; }
  protected get deleteLabel(): string { return ABWAB_LABELS.deleteSectionButton; }
  protected get saveLabel(): string { return ABWAB_LABELS.saveButton; }
  protected get cancelLabel(): string { return ABWAB_LABELS.cancelButton; }
  protected get closeLabel(): string { return ABWAB_LABELS.cancelButton; }
  protected get dirtyCloseConfirmMessage(): string { return ABWAB_LABELS.dirtyCloseConfirm; }
  protected get discardChangesLabel(): string { return ABWAB_LABELS.discardChangesButton; }
  protected get keepEditingLabel(): string { return ABWAB_LABELS.keepEditingButton; }

  constructor() {
    // This modal is a static sibling on the page shell, so the instance outlives every close and
    // only its inner template is destroyed — unlike the door modal, whose drafts live in a
    // `abwab-door-fields-form` that `@if (open())` throws away. Without this reset «تجاهل
    // التغييرات» would hide the draft rather than discard it, and the next open would find the
    // modal dirty before the user touched anything.
    effect(() => {
      if (this.open()) {
        this.resetDraft();
      }
    });
  }

  private resetDraft(): void {
    this.newSectionName.set('');
    this.editingId.set(null);
    this.editingName.set('');
    this.errorMessage.set(null);
    this.confirmingDiscard.set(false);
  }

  protected onNewNameInput(event: Event): void {
    this.newSectionName.set((event.target as HTMLInputElement).value);
  }

  protected onEditingNameInput(event: Event): void {
    this.editingName.set((event.target as HTMLInputElement).value);
  }

  protected add(): void {
    const name = this.newSectionName().trim();
    if (!name) {
      return;
    }
    this.createSection()(name).subscribe((outcome) => {
      if (outcome.kind === 'success') {
        this.newSectionName.set('');
        this.errorMessage.set(null);
      } else {
        this.errorMessage.set(outcome.message);
      }
    });
  }

  protected startRename(section: AbwabTreeSectionDto): void {
    this.editingId.set(section.id);
    this.editingName.set(section.name);
    this.errorMessage.set(null);
  }

  protected cancelRename(): void {
    this.editingId.set(null);
  }

  protected saveRename(id: number): void {
    const current = this.sections().find((section) => section.id === id);
    const name = this.editingName().trim();
    if (!current || !name) {
      return;
    }
    this.renameSection()(id, name, current.version).subscribe((outcome) => {
      if (outcome.kind === 'success') {
        this.editingId.set(null);
        this.errorMessage.set(null);
      } else {
        this.errorMessage.set(outcome.message);
      }
    });
  }

  protected remove(id: number): void {
    this.deleteSection()(id).subscribe((outcome) => {
      this.errorMessage.set(outcome.kind === 'success' ? null : outcome.message);
    });
  }

  protected requestClose(): void {
    if (this.isDirty()) {
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
}
