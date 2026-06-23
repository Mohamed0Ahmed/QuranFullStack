import { ParamMap } from '@angular/router';

import { MUSHAF_ROUTE_PATH } from '../../../core/navigation/route-paths';

import {
  AyahStudyTab,
  DEFAULT_MUSHAF_READER_STATE,
  MUSHAF_URL_KEYS,
  MushafReaderSources,
  PanelMode,
  WordAnalysisTab,
} from '../models/mushaf.models';
import { verseKeyFromWordLocation } from '../utils/mushaf-location-keys';

const VALID_PANELS: ReadonlySet<string> = new Set(['ayah', 'word', 'none']);
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
  ayah: string | null;
  focusAyah: string | null;
  word: string | null;
  segment: string | null;
  panel: PanelMode;
  ayahTab: AyahStudyTab;
  wordTab: WordAnalysisTab;
  sources: MushafReaderSources;
}

// Below this width, `panel` selects the active study section.
export const MUSHAF_WIDE_DESKTOP_MIN_PX = 1024;

export function clampMushafPageNumber(raw: string | null): number {
  const parsed = Number(raw ?? '1');
  if (!Number.isFinite(parsed)) {
    return 1;
  }
  return Math.min(604, Math.max(1, parsed));
}

// Drops unsupported enum values (e.g. `panel=sources`, `ayahTab=links`).
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
  const word = params.get(MUSHAF_URL_KEYS.word);
  const ayahParam = params.get(MUSHAF_URL_KEYS.ayah);
  const ayah = ayahParam ?? (word ? verseKeyFromWordLocation(word) : null);

  return {
    pageNumber: clampMushafPageNumber(params.get(MUSHAF_URL_KEYS.page)),
    ayah,
    focusAyah: params.get(MUSHAF_URL_KEYS.focusAyah),
    word,
    segment: params.get(MUSHAF_URL_KEYS.segment),
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

/**
 * Returns query-param corrections when raw URL values were normalized away.
 * Used to silently fix shareable URLs that contain out-of-scope enum values.
 */
export function buildUrlEnumCorrections(
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

  return corrections;
}

export interface MushafDeepLinkOptions {
  pageNumber: number;
  ayah: string;
  focusAyah: string;
  panel: PanelMode;
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
