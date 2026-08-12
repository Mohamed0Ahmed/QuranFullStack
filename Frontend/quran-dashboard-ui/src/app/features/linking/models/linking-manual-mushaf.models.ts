export interface LinkingManualMushafAyahReference {
  verseKey: string;
  pageNumber: number | null;
  displayHint: string | null;
}

export interface LinkingManualMushafAyahSource {
  manualAyahs: readonly LinkingManualMushafAyahReference[];
}

export type LinkingManualLinkShape = 'grouped' | 'independent';

export type LinkingManualWordIdsByVerseKey = Readonly<Record<string, readonly number[]>>;

export function isLinkingManualMushafAyahSource(value: unknown): value is LinkingManualMushafAyahSource {
  if (!isRecord(value) || !Array.isArray(value['manualAyahs']) || value['manualAyahs'].length === 0) {
    return false;
  }

  return value['manualAyahs'].every(isManualAyahReference);
}

export function isCanonicalQuranWordId(value: unknown): value is number {
  return typeof value === 'number' && Number.isSafeInteger(value) && value > 0;
}

function isManualAyahReference(value: unknown): value is LinkingManualMushafAyahReference {
  return (
    isRecord(value) &&
    isVerseKey(value['verseKey']) &&
    (value['pageNumber'] === null || isPageNumber(value['pageNumber'])) &&
    (value['displayHint'] === null || isNonBlankString(value['displayHint']))
  );
}

function isVerseKey(value: unknown): value is string {
  if (typeof value !== 'string') {
    return false;
  }

  const match = /^(\d{1,3}):(\d{1,3})$/.exec(value);
  if (!match) {
    return false;
  }

  const surahNumber = Number(match[1]);
  const ayahNumber = Number(match[2]);
  return surahNumber >= 1 && surahNumber <= 114 && ayahNumber >= 1 && ayahNumber <= 286;
}

function isPageNumber(value: unknown): value is number {
  return typeof value === 'number' && Number.isSafeInteger(value) && value >= 1 && value <= 604;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null;
}

function isNonBlankString(value: unknown): value is string {
  return typeof value === 'string' && value.trim().length > 0;
}
