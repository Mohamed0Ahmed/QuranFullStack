import { ChangeDetectionStrategy, Component, ElementRef, computed, input, output, viewChild } from '@angular/core';

import { AbwabModalKind } from '../../models/abwab.models';
import { ABWAB_LABELS } from '../../models/abwab.labels';

/**
 * Audit item 11's restore affordance: the only thing on screen that says a closed overlay is
 * still waiting, which is why it names the overlay rather than saying «استعادة» alone and why
 * the discard X — having no text — carries the same name in its `aria-label`.
 *
 * Presentational and page-driven: it neither reads nor writes the URL. `AbwabPageComponent`
 * renders it from the parsed `modal` key and turns both outputs into navigations, the same
 * split every other overlay in this feature uses.
 */
@Component({
  selector: 'qd-abwab-modal-restore',
  standalone: true,
  templateUrl: './abwab-modal-restore.component.html',
  styleUrl: './abwab-modal-restore.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AbwabModalRestoreComponent {
  readonly kind = input.required<AbwabModalKind>();
  /** The door a retained relations state is pinned to, when it carries one of its own rather
   * than following `door=` (ux-slice-l's reveal-retain). Naming it is the whole point: after a
   * reveal, «علاقات الباب» alone leaves the user guessing WHICH door's relations are waiting. */
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

  /** The page calls this after a retaining close, once this control has rendered. */
  focusRestore(): void {
    this.restoreButton().nativeElement.focus();
  }
}
