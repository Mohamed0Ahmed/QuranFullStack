import { ParamMap } from '@angular/router';

import { MUSHAF_ROUTE_PATH } from '../../../core/navigation/route-paths';
import {
  QuranVerseKey,
  QuranWordLocation,
  parseQuranVerseKey,
  parseQuranWordLocation,
  quranVerseKeyFromWordLocation,
} from '../../../shared/quran/quran-location';

import {
  AyahStudyTab,
  DEFAULT_MUSHAF_READER_STATE,
  MUSHAF_URL_KEYS,
  MushafReaderSources,
  PanelMode,
  WordAnalysisTab,
} from '../models/mushaf.models';

const VALID_PANELS: ReadonlySet<string> = new Set(['ayah', 'word', 'doors', 'none']);
const VALID_AYAH_TABS: ReadonlySet<string> = new Set([
  'tafsir',
  'translation',
  'full-i3rab',
  'similar-ayahs',
  'mutashabihat',
]);
const VALID_WORD_TABS: ReadonlySet<string> = new Set(['morphology', 'segments', 'i3rab', 'identity']);

export interface MushafUrlSnapshot {
  pageNumber: number;
  ayah: QuranVerseKey | null;
  focusAyah: QuranVerseKey | null;
  word: QuranWordLocation | null;
  segment: string | null;
  panel: PanelMode;
  ayahTab: AyahStudyTab;
  wordTab: WordAnalysisTab;
  sources: MushafReaderSources;
}

export const MUSHAF_WIDE_DESKTOP_MIN_PX = 1024;

export function clampMushafPageNumber(raw: string | null): number {
  const parsed = Number(raw ?? '1');
  if (!Number.isFinite(parsed)) {
    return 1;
  }
  return Math.min(604, Math.max(1, Math.trunc(parsed)));
}

export function normalizePanelMode(value: string | null): PanelMode {
  if (value && VALID_PANELS.has(value)) {
    return value as PanelMode;
  }
  return DEFAULT_MUSHAF_READER_STATE.panel;
}

export function normalizeAyahTab(value: string | null): AyahStudyTab {
  if (value && VALID_AYAH_TABS.has(value)) {
    return value as AyahStudyTab;
  }
  return DEFAULT_MUSHAF_READER_STATE.ayahTab;
}

export function normalizeWordTab(value: string | null): WordAnalysisTab {
  if (value && VALID_WORD_TABS.has(value)) {
    return value as WordAnalysisTab;
  }
  return DEFAULT_MUSHAF_READER_STATE.wordTab;
}

export function parseMushafUrlParams(params: ParamMap): MushafUrlSnapshot {
  const locations = sanitizeMushafQuranLocations(
    params.get(MUSHAF_URL_KEYS.ayah),
    params.get(MUSHAF_URL_KEYS.word),
    params.get(MUSHAF_URL_KEYS.segment),
  );

  return {
    pageNumber: clampMushafPageNumber(params.get(MUSHAF_URL_KEYS.page)),
    ayah: locations.ayah,
    focusAyah: parseQuranVerseKey(params.get(MUSHAF_URL_KEYS.focusAyah))?.key ?? null,
    word: locations.word,
    segment: locations.segment,
    panel: normalizePanelMode(params.get(MUSHAF_URL_KEYS.panel)),
    ayahTab: normalizeAyahTab(params.get(MUSHAF_URL_KEYS.ayahTab)),
    wordTab: normalizeWordTab(params.get(MUSHAF_URL_KEYS.wordTab)),
    sources: {
      tafsirSource: params.get(MUSHAF_URL_KEYS.tafsirSource),
      translationSource: params.get(MUSHAF_URL_KEYS.translationSource),
      fullI3rabSource: params.get(MUSHAF_URL_KEYS.fullI3rabSource),
    },
  };
}

export function buildMushafWordSelectionQuery(
  wordLocation: QuranWordLocation,
  currentWordLocation: QuranWordLocation | null,
): Partial<
  Record<(typeof MUSHAF_URL_KEYS)[keyof typeof MUSHAF_URL_KEYS], string | number | null>
> {
  if (currentWordLocation === wordLocation) {
    return {
      [MUSHAF_URL_KEYS.word]: null,
      [MUSHAF_URL_KEYS.segment]: null,
      [MUSHAF_URL_KEYS.ayah]: null,
      [MUSHAF_URL_KEYS.focusAyah]: null,
    };
  }

  const verseKey = quranVerseKeyFromWordLocation(wordLocation);
  return {
    [MUSHAF_URL_KEYS.word]: wordLocation,
    [MUSHAF_URL_KEYS.focusAyah]: null,
    [MUSHAF_URL_KEYS.ayah]: verseKey,
  };
}

