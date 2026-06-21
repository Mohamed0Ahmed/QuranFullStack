import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import {
  AyahNavigationTarget,
  ResourceLoadState,
  SIMILAR_AYAHS_EMPTY_MESSAGE,
  SIMILAR_AYAHS_LOADING_MESSAGE,
  SimilarAyahItemDto,
  SimilarAyahsDto,
} from '../../models/mushaf.models';
import { toStudyAyahDisplayText } from '../../utils/mushaf-verse-key-display';

type SimilarAyahDisplayItem = SimilarAyahItemDto & {
  displayText: string;
  navigateLabel: string;
};

@Component({
  selector: 'qd-similar-ayahs-card',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './similar-ayahs-card.component.html',
  styleUrls: ['./similar-ayahs-card.component.scss'],
})
export class SimilarAyahsCardComponent {
  readonly similarAyahs = input<SimilarAyahsDto | null>(null);
  readonly loadState = input.required<ResourceLoadState>();

  readonly ayahNavigate = output<AyahNavigationTarget>();

  protected readonly emptyMessage = SIMILAR_AYAHS_EMPTY_MESSAGE;
  protected readonly loadingMessage = SIMILAR_AYAHS_LOADING_MESSAGE;

  protected readonly displayItems = computed<SimilarAyahDisplayItem[]>(
    () =>
      this.similarAyahs()?.items.map((item) => ({
        ...item,
        displayText: toStudyAyahDisplayText(item.textUthmani),
        navigateLabel: `فتح ${item.surahNameArabic} — ${item.ayahNumber} في المصحف`,
      })) ?? [],
  );

  protected onAyahNavigate(item: SimilarAyahItemDto): void {
    this.ayahNavigate.emit({
      verseKey: item.targetVerseKey,
      pageNumber: item.pageNumber,
    });
  }
}
