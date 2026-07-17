import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { DetailOverlayLinkDirective } from '../../../../core/navigation/detail-overlay/detail-overlay-link.directive';
import { LemmaDetailFrame } from '../../../../core/navigation/detail-overlay/detail-overlay.models';

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
  frame: LemmaDetailFrame;
}

@Component({
  selector: 'qd-root-lemmas-list',
  standalone: true,
  imports: [DetailOverlayLinkDirective],
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

  // Mirrors the retired lemma explorer deep link, which opened the default
  // words view; frame defaults are serialized explicitly per the URL contract.
  protected readonly rows = computed((): readonly RootLemmaRowViewModel[] =>
    this.lemmas().map((item) => ({
      item,
      frame: {
        kind: 'lemma',
        id: item.lemmaId,
        view: 'words',
        wordView: 'simple',
        surahView: 'mentioned',
        detailPage: 1,
        typeCode: null,
      },
    })),
  );
}
