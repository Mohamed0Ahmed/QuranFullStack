import type { QuranVerseKey } from '../../../shared/quran/quran-location';
import { isCanonicalQuranWordId, type LinkingManualLinkShape } from '../models/linking-manual-mushaf.models';
import type { LinkingAyahIdSelection, LinkingDescriptionDraft, LinkingOperationSourceDraft } from '../models/linking-operation-draft.models';
import type { LinkingSourceInitialSelectedWord, LinkingSourceLaunch } from '../models/linking-source-launch.models';
import { isLinkingSourceDescriptor, type LinkingSourceDescriptor } from '../models/linking-source.models';
import { orderedUniqueLinkingVerseKeys } from './linking-verse-order';

type LaunchIntent = {
  readonly label: string;
  readonly contextKey: string | null;
  readonly verseKeys: readonly QuranVerseKey[];
  readonly selectedWords?: readonly LinkingSourceInitialSelectedWord[];
  readonly configuration: 'none' | 'explicit';
};

type PreparedDraftIntent = {
  readonly sourceKey: string;
  readonly linkingDataRevision: number;
  readonly label: string;
  readonly contextKey: string | null;
  readonly ayahs: readonly {
    readonly ayahId: number;
    readonly verseKey: QuranVerseKey;
    readonly selectedWordIds: readonly number[];
  }[];
  readonly descriptions: readonly LinkingDescriptionDraft[];
  readonly manualLinkShape: LinkingManualLinkShape;
};

type ManualDescriptor = Extract<LinkingSourceDescriptor, { kind: 'manual-mushaf-ayahs' }>;
type ReadonlyPreparedDraft = Readonly<Omit<LinkingOperationSourceDraft, 'descriptor' | 'selection' | 'descriptions'>> & {
  readonly descriptor: ManualDescriptor;
  readonly selection: Readonly<LinkingAyahIdSelection>;
  readonly descriptions: readonly Readonly<LinkingDescriptionDraft>[];
};

export const ManualLinkingSourceFactory = {
  createLaunch(intent: LaunchIntent): Readonly<LinkingSourceLaunch> | null {
    const source = descriptor(intent.label, intent.contextKey, intent.verseKeys);
    const selectedWords = normalizedWords(intent.selectedWords ?? []);
    if (!source || !selectedWords) return null;
    return {
      source,
      initialConfiguration: intent.configuration === 'none' ? null : {
        inclusionMode: 'all-except',
        ayahOverrideIds: [],
        selectedWords,
        automaticWordMatchesEnabled: null,
        manualLinkShape: 'independent',
        descriptions: [],
      },
    };
  },

  createPreparedDraft(intent: PreparedDraftIntent): ReadonlyPreparedDraft | null {
    const source = descriptor(intent.label, intent.contextKey, intent.ayahs.map((ayah) => ayah.verseKey));
    const selectedWordIdsByAyahId = normalizedPreparedWords(intent.ayahs);
    if (!source || !selectedWordIdsByAyahId) return null;
    return {
      sourceKey: intent.sourceKey,
      sourceId: null,
      sourceVersion: null,
      linkingDataRevision: intent.linkingDataRevision,
      descriptor: source,
      label: intent.label,
      selection: { mode: 'all-except', ayahIds: [] },
      selectedWordIdsByAyahId,
      descriptions: intent.descriptions,
      automaticWordMatchesEnabled: null,
      manualLinkShape: intent.manualLinkShape,
    };
  },
} as const;

function descriptor(label: string, contextKey: string | null, verseKeys: readonly QuranVerseKey[]): ManualDescriptor | null {
  const value = { kind: 'manual-mushaf-ayahs', label, contextKey, verseKeys: orderedUniqueLinkingVerseKeys(verseKeys) } as const;
  return isLinkingSourceDescriptor(value) ? value : null;
}

function normalizedWords(words: readonly LinkingSourceInitialSelectedWord[]): readonly LinkingSourceInitialSelectedWord[] | null {
  if (words.some((word) => !isPositiveSafeInteger(word.ayahId) || !isCanonicalQuranWordId(word.quranWordId))) return null;
  return [...new Map(words.map((word) => [`${word.ayahId}:${word.quranWordId}`, word])).values()]
    .sort((left, right) => left.ayahId - right.ayahId || left.quranWordId - right.quranWordId);
}

function normalizedPreparedWords(
  ayahs: PreparedDraftIntent['ayahs'],
): Readonly<Record<number, readonly number[]>> | null {
  const grouped = new Map<number, Set<number>>();
  for (const ayah of ayahs) {
    if (!isPositiveSafeInteger(ayah.ayahId) || !ayah.selectedWordIds.every(isCanonicalQuranWordId)) return null;
    const words = grouped.get(ayah.ayahId) ?? new Set<number>();
    ayah.selectedWordIds.forEach((wordId) => words.add(wordId));
    grouped.set(ayah.ayahId, words);
  }
  return Object.fromEntries([...grouped].sort(([left], [right]) => left - right)
    .map(([ayahId, words]) => [ayahId, [...words].sort((left, right) => left - right)]));
}

function isPositiveSafeInteger(value: number): boolean {
  return Number.isSafeInteger(value) && value > 0;
}
