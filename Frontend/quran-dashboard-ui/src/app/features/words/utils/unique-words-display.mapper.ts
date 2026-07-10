import {
  UniqueWordListItemDto,
  UniqueWordListItemViewModel,
  UniqueWordSummaryDto,
} from '../models/unique-words.models';

/**
 * Display text is now resolved by the backend (`displayText` on the DTO), so
 * the list item maps straight through. Kept as a thin seam so call sites stay
 * stable and future display derivation can centralize here.
 */
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
