import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { deepLinkToHref } from '../../../../shared/url/deep-link-href';
import { buildStemsDeepLink } from '../../state/stems-url-sync';
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
  href: string;
}

@Component({
  selector: 'qd-lemma-stems-list',
  standalone: true,
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

  protected readonly rows = computed<readonly LemmaStemRow[]>(() =>
    this.stems().map((stem) => ({
      stem,
      href: deepLinkToHref(
        buildStemsDeepLink({ stemId: stem.stemId, view: 'words', wordView: 'simple' }),
      ),
    })),
  );

}
