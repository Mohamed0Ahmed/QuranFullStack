import { UniqueWordListItemDto, UniqueWordListItemViewModel, UniqueWordSummaryDto } from '../models/unique-words.models';

export function mergeUniqueWordListItems(
  existing: readonly UniqueWordListItemViewModel[],
  next: readonly UniqueWordListItemViewModel[],
): UniqueWordListItemViewModel[] {
  const seen = new Set(existing.map((item) => item.id));
  return [...existing, ...next.filter((item) => !seen.has(item.id))];
}

export function toUniqueWordSummary(word: UniqueWordListItemDto): UniqueWordSummaryDto {
  return {
    id: word.id,
    kind: word.kind,
    displayTextUthmani: word.displayTextUthmani,
    textUthmani: word.textUthmani,
    textUthmaniSimple: word.textUthmaniSimple,
    textImlaeiSimple: word.textImlaeiSimple,
    wordKeyImlaeiSimple: word.wordKeyImlaeiSimple,
    qpcGlyph: word.qpcGlyph,
    occurrencesCount: word.occurrencesCount,
    ayahsCount: word.ayahsCount,
    surahsCount: word.surahsCount,
    missingSurahsCount: word.missingSurahsCount,
    firstVerseKey: word.firstVerseKey,
    firstLocation: word.firstLocation,
  };
}
