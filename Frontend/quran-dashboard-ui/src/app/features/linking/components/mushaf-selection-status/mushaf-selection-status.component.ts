import { ChangeDetectionStrategy, Component, ElementRef, inject, signal, viewChild } from '@angular/core';

import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { QdModalShellComponent } from '../../../../shared/ui/modal-shell/modal-shell.component';
import { LINKING_LABELS } from '../../models/linking.labels';
import { ManualMushafSelectionStore } from '../../state/manual-mushaf-selection.store';

@Component({
  selector: 'qd-mushaf-selection-status',
  standalone: true,
  imports: [QdActionDirective, QdModalShellComponent],
  templateUrl: './mushaf-selection-status.component.html',
  styleUrl: './mushaf-selection-status.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MushafSelectionStatusComponent {
  protected readonly selection = inject(ManualMushafSelectionStore);
  protected readonly labels = LINKING_LABELS;
  protected readonly reviewOpen = signal(false);
  private readonly cancelButton = viewChild<ElementRef<HTMLButtonElement>>('cancelButton');

  focusOwner(): void {
    this.cancelButton()?.nativeElement.focus();
  }

  isFocusOwner(): boolean {
    return document.activeElement === this.cancelButton()?.nativeElement;
  }

  protected cancel(): void {
    this.reviewOpen.set(false);
    this.selection.cancel();
  }

  protected clear(): void {
    this.reviewOpen.set(false);
    this.selection.clear();
  }

  protected toggleReview(): void {
    if (this.selection.selectedCount() > 0) {
      this.reviewOpen.set(true);
    }
  }

  protected addToWorkspace(): void {
    this.selection.addToWorkspace();
  }

  protected startDirectLink(): void {
    this.selection.startDirectLink();
  }

}
