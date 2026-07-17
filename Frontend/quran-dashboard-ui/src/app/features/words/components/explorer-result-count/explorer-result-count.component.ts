import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { WORDS_RESULT_COUNT_LABELS } from '../../models/words-shared.labels';

/**
 * Headline result-count stat for the four "normal" Words explorers (US4).
 *
 * Renders the label-prefix phrasing "عدد الـ…: N" from the page's existing
 * `listState().totalCount` — no new aggregation. States (spec FR-018):
 * loading → non-interactive skeleton; list error → a muted, aria-hidden
 * placeholder line (the page's own error state still owns the message);
 * success → the value, "0" on an empty scope.
 *
 * All three states occupy the same single line box, so the toolbar never moves
 * across a load or a failure (Feature 030, N3 row 6).
 */
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

  // TDZ-safe getter (see words README): label consts read via readonly fields resolve to
  // undefined in the bundled test build.
  protected get labels() { return WORDS_RESULT_COUNT_LABELS; }
}
