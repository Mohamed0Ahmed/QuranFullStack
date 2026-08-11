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
      const sourceKeys = unique([...existing.sourceKeys, source.member.sourceKey]);
      const words = mergeWordSelections(existing.words, ayah.words, source.member.sourceKey);
      mergedByVerseKey.set(ayah.verseKey, {
        ...existing,
        ayah: enrichAyah(existing.ayah, ayah),
        sourceKeys,
        words,
      });
    }
  }
  return {
    ayahs: [...mergedByVerseKey.values()].sort((left, right) => compareLinkingVerseKeys(left.verseKey, right.verseKey)),
  };
}

function assertCompatibleAyahs(left: LinkingAyah, right: LinkingAyah): void {
  if (
    conflicts(left.ayahId, right.ayahId) ||
    conflicts(left.surahNumber, right.surahNumber) ||
    conflicts(left.surahNameArabic, right.surahNameArabic) ||
    left.pageNumber !== right.pageNumber ||
    left.ayahNumber !== right.ayahNumber ||
    left.words.filter((word) => !word.isAyahMarker).length !== right.words.filter((word) => !word.isAyahMarker).length
  ) {
    throw new Error('تعارضت بيانات الآية بين مصادر الربط.');
  }
  const leftWords = left.words.filter((word) => !word.isAyahMarker);
  const rightWords = right.words.filter((word) => !word.isAyahMarker);
  if (leftWords.some((word, index) => !sameOccurrence(word, rightWords[index]))) {
    throw new Error('تعارض تسلسل كلمات الآية بين مصادر الربط.');
  }
}

function mergeWords(words: readonly LinkingAyahWord[], sourceKey: string): readonly MergedLinkingWordSelection[] {
  return words
    .filter((word) => !word.isAyahMarker)
    .map((word) => ({
      renderPosition: word.renderPosition,
      textUthmani: word.textUthmani,
      sourceKeys: word.isSourceMatch ? [sourceKey] : [],
      canonicalQuranWordId: word.canonicalQuranWordId,
      wordLocation: word.wordLocation,
    }));
}

function mergeWordSelections(
  existing: readonly MergedLinkingWordSelection[],
  words: readonly LinkingAyahWord[],
  sourceKey: string,
): readonly MergedLinkingWordSelection[] {
  const incoming = words.filter((word) => !word.isAyahMarker);
  return existing.map((word, index) => {
    const candidate = incoming[index];
    if (candidate === undefined || !sameOccurrence(word, candidate)) {
      throw new Error('تعارض ترتيب كلمات الآية بين مصادر الربط.');
    }
    return {
      ...word,
      sourceKeys: candidate.isSourceMatch ? unique([...word.sourceKeys, sourceKey]) : word.sourceKeys,
      canonicalQuranWordId: word.canonicalQuranWordId ?? candidate.canonicalQuranWordId,
      wordLocation: word.wordLocation ?? candidate.wordLocation,
    };
  });
}

function enrichAyah(left: LinkingAyah, right: LinkingAyah): LinkingAyah {
  return {
    ...left,
    ayahId: left.ayahId ?? right.ayahId,
    surahNumber: left.surahNumber ?? right.surahNumber,
    surahNameArabic: left.surahNameArabic ?? right.surahNameArabic,
  };
}

function sameOccurrence(
  left: Pick<LinkingAyahWord, 'textUthmani' | 'canonicalQuranWordId'>,
  right: Pick<LinkingAyahWord, 'textUthmani' | 'canonicalQuranWordId'>,
): boolean {
  return (
    left.textUthmani === right.textUthmani &&
    !conflicts(left.canonicalQuranWordId, right.canonicalQuranWordId)
  );
}

function conflicts<T>(left: T | null, right: T | null): boolean {
  return left !== null && right !== null && left !== right;
}

function unique(values: readonly string[]): readonly string[] {
  return [...new Set(values)];
}
