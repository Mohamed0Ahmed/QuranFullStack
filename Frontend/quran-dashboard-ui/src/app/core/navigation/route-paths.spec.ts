import { describe, expect, it } from 'vitest';

import { NAV_ITEMS } from './nav-items';
import {
  MUSHAF_ROUTE_PATH,
  WORDS_ROUTE_PATH,
  stemsRoutePath,
  lemmasRoutePath,
  WORDS_UNIQUE_MODE_SEGMENT,
  uniqueWordsRoutePath,
} from './route-paths';

describe('route-paths', () => {
  it('derives feature paths from navigation items', () => {
    expect(MUSHAF_ROUTE_PATH).toBe(NAV_ITEMS.find((item) => item.key === 'mushaf')?.route);
    expect(WORDS_ROUTE_PATH).toBe(NAV_ITEMS.find((item) => item.key === 'words')?.route);
  });

  it('builds unique-words deep-link paths from the shared mode segment', () => {
    expect(WORDS_UNIQUE_MODE_SEGMENT).toBe('unique/:mode');
    expect(uniqueWordsRoutePath('tashkeel')).toBe(`${WORDS_ROUTE_PATH}/unique/tashkeel`);
    expect(uniqueWordsRoutePath('simple')).toBe(`${WORDS_ROUTE_PATH}/unique/simple`);
  });

  it('exposes stable lemmas and stems explorer paths', () => {
    expect(lemmasRoutePath()).toBe(`${WORDS_ROUTE_PATH}/lemmas`);
    expect(stemsRoutePath()).toBe(`${WORDS_ROUTE_PATH}/stems`);
  });
});
