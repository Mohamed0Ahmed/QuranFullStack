import { AyahMatchDto } from '../models/unique-words.models';
import { StemAyahMatchDto } from '../models/stems.models';
import { parseQuranVerseKey } from '../../../shared/quran/quran-location';

export function mapStemAyahMatchToShared(match: StemAyahMatchDto): AyahMatchDto | null {
  const verse = parseQuranVerseKey(match.verseKey);
  if (!verse) {
    return null;
  }
  const matchedQuranWordIds = match.words.reduce<number[]>((ids, word, index) => {
    if (word.isMatched) {
      ids.push(index);
    }

    return ids;
  }, []);

  return {
    ayahId: match.ayahId,
    verseKey: verse.key,
    surahNameArabic: match.surahNameArabic,
    ayahNumber: verse.ayahNumber,
    pageNumber: match.pageNumber,
    matchedQuranWordIds,
    words: match.words.map((word, index) => ({
      quranWordId: index,
      textUthmani: word.textUthmani,
      isAyahMarker: false,
    })),
  };
}
