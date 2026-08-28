import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { AyahCardComponent } from '../../../../shared/ui/ayah-card/ayah-card.component';
import { QdEmptyStateComponent } from '../../../../shared/ui/empty-state/empty-state.component';
import { QdErrorStateComponent } from '../../../../shared/ui/error-state/error-state.component';
import { QdResultListDirective } from '../../../../shared/ui/result-list/result-list.directive';
import {
  AyahNavigationTarget,
  ResourceLoadState,
  SIMILAR_AYAHS_EMPTY_MESSAGE,
  SIMILAR_AYAHS_LOADING_MESSAGE,
  SimilarAyahItemDto,
  SimilarAyahsDto,
} from '../../models/mushaf.models';
import { toStudyAyahDisplayText } from '../../utils/mushaf-verse-key-display';
import { StudyAyahResultComponent } from '../study-ayah-result/study-ayah-result.component';

type SimilarAyahDisplayItem = SimilarAyahItemDto & {
  displayText: string;
  navigateLabel: string;
};

const FALLBACK_PLACEHOLDER_COUNT = 3;
const MAX_PLACEHOLDER_COUNT = 8;

@Component({
  selector: 'qd-similar-ayahs-card',
  standalone: true,
  imports: [
    AyahCardComponent,
    QdEmptyStateComponent,
    QdErrorStateComponent,
    QdResultListDirective,
    StudyAyahResultComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './similar-ayahs-card.component.html',
  styleUrls: ['./similar-ayahs-card.component.scss'],
})
export class SimilarAyahsCardComponent {
  readonly similarAyahs = input<SimilarAyahsDto | null>(null);
  readonly loadState = input.required<ResourceLoadState>();
  readonly expectedItemCount = input<number | null>(null);

  readonly ayahNavigate = output<AyahNavigationTarget>();

  protected readonly emptyMessage = SIMILAR_AYAHS_EMPTY_MESSAGE;
  protected readonly loadingMessage = SIMILAR_AYAHS_LOADING_MESSAGE;

  protected readonly loadingPlaceholders = computed<readonly number[]>(() => {
    const expected = this.expectedItemCount();
    const count =
      expected === null
        ? FALLBACK_PLACEHOLDER_COUNT
        : Math.min(Math.max(expected, 0), MAX_PLACEHOLDER_COUNT);
    return Array.from({ length: count }, (_, index) => index);
  });

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
