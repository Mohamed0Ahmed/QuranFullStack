import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import {
  ROOTS_LEMMA_TEXT_HEADER,
  ROOTS_LOADING_LABEL,
  ROOTS_WORD_OCCURRENCES_HEADER,
} from '../../models/roots.labels';
import { RootLemmaItemDto } from '../../models/roots.models';
import { ROW_NUMBER_HEADER } from '../../models/unique-words.labels';

@Component({
  selector: 'qd-root-lemmas-list',
  standalone: true,
  templateUrl: './root-lemmas-list.component.html',
  styleUrl: './root-lemmas-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RootLemmasListComponent {
  readonly lemmas = input.required<readonly RootLemmaItemDto[]>();
  readonly loading = input(false);

  protected readonly loadingRowPlaceholders = Array.from({ length: 8 });

  protected readonly rowNumberHeader = ROW_NUMBER_HEADER;
  protected readonly lemmaTextHeader = ROOTS_LEMMA_TEXT_HEADER;
  protected readonly occurrencesHeader = ROOTS_WORD_OCCURRENCES_HEADER;
  protected readonly loadingLabel = ROOTS_LOADING_LABEL;
}
