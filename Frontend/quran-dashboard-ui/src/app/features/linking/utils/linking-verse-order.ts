import { isVerseKey } from '../models/linking-source.models';

export function compareLinkingVerseKeys(left: string, right: string): number {
  const leftParts = verseKeyParts(left);
  const rightParts = verseKeyParts(right);

  if (leftParts === null || rightParts === null) {
    return left.localeCompare(right);
  }

  return leftParts.surahNumber - rightParts.surahNumber || leftParts.ayahNumber - rightParts.ayahNumber;
}

export function orderedUniqueLinkingVerseKeys(verseKeys: readonly string[]): readonly string[] {
  return [...new Set(verseKeys.filter(isVerseKey))].sort(compareLinkingVerseKeys);
}

function verseKeyParts(verseKey: string): { surahNumber: number; ayahNumber: number } | null {
  if (!isVerseKey(verseKey)) {
    return null;
  }

  const [surahNumber, ayahNumber] = verseKey.split(':').map(Number);
  return { surahNumber, ayahNumber };
}
