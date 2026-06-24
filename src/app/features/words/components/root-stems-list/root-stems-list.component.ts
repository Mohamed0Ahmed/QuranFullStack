import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import {
  ROOTS_STEM_TEXT_HEADER,
  ROOTS_WORD_OCCURRENCES_HEADER,
} from '../../models/roots.labels';
import { RootStemItemDto } from '../../models/roots.models';
import { ROW_NUMBER_HEADER } from '../../models/unique-words.labels';

@Component({
  selector: 'qd-root-stems-list',
  standalone: true,
  templateUrl: './root-stems-list.component.html',
  styleUrl: './root-stems-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RootStemsListComponent {
  readonly stems = input.required<readonly RootStemItemDto[]>();

  protected readonly rowNumberHeader = ROW_NUMBER_HEADER;
  protected readonly stemTextHeader = ROOTS_STEM_TEXT_HEADER;
  protected readonly occurrencesHeader = ROOTS_WORD_OCCURRENCES_HEADER;
}
