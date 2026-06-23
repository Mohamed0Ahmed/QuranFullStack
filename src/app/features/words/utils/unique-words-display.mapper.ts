import {
  UniqueWordKind,
  UniqueWordListItemDto,
  UniqueWordListItemViewModel,
  UniqueWordSummaryDto,
} from '../models/unique-words.models';

function resolveDisplayText(word: Pick<UniqueWordListItemDto, 'displayTextUthmani' | 'textUthmani' | 'textUthmaniSimple' | 'textImlaeiSimple'>, mode: UniqueWordKind): string {
  if (mode === 'simple') {
    return word.textUthmaniSimple ?? word.textImlaeiSimple ?? word.displayTextUthmani;
  }

  return word.textUthmani ?? word.displayTextUthmani;
}

export function mapUniqueWordListItem(
  word: UniqueWordListItemDto,
  mode: UniqueWordKind,
): UniqueWordListItemViewModel {
  return {
    ...word,
    displayText: resolveDisplayText(word, mode),
  };
}

export function mapUniqueWordListItems(
  words: readonly UniqueWordListItemDto[],
  mode: UniqueWordKind,
): UniqueWordListItemViewModel[] {
  return words.map((word) => mapUniqueWordListItem(word, mode));
}

export function mapUniqueWordSummaryDisplayText(
  word: UniqueWordSummaryDto,
): UniqueWordSummaryDto & { displayText: string } {
  return {
    ...word,
    displayText: resolveDisplayText(word, word.kind),
  };
}
