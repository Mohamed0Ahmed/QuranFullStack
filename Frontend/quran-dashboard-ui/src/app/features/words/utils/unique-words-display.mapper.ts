import {
  UniqueWordListItemDto,
  UniqueWordListItemViewModel,
  UniqueWordSummaryDto,
} from '../models/unique-words.models';

export function mapUniqueWordListItem(word: UniqueWordListItemDto): UniqueWordListItemViewModel {
  return word;
}

export function mapUniqueWordListItems(
  words: readonly UniqueWordListItemDto[],
): UniqueWordListItemViewModel[] {
  return words.map((word) => mapUniqueWordListItem(word));
}

export function mapUniqueWordSummaryDisplayText(
  word: UniqueWordSummaryDto,
): UniqueWordSummaryDto & { displayText: string } {
  return {
    ...word,
    displayText: word.displayText,
  };
}
