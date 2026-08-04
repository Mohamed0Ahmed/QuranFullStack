import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { RESULT_COUNT_LABELS } from './result-count.labels';

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

  protected get labels() { return RESULT_COUNT_LABELS; }
}
