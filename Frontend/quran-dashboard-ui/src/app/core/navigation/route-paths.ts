import { NAV_ITEMS, NavItem } from './nav-items';

function navItem(key: string): NavItem {
  const item = NAV_ITEMS.find((candidate) => candidate.key === key);
  if (!item) {
    throw new Error(`Unknown navigation key: ${key}`);
  }
  return item;
}

function navRoute(key: string): string {
  return navItem(key).route;
}

/** Arabic display label for a nav item — the source of truth for that route's tab title. */
export function navLabel(key: string): string {
  return navItem(key).labelAr;
}

export const MUSHAF_ROUTE_PATH = navRoute('mushaf');
export const WORDS_ROUTE_PATH = navRoute('words');

export const WORDS_UNIQUE_MODE_SEGMENT = 'unique/:mode' as const;

export function uniqueWordsRoutePath(kind: string): string {
  const modeSegment = WORDS_UNIQUE_MODE_SEGMENT.replace(':mode', encodeURIComponent(kind));
  return `${WORDS_ROUTE_PATH}/${modeSegment}`;
}

export const WORDS_ROOTS_SEGMENT = 'roots' as const;

export function rootsRoutePath(): string {
  return `${WORDS_ROUTE_PATH}/${WORDS_ROOTS_SEGMENT}`;
}

export const WORDS_LEMMAS_SEGMENT = 'lemmas' as const;

export function lemmasRoutePath(): string {
  return `${WORDS_ROUTE_PATH}/${WORDS_LEMMAS_SEGMENT}`;
}

export const WORDS_STEMS_SEGMENT = 'stems' as const;

export function stemsRoutePath(): string {
  return `${WORDS_ROUTE_PATH}/${WORDS_STEMS_SEGMENT}`;
}

export const WORDS_TYPES_SEGMENT = 'types' as const;

export function wordTypesRoutePath(): string {
  return `${WORDS_ROUTE_PATH}/${WORDS_TYPES_SEGMENT}`;
}
