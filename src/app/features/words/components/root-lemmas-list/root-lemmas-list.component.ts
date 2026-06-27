import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { deepLinkToHref } from '../../../../shared/url/deep-link-href';
import { buildLemmasDeepLink } from '../../state/lemmas-url-sync';

import {
  ROOTS_LEMMA_TEXT_HEADER,
  ROOTS_OPEN_LEMMA_LABEL,
  ROOTS_WORD_OCCURRENCES_HEADER,
} from '../../models/roots.labels';
import { RootLemmaItemDto } from '../../models/roots.models';
import { WORDS_LOADING_LABEL } from '../../models/words.labels';
import { ROW_NUMBER_HEADER } from '../../models/unique-words.labels';

interface RootLemmaRowViewModel {
  item: RootLemmaItemDto;
  href: string;
}

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
  protected readonly loadingLabel = WORDS_LOADING_LABEL;
  protected readonly openLemmaLabel = ROOTS_OPEN_LEMMA_LABEL;

  protected readonly rows = computed((): readonly RootLemmaRowViewModel[] =>
    this.lemmas().map((item) => ({
      item,
      href: deepLinkToHref(buildLemmasDeepLink({ lemmaId: item.lemmaId })),
    })),
  );
}
