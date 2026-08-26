import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { PhraseAyahWordDto } from '../../../../../core/api/generated/models/phrase-ayah-word-dto';

@Component({
  selector: 'qd-phrase-highlighted-ayah',
  standalone: true,
  templateUrl: './phrase-highlighted-ayah.component.html',
  styleUrl: './phrase-highlighted-ayah.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PhraseHighlightedAyahComponent {
  readonly words = input.required<readonly PhraseAyahWordDto[]>();
  readonly queryQuranWordIds = input.required<readonly number[]>();

  private readonly queryWordIds = computed(() => new Set(this.queryQuranWordIds()));

  protected isQueryWord(quranWordId: number): boolean {
    return this.queryWordIds().has(quranWordId);
  }

  protected wordAriaLabel(word: PhraseAyahWordDto): string {
    return this.isQueryWord(word.quranWordId)
      ? `كلمة من العبارة: ${word.textUthmani}`
      : word.textUthmani;
  }
}
