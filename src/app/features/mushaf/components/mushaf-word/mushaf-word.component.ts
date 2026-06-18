import { Component, input, output } from '@angular/core';

import { MushafWordDto } from '../../models/mushaf.models';

@Component({
  selector: 'qd-mushaf-word',
  standalone: true,
  templateUrl: './mushaf-word.component.html',
  styleUrls: ['./mushaf-word.component.scss'],
})
export class MushafWordComponent {
  readonly word = input.required<MushafWordDto>();
  readonly selectedVerseKey = input<string | null>(null);

  readonly ayahSelect = output<string>();

  protected onWordClick(): void {
    if (!this.word().isAyahMarker) {
      this.ayahSelect.emit(this.word().verseKey);
    }
  }
}
