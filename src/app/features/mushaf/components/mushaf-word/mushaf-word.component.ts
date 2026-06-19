import { Component, computed, input, output } from '@angular/core';

import { MushafWordDto } from '../../models/mushaf.models';
import { toMushafWordDisplayText } from './mushaf-word-display-text';

@Component({
  selector: 'qd-mushaf-word',
  standalone: true,
  templateUrl: './mushaf-word.component.html',
  styleUrls: ['./mushaf-word.component.scss'],
})
export class MushafWordComponent {
  readonly word = input.required<MushafWordDto>();
  readonly selectedWordLocation = input<string | null>(null);

  readonly ayahSelect = output<string>();
  readonly wordSelect = output<string>();

  /** Presentation-only; raw `word().textUthmani` stays authoritative in state. */
  protected readonly displayText = computed(() =>
    toMushafWordDisplayText(this.word().textUthmani),
  );

  protected onWordClick(): void {
    if (!this.word().isAyahMarker) {
      this.ayahSelect.emit(this.word().verseKey);
      this.wordSelect.emit(this.word().wordLocation);
    }
  }
}
