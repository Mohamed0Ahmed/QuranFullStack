import { ChangeDetectionStrategy, Component } from '@angular/core';

import { WordSectionCardComponent } from '../../components/word-section-card/word-section-card.component';
import {
  ACTIVE_HUB_SECTION,
  ADDITIONAL_ACTIVE_HUB_SECTIONS,
  COMING_SOON_BADGE,
  COMING_SOON_HUB_SECTIONS,
  WORDS_HUB_SECTIONS_LABEL,
  WORDS_HUB_SUBTITLE,
  WORDS_HUB_TITLE,
  WordSectionCardLabel,
} from '../../models/unique-words.labels';

interface WordSectionCardViewModel {
  labelAr: string;
  descriptionAr: string;
  route: string | null;
  disabled: boolean;
}

@Component({
  selector: 'qd-words-hub-page',
  standalone: true,
  imports: [WordSectionCardComponent],
  templateUrl: './words-hub-page.component.html',
  styleUrls: ['./words-hub-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WordsHubPageComponent {
  protected readonly title = WORDS_HUB_TITLE;
  protected readonly subtitle = WORDS_HUB_SUBTITLE;
  protected readonly sectionsLabel = WORDS_HUB_SECTIONS_LABEL;
  protected readonly comingSoonBadge = COMING_SOON_BADGE;

  protected readonly activeCard: WordSectionCardViewModel = {
    labelAr: ACTIVE_HUB_SECTION.labelAr,
    descriptionAr: ACTIVE_HUB_SECTION.descriptionAr,
    route: '/dashboard/words/unique',
    disabled: false,
  };

  protected readonly additionalActiveCards: readonly WordSectionCardViewModel[] =
    ADDITIONAL_ACTIVE_HUB_SECTIONS.map((section) => ({
      labelAr: section.labelAr,
      descriptionAr: section.descriptionAr,
      route: section.route,
      disabled: false,
    }));

  protected readonly comingSoonCards: readonly WordSectionCardViewModel[] =
    COMING_SOON_HUB_SECTIONS.map((section: WordSectionCardLabel) => ({
      labelAr: section.labelAr,
      descriptionAr: section.descriptionAr,
      route: null,
      disabled: true,
    }));
}
