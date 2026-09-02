import { parseQuranVerseKey, type QuranVerseKey } from '../../../shared/quran/quran-location';

export interface LinkingManualMushafAyahSource {
  readonly verseKeys: readonly QuranVerseKey[];
  readonly contextKey: string | null;
}

export type LinkingManualLinkShape = 'grouped' | 'independent';

export type LinkingManualWordIdsByVerseKey = Readonly<Record<string, readonly number[]>>;

export function isLinkingManualMushafAyahSource(value: unknown): value is LinkingManualMushafAyahSource {
  if (
    !isRecord(value) ||
    !Array.isArray(value['verseKeys']) ||
    value['verseKeys'].length === 0 ||
    !isContextKey(value['contextKey'])
  ) {
    return false;
  }

  return value['verseKeys'].every(isCanonicalPersistedVerseKey);
}

export function isCanonicalQuranWordId(value: unknown): value is number {
  return typeof value === 'number' && Number.isSafeInteger(value) && value > 0;
}

function isCanonicalPersistedVerseKey(value: unknown): value is QuranVerseKey {
  const parsed = parseQuranVerseKey(value);
  return parsed !== null && parsed.key === value;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null;
}

function isContextKey(value: unknown): value is string | null {
  return value === null ||
    typeof value === 'string' &&
    value.length > 0 &&
    value.length <= 512 &&
    value.trim() === value;
}
