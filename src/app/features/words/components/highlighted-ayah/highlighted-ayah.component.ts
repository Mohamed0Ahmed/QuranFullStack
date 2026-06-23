import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { AyahWordForHighlightDto } from '../../models/unique-words.models';

@Component({
  selector: 'qd-highlighted-ayah',
  standalone: true,
  templateUrl: './highlighted-ayah.component.html',
  styleUrl: './highlighted-ayah.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HighlightedAyahComponent {
  readonly words = input.required<readonly AyahWordForHighlightDto[]>();
  readonly matchedQuranWordIds = input.required<readonly number[]>();

  private readonly matchedSet = computed(() => new Set(this.matchedQuranWordIds()));
  protected readonly visibleWords = computed(() => this.words().filter((word) => !word.isAyahMarker));

  protected isHighlighted(wordId: number): boolean {
    return this.matchedSet().has(wordId);
  }

  protected ariaLabel(word: AyahWordForHighlightDto): string {
    return this.isHighlighted(word.quranWordId)
      ? `كلمة مطابقة: ${word.textUthmani}`
      : word.textUthmani;
  }
}
