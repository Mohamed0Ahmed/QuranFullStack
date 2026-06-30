import { WordTypeAyahMatchDto } from '../models/word-types.models';

export interface WordTypeHighlightedAyahInput {
  verseKey: string;
  ayahText: string;
  matchedWordPositions: readonly number[];
  matchedWordIds: readonly number[];
}

export function mapWordTypeAyahMatchToHighlight(match: WordTypeAyahMatchDto): WordTypeHighlightedAyahInput {
  return {
    verseKey: match.verseKey,
    ayahText: match.ayahText,
    matchedWordPositions: match.matchedWordPositions,
    matchedWordIds: match.matchedWordIds,
  };
}
