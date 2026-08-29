import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterOutlet } from '@angular/router';

import { SessionScrollStateDirective } from '../../../../../shared/navigation/session-scroll-state/session-scroll-state.directive';

@Component({
  selector: 'qd-quran-phrase-search-shell',
  standalone: true,
  imports: [RouterOutlet, SessionScrollStateDirective],
  templateUrl: './quran-phrase-search-shell.component.html',
  styleUrl: './quran-phrase-search-shell.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class QuranPhraseSearchShellComponent {
  private readonly route = inject(ActivatedRoute);

  protected readonly scrollStateKey = signal('');

  protected activateChildScrollState(): void {
    const key = this.route.firstChild?.snapshot.data['scrollStateKey'] as string | undefined;
    this.scrollStateKey.set(key ?? '');
  }
}
