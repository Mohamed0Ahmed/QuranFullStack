import { ChangeDetectionStrategy, Component, computed, effect, inject, input, output, signal, untracked } from '@angular/core';

import { ConfirmDialogComponent } from '../../../../shared/ui/confirm-dialog/confirm-dialog.component';
import { QdStateComponent } from '../../../../shared/ui/state/state.component';
import { AbwabTreeSectionDto } from '../../../../core/api/generated/models/abwab-tree-section-dto';
import { AbwabNode } from '../../models/abwab.models';
import { ABWAB_LABELS } from '../../models/abwab.labels';
import { AbwabWriteController } from '../../state/abwab-write.controller';

/**
 * Confirms restoring an ARCHIVED DOOR, and — for a root whose section was retired meanwhile — asks
 * where it should go. Not to be confused with `abwab-modal-restore`, which reopens a minimized
 * overlay.
 *
 * The destination question is not cosmetic: the backend refuses such a restore without one, so
 * without this modal the archive view's button would produce an unresolvable 400. Sections have no
 * restore route, which is why the old section can never simply be reinstated.
 *
 * A child door has no question to answer — it returns under its live parent, in that parent's
 * current section — so it gets the confirmation and no selector.
 */
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
  /** Ancestor chain, outermost first — the door itself excluded. */
  readonly ancestors = input<readonly AbwabNode[]>([]);

  readonly closed = output<void>();
  readonly restored = output<void>();

  protected readonly chosenSectionId = signal<number | null>(null);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly busy = signal(false);
  /** A required-but-untouched field is not an error yet — same rule the door modal's shell follows. */
  protected readonly sectionTouched = signal(false);

  protected readonly selectId = 'abwab-door-restore-modal-section';

  protected readonly open = computed(() => this.door() !== null);

  /** Only a ROOT is asked: a child derives its parent's section, whatever that now is. */
  protected readonly needsDestination = computed(() => {
    const door = this.door();
    return door !== null && door.parentId === null;
  });

  /** Its section is gone, so "back where it came from" has no meaning and a choice is mandatory. */
  protected readonly destinationRequired = computed(
    () => this.needsDestination() && (this.door()?.sectionRetired ?? false),
  );

  protected readonly noSectionsAvailable = computed(
    () => this.destinationRequired() && this.sections().length === 0,
  );

  protected readonly confirmDisabled = computed(
    () => this.destinationRequired() && this.chosenSectionId() === null,
  );

  /** Only after the user has actually been at the control: announcing an error on a field nobody has
   * touched is noise, and the hint below already says what is wanted. */
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

  /** The subject's identity, not the node object. A snapshot rebuild hands this input a NEW object
   * for the same door, and tracking that object would re-run the reset below and throw away a
   * section the user had already chosen (the `abwab-move-picker` guard, same failure). */
  private readonly doorSubjectId = computed(() => this.door()?.id ?? null);

  constructor() {
    // A door whose section is still live is prefilled with it; one whose section is gone starts
    // empty, so the choice is made rather than inherited from a section that no longer exists.
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
    if (!door || this.confirmDisabled()) {
      return;
    }

    const chosen = this.chosenSectionId();
    this.busy.set(true);
    this.errorMessage.set(null);
    this.writeController
      .restoreDoor(door.id, {
        // Omitted unless this restore is also a re-section: the backend reads an absent key as
        // "back where it came from", which is the ordinary case and the only one a child allows.
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
        // Stays open on failure — a stale version or a name collision is worth retrying from here
        // rather than reopening from the archive list.
        this.errorMessage.set(outcome.message);
      });
  }

  protected cancel(): void {
    this.closed.emit();
  }
}
