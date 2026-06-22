import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { HighlightedAyahComponent } from '../highlighted-ayah/highlighted-ayah.component';
import { PagedResultDto, UniqueWordAyahMatchDto } from '../../models/unique-words.models';

@Component({
  selector: 'qd-ayah-matches-list',
  standalone: true,
  imports: [HighlightedAyahComponent],
  templateUrl: './ayah-matches-list.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AyahMatchesListComponent {
  readonly page = input.required<PagedResultDto<UniqueWordAyahMatchDto>>();
  readonly currentPage = input.required<number>();

  readonly pageChange = output<number>();

  protected readonly lastPage = computed(() =>
    Math.max(1, Math.ceil(this.page().totalCount / this.page().pageSize)),
  );
}
