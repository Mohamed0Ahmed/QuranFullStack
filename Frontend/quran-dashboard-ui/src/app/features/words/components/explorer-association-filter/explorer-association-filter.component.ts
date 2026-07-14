import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  output,
  signal,
} from '@angular/core';

import { AssociationOption } from '../../state/words-association-filters';
import { WORDS_ASSOCIATION_FILTER_LABELS } from '../../models/words-shared.labels';

export type { AssociationOption } from '../../state/words-association-filters';

/**
 * Presentational association-filter picker (Feature 026, US7). A labeled search-select used for the
 * Unique Words primary type / primary root, Lemmas root, and Stems primary root/lemma filters. It owns
 * no data: the page supplies <c>options</c> (loaded via the existing roots/lemmas apis or the word-types
 * tree read) and reacts to <c>searchChange</c>; selecting an option emits <c>selectionChange</c>.
 *
 * - <c>clientFilter</c> = true (small static lists, e.g. the type select): filters options locally.
 * - <c>clientFilter</c> = false (roots/lemmas): the page server-searches and passes the results in.
 */
@Component({
  selector: 'qd-explorer-association-filter',
  standalone: true,
  templateUrl: './explorer-association-filter.component.html',
  styleUrl: './explorer-association-filter.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ExplorerAssociationFilterComponent {
  readonly label = input.required<string>();
  readonly placeholder = input<string>('');
  readonly options = input<readonly AssociationOption[]>([]);
  readonly selectedId = input<string | number | null>(null);
  readonly selectedLabel = input<string | null>(null);
  readonly loading = input<boolean>(false);
  readonly disabled = input<boolean>(false);
  readonly clientFilter = input<boolean>(false);
  readonly testid = input<string>('explorer-association-filter');

  readonly searchChange = output<string>();
  readonly selectionChange = output<AssociationOption | null>();

  protected readonly expanded = signal(false);
  protected readonly query = signal('');

  // TDZ-safe getter (see words README): reading the labels const via a readonly field resolves to
  // undefined in the bundled test build.
  protected get labels() { return WORDS_ASSOCIATION_FILTER_LABELS; }

  protected readonly hasSelection = computed(() => this.selectedId() !== null);

  // The active selection's display text. On URL restore the option list may not carry the selected
  // entry yet (server-searched pickers start empty) — show the neutral active-filter badge rather
  // than leaking the raw id.
  protected readonly selectionText = computed(() => {
    const explicit = this.selectedLabel();
    if (explicit !== null && explicit.length > 0) {
      return explicit;
    }
    const id = this.selectedId();
    if (id === null) {
      return '';
    }
    const match = this.options().find((option) => option.id === id);
    return match?.label ?? this.labels.activeFilter;
  });

  protected readonly visibleOptions = computed<readonly AssociationOption[]>(() => {
    if (!this.clientFilter()) {
      return this.options();
    }
    const term = this.query().trim().toLowerCase();
    if (term.length === 0) {
      return this.options();
    }
    return this.options().filter((option) => option.label.toLowerCase().includes(term));
  });

  protected onQueryInput(value: string): void {
    this.query.set(value);
    if (!this.clientFilter()) {
      this.searchChange.emit(value.trim());
    }
  }

  protected onSelect(option: AssociationOption): void {
    this.selectionChange.emit(option);
    this.expanded.set(false);
    this.query.set('');
  }

  protected onClear(): void {
    this.selectionChange.emit(null);
    this.query.set('');
  }
}
