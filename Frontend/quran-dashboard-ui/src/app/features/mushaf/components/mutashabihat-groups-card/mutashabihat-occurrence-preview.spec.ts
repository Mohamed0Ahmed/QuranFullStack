import { describe, expect, it } from 'vitest';

import { MutashabihatOccurrenceDto } from '../../models/mushaf.models';
import { buildCollapsedOccurrencePreview } from './mutashabihat-occurrence-preview';

function buildOccurrence(ayahNumber: number, isSelectedAyah: boolean): MutashabihatOccurrenceDto {
  return {
    verseKey: `2:${ayahNumber}`,
    surahNumber: 2,
    surahNameArabic: 'البقرة',
    ayahNumber,
    pageNumber: ayahNumber,
    wordFrom: 1,
    wordTo: 2,
    isSelectedAyah,
    isRepresentative: false,
    textUthmani: `نص-آية-${ayahNumber}`,
    phraseTextUthmani: `عبارة-${ayahNumber}`,
  };
}

describe('buildCollapsedOccurrencePreview', () => {
  it('returns the full list when it fits within the preview count', () => {
    const occurrences = [buildOccurrence(25, true), buildOccurrence(26, false)];

    expect(buildCollapsedOccurrencePreview(occurrences, 5)).toEqual(occurrences);
  });

  it('returns the first preview slice when no selected ayah sits outside it', () => {
    const occurrences = Array.from({ length: 7 }, (_, index) => buildOccurrence(30 + index, index === 0));

    expect(buildCollapsedOccurrencePreview(occurrences, 5)).toHaveLength(5);
    expect(buildCollapsedOccurrencePreview(occurrences, 5)[0].ayahNumber).toBe(30);
  });

  it('pins selected ayah occurrences that fall outside the preview slice', () => {
    const occurrences = Array.from({ length: 7 }, (_, index) => buildOccurrence(30 + index, index === 6));

    const preview = buildCollapsedOccurrencePreview(occurrences, 5);

    expect(preview).toHaveLength(6);
    expect(preview.at(-1)?.ayahNumber).toBe(36);
    expect(preview.at(-1)?.isSelectedAyah).toBe(true);
  });
});
