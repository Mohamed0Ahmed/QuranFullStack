import { ChangeDetectionStrategy, Component } from '@angular/core';

import { SessionScrollStateDirective } from '../../../../shared/navigation/session-scroll-state/session-scroll-state.directive';
import { WordSectionCardComponent } from '../../components/word-section-card/word-section-card.component';
import { WordsLocalNavComponent } from '../../components/words-local-nav/words-local-nav.component';
import { WORDS_HUB_SECTIONS_LABEL } from '../../models/unique-words.labels';
import {
  WORDS_EXPLAINER_CONTENT,
  WORDS_EXPLAINER_ORDER,
  WORDS_HUB_CHAIN,
  WORDS_HUB_INTRO,
  WordsExplainerKey,
} from '../../models/words-explainer.content';
import {
  lemmasRoutePath,
  phraseSearchRoutePath,
  rootsRoutePath,
  stemsRoutePath,
  uniqueWordsRoutePath,
  wordTypesRoutePath,
} from '../../../../core/navigation/route-paths';

interface WordsHubCardViewModel {
  key: WordsExplainerKey;
  ordinal: string;
  eyebrow: string;
  title: string;
  description: string;
  route: string;
}

const HUB_CARD_ROUTES: Record<WordsExplainerKey, string> = {
  unique: uniqueWordsRoutePath('tashkeel'),
  roots: rootsRoutePath(),
  lemmas: lemmasRoutePath(),
  stems: stemsRoutePath(),
  'word-types': wordTypesRoutePath(),
};

@Component({
  selector: 'qd-words-hub-page',
  standalone: true,
  imports: [SessionScrollStateDirective, WordSectionCardComponent, WordsLocalNavComponent],
  templateUrl: './words-hub-page.component.html',
  styleUrls: ['./words-hub-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WordsHubPageComponent {
  private cardsCache?: readonly WordsHubCardViewModel[];

  protected get title(): string {
    return WORDS_HUB_INTRO.title;
  }
  protected get subtitle(): string {
    return WORDS_HUB_INTRO.subtitle;
  }
  protected get sectionsLabel(): string {
    return WORDS_HUB_SECTIONS_LABEL;
  }
  protected get chain() {
    return WORDS_HUB_CHAIN;
  }
  protected readonly phraseSearchCard = {
    ordinal: 'بحث',
    eyebrow: 'استكشاف العبارات',
    title: 'البحث في القرآن',
    description:
      'استعرض العبارات المتكررة ومواضعها داخل الآيات، مع الحفاظ على النص العثماني وحدود الكلمات الأصلية.',
    route: phraseSearchRoutePath(),
  } as const;

  protected get cards(): readonly WordsHubCardViewModel[] {
    return (this.cardsCache ??= WORDS_EXPLAINER_ORDER.map((key) => {
      const content = WORDS_EXPLAINER_CONTENT[key];
      return {
        key,
        ordinal: content.ordinal,
        eyebrow: content.eyebrow,
        title: content.title,
        description: content.tagline,
        route: HUB_CARD_ROUTES[key],
      };
    }));
  }
}
