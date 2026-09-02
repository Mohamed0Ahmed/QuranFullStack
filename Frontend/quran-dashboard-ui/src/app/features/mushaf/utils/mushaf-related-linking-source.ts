import { LinkingSourceLaunch } from '../../linking/models/linking-source-launch.models';
import { ManualLinkingSourceFactory } from '../../linking/utils/manual-linking-source.factory';
import {
  AyahCoreDto,
  MutashabihatOccurrenceDto,
  SimilarAyahItemDto,
} from '../models/mushaf.models';
import { parseQuranVerseKey, type QuranVerseKey } from '../../../shared/quran/quran-location';

export interface MutashabihatLinkingOccurrence {
  readonly sourceGroupId: number;
  readonly occurrence: MutashabihatOccurrenceDto;
}

export function createSimilarAyahsLinkingLaunch(
  selectedAyah: AyahCoreDto,
  selectedRelatedAyahs: readonly SimilarAyahItemDto[],
): LinkingSourceLaunch | null {
  const selectedVerse = canonicalVerseKey(selectedAyah.verseKey);
  if (!selectedVerse) {
    return null;
  }
  const verseKeys = new Set<QuranVerseKey>([selectedVerse]);

  for (const relatedAyah of selectedRelatedAyahs) {
    const relatedVerse = canonicalVerseKey(relatedAyah.targetVerseKey);
    if (!relatedVerse) {
      return null;
    }
    if (relatedVerse !== selectedVerse) {
      verseKeys.add(relatedVerse);
    }
  }

  if (verseKeys.size < 2) {
    return null;
  }

  return ManualLinkingSourceFactory.createLaunch({
    label: `الآيات القريبة من الآية ${selectedAyah.ayahNumber} سورة ${selectedAyah.surahNameArabic}`,
    contextKey: `mushaf-similar-ayahs:${selectedAyah.verseKey}`,
    verseKeys: [...verseKeys],
    configuration: 'explicit',
  });
}

export function createMutashabihatLinkingLaunch(
  selectedAyah: AyahCoreDto,
  selectedOccurrences: readonly MutashabihatLinkingOccurrence[],
): LinkingSourceLaunch | null {
  if (selectedOccurrences.length === 0) {
    return null;
  }
  const selectedVerse = canonicalVerseKey(selectedAyah.verseKey);
  if (!selectedVerse) {
    return null;
  }

  const ayahs = new Map<number, { verseKey: QuranVerseKey; wordIds: Set<number> }>();
  const sourceGroupIds = new Set<number>();

  for (const selectedOccurrence of selectedOccurrences) {
    const { occurrence, sourceGroupId } = selectedOccurrence;
    if (!hasCompleteCanonicalTargets(occurrence) || !isPositiveSafeInteger(sourceGroupId)) {
      return null;
    }
    const occurrenceVerse = canonicalVerseKey(occurrence.verseKey);
    if (!occurrenceVerse) {
      return null;
    }

    sourceGroupIds.add(sourceGroupId);
    const existing = ayahs.get(occurrence.ayahId);
    if (existing !== undefined && existing.verseKey !== occurrence.verseKey) {
      return null;
    }

    const member = existing ?? {
      verseKey: occurrenceVerse,
      wordIds: new Set<number>(),
    };
    occurrence.matchedQuranWordIds.forEach((wordId) => member.wordIds.add(wordId));
    ayahs.set(occurrence.ayahId, member);
  }

  const selectedWords = [...ayahs.entries()]
    .sort(([leftAyahId], [rightAyahId]) => leftAyahId - rightAyahId)
    .flatMap(([ayahId, ayah]) =>
      [...ayah.wordIds]
        .sort((leftWordId, rightWordId) => leftWordId - rightWordId)
        .map((quranWordId) => ({ ayahId, quranWordId })),
    );
  const groups = [...sourceGroupIds].sort((leftGroupId, rightGroupId) => leftGroupId - rightGroupId);

  if (ayahs.size === 0 || selectedWords.length === 0 || groups.length === 0) {
    return null;
  }

  return ManualLinkingSourceFactory.createLaunch({
    label: `متشابهات الآية ${selectedAyah.ayahNumber} سورة ${selectedAyah.surahNameArabic}`,
    contextKey: `mushaf-mutashabihat:${selectedAyah.verseKey}:groups:${groups.join(',')}`,
    verseKeys: [...ayahs.values()].map((ayah) => ayah.verseKey),
    selectedWords,
    configuration: 'explicit',
  });
}

function canonicalVerseKey(value: unknown): QuranVerseKey | null {
  const parsed = parseQuranVerseKey(value);
  return parsed && parsed.key === value ? parsed.key : null;
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
