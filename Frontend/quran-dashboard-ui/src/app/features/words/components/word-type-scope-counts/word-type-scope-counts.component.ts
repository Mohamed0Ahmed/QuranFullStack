import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';

import {
  WORD_TYPE_TABLE_VIEW_OPTIONS,
  WORD_TYPES_LOADING_LABEL,
  WORD_TYPES_RETRY_LABEL,
  WORD_TYPES_SCOPE_COUNTS_ERROR_LABEL,
  WORD_TYPES_SCOPE_COUNTS_LABEL,
} from '../../models/word-types.labels';
import { WordTypeScopeCountsDto, WordTypeTableView } from '../../models/word-types.models';
import { WordTypesExplorerFacade } from '../../state/word-types-explorer.facade';

/**
 * Scoped four-count summary strip for the Word Types page (Feature 026, US8). Sits between the filter
 * strip and the table-view tabs and shows how many words / roots / stems / lemmas the active scope
 * contains, reusing the view tabs' SHORT labels verbatim (كلمات | جذور | أصول | صيغ, same order and text
 * — the tabs are not renamed). Non-interactive apart from the error-state retry.
 *
 * This is a self-contained scope-counts widget for the one page it belongs to: it reads the facade's
 * scope-counts state and triggers a counts-only refetch directly, so the page class stays thin (the US8
 * additions live here, not in `word-types-explorer-page.component.ts`). Its own load lifecycle is fully
 * independent of the table — a counts failure shows the strip's compact error + retry and never blocks
 * the table — while a `tableFailed` table read hides the strip numbers (the scope is unconfirmed). The
 * host stays mounted through every transition (mounted-shell invariant); only its inner content toggles.
 */
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

  private readonly facade = inject(WordTypesExplorerFacade);
  protected readonly state = this.facade.scopeCountsState;

  // The four labeled counts, in the tabs' RTL order, each reusing the tab's short label verbatim.
  protected readonly items = computed<readonly ScopeCountItem[]>(() => {
    const counts = this.state().counts;
    return WORD_TYPE_TABLE_VIEW_OPTIONS.map((option) => ({
      key: option.value,
      label: option.label,
      value: counts ? countFor(counts, option.value) : 0,
    }));
  });

  // TDZ-safe getters (see words README): label consts read via readonly fields resolve to undefined in
  // the bundled test build.
  protected get ariaLabel() { return WORD_TYPES_SCOPE_COUNTS_LABEL; }
  protected get errorLabel() { return WORD_TYPES_SCOPE_COUNTS_ERROR_LABEL; }
  protected get retryLabel() { return WORD_TYPES_RETRY_LABEL; }
  protected get loadingLabel() { return WORD_TYPES_LOADING_LABEL; }

  protected retry(): void {
    this.facade.retryScopeCounts();
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
