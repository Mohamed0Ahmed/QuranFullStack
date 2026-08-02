import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * The feature-scoped operation-message region (plan-slice-b.md §4.1) — a single
 * `aria-live="polite"` `role="status"` strip standing in for a global toast that this
 * feature does not warrant (one caller does not earn a shared `qd-` contract). Always
 * mounted, even with no message, so a screen reader keeps one stable live region to
 * watch rather than one that mounts/unmounts per operation.
 */
@Component({
  selector: 'qd-abwab-announcer',
  standalone: true,
  templateUrl: './abwab-announcer.component.html',
  styleUrl: './abwab-announcer.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AbwabAnnouncerComponent {
  readonly message = input<string | null>(null);
}
