import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { QdActionDirective } from '../action/action.directive';

@Component({
  selector: 'qd-empty-state',
  standalone: true,
  imports: [QdActionDirective],
  templateUrl: './empty-state.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class QdEmptyStateComponent {
  readonly message = input.required<string>();
  readonly actionLabel = input<string | null>(null);
  readonly reserve = input(false);
  readonly testId = input('qd-empty-state');
  readonly actionTestId = input('qd-empty-state-action');

  readonly action = output<void>();
}
