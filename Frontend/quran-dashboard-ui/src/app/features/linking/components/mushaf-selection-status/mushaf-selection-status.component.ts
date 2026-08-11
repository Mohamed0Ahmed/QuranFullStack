import { ChangeDetectionStrategy, Component, ElementRef, inject, viewChild } from '@angular/core';

import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { LINKING_LABELS } from '../../models/linking.labels';
import { ManualMushafSelectionStore } from '../../state/manual-mushaf-selection.store';

@Component({
  selector: 'qd-mushaf-selection-status',
  standalone: true,
  imports: [QdActionDirective],
  templateUrl: './mushaf-selection-status.component.html',
  styleUrl: './mushaf-selection-status.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MushafSelectionStatusComponent {
  protected readonly selection = inject(ManualMushafSelectionStore);
  protected readonly labels = LINKING_LABELS;
  private readonly cancelButton = viewChild<ElementRef<HTMLButtonElement>>('cancelButton');

  focusOwner(): void {
    this.cancelButton()?.nativeElement.focus();
  }

  isFocusOwner(): boolean {
    return document.activeElement === this.cancelButton()?.nativeElement;
  }

  protected cancel(): void {
    this.selection.cancel();
  }

  protected clear(): void {
    this.selection.clear();
  }

  protected addToWorkspace(): void {
    this.selection.addToWorkspace();
  }

  protected retry(verseKey: string): void {
    this.selection.retry(verseKey);
  }

  protected remove(verseKey: string): void {
    this.selection.remove(verseKey);
  }
}
