import { describe, expect, it } from 'vitest';

import { mapLemmaAyahMatchToShared } from './lemma-ayah-match.mapper';

describe('mapLemmaAyahMatchToShared', () => {
  it('maps lemma ayah match into shared ayah match shape', () => {
    expect(
      mapLemmaAyahMatchToShared({
        ayahId: 7,
        verseKey: '2:255',
        surahNameArabic: 'البقرة',
        pageNumber: 42,
        words: [
          { textUthmani: 'الأول', isMatched: true },
          { textUthmani: 'الثاني', isMatched: false },
        ],
      }),
    ).toEqual({
      ayahId: 7,
      verseKey: '2:255',
      surahNameArabic: 'البقرة',
      ayahNumber: 255,
      pageNumber: 42,
      matchedQuranWordIds: [0],
      words: [
        { quranWordId: 0, textUthmani: 'الأول', isAyahMarker: false },
        { quranWordId: 1, textUthmani: 'الثاني', isAyahMarker: false },
      ],
    });
  });
});
