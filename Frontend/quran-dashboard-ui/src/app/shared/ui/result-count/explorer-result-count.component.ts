import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { RESULT_COUNT_LABELS } from './result-count.labels';

// `qd-explorer-result-count` is kept as a thin alias so the four existing words call-sites keep
// working untouched (Slice B2, T1001 — same dual-selector mechanism as `qd-panel-skeleton,
// qd-explorer-panel-skeleton`); `qd-result-count` is the neutral name new call-sites (abwab) use.
@Component({
  selector: 'qd-result-count, qd-explorer-result-count',
  standalone: true,
  templateUrl: './explorer-result-count.component.html',
  styleUrl: './explorer-result-count.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ExplorerResultCountComponent {
  readonly count = input.required<number>();
  readonly labelPrefix = input.required<string>();
  readonly loading = input(false);
  readonly hasError = input(false);

  protected readonly ariaLabel = computed(() => `${this.labelPrefix()}: ${this.count()}`);

  // TDZ-safe getter (see shared/README.md): label consts read via readonly fields resolve to
  // undefined in the bundled test build.
  protected get labels() { return RESULT_COUNT_LABELS; }
}
