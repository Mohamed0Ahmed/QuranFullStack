import { ParamMap } from '@angular/router';

import {
  DEFAULT_MUSHAF_READER_STATE,
  MUSHAF_URL_KEYS,
  MushafReaderSources,
} from '../models/mushaf.models';
import {
  MushafUrlSnapshot,
  clampMushafPageNumber,
  normalizeAyahTab,
  normalizePanelMode,
  normalizeWordTab,
} from './mushaf-url-sync';

const SESSION_STORAGE_KEY = 'qd-mushaf-reader-session';

type MushafQueryParams = Partial<
  Record<(typeof MUSHAF_URL_KEYS)[keyof typeof MUSHAF_URL_KEYS], string | number | null>
>;

/** True when the reader route has no query string (navbar return without deep-link params). */
export function isBareMushafEntry(params: ParamMap): boolean {
  return params.keys.length === 0;
}

/** Maps a normalized reader snapshot to URL query params, omitting v1 defaults. */
export function mushafSnapshotToQueryParams(snapshot: MushafUrlSnapshot): MushafQueryParams {
  const params: MushafQueryParams = {};

  if (snapshot.pageNumber !== DEFAULT_MUSHAF_READER_STATE.pageNumber) {
    params[MUSHAF_URL_KEYS.page] = snapshot.pageNumber;
  }

  if (snapshot.ayah) {
    params[MUSHAF_URL_KEYS.ayah] = snapshot.ayah;
  }

  if (snapshot.word) {
    params[MUSHAF_URL_KEYS.word] = snapshot.word;
  }

  if (snapshot.segment) {
    params[MUSHAF_URL_KEYS.segment] = snapshot.segment;
  }

  if (snapshot.panel !== DEFAULT_MUSHAF_READER_STATE.panel) {
    params[MUSHAF_URL_KEYS.panel] = snapshot.panel;
  }

  if (snapshot.ayahTab !== DEFAULT_MUSHAF_READER_STATE.ayahTab) {
    params[MUSHAF_URL_KEYS.ayahTab] = snapshot.ayahTab;
  }

  if (snapshot.wordTab !== DEFAULT_MUSHAF_READER_STATE.wordTab) {
    params[MUSHAF_URL_KEYS.wordTab] = snapshot.wordTab;
  }

  if (snapshot.sources.tafsirSource) {
    params[MUSHAF_URL_KEYS.tafsirSource] = snapshot.sources.tafsirSource;
  }

  if (snapshot.sources.translationSource) {
    params[MUSHAF_URL_KEYS.translationSource] = snapshot.sources.translationSource;
  }

  if (snapshot.sources.fullI3rabSource) {
    params[MUSHAF_URL_KEYS.fullI3rabSource] = snapshot.sources.fullI3rabSource;
  }

  return params;
}

export function saveMushafReaderSession(snapshot: MushafUrlSnapshot): void {
  try {
    sessionStorage.setItem(
      SESSION_STORAGE_KEY,
      JSON.stringify({ ...snapshot, focusAyah: null }),
    );
  } catch {
    // Storage may be unavailable in restricted browser contexts.
  }
}

export function loadMushafReaderSession(): MushafUrlSnapshot | null {
  try {
    const raw = sessionStorage.getItem(SESSION_STORAGE_KEY);
    if (!raw) {
      return null;
    }

    return parseStoredSession(raw);
  } catch {
    return null;
  }
}

function parseStoredSession(raw: string): MushafUrlSnapshot | null {
  let parsed: unknown;

  try {
    parsed = JSON.parse(raw);
  } catch {
    return null;
  }

  if (!parsed || typeof parsed !== 'object') {
    return null;
  }

  const record = parsed as Record<string, unknown>;
  const sources = record['sources'];

  if (!sources || typeof sources !== 'object') {
    return null;
  }

  const sourceRecord = sources as Record<string, unknown>;

  return {
    pageNumber: clampMushafPageNumber(String(record['pageNumber'] ?? '1')),
    ayah: readNullableString(record['ayah']),
    focusAyah: null,
    word: readNullableString(record['word']),
    segment: readNullableString(record['segment']),
    panel: normalizePanelMode(readNullableString(record['panel'])),
    ayahTab: normalizeAyahTab(readNullableString(record['ayahTab'])),
    wordTab: normalizeWordTab(readNullableString(record['wordTab'])),
    sources: readSources(sourceRecord),
  };
}

function readNullableString(value: unknown): string | null {
  return typeof value === 'string' && value.length > 0 ? value : null;
}

function readSources(record: Record<string, unknown>): MushafReaderSources {
  return {
    tafsirSource: readNullableString(record['tafsirSource']),
    translationSource: readNullableString(record['translationSource']),
    fullI3rabSource: readNullableString(record['fullI3rabSource']),
  };
}
