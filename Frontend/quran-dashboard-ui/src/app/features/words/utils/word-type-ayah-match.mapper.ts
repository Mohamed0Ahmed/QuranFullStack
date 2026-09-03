import { AyahMatchDto } from '../models/unique-words.models';
import { WordTypeAyahMatchDto } from '../models/word-types.models';
import {
  buildQuranWordLocation,
  parseQuranVerseKey,
} from '../../../shared/quran/quran-location';

export function mapWordTypeAyahMatchToShared(match: WordTypeAyahMatchDto): AyahMatchDto | null {
  const verse = parseQuranVerseKey(match.verseKey);
  if (!verse) {
    return null;
  }
  const matchedSet = new Set(match.matchedWordIds);
  const visibleWords = match.words.filter((word) => !word.isAyahMarker);
  const firstMatchedPosition = match.matchedWordPositions[0];
  const analysisLocation = firstMatchedPosition
    ? buildQuranWordLocation(verse.surahNumber, verse.ayahNumber, firstMatchedPosition)
    : null;
  if (firstMatchedPosition && !analysisLocation) {
    return null;
  }

  return {
    ayahId: 0,
    verseKey: verse.key,
    surahNameArabic: match.surahNameArabic,
    ayahNumber: match.ayahNumber,
    pageNumber: match.pageNumber,
    analysisLocation,
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
