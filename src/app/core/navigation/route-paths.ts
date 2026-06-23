import { NAV_ITEMS } from './nav-items';

function navRoute(key: string): string {
  const item = NAV_ITEMS.find((navItem) => navItem.key === key);
  if (!item) {
    throw new Error(`Unknown navigation key: ${key}`);
  }
  return item.route;
}

export const MUSHAF_ROUTE_PATH = navRoute('mushaf');
export const WORDS_ROUTE_PATH = navRoute('words');

/** Relative segment under `WORDS_ROUTE_PATH`; must stay aligned with `WORDS_UNIQUE_MODE_ROUTE`. */
export const WORDS_UNIQUE_MODE_SEGMENT = 'unique/:mode' as const;

export function uniqueWordsRoutePath(kind: string): string {
  const modeSegment = WORDS_UNIQUE_MODE_SEGMENT.replace(':mode', encodeURIComponent(kind));
  return `${WORDS_ROUTE_PATH}/${modeSegment}`;
}

/**
 * Relative segment under `WORDS_ROUTE_PATH` for the Roots Explorer (Feature
 * 015); must stay aligned with `WORDS_ROOTS_ROUTE`. Stable route key, not a
 * translated label. Generic fallback must not handle `words/roots`.
 */
export const WORDS_ROOTS_SEGMENT = 'roots' as const;

export function rootsRoutePath(): string {
  return `${WORDS_ROUTE_PATH}/${WORDS_ROOTS_SEGMENT}`;
}
