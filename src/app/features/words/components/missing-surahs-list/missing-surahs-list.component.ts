import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { MissingSurahItemDto } from '../../models/unique-words.models';

@Component({
  selector: 'qd-missing-surahs-list',
  standalone: true,
  templateUrl: './missing-surahs-list.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MissingSurahsListComponent {
  readonly surahs = input.required<readonly MissingSurahItemDto[]>();
}
