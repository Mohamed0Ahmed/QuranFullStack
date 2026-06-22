import { ChangeDetectionStrategy, Component } from '@angular/core';

import { WordSectionCardComponent } from '../../components/word-section-card/word-section-card.component';
import {
  ACTIVE_HUB_SECTION,
  COMING_SOON_BADGE,
  COMING_SOON_HUB_SECTIONS,
  WORDS_HUB_SUBTITLE,
  WORDS_HUB_TITLE,
} from '../../models/unique-words.labels';
import { WordSectionCardLabel } from '../../models/unique-words.labels';

/** View model for a single hub section card. */
interface WordSectionCardViewModel {
  labelAr: string;
  descriptionAr: string;
  route: string | null;
  disabled: boolean;
}

/**
 * Words hub page. Renders one active v1 section (`الكلمات الفريدة`) and four
 * disabled coming-soon sections. No backend reads are required for this page.
 *
 * The card view models are plain readonly properties rather than signals: they
 * are constant for the lifetime of the page, so there is no state to track.
 */
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
  protected readonly comingSoonBadge = COMING_SOON_BADGE;

  /** The only active v1 section; navigates to the default explorer mode. */
  protected readonly activeCard: WordSectionCardViewModel = {
    labelAr: ACTIVE_HUB_SECTION.labelAr,
    descriptionAr: ACTIVE_HUB_SECTION.descriptionAr,
    route: '/dashboard/words/unique',
    disabled: false,
  };

  /** Future sections shown as disabled, non-navigable coming-soon cards. */
  protected readonly comingSoonCards: readonly WordSectionCardViewModel[] =
    COMING_SOON_HUB_SECTIONS.map((section: WordSectionCardLabel) => ({
      labelAr: section.labelAr,
      descriptionAr: section.descriptionAr,
      route: null,
      disabled: true,
    }));
}

