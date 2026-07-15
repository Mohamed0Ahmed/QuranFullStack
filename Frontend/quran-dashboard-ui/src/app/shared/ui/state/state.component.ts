import { ChangeDetectionStrategy, Component, input } from '@angular/core';

export type QdStateVariant = 'empty' | 'loading' | 'error';

/**
 * The one empty / loading / error presentation app-wide
 * (UI_STYLE_SYSTEM.md §17 `qd-state`).
 *
 * Backed by the existing `.qd-empty-state` / `.qd-loading-state` /
 * `.qd-error-state` classes in `_components.scss` — those classes remain the
 * visual/backing layer; this component only standardizes which role each
 * variant renders with.
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
}
