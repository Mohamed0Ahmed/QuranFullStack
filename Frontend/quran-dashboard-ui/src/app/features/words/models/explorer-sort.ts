export type ExplorerSortDirection = 'asc' | 'desc';

type OppositeDirection<TDirection extends ExplorerSortDirection> = TDirection extends 'asc'
  ? 'desc'
  : 'asc';

export type CanonicalSortTokens<
  TColumn extends string,
  TNatural extends ExplorerSortDirection,
> = TColumn | `${TColumn}-${OppositeDirection<TNatural>}`;

// Ascending-only and bare-only: mushaf-order-asc/-desc are rejected here (backend 400s).
export const MUSHAF_ORDER_SORT = 'mushaf-order';
export type MushafOrderSort = typeof MUSHAF_ORDER_SORT;

const ASCENDING_SUFFIX = '-asc';
const DESCENDING_SUFFIX = '-desc';

export interface ExplorerSortColumn<TColumn extends string = string> {
  readonly key: TColumn;
  readonly natural: ExplorerSortDirection;
  readonly label: string;
}

export interface ExplorerSortOption {
  readonly value: string;
  readonly label: string;
}

export function oppositeDirection(direction: ExplorerSortDirection): ExplorerSortDirection {
  return direction === 'asc' ? 'desc' : 'asc';
}

export function sortTokenFor(
  column: ExplorerSortColumn,
  direction: ExplorerSortDirection,
): string {
  return direction === column.natural ? column.key : `${column.key}${suffixOf(direction)}`;
}

// Fail-closed: exact match (no trim/case-fold); returns null for any non-allowlisted token so it
// never reaches the API, and collapses aliases so a URL/cache key can't fork on spelling.
export function canonicalizeSortToken(
  token: string | null | undefined,
  columns: readonly ExplorerSortColumn[],
): string | null {
  if (token === null || token === undefined || token.length === 0) {
    return null;
  }

  const { columnKey, direction } = splitSortToken(token);

  if (columnKey === MUSHAF_ORDER_SORT) {
    return direction === null ? MUSHAF_ORDER_SORT : null;
  }

  const column = columns.find((candidate) => candidate.key === columnKey);
  return column === undefined ? null : sortTokenFor(column, direction ?? column.natural);
}

export function sortDirectionOf(
  sort: string,
  column: ExplorerSortColumn,
): ExplorerSortDirection | null {
  if (sort === column.key) {
    return column.natural;
  }
  const opposite = oppositeDirection(column.natural);
  return sort === sortTokenFor(column, opposite) ? opposite : null;
}

export function nextSortToken(sort: string, column: ExplorerSortColumn): string | null {
  const active = sortDirectionOf(sort, column);
  if (active === null) {
    return sortTokenFor(column, column.natural);
  }
  return active === column.natural
    ? sortTokenFor(column, oppositeDirection(column.natural))
    : null;
}

export function ariaSortOf(
  sort: string,
  column: ExplorerSortColumn,
): 'ascending' | 'descending' | null {
  const active = sortDirectionOf(sort, column);
  if (active === null) {
    return null;
  }
  return active === 'asc' ? 'ascending' : 'descending';
}

export function canonicalSortTokens(columns: readonly ExplorerSortColumn[]): readonly string[] {
  return columns.flatMap((column) => [
    sortTokenFor(column, column.natural),
    sortTokenFor(column, oppositeDirection(column.natural)),
  ]);
}

export function explorerSortOptions(
  columns: readonly ExplorerSortColumn[],
  mushafOrderLabel: string,
): readonly ExplorerSortOption[] {
  return [
    { value: MUSHAF_ORDER_SORT, label: mushafOrderLabel },
    ...columns.flatMap((column) => [
      sortOptionFor(column, column.natural),
      sortOptionFor(column, oppositeDirection(column.natural)),
    ]),
  ];
}

export function sortQueryValue<TSort extends string>(sort: TSort, defaultSort: TSort): TSort | null {
  return sort === defaultSort ? null : sort;
}

function sortOptionFor(
  column: ExplorerSortColumn,
  direction: ExplorerSortDirection,
): ExplorerSortOption {
  return {
    value: sortTokenFor(column, direction),
    label: `${column.label}: ${EXPLORER_SORT_DIRECTION_LABELS[direction]}`,
  };
}

function splitSortToken(token: string): { columnKey: string; direction: ExplorerSortDirection | null } {
  if (token.endsWith(DESCENDING_SUFFIX)) {
    return { columnKey: token.slice(0, -DESCENDING_SUFFIX.length), direction: 'desc' };
  }
  if (token.endsWith(ASCENDING_SUFFIX)) {
    return { columnKey: token.slice(0, -ASCENDING_SUFFIX.length), direction: 'asc' };
  }
  return { columnKey: token, direction: null };
}

function suffixOf(direction: ExplorerSortDirection): string {
  return direction === 'asc' ? ASCENDING_SUFFIX : DESCENDING_SUFFIX;
}

export const EXPLORER_SORT_DIRECTION_LABELS: Record<ExplorerSortDirection, string> = {
  asc: 'تصاعدي',
  desc: 'تنازلي',
};

export const EXPLORER_SORT_GLYPHS: Record<ExplorerSortDirection, string> = {
  asc: '▲',
  desc: '▼',
};

export function sortGlyphOf(sort: string, column: ExplorerSortColumn): string | null {
  const active = sortDirectionOf(sort, column);
  return active === null ? null : EXPLORER_SORT_GLYPHS[active];
}

export function explorerSortActionAria(sort: string, column: ExplorerSortColumn): string {
  const next = nextSortToken(sort, column);
  if (next === null) {
    return `إلغاء الترتيب حسب ${column.label}`;
  }
  const direction = sortDirectionOf(next, column) ?? column.natural;
  return `ترتيب حسب ${column.label} ${EXPLORER_SORT_ACTION_DIRECTION_LABELS[direction]}`;
}

const EXPLORER_SORT_ACTION_DIRECTION_LABELS: Record<ExplorerSortDirection, string> = {
  asc: 'تصاعديًا',
  desc: 'تنازليًا',
};
