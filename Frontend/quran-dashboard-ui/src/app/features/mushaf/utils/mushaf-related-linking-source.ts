import { LinkingManualMushafAyahReference } from '../../linking/models/linking-manual-mushaf.models';
import { LinkingSourceLaunch } from '../../linking/models/linking-source-launch.models';
import { AyahCoreDto, SimilarAyahItemDto } from '../models/mushaf.models';

export function createSimilarAyahsLinkingLaunch(
  selectedAyah: AyahCoreDto,
  selectedRelatedAyahs: readonly SimilarAyahItemDto[],
): LinkingSourceLaunch | null {
  const manualAyahs = new Map<string, LinkingManualMushafAyahReference>([
    [selectedAyah.verseKey, toSelectedAyahReference(selectedAyah)],
  ]);

  for (const relatedAyah of selectedRelatedAyahs) {
    if (relatedAyah.targetVerseKey !== selectedAyah.verseKey) {
      manualAyahs.set(relatedAyah.targetVerseKey, toRelatedAyahReference(relatedAyah));
    }
  }

  if (manualAyahs.size < 2) {
    return null;
  }

  return {
    source: {
      kind: 'manual-mushaf-ayahs',
      label: `الآيات القريبة من الآية ${selectedAyah.ayahNumber} سورة ${selectedAyah.surahNameArabic}`,
      contextKey: `mushaf-similar-ayahs:${selectedAyah.verseKey}`,
      manualAyahs: [...manualAyahs.values()],
    },
    initialConfiguration: {
      inclusionMode: 'all-except',
      ayahOverrideIds: [],
      selectedWords: [],
      automaticWordMatchesEnabled: null,
      manualLinkShape: 'grouped',
      descriptions: [],
    },
  };
}

function toSelectedAyahReference(ayah: AyahCoreDto): LinkingManualMushafAyahReference {
  return {
    verseKey: ayah.verseKey,
    pageNumber: ayah.pageFrom,
    displayHint: ayah.verseKey,
  };
}

function toRelatedAyahReference(ayah: SimilarAyahItemDto): LinkingManualMushafAyahReference {
  return {
    verseKey: ayah.targetVerseKey,
    pageNumber: ayah.pageNumber,
    displayHint: ayah.targetVerseKey,
  };
}
