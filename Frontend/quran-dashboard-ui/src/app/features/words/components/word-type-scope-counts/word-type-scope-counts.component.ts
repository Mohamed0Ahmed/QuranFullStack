import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import {
  WORD_TYPE_TABLE_VIEW_OPTIONS,
  WORD_TYPES_LOADING_LABEL,
  WORD_TYPES_RETRY_LABEL,
  WORD_TYPES_SCOPE_COUNTS_ERROR_LABEL,
  WORD_TYPES_SCOPE_COUNTS_LABEL,
} from '../../models/word-types.labels';
import { WordTypeScopeCountsDto, WordTypeTableView, WordTypesScopeCountsState } from '../../models/word-types.models';

@Component({
  selector: 'qd-word-type-scope-counts',
  standalone: true,
  templateUrl: './word-type-scope-counts.component.html',
  styleUrl: './word-type-scope-counts.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WordTypeScopeCountsComponent {
  // The table's failure hides the strip's numbers (scope unconfirmed); the strip's own failure is owned
  // by its error state and never propagates to the table.
  readonly tableFailed = input(false);

  // The page owns the scope-counts load lifecycle (facade) and passes the current state in; retry is
  // delegated back to the page via an output so this strip stays presentational.
  readonly state = input.required<WordTypesScopeCountsState>();
  readonly retryRequested = output<void>();

  // The four labeled counts, in the tabs' RTL order, each reusing the tab's short label verbatim.
  protected readonly items = computed<readonly ScopeCountItem[]>(() => {
    const counts = this.state().counts;
    return WORD_TYPE_TABLE_VIEW_OPTIONS.map((option) => ({
      key: option.value,
      label: option.label,
      value: counts ? countFor(counts, option.value) : 0,
    }));
  });

  // Labels read through getters, not readonly fields (see words README).
  protected get ariaLabel() { return WORD_TYPES_SCOPE_COUNTS_LABEL; }
  protected get errorLabel() { return WORD_TYPES_SCOPE_COUNTS_ERROR_LABEL; }
  protected get retryLabel() { return WORD_TYPES_RETRY_LABEL; }
  protected get loadingLabel() { return WORD_TYPES_LOADING_LABEL; }

  protected retry(): void {
    this.retryRequested.emit();
  }
}

interface ScopeCountItem {
  readonly key: WordTypeTableView;
  readonly label: string;
  readonly value: number;
}

function countFor(counts: WordTypeScopeCountsDto, view: WordTypeTableView): number {
  switch (view) {
    case 'words': return counts.wordsCount;
    case 'roots': return counts.rootsCount;
    case 'stems': return counts.stemsCount;
    case 'lemmas': return counts.lemmasCount;
  }
}
