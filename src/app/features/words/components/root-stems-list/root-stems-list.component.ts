import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { deepLinkToHref } from '../../../../shared/url/deep-link-href';
import { buildStemsDeepLink } from '../../state/stems-url-sync';

import {
  ROOTS_OPEN_STEM_LABEL,
  ROOTS_STEM_TEXT_HEADER,
  ROOTS_WORD_OCCURRENCES_HEADER,
} from '../../models/roots.labels';
import { RootStemItemDto } from '../../models/roots.models';
import { WORDS_LOADING_LABEL } from '../../models/words.labels';
import { ROW_NUMBER_HEADER } from '../../models/unique-words.labels';

interface RootStemRowViewModel {
  item: RootStemItemDto;
  href: string;
}

@Component({
  selector: 'qd-root-stems-list',
  standalone: true,
  templateUrl: './root-stems-list.component.html',
  styleUrl: './root-stems-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RootStemsListComponent {
  readonly stems = input.required<readonly RootStemItemDto[]>();
  readonly loading = input(false);

  protected readonly loadingRowPlaceholders = Array.from({ length: 8 });

  protected readonly rowNumberHeader = ROW_NUMBER_HEADER;
  protected readonly stemTextHeader = ROOTS_STEM_TEXT_HEADER;
  protected readonly occurrencesHeader = ROOTS_WORD_OCCURRENCES_HEADER;
  protected readonly loadingLabel = WORDS_LOADING_LABEL;
  protected readonly openStemLabel = ROOTS_OPEN_STEM_LABEL;

  protected readonly rows = computed((): readonly RootStemRowViewModel[] =>
    this.stems().map((item) => ({
      item,
      href: deepLinkToHref(buildStemsDeepLink({ stemId: item.stemId })),
    })),
  );
}
