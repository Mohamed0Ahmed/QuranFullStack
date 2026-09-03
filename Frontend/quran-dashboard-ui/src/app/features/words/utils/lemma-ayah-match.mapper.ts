import { AyahMatchDto } from '../models/unique-words.models';
import { LemmaAyahMatchDto } from '../models/lemmas.models';
import { parseQuranVerseKey } from '../../../shared/quran/quran-location';

export function mapLemmaAyahMatchToShared(match: LemmaAyahMatchDto): AyahMatchDto | null {
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
