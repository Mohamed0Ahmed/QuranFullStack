import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
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
import { PhraseLinkingAyahSelectionStore } from '../../state/phrase-linking-ayah-selection.store';
import { PhraseHighlightedAyahComponent } from '../phrase-highlighted-ayah/phrase-highlighted-ayah.component';
import { parseQuranVerseKey } from '../../../../../shared/quran/quran-location';

interface ContextAyahRow {
  readonly ayah: PhraseContextAyahDto;
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
  protected readonly selection = inject(PhraseLinkingAyahSelectionStore);

  readonly ayahs = input.required<readonly PhraseContextAyahDto[]>();
  readonly totalCount = input.required<number>();
  readonly resultSetKey = input.required<string>();
  readonly firstRowNumber = input(1);
  readonly busy = input(false);

  protected readonly rowHeight = ROW_HEIGHT;
  protected readonly compactRowHeight = COMPACT_ROW_HEIGHT;
  protected readonly rowIdentity = (row: ContextAyahRow): number =>
    row.ayah.ayahId;
  protected readonly isRowSelected = (row: ContextAyahRow): boolean =>
    this.selection.isSelected(row.ayah.ayahId);
  protected readonly rowNumber = (index: number): number => this.firstRowNumber() + index;
  private readonly table = viewChild(QdDataTableComponent<ContextAyahRow>);
  private lastResultSetKey = '';

  protected readonly rows = computed<readonly ContextAyahRow[]>(() =>
    this.ayahs().flatMap((ayah) => {
      const verse = parseQuranVerseKey(ayah.verseKey);
      if (!verse) {
        return [];
      }
      const deepLink = buildMushafDeepLink({
        pageNumber: ayah.pageFrom,
        ayah: verse.key,
        focusAyah: verse.key,
        panel: 'ayah',
      });
      return [{
        ayah,
        highlights: ayah.highlights,
        mushafTarget: { basePath: deepLink.path, queryParams: deepLink.queryParams },
      }];
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

  protected toggleAll(event: Event): void {
    const checked = checkboxValue(event);
    checked ? this.selection.selectAll() : this.selection.clearAll();
  }

  protected toggleAyah(event: Event, ayahId: number): void {
    this.selection.setSelected(ayahId, checkboxValue(event));
  }

  protected toggleRow(row: ContextAyahRow): void {
    const ayahId = row.ayah.ayahId;
    this.selection.setSelected(ayahId, !this.selection.isSelected(ayahId));
  }
}

function checkboxValue(event: Event): boolean {
  return event.target instanceof HTMLInputElement && event.target.checked;
}
