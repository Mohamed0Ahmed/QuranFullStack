import { ChangeDetectionStrategy, Component, ElementRef, effect, input, output, viewChild } from '@angular/core';

import { QdActionDirective } from '../action/action.directive';
import { QdModalShellComponent } from '../modal-shell/modal-shell.component';

@Component({
  selector: 'qd-confirm-dialog',
  standalone: true,
  imports: [QdActionDirective, QdModalShellComponent],
  templateUrl: './confirm-dialog.component.html',
  styleUrl: './confirm-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConfirmDialogComponent {
  readonly open = input(false);
  readonly titleText = input.required<string>();
  readonly confirmLabel = input.required<string>();
  readonly cancelLabel = input.required<string>();
  readonly tone = input<'default' | 'danger'>('default');
  readonly busy = input(false);
  readonly confirmDisabled = input(false);
  readonly testIdPrefix = input('qd-confirm-dialog');

  readonly confirmed = output<void>();
  readonly cancelled = output<void>();

  private readonly cancelButton = viewChild<ElementRef<HTMLButtonElement>>('cancelButton');

  constructor() {
    effect(() => {
      if (this.open()) {
        setTimeout(() => this.cancelButton()?.nativeElement.focus());
      }
    });
  }

  protected confirm(): void {
    if (this.busy() || this.confirmDisabled()) {
      return;
    }
    this.confirmed.emit();
  }

  protected cancel(): void {
    if (this.busy()) {
      return;
    }
    this.cancelled.emit();
  }
}
