import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

import {
  WORDS_ROUTE_PATH,
  lemmasRoutePath,
  phraseSearchRoutePath,
  rootsRoutePath,
  stemsRoutePath,
  uniqueWordsRoutePath,
  wordTypesRoutePath,
} from '../../../../core/navigation/route-paths';
import { LEMMAS_PAGE_TITLE } from '../../models/lemmas.labels';
import { ROOTS_PAGE_TITLE } from '../../models/roots.labels';
import { STEMS_PAGE_TITLE } from '../../models/stems.labels';
import { ACTIVE_HUB_SECTION } from '../../models/unique-words.labels';
import { WORD_TYPES_PAGE_TITLE } from '../../models/word-types.labels';

export type WordsLocalNavSection =
  | 'hub'
  | 'unique'
  | 'roots'
  | 'lemmas'
  | 'stems'
  | 'types'
  | 'phrases';

@Component({
  selector: 'qd-words-local-nav',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './words-local-nav.component.html',
  styleUrl: './words-local-nav.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WordsLocalNavComponent {
  readonly activeSection = input.required<WordsLocalNavSection>();

  protected readonly items = [
    { key: 'hub', label: 'نظرة عامة', route: WORDS_ROUTE_PATH },
    { key: 'unique', label: ACTIVE_HUB_SECTION.labelAr, route: uniqueWordsRoutePath('tashkeel') },
    { key: 'roots', label: ROOTS_PAGE_TITLE, route: rootsRoutePath() },
    { key: 'lemmas', label: LEMMAS_PAGE_TITLE, route: lemmasRoutePath() },
    { key: 'stems', label: STEMS_PAGE_TITLE, route: stemsRoutePath() },
    { key: 'types', label: WORD_TYPES_PAGE_TITLE, route: wordTypesRoutePath() },
    { key: 'phrases', label: 'البحث في القرآن', route: phraseSearchRoutePath() },
  ] as const;
}
