import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { AyahCardComponent } from '../../../../shared/ui/ayah-card/ayah-card.component';
import { LinkingAyah } from '../../models/linking-ayah.models';

@Component({
  selector: 'qd-linking-ayah-card',
  standalone: true,
  imports: [AyahCardComponent],
  templateUrl: './linking-ayah-card.component.html',
  styleUrl: './linking-ayah-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LinkingAyahCardComponent {
  readonly ayah = input.required<LinkingAyah>();
  readonly highlightSourceWords = input(true);

  protected readonly displayWords = computed(() => this.ayah().words);

  protected isMatched(isSourceMatch: boolean): boolean {
    return this.highlightSourceWords() && isSourceMatch;
  }
}
