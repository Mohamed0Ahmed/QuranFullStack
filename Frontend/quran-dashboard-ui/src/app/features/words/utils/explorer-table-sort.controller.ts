import {
  ExplorerSortColumn,
  ariaSortOf,
  explorerSortActionAria,
  nextSortToken,
  sortDirectionOf,
  sortGlyphOf,
} from '../models/explorer-sort';

// One source of the 3-state sort cycle for the five explorer tables (Feature 030, N8) so none can
// drift. The controller only reports the cycle's next token; the page owns the URL write. null means
// "release" — remove the param, which lands on the explorer's default.
export class ExplorerTableSortController<TSort extends string> {
  constructor(
    private readonly currentSort: () => TSort,
    // Fails closed on anything unknown; on a token this controller built it is the identity.
    private readonly normalize: (value: string) => TSort,
    private readonly emit: (sort: TSort | null) => void,
  ) {}

  isSorted(column: ExplorerSortColumn): boolean {
    return sortDirectionOf(this.currentSort(), column) !== null;
  }

  // Absent (null) when the column is inactive — the attribute must not render at all.
  ariaSort(column: ExplorerSortColumn): 'ascending' | 'descending' | null {
    return ariaSortOf(this.currentSort(), column);
  }

  glyph(column: ExplorerSortColumn): string | null {
    return sortGlyphOf(this.currentSort(), column);
  }

  actionAria(column: ExplorerSortColumn): string {
    return explorerSortActionAria(this.currentSort(), column);
  }

  // Click / Enter / Space on a sortable header — a native button gives the two keys for free.
  cycle(column: ExplorerSortColumn): void {
    const next = nextSortToken(this.currentSort(), column);
    this.emit(next === null ? null : this.normalize(next));
  }
}
