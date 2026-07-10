import { NgTemplateOutlet } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  afterNextRender,
  inject,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { CdkVirtualScrollViewport, ScrollingModule } from '@angular/cdk/scrolling';

import { WordCountChipComponent } from '../word-count-chip/word-count-chip.component';
import {
  LOADING_LABEL,
  OCCURRENCES_CHIP_LABEL,
  ROW_NUMBER_HEADER,
  UNIQUE_WORD_NULL_PLACEHOLDER,
  UNIQUE_WORD_ROOT_HEADER,
  UNIQUE_WORD_TABLE_BODY_LABEL,
  UNIQUE_WORD_TABLE_LABEL,
  UNIQUE_WORD_TYPE_HEADER,
  UNIQUE_WORD_WORD_HEADER,
  WORD_DRILLDOWN_VIEW_LABELS,
} from '../../models/unique-words.labels';
import {
  UNIQUE_WORDS_PAGE_SIZE,
  UniqueWordListItemViewModel,
  WordDrilldownView,
} from '../../models/unique-words.models';
import {
  isUniqueWordCountActive,
  UniqueWordsColumnKey,
} from '../../utils/explorer-count-active';
import {
  ExplorerInteractionSource,
  handleExplorerTableKeydown,
} from '../../utils/explorer-table-keydown';
import {
  ExplorerRowNavDirection,
  scrollExplorerRowIntoView,
} from '../../utils/explorer-table-scroll';
import { pageRelativeRowNumber } from '../../utils/unique-words-pagination-display';
import { syncTableScrollbarGutter } from '../../utils/table-scrollbar-gutter-sync';
import { buildRootsDeepLink } from '../../state/roots-url-sync';
import { deepLinkToHref } from '../../../../shared/url/deep-link-href';

import { QD_BP_PHONE_MAX_QUERY } from '../../../../shared/layout/breakpoints';

const ROW_HEIGHT_DESKTOP = 48;
const ROW_HEIGHT_MOBILE = 68;
const HAS_RESIZE_OBSERVER = typeof ResizeObserver !== 'undefined';

const UNIQUE_WORDS_COLUMN_ORDER = [
  'missing',
  'surahs',
  'ayahs',
] as const satisfies readonly UniqueWordsColumnKey[];

export interface UniqueWordsDrilldownOpenEvent {
  word: UniqueWordListItemViewModel;
  column: UniqueWordsColumnKey;
  view: WordDrilldownView;
  source?: ExplorerInteractionSource;
}

