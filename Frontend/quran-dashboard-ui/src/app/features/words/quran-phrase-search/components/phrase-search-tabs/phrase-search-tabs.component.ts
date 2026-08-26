import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

import {
  WORDS_PHRASES_CONTEXT_SEGMENT,
  WORDS_PHRASES_REPETITIONS_SEGMENT,
  WORDS_PHRASES_SIMILARITY_SEGMENT,
  phraseSearchRoutePath,
} from '../../../../../core/navigation/route-paths';
import { QdTabDirective } from '../../../../../shared/ui/tabs/tab.directive';
import { QdTabsComponent } from '../../../../../shared/ui/tabs/tabs.component';

@Component({
  selector: 'qd-phrase-search-tabs',
  standalone: true,
  imports: [QdTabDirective, QdTabsComponent, RouterLink, RouterLinkActive],
  templateUrl: './phrase-search-tabs.component.html',
  styleUrl: './phrase-search-tabs.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PhraseSearchTabsComponent {
  protected readonly tabs = [
    {
      key: WORDS_PHRASES_REPETITIONS_SEGMENT,
      label: 'التكرارات',
      route: phraseSearchRoutePath(WORDS_PHRASES_REPETITIONS_SEGMENT),
    },
    {
      key: WORDS_PHRASES_CONTEXT_SEGMENT,
      label: 'البحث اليدوي',
      route: phraseSearchRoutePath(WORDS_PHRASES_CONTEXT_SEGMENT),
    },
    {
      key: WORDS_PHRASES_SIMILARITY_SEGMENT,
      label: 'المتشابهات',
      route: phraseSearchRoutePath(WORDS_PHRASES_SIMILARITY_SEGMENT),
    },
  ] as const;
}
