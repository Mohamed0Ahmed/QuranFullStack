import { DoorLinkAyahDto } from '../../../../core/api/generated/models/door-link-ayah-dto';
import { LinkingAyah } from '../../../linking/models/linking-ayah.models';

export function toAbwabLinkingAyah(
  source: DoorLinkAyahDto,
  selectedWordIds: readonly number[] = source.selectedWordIds,
): LinkingAyah {
  const selectedIds = new Set(selectedWordIds);
  return {
    verseKey: source.verseKey,
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
    })),
  };
}
