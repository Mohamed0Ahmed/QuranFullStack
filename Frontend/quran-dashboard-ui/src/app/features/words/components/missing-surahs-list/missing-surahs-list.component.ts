import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { WORDS_LOADING_LABEL } from '../../models/words.labels';
import { MissingSurahItemDto } from '../../models/unique-words.models';
import { ROW_NUMBER_HEADER, SURAH_NAME_HEADER } from '../../models/unique-words.labels';

@Component({
  selector: 'qd-missing-surahs-list',
  standalone: true,
  templateUrl: './missing-surahs-list.component.html',
  styleUrl: './missing-surahs-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MissingSurahsListComponent {
  readonly surahs = input.required<readonly MissingSurahItemDto[]>();
  readonly loading = input(false);

  protected readonly loadingRowPlaceholders = Array.from({ length: 8 });

  protected readonly rowNumberHeader = ROW_NUMBER_HEADER;
  protected readonly surahNameHeader = SURAH_NAME_HEADER;
  protected readonly loadingLabel = WORDS_LOADING_LABEL;
}
