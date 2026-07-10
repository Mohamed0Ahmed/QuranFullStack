import { describe, expect, it } from 'vitest';

import { deepLinkToHref } from './deep-link-href';

describe('deepLinkToHref', () => {
  it('serializes query params and preserves colon separators in verse keys', () => {
    expect(
      deepLinkToHref({
        path: '/dashboard/mushaf',
        queryParams: {
          page: '92',
          ayah: '4:57',
          focusAyah: '4:57',
          panel: 'ayah',
        },
      }),
    ).toBe('/dashboard/mushaf?page=92&ayah=4:57&focusAyah=4:57&panel=ayah');
  });

  it('omits null and undefined query params', () => {
    expect(
      deepLinkToHref({
        path: '/dashboard/words/unique/tashkeel',
        queryParams: {
          word: '42',
          view: 'ayahs',
          search: null,
          sort: undefined,
        },
      }),
    ).toBe('/dashboard/words/unique/tashkeel?word=42&view=ayahs');
  });

  it('encodes Arabic search values in the href', () => {
    expect(
      deepLinkToHref({
        path: '/dashboard/words/unique/simple',
        queryParams: {
          search: 'اسم',
        },
      }),
    ).toBe('/dashboard/words/unique/simple?search=%D8%A7%D8%B3%D9%85');
  });

  it('returns the path alone when all query params are absent', () => {
    expect(
      deepLinkToHref({
        path: '/dashboard/words/unique/tashkeel',
        queryParams: {},
      }),
    ).toBe('/dashboard/words/unique/tashkeel');
  });
});
