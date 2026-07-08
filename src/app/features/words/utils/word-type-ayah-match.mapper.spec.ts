import { describe, expect, it } from 'vitest';

import { mapWordTypeAyahMatchToShared } from './word-type-ayah-match.mapper';

describe('mapWordTypeAyahMatchToShared', () => {
  it('maps matched word ids to shared ayah highlight indices without string replacement', () => {
    expect(
      mapWordTypeAyahMatchToShared({
        verseKey: '1:1',
        surahNumber: 1,
        ayahNumber: 1,
        pageNumber: 92,
        matchedWordPositions: [2],
        matchedWordIds: [202],
        words: [
          { quranWordId: 201, textUthmani: 'أول', isAyahMarker: false },
          { quranWordId: 202, textUthmani: 'ثاني', isAyahMarker: false },
        ],
      }),
    ).toEqual({
      ayahId: 0,
      verseKey: '1:1',
      surahNameArabic: '',
      ayahNumber: 1,
      pageNumber: 92,
      analysisLocation: '1:1:2',
      matchedQuranWordIds: [1],
      words: [
        { quranWordId: 0, textUthmani: 'أول', isAyahMarker: false },
        { quranWordId: 1, textUthmani: 'ثاني', isAyahMarker: false },
      ],
    });
  });
});
