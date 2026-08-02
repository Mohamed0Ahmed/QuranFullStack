import { A11yModule } from '@angular/cdk/a11y';
import { ChangeDetectionStrategy, Component, ElementRef, effect, input, output, viewChild } from '@angular/core';

import { ModalScrollLockDirective } from '../modal-scroll-lock/modal-scroll-lock.directive';

let nextDialogId = 0;

/**
 * The house confirmation dialog. Supersedes hand-rolled `role="alertdialog"` blocks: those get the
 * role right and the focus handling wrong, and each one drifts from the next.
 *
 * Body content is projected, so a consumer composes whatever the decision needs — a path, a
 * selector, an inline error — while this keeps the framing, the roles, the focus trap, and the
 * dismissal routes. It does NOT replace an authoring-modal shell: those own a form and its dirty
 * state, which is a different contract.
 *
 * Initial focus is the CANCEL button, deliberately. A confirm dialog interrupts; the safe answer
 * should be the one a reflexive Enter produces.
 */
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
  /** `danger` maps confirm to the destructive role (UI_STYLE_SYSTEM §16.1). */
  readonly tone = input<'default' | 'danger'>('default');
  /** Disables both buttons while the decision is in flight, so a double confirm cannot fire twice. */
  readonly busy = input(false);
  readonly confirmDisabled = input(false);

  readonly confirmed = output<void>();
  readonly cancelled = output<void>();

  protected readonly titleId = `qd-confirm-dialog-title-${nextDialogId++}`;

  private readonly cancelButton = viewChild<ElementRef<HTMLButtonElement>>('cancelButton');

  constructor() {
    // cdkTrapFocusAutoCapture does not run in jsdom, and can resolve before the button renders in a
    // browser; this is both the test path and the guard for that race.
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
