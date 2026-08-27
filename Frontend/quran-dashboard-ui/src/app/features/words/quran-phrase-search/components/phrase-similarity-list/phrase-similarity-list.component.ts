import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, effect, input, output, viewChild } from '@angular/core';

import { PhraseSimilarityAyahDto } from '../../../../../core/api/generated/models/phrase-similarity-ayah-dto';
import { PhraseSimilarityPhraseDto } from '../../../../../core/api/generated/models/phrase-similarity-phrase-dto';
import {
  DetailOverlayAyahLinkDirective,
  DetailOverlayBaseTarget,
} from '../../../../../core/navigation/detail-overlay/detail-overlay-ayah-link.directive';
import { QdDataTableComponent } from '../../../../../shared/ui/data-table/data-table.component';
import { PaginationComponent } from '../../../../../shared/ui/pagination/pagination.component';
import { buildMushafDeepLink } from '../../../../mushaf/state/mushaf-url-sync';
import { PHRASE_SIMILARITY_AYAH_PAGE_SIZE } from '../../models/phrase-similarity.models';
import { PhraseHighlightedAyahComponent } from '../phrase-highlighted-ayah/phrase-highlighted-ayah.component';

interface SimilarityAyahRow {
  readonly ayah: PhraseSimilarityAyahDto;
  readonly mushafTarget: DetailOverlayBaseTarget;
}

const ROW_HEIGHT = 112;
const COMPACT_ROW_HEIGHT = 164;

@Component({
  selector: 'qd-phrase-similarity-list',
  standalone: true,
  imports: [
    DecimalPipe,
    DetailOverlayAyahLinkDirective,
    PaginationComponent,
    PhraseHighlightedAyahComponent,
    QdDataTableComponent,
  ],
  templateUrl: './phrase-similarity-list.component.html',
  styleUrl: './phrase-similarity-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PhraseSimilarityListComponent {
  readonly items = input.required<readonly PhraseSimilarityAyahDto[]>();
  readonly queryPhrase = input.required<PhraseSimilarityPhraseDto>();
  readonly totalAyahCount = input.required<number>();
  readonly totalOccurrenceCount = input.required<number>();
  readonly page = input.required<number>();
  readonly resultSetKey = input.required<string>();
  readonly busy = input(false);

  readonly pageChange = output<number>();

  protected readonly pageSize = PHRASE_SIMILARITY_AYAH_PAGE_SIZE;
  protected readonly rowHeight = ROW_HEIGHT;
  protected readonly compactRowHeight = COMPACT_ROW_HEIGHT;
  protected readonly rowIdentity = (row: SimilarityAyahRow): number => row.ayah.ayahId;
  protected readonly rows = computed<readonly SimilarityAyahRow[]>(() =>
    this.items().map((ayah) => ({ ayah, mushafTarget: target(ayah) })),
  );
  private readonly table = viewChild(QdDataTableComponent<SimilarityAyahRow>);
  private lastResultSetKey = '';

  constructor() {
    effect(() => {
      const resultSetKey = this.resultSetKey();
      const table = this.table();
      if (!table || resultSetKey === this.lastResultSetKey) {
        return;
      }
      this.lastResultSetKey = resultSetKey;
      table.scrollToTop();
    });
  }

  protected rowNumber(index: number): number {
    return (this.page() - 1) * this.pageSize + index + 1;
  }

  protected differenceLabel(ayah: PhraseSimilarityAyahDto): string {
    if (ayah.minimumDifferenceCount === 0) {
      return 'مطابقة تامة';
    }
    return ayah.minimumDifferenceCount === 1
      ? 'اختلاف واحد'
      : `${ayah.minimumDifferenceCount} اختلافات`;
  }
}

function target(ayah: PhraseSimilarityAyahDto): DetailOverlayBaseTarget {
  const deepLink = buildMushafDeepLink({
    pageNumber: ayah.pageFrom,
    ayah: ayah.verseKey,
    focusAyah: ayah.verseKey,
    panel: 'ayah',
  });
  return { basePath: deepLink.path, queryParams: deepLink.queryParams };
}
