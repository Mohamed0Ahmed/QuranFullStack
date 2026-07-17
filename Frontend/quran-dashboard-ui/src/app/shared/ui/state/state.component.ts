import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

export type QdStateVariant = 'empty' | 'loading' | 'error';

/**
 * The one empty / loading / error presentation app-wide
 * (UI_STYLE_SYSTEM.md §17 `qd-state`).
 *
 * Backed by the existing `.qd-empty-state` / `.qd-loading-state` /
 * `.qd-error-state` classes in `_components.scss` — those classes remain the
 * visual/backing layer; this component only standardizes which role each
 * variant renders with.
 *
 * An `error` may offer exactly ONE recovery action: pass `actionLabel` and
 * handle `action`. Without a label the error stays plain text, and `loading`
 * and `empty` are never interactive. This is the single retry affordance for
 * transient failures (Feature 030, M3) — a sticky error the user cannot retry
 * leaves the detail unusable until they change identity or reload the page.
 */
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
