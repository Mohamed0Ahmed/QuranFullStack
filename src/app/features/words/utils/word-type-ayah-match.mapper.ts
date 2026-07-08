import { AyahMatchDto } from '../models/unique-words.models';
import { WordTypeAyahMatchDto } from '../models/word-types.models';
import { parseVerseKey } from './verse-key';

export function mapWordTypeAyahMatchToShared(match: WordTypeAyahMatchDto): AyahMatchDto {
  const { ayahNumber } = parseVerseKey(match.verseKey);
  const matchedSet = new Set(match.matchedWordIds);
  const visibleWords = match.words.filter((word) => !word.isAyahMarker);
  const firstMatchedPosition = match.matchedWordPositions[0];

  return {
    ayahId: 0,
    verseKey: match.verseKey,
    surahNameArabic: '',
    ayahNumber,
    pageNumber: match.pageNumber,
    analysisLocation: firstMatchedPosition ? `${match.verseKey}:${firstMatchedPosition}` : null,
    matchedQuranWordIds: visibleWords.reduce<number[]>((ids, word, index) => {
      if (matchedSet.has(word.quranWordId)) {
        ids.push(index);
      }

      return ids;
    }, []),
    words: visibleWords.map((word, index) => ({
      quranWordId: index,
      textUthmani: word.textUthmani,
      isAyahMarker: false,
    })),
  };
}
