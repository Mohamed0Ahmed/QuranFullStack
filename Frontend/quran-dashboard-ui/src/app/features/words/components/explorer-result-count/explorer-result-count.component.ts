import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

/**
 * Headline result-count stat for the four "normal" Words explorers (US4).
 *
 * Renders the label-prefix phrasing "عدد الـ…: N" from the page's existing
 * `listState().totalCount` — no new aggregation. States (spec FR-018):
 * loading → non-interactive skeleton; list error → renders nothing (the page's
 * own error state owns the message); success → the value, "0" on an empty scope.
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
}
