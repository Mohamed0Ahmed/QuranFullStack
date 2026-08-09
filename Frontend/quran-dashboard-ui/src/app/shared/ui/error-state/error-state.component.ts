import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { QdActionDirective } from '../action/action.directive';

export type QdErrorSeverity = 'read' | 'write';

@Component({
  selector: 'qd-error-state',
  standalone: true,
  imports: [QdActionDirective],
  templateUrl: './error-state.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class QdErrorStateComponent {
  readonly message = input.required<string>();
  readonly severity = input<QdErrorSeverity>('read');
  readonly actionLabel = input<string | null>(null);
  readonly actionAriaLabel = input<string | null>(null);
  readonly reserve = input(false);
  readonly testId = input('qd-error-state');
  readonly actionTestId = input('qd-error-state-action');

  readonly action = output<void>();

  protected readonly role = computed(() => (this.severity() === 'write' ? 'alert' : null));
  protected readonly quiet = computed(() => this.reserve() && this.message() === '');
}
