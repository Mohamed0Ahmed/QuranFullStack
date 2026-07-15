import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

/**
 * Shared presentational search-row shell (Feature 027, Phase 1) for the four "words" explorers
 * (stems, unique-words, lemmas, roots). Renders the sr-only-labelled main search `<input>` and
 * projects each explorer's association-filter fields (root/lemma/type) beside it via
 * `<ng-content>`. Owns no state and no query-key knowledge: every page keeps its own
 * `searchDraft` signal and debounce `Subject`; this shell only relays the raw `input` event value
 * on every keystroke.
 *
 * Layout (flex row, RTL wrap/stacking at narrower widths) lives in the shared
 * `.qd-explorer-search-row` class (`src/styles/_words-explorer-layout.scss`) — applied on this
 * component's host — because the row is a transparent layout container with no own
 * background/border/shadow: it lives inside the recessed `.uw-toolbar-recess` surface, and
 * elevation belongs to the association-filter popover, not the row.
 */
@Component({
  selector: 'qd-explorer-search-row',
  standalone: true,
  templateUrl: './explorer-search-row.component.html',
  styleUrl: './explorer-search-row.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    role: 'search',
    class: 'qd-explorer-search-row',
  },
})
export class ExplorerSearchRowComponent {
  readonly searchValue = input.required<string>();
  readonly searchLabel = input.required<string>();
  readonly searchPlaceholder = input.required<string>();
  readonly searchTestid = input.required<string>();
  readonly disabled = input(false);

  readonly searchChange = output<string>();
}
