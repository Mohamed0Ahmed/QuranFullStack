import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { DetailOverlayLinkDirective } from '../../../../core/navigation/detail-overlay/detail-overlay-link.directive';
import { LemmaDetailFrame } from '../../../../core/navigation/detail-overlay/detail-overlay.models';
import { StemLemmaItemDto } from '../../models/stems.models';
import {
  STEMS_LEMMAS_LIST_EMPTY_LABEL,
  STEMS_LEMMAS_LIST_LABEL,
  STEMS_LEMMAS_LIST_LOADING_LABEL,
  STEMS_LEMMA_TEXT_HEADER,
  STEMS_WORD_OCCURRENCES_HEADER,
} from '../../models/stems.labels';
import { ROW_NUMBER_HEADER } from '../../models/unique-words.labels';

interface StemLemmaRow {
  lemma: StemLemmaItemDto;
  frame: LemmaDetailFrame;
}

@Component({
  selector: 'qd-stem-lemmas-list',
  standalone: true,
  imports: [DetailOverlayLinkDirective],
  templateUrl: './stem-lemmas-list.component.html',
  styleUrl: './stem-lemmas-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StemLemmasListComponent {
  readonly lemmas = input.required<readonly StemLemmaItemDto[]>();
  readonly loading = input(false);

  protected readonly rowNumberHeader = ROW_NUMBER_HEADER;
  protected readonly lemmaTextHeader = STEMS_LEMMA_TEXT_HEADER;
  protected readonly occurrencesHeader = STEMS_WORD_OCCURRENCES_HEADER;
  protected readonly listLabel = STEMS_LEMMAS_LIST_LABEL;
  protected readonly loadingLabel = STEMS_LEMMAS_LIST_LOADING_LABEL;
  protected readonly emptyLabel = STEMS_LEMMAS_LIST_EMPTY_LABEL;

  protected readonly rows = computed<readonly StemLemmaRow[]>(() =>
    this.lemmas().map((lemma) => ({
      lemma,
      frame: {
        kind: 'lemma',
        id: lemma.lemmaId,
        view: 'words',
        wordView: 'simple',
        surahView: 'mentioned',
        detailPage: 1,
        typeCode: null,
      },
    })),
  );

}
