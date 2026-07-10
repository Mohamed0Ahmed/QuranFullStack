import { UniqueWordListItemDto, UniqueWordSummaryDto } from '../models/unique-words.models';

export function toUniqueWordSummary(word: UniqueWordListItemDto): UniqueWordSummaryDto {
  return {
    id: word.id,
    kind: word.kind,
    displayText: word.displayText,
    occurrencesCount: word.occurrencesCount,
    ayahsCount: word.ayahsCount,
    surahsCount: word.surahsCount,
    missingSurahsCount: word.missingSurahsCount,
  };
}
