import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { MushafWordDto } from '../../models/mushaf.models';
import { toMushafWordDisplayText } from './mushaf-word-display-text';

@Component({
  selector: 'qd-mushaf-word',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './mushaf-word.component.html',
  styleUrls: ['./mushaf-word.component.scss'],
})
export class MushafWordComponent {
  readonly word = input.required<MushafWordDto>();
  readonly highlightedVerseKey = input<string | null>(null);
  readonly selectedWordLocation = input<string | null>(null);
  readonly ayahSelectionMode = input(false);
  readonly selectedVerseKeys = input<readonly string[]>([]);

  readonly ayahSelect = output<string>();
  readonly wordSelect = output<string>();

  protected readonly displayText = computed(() => toMushafWordDisplayText(this.word().textUthmani));

  protected readonly isSelectedAyah = computed(
    () => this.ayahSelectionMode() && this.selectedVerseKeys().includes(this.word().verseKey),
  );

  protected readonly selectionLabel = computed(() => {
    const word = this.word();
    if (!this.ayahSelectionMode() || word.isAyahMarker) {
      return null;
    }

    return `${this.isSelectedAyah() ? 'إلغاء تحديد' : 'تحديد'} الآية ${word.verseKey}`;
  });

  protected readonly isHighlightedAyahWord = computed(() => {
    const word = this.word();
    const highlightedVerseKey = this.highlightedVerseKey();
    if (!highlightedVerseKey || word.isAyahMarker) {
      return false;
    }

    if (this.selectedWordLocation() === word.wordLocation) {
      return false;
    }

    return word.verseKey === highlightedVerseKey;
  });

  protected onWordClick(): void {
    if (!this.word().isAyahMarker) {
      this.ayahSelect.emit(this.word().verseKey);
      this.wordSelect.emit(this.word().wordLocation);
    }
  }
}
