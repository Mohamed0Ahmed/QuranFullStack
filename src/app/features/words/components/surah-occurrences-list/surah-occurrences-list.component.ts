import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { UniqueWordSurahItemDto } from '../../models/unique-words.models';
import { ROW_NUMBER_HEADER, SURAH_OCCURRENCES_COUNT_HEADER } from '../../models/unique-words.labels';

@Component({
  selector: 'qd-surah-occurrences-list',
  standalone: true,
  templateUrl: './surah-occurrences-list.component.html',
  styleUrl: './surah-occurrences-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SurahOccurrencesListComponent {
  readonly surahs = input.required<readonly UniqueWordSurahItemDto[]>();

  protected readonly rowNumberHeader = ROW_NUMBER_HEADER;
  protected readonly occurrencesCountHeader = SURAH_OCCURRENCES_COUNT_HEADER;
}