export function buildMushafUrlCorrections(
  raw: ParamMap,
  snapshot: MushafUrlSnapshot,
): Partial<Record<(typeof MUSHAF_URL_KEYS)[keyof typeof MUSHAF_URL_KEYS], string | number | null>> {
  const corrections: Partial<
    Record<(typeof MUSHAF_URL_KEYS)[keyof typeof MUSHAF_URL_KEYS], string | number | null>
  > = {};

  const rawPage = raw.get(MUSHAF_URL_KEYS.page);
  if (rawPage !== null) {
    const numericPage = Number(rawPage);
    if (!Number.isFinite(numericPage) || numericPage !== snapshot.pageNumber) {
      corrections[MUSHAF_URL_KEYS.page] = snapshot.pageNumber;
    }
  }

  const rawPanel = raw.get(MUSHAF_URL_KEYS.panel);
  if (rawPanel !== null && normalizePanelMode(rawPanel) !== rawPanel) {
    corrections[MUSHAF_URL_KEYS.panel] =
      snapshot.panel === DEFAULT_MUSHAF_READER_STATE.panel ? null : snapshot.panel;
  }

  const rawAyahTab = raw.get(MUSHAF_URL_KEYS.ayahTab);
  if (rawAyahTab !== null && normalizeAyahTab(rawAyahTab) !== rawAyahTab) {
    corrections[MUSHAF_URL_KEYS.ayahTab] = snapshot.ayahTab;
  }

  const rawWordTab = raw.get(MUSHAF_URL_KEYS.wordTab);
  if (rawWordTab !== null && normalizeWordTab(rawWordTab) !== rawWordTab) {
    corrections[MUSHAF_URL_KEYS.wordTab] = snapshot.wordTab;
  }

  const rawAyah = raw.get(MUSHAF_URL_KEYS.ayah);
  const rawWord = raw.get(MUSHAF_URL_KEYS.word);
  setCanonicalLocationCorrection(corrections, MUSHAF_URL_KEYS.ayah, rawAyah, snapshot.ayah);
  if (rawAyah === null && rawWord !== null && snapshot.ayah !== null) {
    corrections[MUSHAF_URL_KEYS.ayah] = snapshot.ayah;
  }
  setCanonicalLocationCorrection(
    corrections,
    MUSHAF_URL_KEYS.focusAyah,
    raw.get(MUSHAF_URL_KEYS.focusAyah),
    snapshot.focusAyah,
  );
  setCanonicalLocationCorrection(corrections, MUSHAF_URL_KEYS.word, rawWord, snapshot.word);

  const rawSegment = raw.get(MUSHAF_URL_KEYS.segment);
  if (rawSegment !== null && snapshot.segment === null) {
    corrections[MUSHAF_URL_KEYS.segment] = null;
  }

  return corrections;
}

export interface MushafDeepLinkOptions {
  pageNumber: number;
  ayah: QuranVerseKey;
  focusAyah: QuranVerseKey;
  panel: PanelMode;
}

export function sanitizeMushafQuranLocations(
  ayahValue: unknown,
  wordValue: unknown,
  segmentValue: unknown,
): Pick<MushafUrlSnapshot, 'ayah' | 'word' | 'segment'> {
  const parsedAyah = parseQuranVerseKey(ayahValue);
  const parsedWord = parseQuranWordLocation(wordValue);
  const wordParent = parsedWord ? quranVerseKeyFromWordLocation(parsedWord.location) : null;
  const matchingWord = parsedWord && (!parsedAyah || parsedAyah.key === wordParent)
    ? parsedWord.location
    : null;

  return {
    ayah: parsedAyah?.key ?? wordParent,
    word: matchingWord,
    segment: matchingWord && typeof segmentValue === 'string' && segmentValue.length > 0
      ? segmentValue
      : null,
  };
}

function setCanonicalLocationCorrection(
  corrections: Partial<
    Record<(typeof MUSHAF_URL_KEYS)[keyof typeof MUSHAF_URL_KEYS], string | number | null>
  >,
  key: typeof MUSHAF_URL_KEYS.ayah | typeof MUSHAF_URL_KEYS.focusAyah | typeof MUSHAF_URL_KEYS.word,
  rawValue: string | null,
  canonicalValue: QuranVerseKey | QuranWordLocation | null,
): void {
  if (rawValue !== null && rawValue !== canonicalValue) {
    corrections[key] = canonicalValue;
  }
}

export interface MushafDeepLinkTarget {
  path: string;
  queryParams: Record<string, string>;
}

export function buildMushafDeepLink(options: MushafDeepLinkOptions): MushafDeepLinkTarget {
  return {
    path: MUSHAF_ROUTE_PATH,
    queryParams: {
      [MUSHAF_URL_KEYS.page]: String(options.pageNumber),
      [MUSHAF_URL_KEYS.ayah]: options.ayah,
      [MUSHAF_URL_KEYS.focusAyah]: options.focusAyah,
      [MUSHAF_URL_KEYS.panel]: options.panel,
    },
  };
}
