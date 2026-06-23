import { ChangeDetectionStrategy, Component, input } from '@angular/core';

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

  protected readonly rowNumberHeader = ROW_NUMBER_HEADER;
  protected readonly surahNameHeader = SURAH_NAME_HEADER;
}
