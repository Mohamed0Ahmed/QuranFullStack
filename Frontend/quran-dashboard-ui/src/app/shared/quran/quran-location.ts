declare const quranVerseKeyBrand: unique symbol;
declare const quranWordLocationBrand: unique symbol;

export type QuranVerseKey = string & { readonly [quranVerseKeyBrand]: true };
export type QuranWordLocation = string & { readonly [quranWordLocationBrand]: true };

export interface ParsedQuranVerseKey {
  readonly key: QuranVerseKey;
  readonly surahNumber: number;
  readonly ayahNumber: number;
}

export interface ParsedQuranWordLocation extends ParsedQuranVerseKey {
  readonly location: QuranWordLocation;
  readonly wordNumber: number;
}

const VERSE_KEY_PATTERN = /^(\d+):(\d+)$/;
const WORD_LOCATION_PATTERN = /^(\d+):(\d+):(\d+)$/;

export function parseQuranVerseKey(value: unknown): ParsedQuranVerseKey | null {
  if (typeof value !== 'string') {
    return null;
  }

  const match = VERSE_KEY_PATTERN.exec(value);
  if (!match) {
    return null;
  }

  const surahNumber = Number(match[1]);
  const ayahNumber = Number(match[2]);
  const key = buildQuranVerseKey(surahNumber, ayahNumber);
  return key ? { key, surahNumber, ayahNumber } : null;
}

export function buildQuranVerseKey(
  surahNumber: number,
  ayahNumber: number,
): QuranVerseKey | null {
  if (!isSafeIntegerBetween(surahNumber, 1, 114) || !isSafeIntegerBetween(ayahNumber, 1, 286)) {
    return null;
  }

  return `${surahNumber}:${ayahNumber}` as QuranVerseKey;
}

export function parseQuranWordLocation(value: unknown): ParsedQuranWordLocation | null {
  if (typeof value !== 'string') {
    return null;
  }

  const match = WORD_LOCATION_PATTERN.exec(value);
  if (!match) {
    return null;
  }

  const surahNumber = Number(match[1]);
  const ayahNumber = Number(match[2]);
  const wordNumber = Number(match[3]);
  const location = buildQuranWordLocation(surahNumber, ayahNumber, wordNumber);
  const key = buildQuranVerseKey(surahNumber, ayahNumber);
  return location && key ? { location, key, surahNumber, ayahNumber, wordNumber } : null;
}

export function buildQuranWordLocation(
  surahNumber: number,
  ayahNumber: number,
  wordNumber: number,
): QuranWordLocation | null {
  const key = buildQuranVerseKey(surahNumber, ayahNumber);
  if (!key || !isSafeIntegerBetween(wordNumber, 1, Number.MAX_SAFE_INTEGER)) {
    return null;
  }

  return `${key}:${wordNumber}` as QuranWordLocation;
}

export function quranVerseKeyFromWordLocation(location: QuranWordLocation): QuranVerseKey {
  const separator = location.lastIndexOf(':');
  return location.slice(0, separator) as QuranVerseKey;
}

export function compareQuranVerseKeys(left: QuranVerseKey, right: QuranVerseKey): number {
  const leftSeparator = left.indexOf(':');
  const rightSeparator = right.indexOf(':');
  const leftSurah = Number(left.slice(0, leftSeparator));
  const rightSurah = Number(right.slice(0, rightSeparator));
  return leftSurah - rightSurah || Number(left.slice(leftSeparator + 1)) - Number(right.slice(rightSeparator + 1));
}

function isSafeIntegerBetween(value: number, minimum: number, maximum: number): boolean {
  return Number.isSafeInteger(value) && value >= minimum && value <= maximum;
}
