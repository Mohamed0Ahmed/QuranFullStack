import { parseQuranVerseKey, type QuranVerseKey } from '../../../shared/quran/quran-location';

export interface LinkingManualMushafAyahReference {
  verseKey: QuranVerseKey;
  pageNumber: number | null;
  displayHint: string | null;
}

export interface LinkingManualMushafAyahSource {
  manualAyahs: readonly LinkingManualMushafAyahReference[];
  contextKey: string | null;
}

export type LinkingManualLinkShape = 'grouped' | 'independent';

export type LinkingManualWordIdsByVerseKey = Readonly<Record<string, readonly number[]>>;

export function isLinkingManualMushafAyahSource(value: unknown): value is LinkingManualMushafAyahSource {
  if (
    !isRecord(value) ||
    !Array.isArray(value['manualAyahs']) ||
    value['manualAyahs'].length === 0 ||
    !isContextKey(value['contextKey'])
  ) {
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
    isCanonicalPersistedVerseKey(value['verseKey']) &&
    (value['pageNumber'] === null || isPageNumber(value['pageNumber'])) &&
    (value['displayHint'] === null || isNonBlankString(value['displayHint']))
  );
}

function isCanonicalPersistedVerseKey(value: unknown): value is QuranVerseKey {
  const parsed = parseQuranVerseKey(value);
  return parsed !== null && parsed.key === value;
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

function isContextKey(value: unknown): value is string | null {
  return value === null ||
    typeof value === 'string' &&
    value.length > 0 &&
    value.length <= 512 &&
    value.trim() === value;
}
