import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  input,
  viewChild,
} from '@angular/core';

import { PhraseContextAyahDto } from '../../../../../core/api/generated/models/phrase-context-ayah-dto';
import { PhraseContextHighlightsDto } from '../../../../../core/api/generated/models/phrase-context-highlights-dto';
import {
  DetailOverlayAyahLinkDirective,
  DetailOverlayBaseTarget,
} from '../../../../../core/navigation/detail-overlay/detail-overlay-ayah-link.directive';
import { QdDataTableComponent } from '../../../../../shared/ui/data-table/data-table.component';
import { buildMushafDeepLink } from '../../../../mushaf/state/mushaf-url-sync';
import { PhraseHighlightedAyahComponent } from '../phrase-highlighted-ayah/phrase-highlighted-ayah.component';

interface ContextOccurrenceRow {
  readonly occurrence: PhraseContextAyahDto;
  readonly highlights: PhraseContextHighlightsDto;
  readonly mushafTarget: DetailOverlayBaseTarget;
}

const ROW_HEIGHT = 76;
const COMPACT_ROW_HEIGHT = 104;

@Component({
  selector: 'qd-phrase-context-occurrence-list',
  standalone: true,
  imports: [
    DetailOverlayAyahLinkDirective,
    PhraseHighlightedAyahComponent,
    QdDataTableComponent,
  ],
  templateUrl: './phrase-context-occurrence-list.component.html',
  styleUrl: './phrase-context-occurrence-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PhraseContextOccurrenceListComponent {
  readonly items = input.required<readonly PhraseContextAyahDto[]>();
  readonly totalCount = input.required<number>();
  readonly resultSetKey = input.required<string>();
  readonly firstRowNumber = input(1);
  readonly busy = input(false);

  protected readonly rowHeight = ROW_HEIGHT;
  protected readonly compactRowHeight = COMPACT_ROW_HEIGHT;
  protected readonly rowIdentity = (row: ContextOccurrenceRow): number =>
    row.occurrence.ayahId;
  protected readonly rowNumber = (index: number): number => this.firstRowNumber() + index;
  private readonly table = viewChild(QdDataTableComponent<ContextOccurrenceRow>);
  private lastResultSetKey = '';

  protected readonly rows = computed<readonly ContextOccurrenceRow[]>(() =>
    this.items().map((occurrence) => {
      const deepLink = buildMushafDeepLink({
        pageNumber: occurrence.pageFrom,
        ayah: occurrence.verseKey,
        focusAyah: occurrence.verseKey,
        panel: 'ayah',
      });
      return {
        occurrence,
        highlights: occurrence.highlights,
        mushafTarget: { basePath: deepLink.path, queryParams: deepLink.queryParams },
      };
    }),
  );

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
}
