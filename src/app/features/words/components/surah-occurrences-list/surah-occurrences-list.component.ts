import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { WORDS_LOADING_LABEL } from '../../models/words.labels';
import { UniqueWordSurahItemDto } from '../../models/unique-words.models';
import { ROW_NUMBER_HEADER, SURAH_NAME_HEADER, SURAH_OCCURRENCES_COUNT_HEADER } from '../../models/unique-words.labels';

@Component({
  selector: 'qd-surah-occurrences-list',
  standalone: true,
  templateUrl: './surah-occurrences-list.component.html',
  styleUrl: './surah-occurrences-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SurahOccurrencesListComponent {
  readonly surahs = input.required<readonly UniqueWordSurahItemDto[]>();
  readonly loading = input(false);

  protected readonly loadingRowPlaceholders = Array.from({ length: 8 });

  protected readonly rowNumberHeader = ROW_NUMBER_HEADER;
  protected readonly surahNameHeader = SURAH_NAME_HEADER;
  protected readonly occurrencesCountHeader = SURAH_OCCURRENCES_COUNT_HEADER;
  protected readonly loadingLabel = WORDS_LOADING_LABEL;
}
