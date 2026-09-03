import { PhraseOccurrenceDto } from '../../../../core/api/generated/models/phrase-occurrence-dto';
import { PhraseOccurrencePageResponse } from '../../../../core/api/generated/models/phrase-occurrence-page-response';
import { isCanonicalQuranWordId } from '../../../linking/models/linking-manual-mushaf.models';
import { LinkingSourceLaunch } from '../../../linking/models/linking-source-launch.models';
import { ManualLinkingSourceFactory } from '../../../linking/utils/manual-linking-source.factory';
import {
  compareQuranVerseKeys,
  parseQuranVerseKey,
  type QuranVerseKey,
} from '../../../../shared/quran/quran-location';

interface CollectedPhraseAyah {
  readonly verseKey: QuranVerseKey;
  readonly pageNumber: number;
  readonly wordIds: Set<number>;
}

export function createPhraseRepetitionLinkingLaunch(
  response: PhraseOccurrencePageResponse,
): LinkingSourceLaunch | null {
  if (!isCompleteOccurrenceResponse(response)) {
    return null;
  }

  const ayahs = new Map<number, CollectedPhraseAyah>();
  const occurrenceIds = new Set<number>();

  for (const occurrence of response.items) {
    if (
      !isValidOccurrence(occurrence, response.phrase.wordCount) ||
      occurrenceIds.has(occurrence.occurrenceId)
    ) {
      return null;
    }

    occurrenceIds.add(occurrence.occurrenceId);
    const verse = parseQuranVerseKey(occurrence.verseKey);
    if (!verse || verse.key !== occurrence.verseKey) {
      return null;
    }
    const existing = ayahs.get(occurrence.ayahId);
    if (
      existing !== undefined &&
      (existing.verseKey !== occurrence.verseKey ||
        existing.pageNumber !== occurrence.pageFrom)
    ) {
      return null;
    }

    const collected = existing ?? {
      verseKey: verse.key,
      pageNumber: occurrence.pageFrom,
      wordIds: new Set<number>(),
    };
    occurrence.highlights.queryQuranWordIds.forEach((wordId) => collected.wordIds.add(wordId));
    ayahs.set(occurrence.ayahId, collected);
  }

  if (ayahs.size !== response.phrase.ayahCount) {
    return null;
  }

  const orderedAyahs = [...ayahs.entries()].sort(([, left], [, right]) =>
    compareQuranVerseKeys(left.verseKey, right.verseKey),
  );
  const selectedWords = orderedAyahs
    .flatMap(([ayahId, ayah]) =>
      [...ayah.wordIds]
        .sort((leftWordId, rightWordId) => leftWordId - rightWordId)
        .map((quranWordId) => ({ ayahId, quranWordId })),
    );

  if (orderedAyahs.length === 0 || selectedWords.length === 0) {
    return null;
  }

  return ManualLinkingSourceFactory.createLaunch({
    label: `تكرارات العبارة «${response.phrase.displayText}»`,
    contextKey: `quran-phrase-repetition:${response.activeBuildId}:${response.phrase.variantId}`,
    verseKeys: orderedAyahs.map(([, ayah]) => ayah.verseKey),
    selectedWords,
    configuration: 'explicit',
  });
}

function isCompleteOccurrenceResponse(response: PhraseOccurrencePageResponse): boolean {
  return (
    response.activeBuildId.trim().length > 0 &&
    Number.isSafeInteger(response.phrase.variantId) &&
    response.phrase.variantId > 0 &&
    response.phrase.displayText.trim().length > 0 &&
    Number.isSafeInteger(response.phrase.wordCount) &&
    response.phrase.wordCount > 0 &&
    response.page === 1 &&
    response.totalCount > 0 &&
    response.items.length === response.totalCount &&
    response.phrase.occurrenceCount === response.totalCount &&
    response.phrase.ayahCount > 0
  );
}

function isValidOccurrence(occurrence: PhraseOccurrenceDto, phraseWordCount: number): boolean {
  const targetWordIds = occurrence.highlights.queryQuranWordIds;
  const expectedTargetCount = occurrence.endWordNumber - occurrence.startWordNumber + 1;
  const canonicalWordIds = new Set(occurrence.words.map((word) => word.quranWordId));

  return (
    Number.isSafeInteger(occurrence.occurrenceId) &&
    occurrence.occurrenceId > 0 &&
    Number.isSafeInteger(occurrence.ayahId) &&
    occurrence.ayahId > 0 &&
    isCanonicalVerseKey(occurrence.verseKey) &&
    Number.isSafeInteger(occurrence.pageFrom) &&
    occurrence.pageFrom >= 1 &&
    occurrence.pageFrom <= 604 &&
    Number.isSafeInteger(occurrence.startWordNumber) &&
    occurrence.startWordNumber > 0 &&
    Number.isSafeInteger(occurrence.endWordNumber) &&
    occurrence.endWordNumber >= occurrence.startWordNumber &&
    expectedTargetCount === phraseWordCount &&
    targetWordIds.length === expectedTargetCount &&
    new Set(targetWordIds).size === targetWordIds.length &&
    targetWordIds.every(
      (wordId) => isCanonicalQuranWordId(wordId) && canonicalWordIds.has(wordId),
    )
  );
}

function isCanonicalVerseKey(value: unknown): boolean {
  const parsed = parseQuranVerseKey(value);
  return parsed !== null && parsed.key === value;
}
