import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { ScrollingModule } from '@angular/cdk/scrolling';

import { MissingSurahItemDto } from '../../models/unique-words.models';
import { ROW_NUMBER_HEADER, SURAH_NAME_HEADER } from '../../models/unique-words.labels';

const ROW_HEIGHT = 44;

@Component({
  selector: 'qd-missing-surahs-list',
  standalone: true,
  imports: [ScrollingModule],
  templateUrl: './missing-surahs-list.component.html',
  styleUrl: './missing-surahs-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MissingSurahsListComponent {
  readonly surahs = input.required<readonly MissingSurahItemDto[]>();

  protected readonly rowHeight = ROW_HEIGHT;
  protected readonly rowNumberHeader = ROW_NUMBER_HEADER;
  protected readonly surahNameHeader = SURAH_NAME_HEADER;
}
