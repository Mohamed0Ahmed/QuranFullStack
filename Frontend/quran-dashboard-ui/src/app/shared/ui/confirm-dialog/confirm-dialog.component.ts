import { A11yModule } from '@angular/cdk/a11y';
import { ChangeDetectionStrategy, Component, ElementRef, effect, input, output, viewChild } from '@angular/core';

import { ModalScrollLockDirective } from '../modal-scroll-lock/modal-scroll-lock.directive';

let nextDialogId = 0;

@Component({
  selector: 'qd-confirm-dialog',
  standalone: true,
  imports: [A11yModule, ModalScrollLockDirective],
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

  protected readonly titleId = `qd-confirm-dialog-title-${nextDialogId++}`;

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
