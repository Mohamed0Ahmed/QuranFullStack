import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

export type QdStateVariant = 'empty' | 'loading' | 'error';

@Component({
  selector: 'qd-state',
  standalone: true,
  templateUrl: './state.component.html',
  styleUrl: './state.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class QdStateComponent {
  readonly variant = input.required<QdStateVariant>();
  readonly message = input.required<string>();

  readonly actionLabel = input<string | null>(null);

  readonly action = output<void>();
}
