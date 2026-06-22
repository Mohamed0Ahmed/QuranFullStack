import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { UniqueWordSurahItemDto } from '../../models/unique-words.models';

@Component({
  selector: 'qd-surah-occurrences-list',
  standalone: true,
  templateUrl: './surah-occurrences-list.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SurahOccurrencesListComponent {
  readonly surahs = input.required<readonly UniqueWordSurahItemDto[]>();
}
