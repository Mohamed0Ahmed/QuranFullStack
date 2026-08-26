import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { DecimalPipe } from '@angular/common';

import { PhraseSimilarityGroupDto } from '../../../../../core/api/generated/models/phrase-similarity-group-dto';
import { PhraseSimilarityMatchDto } from '../../../../../core/api/generated/models/phrase-similarity-match-dto';
import { PhraseSimilarityOccurrenceDto } from '../../../../../core/api/generated/models/phrase-similarity-occurrence-dto';
import {
  DetailOverlayAyahLinkDirective,
  DetailOverlayBaseTarget,
} from '../../../../../core/navigation/detail-overlay/detail-overlay-ayah-link.directive';
import { AyahCardComponent } from '../../../../../shared/ui/ayah-card/ayah-card.component';
import { PaginationComponent } from '../../../../../shared/ui/pagination/pagination.component';
import {
  QdResultItemDirective,
  QdResultListDirective,
} from '../../../../../shared/ui/result-list/result-list.directive';
import { buildMushafDeepLink } from '../../../../mushaf/state/mushaf-url-sync';
import { PhraseSimilaritySource } from '../../models/phrase-similarity.models';
import { PhraseHighlightedAyahComponent } from '../phrase-highlighted-ayah/phrase-highlighted-ayah.component';

@Component({
  selector: 'qd-phrase-similarity-list',
  standalone: true,
  imports: [
    AyahCardComponent,
    DecimalPipe,
    DetailOverlayAyahLinkDirective,
    PaginationComponent,
    PhraseHighlightedAyahComponent,
    QdResultItemDirective,
    QdResultListDirective,
  ],
  templateUrl: './phrase-similarity-list.component.html',
  styleUrl: './phrase-similarity-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PhraseSimilarityListComponent {
  readonly source = input.required<PhraseSimilaritySource>();
  readonly groups = input.required<readonly PhraseSimilarityGroupDto[]>();
  readonly matches = input.required<readonly PhraseSimilarityMatchDto[]>();
  readonly selectedAnchor = input<PhraseSimilarityGroupDto | null>(null);
  readonly totalCount = input.required<number>();
  readonly page = input.required<number>();
  readonly pageSize = input.required<number>();
  readonly busy = input(false);

  readonly anchorSelected = output<PhraseSimilarityGroupDto>();
  readonly anchorCleared = output<void>();
  readonly pageChange = output<number>();

  protected readonly listLabel = computed(() =>
    this.selectedAnchor() ? 'التشابهات المباشرة للعبارة' : 'مجموعات التشابه القرآني',
  );

  protected target(occurrence: PhraseSimilarityOccurrenceDto): DetailOverlayBaseTarget {
    const deepLink = buildMushafDeepLink({
      pageNumber: occurrence.pageFrom,
      ayah: occurrence.verseKey,
      focusAyah: occurrence.verseKey,
      panel: 'ayah',
    });
    return { basePath: deepLink.path, queryParams: deepLink.queryParams };
  }

  protected position(index: number): number {
    return (this.page() - 1) * this.pageSize() + index + 1;
  }
}