@Component({
  selector: 'qd-unique-words-table',
  standalone: true,
  imports: [NgTemplateOutlet, ScrollingModule, WordCountChipComponent],
  templateUrl: './unique-words-table.component.html',
  styleUrl: './unique-words-table.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UniqueWordsTableComponent {
  private readonly host = inject(ElementRef<HTMLElement>);
  private readonly destroyRef = inject(DestroyRef);

  readonly rows = input.required<readonly UniqueWordListItemViewModel[]>();
  readonly loading = input(false);
  readonly selectedWordId = input<number | null>(null);
  readonly currentPage = input(1);
  readonly pageSize = input(UNIQUE_WORDS_PAGE_SIZE);
  readonly drilldownIsOpen = input(false);
  readonly activeColumn = input<UniqueWordsColumnKey | null>(null);

  readonly rowSelected = output<UniqueWordListItemViewModel>();
  readonly drilldownOpen = output<UniqueWordsDrilldownOpenEvent>();

  protected readonly loadingRowPlaceholders = Array.from({ length: 12 });
  protected readonly rowHeight = signal(ROW_HEIGHT_DESKTOP);
  protected readonly useVirtualScroll = HAS_RESIZE_OBSERVER;

  private readonly viewport = viewChild(CdkVirtualScrollViewport);

  constructor() {
    afterNextRender(() => {
      if (typeof window !== 'undefined' && typeof window.matchMedia === 'function') {
        const mobileMq = window.matchMedia(QD_BP_PHONE_MAX_QUERY);
        const syncRowHeight = () => {
          this.rowHeight.set(mobileMq.matches ? ROW_HEIGHT_MOBILE : ROW_HEIGHT_DESKTOP);
        };
        syncRowHeight();
        if (typeof mobileMq.addEventListener === 'function') {
          mobileMq.addEventListener('change', syncRowHeight);
          this.destroyRef.onDestroy(() => mobileMq.removeEventListener('change', syncRowHeight));
        }
      }

      const disconnect = syncTableScrollbarGutter(
        this.host.nativeElement,
        '--unique-words-table-scrollbar-gutter',
        '.unique-words-table__body',
        '.unique-words-table',
      );
      this.destroyRef.onDestroy(disconnect);
    });
  }

  protected get rowNumberHeader(): string {
    return ROW_NUMBER_HEADER;
  }

  protected get wordHeader(): string {
    return UNIQUE_WORD_WORD_HEADER;
  }

  protected get typeHeader(): string {
    return UNIQUE_WORD_TYPE_HEADER;
  }

  protected get rootHeader(): string {
    return UNIQUE_WORD_ROOT_HEADER;
  }

  protected get nullPlaceholder(): string {
    return UNIQUE_WORD_NULL_PLACEHOLDER;
  }

  protected get occurrencesLabel(): string {
    return OCCURRENCES_CHIP_LABEL;
  }

  protected get ayahsLabel(): string {
    return WORD_DRILLDOWN_VIEW_LABELS.ayahs;
  }

  protected get surahsLabel(): string {
    return WORD_DRILLDOWN_VIEW_LABELS.surahs;
  }

  protected get missingLabel(): string {
    return WORD_DRILLDOWN_VIEW_LABELS.missing;
  }

  protected get loadingLabel(): string {
    return LOADING_LABEL;
  }

  protected get tableLabel(): string {
    return UNIQUE_WORD_TABLE_LABEL;
  }

  protected get tableBodyLabel(): string {
    return UNIQUE_WORD_TABLE_BODY_LABEL;
  }

  protected wordTypeLabel(row: UniqueWordListItemViewModel): string {
    return row.primaryWordTypeBroadArabicLabel ?? this.nullPlaceholder;
  }

  protected hasRoot(row: UniqueWordListItemViewModel): boolean {
    return row.rootId !== null && Boolean(row.rootText);
  }

  protected rootHref(row: UniqueWordListItemViewModel): string {
    return deepLinkToHref(
      buildRootsDeepLink({
        rootId: row.rootId ?? undefined,
        view: 'words',
        wordView: 'simple',
      }),
    );
  }

  protected selectRow(row: UniqueWordListItemViewModel): void {
    this.rowSelected.emit(row);
  }

  protected openDrilldown(
    row: UniqueWordListItemViewModel,
    column: UniqueWordsColumnKey,
    view: WordDrilldownView,
    source: ExplorerInteractionSource = 'immediate',
  ): void {
    this.drilldownOpen.emit({ word: row, column, view, source });
  }

  protected isSelected(row: UniqueWordListItemViewModel): boolean {
    return this.selectedWordId() === row.id;
  }

  protected rowNumber(index: number): number {
    return pageRelativeRowNumber(this.currentPage(), this.pageSize(), index);
  }

  protected trackRowById(_index: number, row: UniqueWordListItemViewModel): number {
    return row.id;
  }

  protected isCountActive(
    row: UniqueWordListItemViewModel,
    column: UniqueWordsColumnKey,
    count: number,
  ): boolean {
    return isUniqueWordCountActive({
      rowId: row.id,
      selectedWordId: this.selectedWordId(),
      column,
      activeColumn: this.activeColumn(),
      drilldownOpen: this.drilldownIsOpen(),
      disabled: count === 0,
    });
  }

  protected onTableKeydown(event: KeyboardEvent): void {
    if (this.loading()) {
      return;
    }
    handleExplorerTableKeydown({
      event,
      rows: this.rows(),
      selectedRowId: this.selectedWordId(),
      currentColumn: this.activeColumn(),
      columnOrder: UNIQUE_WORDS_COLUMN_ORDER,
      isColumnEnabled: (row, column) => this.isColumnEnabled(row, column),
      emitColumnTarget: (row, column, source) => this.emitColumnTarget(row, column, source),
      scrollToRow: (index, direction) => this.scrollToRow(index, direction),
    });
  }

  scrollToTop(): void {
    const viewport = this.viewport();
    if (this.useVirtualScroll && viewport) {
      viewport.scrollToIndex(0, 'auto');
      return;
    }

    const body = this.host.nativeElement.querySelector('.unique-words-table__body') as HTMLElement | null;
    if (body) {
      body.scrollTop = 0;
    }
  }

  private emitColumnTarget(
    row: UniqueWordListItemViewModel,
    column: UniqueWordsColumnKey,
    source: ExplorerInteractionSource,
  ): void {
    this.openDrilldown(row, column, column, source);
  }

  private isColumnEnabled(row: UniqueWordListItemViewModel, column: UniqueWordsColumnKey): boolean {
    switch (column) {
      case 'ayahs':
        return row.ayahsCount > 0;
      case 'surahs':
        return row.surahsCount > 0;
      case 'missing':
        return row.missingSurahsCount > 0;
    }
  }

  private scrollToRow(index: number, direction: ExplorerRowNavDirection): void {
    const viewport = this.viewport();
    if (this.useVirtualScroll && viewport) {
      scrollExplorerRowIntoView({
        targetIndex: index,
        direction,
        itemSize: this.rowHeight(),
        viewport,
      });
      return;
    }

    const body = this.host.nativeElement.querySelector('.unique-words-table__body') as HTMLElement | null;
    scrollExplorerRowIntoView({
      targetIndex: index,
      direction,
      itemSize: this.rowHeight(),
      container: body,
    });
  }
}
