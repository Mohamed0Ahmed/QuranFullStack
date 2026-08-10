import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  Injector,
  afterNextRender,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
  untracked,
  viewChild,
} from '@angular/core';
import { Observable } from 'rxjs';

import { AbwabTreeSectionDto } from '../../../../core/api/generated/models/abwab-tree-section-dto';
import { AbwabSectionDto } from '../../../../core/api/generated/models/abwab-section-dto';
import { AbwabWriteOutcome } from '../../state/abwab-write.controller';
import { ABWAB_LABELS } from '../../models/abwab.labels';
import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { QdStateComponent } from '../../../../shared/ui/state/state.component';
import {
  QdModalShellComponent,
  QdModalShellDismissReason,
} from '../../../../shared/ui/modal-shell/modal-shell.component';
import { ConfirmDialogComponent } from '../../../../shared/ui/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'qd-abwab-sections-modal',
  standalone: true,
  imports: [QdActionDirective, QdStateComponent, QdModalShellComponent, ConfirmDialogComponent],
  templateUrl: './abwab-sections-modal.component.html',
  styleUrl: './abwab-sections-modal.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AbwabSectionsModalComponent {
  private readonly injector = inject(Injector);
  private readonly elementRef = inject<ElementRef<HTMLElement>>(ElementRef);

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
  readonly canCreateSection = input(false);
  readonly canEditSection = input(false);
  readonly canReorderSection = input(false);
  readonly canDeleteSection = input(false);

  readonly closed = output<void>();

  protected readonly newSectionName = signal('');
  protected readonly editingId = signal<number | null>(null);
  protected readonly editingName = signal('');
  protected readonly editingOrderId = signal<number | null>(null);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly addBusy = signal(false);

  protected readonly deleteConfirmId = signal<number | null>(null);
  protected readonly deleteBusy = signal(false);
  protected readonly deleteError = signal<string | null>(null);
  protected readonly deleteConfirmTarget = computed(
    () => this.sections().find((section) => section.id === this.deleteConfirmId()) ?? null,
  );
  protected readonly confirmingDiscard = signal(false);

  private readonly orderInput = viewChild<ElementRef<HTMLInputElement>>('orderInput');

  private readonly pendingOrderFocusId = signal<number | null>(null);

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
  protected get deleteConfirmTitle(): string { return ABWAB_LABELS.sectionDeleteConfirmTitle; }
  protected deleteConfirmBody(name: string): string { return ABWAB_LABELS.sectionDeleteConfirmBody(name); }
  protected get saveLabel(): string { return ABWAB_LABELS.saveButton; }
  protected get cancelLabel(): string { return ABWAB_LABELS.cancelButton; }
  protected get closeLabel(): string { return ABWAB_LABELS.cancelButton; }
  protected get dirtyCloseConfirmMessage(): string { return ABWAB_LABELS.dirtyCloseConfirm; }
  protected get discardChangesLabel(): string { return ABWAB_LABELS.discardChangesButton; }
  protected get keepEditingLabel(): string { return ABWAB_LABELS.keepEditingButton; }

  protected orderAriaLabel(sectionName: string, order: number): string {
    return ABWAB_LABELS.sectionOrderAriaLabel(sectionName, order);
  }

  protected orderInputAriaLabel(sectionName: string): string {
    return ABWAB_LABELS.sectionOrderInputAriaLabel(sectionName);
  }

  constructor() {
    effect(() => {
      if (this.open()) {
        this.resetDraft();
      }
    });

    effect(() => {
      this.sections();
      const id = untracked(this.pendingOrderFocusId);
      if (id === null) {
        return;
      }
      this.pendingOrderFocusId.set(null);
      this.focusOrderButton(id);
    });

    effect(() => {
      if (!this.canCreateSection()) {
        this.newSectionName.set('');
      }
      if (!this.canEditSection()) {
        this.editingId.set(null);
        this.editingName.set('');
      }
      if (!this.canReorderSection()) {
        this.editingOrderId.set(null);
      }
      if (!this.canDeleteSection()) {
        this.deleteConfirmId.set(null);
        this.deleteError.set(null);
      }
    });
  }

  private resetDraft(): void {
    this.newSectionName.set('');
    this.editingId.set(null);
    this.editingName.set('');
    this.editingOrderId.set(null);
    this.pendingOrderFocusId.set(null);
    this.errorMessage.set(null);
    this.addBusy.set(false);
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
    if (!this.canCreateSection() || !name || this.addBusy()) {
      return;
    }
    this.addBusy.set(true);
    this.createSection()(name).subscribe((outcome) => {
      this.addBusy.set(false);
      if (outcome.kind === 'success') {
        this.newSectionName.set('');
        this.errorMessage.set(null);
      } else {
        this.errorMessage.set(outcome.message);
      }
    });
  }

  protected startRename(section: AbwabTreeSectionDto): void {
    if (!this.canEditSection()) {
      return;
    }
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
    if (!this.canEditSection() || !current || !name) {
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

  protected requestRemove(id: number): void {
    if (!this.canDeleteSection()) {
      return;
    }
    this.deleteConfirmId.set(id);
    this.deleteError.set(null);
    this.deleteBusy.set(false);
  }

  protected cancelRemove(): void {
    if (this.deleteBusy()) {
      return;
    }
    this.deleteConfirmId.set(null);
    this.deleteError.set(null);
  }

  protected confirmRemove(): void {
    const id = this.deleteConfirmId();
    if (!this.canDeleteSection() || id === null || this.deleteBusy()) {
      return;
    }
    this.deleteBusy.set(true);
    this.deleteError.set(null);
    this.deleteSection()(id).subscribe((outcome) => {
      this.deleteBusy.set(false);
      if (outcome.kind === 'success') {
        this.deleteConfirmId.set(null);
        this.errorMessage.set(null);
        return;
      }
      this.deleteError.set(outcome.message);
    });
  }

  protected startOrderEdit(section: AbwabTreeSectionDto, event: Event): void {
    event.stopPropagation();
    if (!this.canReorderSection()) {
      return;
    }
    this.editingOrderId.set(section.id);
    this.errorMessage.set(null);
    afterNextRender(() => this.orderInput()?.nativeElement.focus(), { injector: this.injector });
  }

  protected onOrderKeydown(event: KeyboardEvent, id: number): void {
    event.stopPropagation();
    if (!this.canReorderSection()) {
      this.cancelOrderEdit(id);
      return;
    }
    if (event.key === 'Enter') {
      this.commitOrderEdit(id, event.target);
    } else if (event.key === 'Escape') {
      this.cancelOrderEdit(id);
    }
  }

  protected cancelOrderEdit(id: number): void {
    if (this.editingOrderId() !== id) {
      return;
    }
    this.editingOrderId.set(null);
    if (this.canReorderSection()) {
      this.focusOrderButton(id);
    }
  }

  protected commitOrderEdit(id: number, target: EventTarget | null): void {
    if (!this.canReorderSection() || this.editingOrderId() !== id) {
      return;
    }
    this.editingOrderId.set(null);
    this.focusOrderButton(id);

    const input = target as HTMLInputElement | null;
    const value = input ? Number(input.value) : Number.NaN;
    if (!Number.isInteger(value) || value < 1) {
      return;
    }
    const current = this.sections().find((section) => section.id === id);
    if (!current) {
      return;
    }
    this.pendingOrderFocusId.set(id);
    this.reorderSection()(id, value, current.version).subscribe((outcome) => {
      if (outcome.kind === 'success') {
        this.errorMessage.set(null);
      } else {
        this.pendingOrderFocusId.set(null);
        this.errorMessage.set(outcome.message);
      }
    });
  }

  private focusOrderButton(id: number): void {
    afterNextRender(
      () => this.elementRef.nativeElement
        .querySelector<HTMLButtonElement>(`[data-testid="abwab-sections-modal-order-${id}"]`)
        ?.focus(),
      { injector: this.injector },
    );
  }

  protected onDismissed(reason: QdModalShellDismissReason): void {
    if (reason === 'escape') {
      this.onEscape();
      return;
    }
    this.requestClose();
  }

  protected onEscape(): void {
    if (this.confirmingDiscard()) {
      this.cancelDiscard();
      return;
    }
    this.requestClose();
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
