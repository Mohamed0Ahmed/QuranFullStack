import { AyahMatchDto } from '../models/unique-words.models';
import { LemmaAyahMatchDto } from '../models/lemmas.models';
import { parseVerseKey } from './verse-key';

export function mapLemmaAyahMatchToShared(match: LemmaAyahMatchDto): AyahMatchDto {
  const { ayahNumber } = parseVerseKey(match.verseKey);
  const matchedQuranWordIds = match.words.reduce<number[]>((ids, word, index) => {
    if (word.isMatched) {
      ids.push(index);
    }

    return ids;
  }, []);

  return {
    ayahId: match.ayahId,
    verseKey: match.verseKey,
    surahNameArabic: match.surahNameArabic,
    ayahNumber,
    pageNumber: match.pageNumber,
    matchedQuranWordIds,
    words: match.words.map((word, index) => ({
      quranWordId: index,
      textUthmani: word.textUthmani,
      isAyahMarker: false,
    })),
  };
}
