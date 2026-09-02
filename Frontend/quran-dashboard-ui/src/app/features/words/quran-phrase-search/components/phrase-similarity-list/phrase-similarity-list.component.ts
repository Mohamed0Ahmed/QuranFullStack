import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  output,
  viewChild,
} from '@angular/core';

import { PhraseSimilarityAyahDto } from '../../../../../core/api/generated/models/phrase-similarity-ayah-dto';
import { PhraseSimilarityPhraseDto } from '../../../../../core/api/generated/models/phrase-similarity-phrase-dto';
import {
  DetailOverlayAyahLinkDirective,
  DetailOverlayBaseTarget,
} from '../../../../../core/navigation/detail-overlay/detail-overlay-ayah-link.directive';
import { QdDataTableComponent } from '../../../../../shared/ui/data-table/data-table.component';
import { PaginationComponent } from '../../../../../shared/ui/pagination/pagination.component';
import { buildMushafDeepLink } from '../../../../mushaf/state/mushaf-url-sync';
import { parseQuranVerseKey } from '../../../../../shared/quran/quran-location';
import { PHRASE_SIMILARITY_AYAH_PAGE_SIZE } from '../../models/phrase-similarity.models';
import { PhraseSimilarityAyahSelectionStore } from '../../state/phrase-similarity-ayah-selection.store';
import { PhraseHighlightedAyahComponent } from '../phrase-highlighted-ayah/phrase-highlighted-ayah.component';

interface SimilarityAyahRow {
  readonly ayah: PhraseSimilarityAyahDto;
  readonly matchPercentLabel: string;
  readonly mushafTarget: DetailOverlayBaseTarget;
}

const ROW_HEIGHT = 112;
const COMPACT_ROW_HEIGHT = 164;
const matchPercentFormatter = new Intl.NumberFormat('en-US', {
  minimumFractionDigits: 0,
  maximumFractionDigits: 1,
  useGrouping: false,
});

@Component({
  selector: 'qd-phrase-similarity-list',
  standalone: true,
  imports: [
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
  protected readonly selection = inject(PhraseSimilarityAyahSelectionStore);

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
  protected readonly isRowSelected = (row: SimilarityAyahRow): boolean =>
    this.selection.isSelected(row.ayah.ayahId);
  protected readonly rows = computed<readonly SimilarityAyahRow[]>(() =>
    this.items().flatMap((ayah) => {
      const mushafTarget = target(ayah);
      return mushafTarget
        ? [{ ayah, matchPercentLabel: matchPercentFormatter.format(ayah.bestMatchPercent), mushafTarget }]
        : [];
    }),
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

  protected toggleAll(event: Event): void {
    const checked = checkboxValue(event);
    checked ? this.selection.selectAll() : this.selection.clearAll();
  }

  protected toggleAyah(event: Event, ayahId: number): void {
    this.selection.setSelected(ayahId, checkboxValue(event));
  }

  protected toggleRow(row: SimilarityAyahRow): void {
    const ayahId = row.ayah.ayahId;
    this.selection.setSelected(ayahId, !this.selection.isSelected(ayahId));
  }
}

function checkboxValue(event: Event): boolean {
  return event.target instanceof HTMLInputElement && event.target.checked;
}

function target(ayah: PhraseSimilarityAyahDto): DetailOverlayBaseTarget | null {
  const verse = parseQuranVerseKey(ayah.verseKey);
  if (!verse) {
    return null;
  }
  const deepLink = buildMushafDeepLink({
    pageNumber: ayah.pageFrom,
    ayah: verse.key,
    focusAyah: verse.key,
    panel: 'ayah',
  });
  return { basePath: deepLink.path, queryParams: deepLink.queryParams };
}
