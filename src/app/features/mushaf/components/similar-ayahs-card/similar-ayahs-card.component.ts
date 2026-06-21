import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';

import {
  AyahNavigationTarget,
  ResourceLoadState,
  SIMILAR_AYAHS_EMPTY_MESSAGE,
  SIMILAR_AYAHS_LOADING_MESSAGE,
  SimilarAyahItemDto,
  SimilarAyahsDto,
} from '../../models/mushaf.models';
import { toStudyAyahDisplayText } from '../../utils/mushaf-verse-key-display';

@Component({
  selector: 'qd-similar-ayahs-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './similar-ayahs-card.component.html',
  styleUrls: ['./similar-ayahs-card.component.scss'],
})
export class SimilarAyahsCardComponent {
  readonly similarAyahs = input<SimilarAyahsDto | null>(null);
  readonly loadState = input.required<ResourceLoadState>();

  readonly ayahNavigate = output<AyahNavigationTarget>();

  protected readonly emptyMessage = SIMILAR_AYAHS_EMPTY_MESSAGE;
  protected readonly loadingMessage = SIMILAR_AYAHS_LOADING_MESSAGE;

  protected displayAyahText(textUthmani: string): string {
    return toStudyAyahDisplayText(textUthmani);
  }

  protected ayahNavigateLabel(item: SimilarAyahItemDto): string {
    return `فتح ${item.surahNameArabic} — ${item.ayahNumber} في المصحف`;
  }

  protected onAyahNavigate(item: SimilarAyahItemDto): void {
    this.ayahNavigate.emit({
      verseKey: item.targetVerseKey,
      pageNumber: item.pageNumber,
    });
  }
}
