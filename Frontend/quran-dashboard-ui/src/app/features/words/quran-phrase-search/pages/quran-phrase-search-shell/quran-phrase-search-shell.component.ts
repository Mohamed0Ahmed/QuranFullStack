import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { SessionScrollStateDirective } from '../../../../../shared/navigation/session-scroll-state/session-scroll-state.directive';

@Component({
  selector: 'qd-quran-phrase-search-shell',
  standalone: true,
  imports: [RouterOutlet, SessionScrollStateDirective],
  templateUrl: './quran-phrase-search-shell.component.html',
  styleUrl: './quran-phrase-search-shell.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class QuranPhraseSearchShellComponent {}
