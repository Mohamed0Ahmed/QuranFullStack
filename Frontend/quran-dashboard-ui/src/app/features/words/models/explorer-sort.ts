/**
 * The `sort` token grammar shared by the five Words explorers — the frontend half of the backend
 * contract in `Backend/.../Application.Abstractions/Quran/Words/WordSortToken.cs`:
 *
 * ```
 * token := column | column "-asc" | column "-desc"
 * ```
 *
 * A BARE token means the column's natural direction (counts read most-first and descend; text
 * ascends), so every pre-030 token keeps its exact meaning as an alias — `occurrences` ≡
 * `occurrences-desc`, `alpha` ≡ `alpha-asc`. The bare form is therefore the CANONICAL serialization
 * of a column's natural direction, and the suffixed form is canonical only for the opposite one:
 * the canonical set for a count column is `{ occurrences, occurrences-asc }`, never
 * `occurrences-desc`. Canonicalizing on the way in collapses aliases onto one token, so an old
 * shared link cannot make the URL or a client cache key carry a second spelling of one ordering.
 *
 * This module only decomposes and re-composes the grammar and drives the header cycle. Each
 * explorer owns its own column allowlist and applies it here, mirroring the backend's per-explorer
 * parsers — an unlisted column never becomes a request token.
 */

export type ExplorerSortDirection = 'asc' | 'desc';

type OppositeDirection<TDirection extends ExplorerSortDirection> = TDirection extends 'asc'
  ? 'desc'
  : 'asc';

/**
 * The canonical token set for one column: the bare key at its natural direction, the suffixed key
 * at the opposite one. Explorer sort unions compose this per column class instead of listing all
 * ~17 tokens by hand, which keeps the alias spellings out of the type by construction.
 */
export type CanonicalSortTokens<
  TColumn extends string,
  TNatural extends ExplorerSortDirection,
> = TColumn | `${TColumn}-${OppositeDirection<TNatural>}`;

/**
 * The release/browse order every explorer allowlists. It is not a data column: ascending-only and
 * bare-only, so `mushaf-order-asc`/`-desc` are rejected here exactly as the backend 400s on them.
 * It renders no sortable header — it is the state a header cycle releases INTO.
 */
export const MUSHAF_ORDER_SORT = 'mushaf-order';
export type MushafOrderSort = typeof MUSHAF_ORDER_SORT;

const ASCENDING_SUFFIX = '-asc';
const DESCENDING_SUFFIX = '-desc';

export interface ExplorerSortColumn<TColumn extends string = string> {
  readonly key: TColumn;
  readonly natural: ExplorerSortDirection;
  /** The Arabic column header — also the stem of the aria-label and of the select option text. */
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

/**
 * Canonicalizes a raw `sort` value against a column allowlist, or returns null when it names no
 * allowlisted ordering. Legacy aliases canonicalize OUT (`occurrences-desc` → `occurrences`), so
 * callers can treat the result as the single spelling of that ordering. Callers fail closed to
 * their own default on null — a rejected token never reaches the API.
 *
 * Matching is EXACT: no trimming, no case folding. The backend parser tolerates both
 * (`WordSortToken.TrySplit` trims and lower-cases), but the frontend is deliberately stricter —
 * `?sort=ALPHA` and `?sort=occurrences%20` have always fallen back to the default here, and that
 * fail-closed behavior is spec'd. Being strict also guarantees we only ever emit one spelling per
 * ordering, so a URL or cache key cannot fork on casing.
 */
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

/**
 * The 3-state header cycle: natural direction → the opposite one → release. Release is `null`,
 * meaning "remove the param", which lands on the explorer's default order.
 *
 * WORD TYPES QUIRK: that explorer's default IS `occurrences` desc, so its المواضع header renders
 * actively sorted descending in the default state and this cycle collapses there to
 * desc(default) ⇄ asc — releasing from asc returns to desc, which is the default itself, so the
 * column never shows a third, unsorted state. Every other Word Types column, and every column on
 * the other four explorers (whose default is `mushaf-order`), walks all three states.
 */
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

/**
 * Options for the ≤1023px fallback `<select>`, where the table header row is `display: none` and
 * the header cycle is therefore unreachable. Same URL contract as the headers: `mushaf-order`
 * first (the release order — the default on four explorers, an ordinary ordering on Word Types),
 * then every sortable column in both directions.
 */
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

/**
 * The URL value for a chosen ordering: null (param absent) when it IS the explorer's default, so
 * the default state stays param-free and a header release and a select choice of the same ordering
 * produce byte-identical URLs.
 */
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

/** Direction wording, shared by the select options and the header aria-labels. */
export const EXPLORER_SORT_DIRECTION_LABELS: Record<ExplorerSortDirection, string> = {
  asc: 'تصاعدي',
  desc: 'تنازلي',
};

/**
 * The direction glyph rendered beside an active header label. It sits in its own `aria-hidden`
 * span — `aria-sort` on the columnheader is the a11y channel — and up/down glyphs carry no
 * horizontal direction, so they stay correct under RTL.
 */
export const EXPLORER_SORT_GLYPHS: Record<ExplorerSortDirection, string> = {
  asc: '▲',
  desc: '▼',
};

export function sortGlyphOf(sort: string, column: ExplorerSortColumn): string | null {
  const active = sortDirectionOf(sort, column);
  return active === null ? null : EXPLORER_SORT_GLYPHS[active];
}

/**
 * The Arabic aria-label for a sort header: it names the column AND the state the NEXT click moves
 * to, because the button's accessible name must describe its action, while `aria-sort` on the
 * columnheader carries the current state.
 */
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
