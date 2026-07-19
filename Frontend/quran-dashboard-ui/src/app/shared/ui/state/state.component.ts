import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

export type QdStateVariant = 'empty' | 'loading' | 'error';

// An error may offer exactly ONE recovery action (pass `actionLabel`, handle
// `action`); loading and empty are never interactive. That retry is the only
// escape from a transient failure — without it a sticky error leaves the detail
// unusable until the user changes identity or reloads.
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

  /** Arabic label of the error's single recovery action; omit for plain text. */
  readonly actionLabel = input<string | null>(null);

  readonly action = output<void>();
}
