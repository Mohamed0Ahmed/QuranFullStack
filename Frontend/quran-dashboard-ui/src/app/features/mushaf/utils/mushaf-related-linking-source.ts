import { LinkingManualMushafAyahReference } from '../../linking/models/linking-manual-mushaf.models';
import { LinkingSourceLaunch } from '../../linking/models/linking-source-launch.models';
import {
  AyahCoreDto,
  MutashabihatOccurrenceDto,
  SimilarAyahItemDto,
} from '../models/mushaf.models';

export interface MutashabihatLinkingOccurrence {
  readonly sourceGroupId: number;
  readonly occurrence: MutashabihatOccurrenceDto;
}

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

export function createMutashabihatLinkingLaunch(
  selectedAyah: AyahCoreDto,
  selectedOccurrences: readonly MutashabihatLinkingOccurrence[],
): LinkingSourceLaunch | null {
  if (selectedOccurrences.length === 0) {
    return null;
  }

  const ayahs = new Map<number, { reference: LinkingManualMushafAyahReference; wordIds: Set<number> }>();
  const sourceGroupIds = new Set<number>();

  for (const selectedOccurrence of selectedOccurrences) {
    const { occurrence, sourceGroupId } = selectedOccurrence;
    if (!hasCompleteCanonicalTargets(occurrence) || !isPositiveSafeInteger(sourceGroupId)) {
      return null;
    }

    sourceGroupIds.add(sourceGroupId);
    const existing = ayahs.get(occurrence.ayahId);
    if (existing !== undefined && existing.reference.verseKey !== occurrence.verseKey) {
      return null;
    }

    const member = existing ?? {
      reference: toMutashabihatAyahReference(occurrence),
      wordIds: new Set<number>(),
    };
    occurrence.matchedQuranWordIds.forEach((wordId) => member.wordIds.add(wordId));
    ayahs.set(occurrence.ayahId, member);
  }

  const manualAyahs = [...ayahs.values()].map((ayah) => ayah.reference);
  const selectedWords = [...ayahs.entries()]
    .sort(([leftAyahId], [rightAyahId]) => leftAyahId - rightAyahId)
    .flatMap(([ayahId, ayah]) =>
      [...ayah.wordIds]
        .sort((leftWordId, rightWordId) => leftWordId - rightWordId)
        .map((quranWordId) => ({ ayahId, quranWordId })),
    );
  const groups = [...sourceGroupIds].sort((leftGroupId, rightGroupId) => leftGroupId - rightGroupId);

  if (manualAyahs.length === 0 || selectedWords.length === 0 || groups.length === 0) {
    return null;
  }

  return {
    source: {
      kind: 'manual-mushaf-ayahs',
      label: `متشابهات الآية ${selectedAyah.ayahNumber} سورة ${selectedAyah.surahNameArabic}`,
      contextKey: `mushaf-mutashabihat:${selectedAyah.verseKey}:groups:${groups.join(',')}`,
      manualAyahs,
    },
    initialConfiguration: {
      inclusionMode: 'all-except',
      ayahOverrideIds: [],
      selectedWords,
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

function toMutashabihatAyahReference(
  occurrence: MutashabihatOccurrenceDto,
): LinkingManualMushafAyahReference {
  return {
    verseKey: occurrence.verseKey,
    pageNumber: occurrence.pageNumber,
    displayHint: occurrence.verseKey,
  };
}

function hasCompleteCanonicalTargets(occurrence: MutashabihatOccurrenceDto): boolean {
  const expectedTargetCount = occurrence.wordTo - occurrence.wordFrom + 1;
  const targetWordIds = occurrence.matchedQuranWordIds;
  return (
    isPositiveSafeInteger(occurrence.ayahId) &&
    isPositiveSafeInteger(occurrence.wordFrom) &&
    isPositiveSafeInteger(occurrence.wordTo) &&
    occurrence.wordTo >= occurrence.wordFrom &&
    targetWordIds.length === expectedTargetCount &&
    new Set(targetWordIds).size === targetWordIds.length &&
    targetWordIds.every(isPositiveSafeInteger)
  );
}

function isPositiveSafeInteger(value: number): boolean {
  return Number.isSafeInteger(value) && value > 0;
}
