import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';

import { MushafLineDto, MushafPageViewModel } from '../../models/mushaf.models';
import { MushafLineComponent } from '../mushaf-line/mushaf-line.component';

@Component({
  selector: 'qd-mushaf-page-view',
  standalone: true,
  imports: [CommonModule, MushafLineComponent],
  templateUrl: './mushaf-page-view.component.html',
  styleUrls: ['./mushaf-page-view.component.scss'],
})
export class MushafPageViewComponent {
  readonly page = input.required<MushafPageViewModel>();
  readonly selectedVerseKey = input<string | null>(null);
  readonly selectedWordLocation = input<string | null>(null);

  readonly ayahSelect = output<string>();
  readonly wordSelect = output<string>();

  protected surahNameArabicForLine(line: MushafLineDto): string | null {
    const surahNumber = line.surahNumber;
    if (surahNumber === null) {
      return null;
    }

    return this.page().surahs.find((surah) => surah.surahNumber === surahNumber)?.nameArabic ?? null;
  }
}
