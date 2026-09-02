import { DoorLinkAyahDto } from '../../../../core/api/generated/models/door-link-ayah-dto';
import { LinkingAyah } from '../../../linking/models/linking-ayah.models';
import { parseQuranVerseKey } from '../../../../shared/quran/quran-location';

export function toAbwabLinkingAyah(
  source: DoorLinkAyahDto,
  selectedWordIds: readonly number[] = source.selectedWordIds,
): LinkingAyah | null {
  const verse = parseQuranVerseKey(source.verseKey);
  if (
    !verse ||
    verse.key !== source.verseKey ||
    verse.surahNumber !== source.surahNumber ||
    verse.ayahNumber !== source.ayahNumber
  ) {
    return null;
  }
  const selectedIds = new Set(selectedWordIds);
  return {
    verseKey: verse.key,
    ayahId: source.ayahId,
    surahNumber: source.surahNumber,
    surahNameArabic: source.surahNameArabic,
    ayahNumber: source.ayahNumber,
    pageNumber: source.pageFrom,
    words: source.words.map((word) => ({
      renderPosition: word.wordNumber,
      canonicalQuranWordId: word.quranWordId,
      textUthmani: word.textUthmani,
      isAyahMarker: word.isAyahMarker,
      isSourceMatch: selectedIds.has(word.quranWordId),
      isExcludedSourceMatch: false,
    })),
  };
}
