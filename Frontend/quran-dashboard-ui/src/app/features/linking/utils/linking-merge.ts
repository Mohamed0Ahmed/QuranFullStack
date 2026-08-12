import { LinkingAyah, LinkingAyahWord } from '../models/linking-ayah.models';
import { MergedAyahSelection, MergedLinkingSelection, MergedLinkingWordSelection } from '../models/linking-merge.models';
import { LinkingOperationMember } from '../models/linking-operation.models';
import { compareLinkingVerseKeys } from './linking-verse-order';

export interface ResolvedLinkingSourceMember {
  member: LinkingOperationMember;
  ayahs: readonly LinkingAyah[];
}

export function mergeLinkingSources(sources: readonly ResolvedLinkingSourceMember[]): MergedLinkingSelection {
  const mergedByVerseKey = new Map<string, MergedAyahSelection>();
  for (const source of sources) {
    for (const ayah of source.ayahs) {
      const existing = mergedByVerseKey.get(ayah.verseKey);
      if (existing === undefined) {
        mergedByVerseKey.set(ayah.verseKey, {
          verseKey: ayah.verseKey,
          ayah,
          sourceKeys: [source.member.sourceKey],
          words: mergeWords(ayah.words, source.member.sourceKey),
        });
        continue;
      }
      assertCompatibleAyahs(existing.ayah, ayah);
      const incoming = nonMarkerWords(ayah.words);
      assertCompatibleWords(existing.words, incoming);
      mergedByVerseKey.set(ayah.verseKey, {
        ...existing,
        sourceKeys: unique([...existing.sourceKeys, source.member.sourceKey]),
        words: mergeWordSelections(existing.words, incoming, source.member.sourceKey),
      });
    }
  }
  return {
    ayahs: [...mergedByVerseKey.values()].sort((left, right) => compareLinkingVerseKeys(left.verseKey, right.verseKey)),
  };
}

function assertCompatibleAyahs(left: LinkingAyah, right: LinkingAyah): void {
  if (
    left.ayahId !== right.ayahId ||
    left.surahNumber !== right.surahNumber ||
    left.ayahNumber !== right.ayahNumber ||
    left.surahNameArabic !== right.surahNameArabic ||
    left.pageNumber !== right.pageNumber
  ) {
    throw new Error('تعارضت بيانات الآية بين مصادر الربط.');
  }
}

function assertCompatibleWords(
  existing: readonly MergedLinkingWordSelection[],
  incoming: readonly LinkingAyahWord[],
): void {
  const existingByQuranWordId = new Map(existing.map((word) => [word.canonicalQuranWordId, word]));
  const incomingByQuranWordId = new Map(incoming.map((word) => [word.canonicalQuranWordId, word]));
  if (
    existingByQuranWordId.size !== existing.length ||
    incomingByQuranWordId.size !== incoming.length ||
    existingByQuranWordId.size !== incomingByQuranWordId.size
  ) {
    throw new Error('تعارضت كلمات الآية بين مصادر الربط.');
  }
  for (const [quranWordId, word] of incomingByQuranWordId) {
    const known = existingByQuranWordId.get(quranWordId);
    if (
      known === undefined ||
      known.renderPosition !== word.renderPosition ||
      known.textUthmani !== word.textUthmani
    ) {
      throw new Error('تعارضت كلمات الآية بين مصادر الربط.');
    }
  }
}

function nonMarkerWords(words: readonly LinkingAyahWord[]): readonly LinkingAyahWord[] {
  return words.filter((word) => !word.isAyahMarker);
}

function mergeWords(words: readonly LinkingAyahWord[], sourceKey: string): readonly MergedLinkingWordSelection[] {
  return nonMarkerWords(words).map((word) => toWordSelection(word, sourceKey));
}

function mergeWordSelections(
  existing: readonly MergedLinkingWordSelection[],
  incoming: readonly LinkingAyahWord[],
  sourceKey: string,
): readonly MergedLinkingWordSelection[] {
  const matchedQuranWordIds = new Set(
    incoming.filter((word) => word.isSourceMatch).map((word) => word.canonicalQuranWordId),
  );
  return existing.map((word) =>
    matchedQuranWordIds.has(word.canonicalQuranWordId)
      ? { ...word, sourceKeys: unique([...word.sourceKeys, sourceKey]) }
      : word,
  );
}

function toWordSelection(word: LinkingAyahWord, sourceKey: string): MergedLinkingWordSelection {
  return {
    renderPosition: word.renderPosition,
    textUthmani: word.textUthmani,
    sourceKeys: word.isSourceMatch ? [sourceKey] : [],
    canonicalQuranWordId: word.canonicalQuranWordId,
  };
}

function unique(values: readonly string[]): readonly string[] {
  return [...new Set(values)];
}
