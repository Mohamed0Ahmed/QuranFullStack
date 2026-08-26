import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { SessionScrollStateDirective } from '../../../../../shared/navigation/session-scroll-state/session-scroll-state.directive';
import { WordsLocalNavComponent } from '../../../components/words-local-nav/words-local-nav.component';
import { PhraseSearchTabsComponent } from '../../components/phrase-search-tabs/phrase-search-tabs.component';

@Component({
  selector: 'qd-quran-phrase-search-shell',
  standalone: true,
  imports: [
    PhraseSearchTabsComponent,
    RouterOutlet,
    SessionScrollStateDirective,
    WordsLocalNavComponent,
  ],
  templateUrl: './quran-phrase-search-shell.component.html',
  styleUrl: './quran-phrase-search-shell.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class QuranPhraseSearchShellComponent {}
