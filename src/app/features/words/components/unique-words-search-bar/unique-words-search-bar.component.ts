import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import {
  SEARCH_LABEL,
  SEARCH_PLACEHOLDER,
  SORT_LABEL,
  UNIQUE_WORD_SORT_LABELS,
} from '../../models/unique-words.labels';
import {
  UNIQUE_WORD_SORT_KEYS,
  UniqueWordSort,
} from '../../models/unique-words.models';

interface SortOptionViewModel {
  key: UniqueWordSort;
  labelAr: string;
}

@Component({
  selector: 'qd-unique-words-search-bar',
  standalone: true,
  templateUrl: './unique-words-search-bar.component.html',
  styleUrl: './unique-words-search-bar.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UniqueWordsSearchBarComponent {
  readonly search = input<string>('');
  readonly sort = input<UniqueWordSort>('mushaf-order');

  readonly searchChange = output<string>();
  readonly sortChange = output<UniqueWordSort>();

  protected readonly searchLabel = SEARCH_LABEL;
  protected readonly sortLabel = SORT_LABEL;
  protected readonly placeholder = SEARCH_PLACEHOLDER;

  protected readonly sortOptions: readonly SortOptionViewModel[] = UNIQUE_WORD_SORT_KEYS.map((key) => ({
    key,
    labelAr: UNIQUE_WORD_SORT_LABELS[key],
  }));

  onSearchInput(event: Event): void {
    this.searchChange.emit((event.target as HTMLInputElement).value);
  }

  onSortChange(event: Event): void {
    const value = (event.target as HTMLSelectElement).value as UniqueWordSort;
    this.sortChange.emit(value);
  }
}
