import { UniqueWordListItemDto, UniqueWordSummaryDto } from '../models/unique-words.models';

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
