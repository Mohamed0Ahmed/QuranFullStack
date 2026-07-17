import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { DetailOverlayLinkDirective } from '../../../../core/navigation/detail-overlay/detail-overlay-link.directive';
import { StemDetailFrame } from '../../../../core/navigation/detail-overlay/detail-overlay.models';
import { LemmaStemItemDto } from '../../models/lemmas.models';
import {
  LEMMAS_STEMS_LIST_EMPTY_LABEL,
  LEMMAS_STEMS_LIST_LABEL,
  LEMMAS_STEMS_LIST_LOADING_LABEL,
  LEMMAS_STEM_TEXT_HEADER,
  LEMMAS_WORD_OCCURRENCES_HEADER,
} from '../../models/lemmas.labels';
import { ROW_NUMBER_HEADER } from '../../models/unique-words.labels';

interface LemmaStemRow {
  stem: LemmaStemItemDto;
  frame: StemDetailFrame;
}

@Component({
  selector: 'qd-lemma-stems-list',
  standalone: true,
  imports: [DetailOverlayLinkDirective],
  templateUrl: './lemma-stems-list.component.html',
  styleUrl: './lemma-stems-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LemmaStemsListComponent {
  readonly stems = input.required<readonly LemmaStemItemDto[]>();
  readonly loading = input(false);

  protected readonly rowNumberHeader = ROW_NUMBER_HEADER;
  protected readonly stemTextHeader = LEMMAS_STEM_TEXT_HEADER;
  protected readonly occurrencesHeader = LEMMAS_WORD_OCCURRENCES_HEADER;
  protected readonly listLabel = LEMMAS_STEMS_LIST_LABEL;
  protected readonly loadingLabel = LEMMAS_STEMS_LIST_LOADING_LABEL;
  protected readonly emptyLabel = LEMMAS_STEMS_LIST_EMPTY_LABEL;

  // Mirrors the retired stem explorer deep link (words view, simple word
  // view); frame defaults are serialized explicitly per the URL contract.
  protected readonly rows = computed<readonly LemmaStemRow[]>(() =>
    this.stems().map((stem) => ({
      stem,
      frame: {
        kind: 'stem',
        id: stem.stemId,
        view: 'words',
        wordView: 'simple',
        surahView: 'mentioned',
        detailPage: 1,
        typeCode: null,
      },
    })),
  );

}
