import {
  ExplorerSortColumn,
  ariaSortOf,
  explorerSortActionAria,
  nextSortToken,
  sortDirectionOf,
  sortGlyphOf,
} from '../models/explorer-sort';

export class ExplorerTableSortController<TSort extends string> {
  constructor(
    private readonly currentSort: () => TSort,
    private readonly normalize: (value: string) => TSort,
    private readonly emit: (sort: TSort | null) => void,
  ) {}

  isSorted(column: ExplorerSortColumn): boolean {
    return sortDirectionOf(this.currentSort(), column) !== null;
  }

  ariaSort(column: ExplorerSortColumn): 'ascending' | 'descending' | null {
    return ariaSortOf(this.currentSort(), column);
  }

  glyph(column: ExplorerSortColumn): string | null {
    return sortGlyphOf(this.currentSort(), column);
  }

  actionAria(column: ExplorerSortColumn): string {
    return explorerSortActionAria(this.currentSort(), column);
  }

  cycle(column: ExplorerSortColumn): void {
    const next = nextSortToken(this.currentSort(), column);
    this.emit(next === null ? null : this.normalize(next));
  }
}
