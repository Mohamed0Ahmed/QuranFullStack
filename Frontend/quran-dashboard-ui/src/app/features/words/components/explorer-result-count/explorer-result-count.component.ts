import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { WORDS_RESULT_COUNT_LABELS } from '../../models/words-shared.labels';

@Component({
  selector: 'qd-explorer-result-count',
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

  // TDZ-safe getter: label consts read via readonly fields resolve to undefined in the bundled test build.
  protected get labels() { return WORDS_RESULT_COUNT_LABELS; }
}
