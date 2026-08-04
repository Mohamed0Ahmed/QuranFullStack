import { ChangeDetectionStrategy, Component, ElementRef, computed, input, output, viewChild } from '@angular/core';

import { AbwabModalKind } from '../../models/abwab.models';
import { ABWAB_LABELS } from '../../models/abwab.labels';

@Component({
  selector: 'qd-abwab-modal-restore',
  standalone: true,
  templateUrl: './abwab-modal-restore.component.html',
  styleUrl: './abwab-modal-restore.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AbwabModalRestoreComponent {
  readonly kind = input.required<AbwabModalKind>();
  readonly subjectDoorName = input<string | null>(null);

  readonly restore = output<void>();
  readonly discard = output<void>();

  private readonly restoreButton = viewChild.required<ElementRef<HTMLButtonElement>>('restoreButton');

  private readonly kindName = computed(() => {
    const doorName = this.subjectDoorName();
    return this.kind() === 'relations' && doorName !== null
      ? ABWAB_LABELS.relationsOfDoorKindName(doorName)
      : ABWAB_LABELS.modalKindNames[this.kind()];
  });

  protected readonly restoreLabel = computed(() => ABWAB_LABELS.modalRestoreLabel(this.kindName()));
  protected readonly discardAriaLabel = computed(() => ABWAB_LABELS.modalDiscardAriaLabel(this.kindName()));

  focusRestore(): void {
    this.restoreButton().nativeElement.focus();
  }
}
