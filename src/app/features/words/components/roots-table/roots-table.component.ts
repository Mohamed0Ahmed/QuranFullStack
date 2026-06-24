import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  afterNextRender,
  inject,
  input,
  output,
  viewChild,
} from '@angular/core';
import { CdkVirtualScrollViewport, ScrollingModule } from '@angular/cdk/scrolling';

import { WordCountChipComponent } from '../word-count-chip/word-count-chip.component';
import {
  ROOTS_COLUMN_COUNT_LABELS,
  ROOTS_COLUMN_HEADERS,
  ROOTS_LOADING_LABEL,
} from '../../models/roots.labels';
import {
  ROOTS_LIST_PAGE_SIZE,
  RootListItemViewModel,
  RootSurahView,
  RootView,
  RootWordView,
} from '../../models/roots.models';
import { pageRelativeRowNumber } from '../../utils/unique-words-pagination-display';
import { syncTableScrollbarGutter } from '../../utils/table-scrollbar-gutter-sync';

const ROW_HEIGHT = 48;
const HAS_RESIZE_OBSERVER = typeof ResizeObserver !== 'undefined';

export interface RootCountOpenedEvent {
  root: RootListItemViewModel;
  view: RootView;
  wordView?: RootWordView;
  surahView?: RootSurahView;
}
@Component({
  selector: 'qd-roots-table',
  standalone: true,
  imports: [ScrollingModule, WordCountChipComponent],
  templateUrl: './roots-table.component.html',
  styleUrl: './roots-table.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RootsTableComponent {
  private readonly host = inject(ElementRef<HTMLElement>);
  private readonly destroyRef = inject(DestroyRef);

  readonly rows = input.required<readonly RootListItemViewModel[]>();
  readonly loading = input(false);
  readonly selectedRootId = input<number | null>(null);
  readonly currentPage = input(1);
  readonly pageSize = input(ROOTS_LIST_PAGE_SIZE);

  readonly rowSelected = output<RootListItemViewModel>();
  readonly countOpened = output<RootCountOpenedEvent>();

  protected get headers() {
    return ROOTS_COLUMN_HEADERS;
  }
  protected get countLabels() {
    return ROOTS_COLUMN_COUNT_LABELS;
  }
  protected readonly loadingLabel = ROOTS_LOADING_LABEL;
  protected readonly loadingRowPlaceholders = Array.from({ length: 12 });
  protected readonly rowHeight = ROW_HEIGHT;
  protected readonly useVirtualScroll = HAS_RESIZE_OBSERVER;

  private readonly viewport = viewChild(CdkVirtualScrollViewport);

  constructor() {
    afterNextRender(() => {
      const disconnect = syncTableScrollbarGutter(
        this.host.nativeElement,
        '--roots-table-scrollbar-gutter',
        '.roots-table__body',
        '.roots-table',
      );
      this.destroyRef.onDestroy(disconnect);
    });
  }

  protected selectRow(root: RootListItemViewModel): void {
    this.rowSelected.emit(root);
  }

  protected openCount(
    root: RootListItemViewModel,
    view: RootView,
    options: { wordView?: RootWordView; surahView?: RootSurahView } = {},
  ): void {
    this.countOpened.emit({ root, view, ...options });
  }

  protected isSelected(root: RootListItemViewModel): boolean {
    return this.selectedRootId() === root.id;
  }

  protected rowNumber(index: number): number {
    return pageRelativeRowNumber(this.currentPage(), this.pageSize(), index);
  }

  protected trackRowById(_index: number, root: RootListItemViewModel): number {
    return root.id;
  }

  scrollToTop(): void {
    const viewport = this.viewport();
    if (this.useVirtualScroll && viewport) {
      viewport.scrollToIndex(0, 'auto');
      return;
    }

    const body = this.host.nativeElement.querySelector('.roots-table__body') as HTMLElement | null;
    if (body) {
      body.scrollTop = 0;
    }
  }
}
