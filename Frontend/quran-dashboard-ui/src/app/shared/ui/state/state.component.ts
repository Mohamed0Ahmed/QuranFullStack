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

  readonly actionLabel = input<string | null>(null);

  // Additive, default off: every existing call-site stays byte-identical. On, the box's
  // block-size is reserved (never appears/disappears) and only the message fades in — see
  // state.component.scss and UI_STYLE_SYSTEM.md §17 (qd-state) / its N3 cross-reference.
  readonly reserve = input(false);

  readonly action = output<void>();
}
