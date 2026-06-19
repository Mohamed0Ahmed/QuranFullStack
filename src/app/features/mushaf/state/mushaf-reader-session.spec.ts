import { describe, expect, it, beforeEach, afterEach } from 'vitest';
import { convertToParamMap } from '@angular/router';

import { DEFAULT_MUSHAF_READER_STATE } from '../models/mushaf.models';
import { MushafUrlSnapshot } from './mushaf-url-sync';
import {
  isBareMushafEntry,
  loadMushafReaderSession,
  mushafSnapshotToQueryParams,
  saveMushafReaderSession,
} from './mushaf-reader-session';

const SESSION_STORAGE_KEY = 'qd-mushaf-reader-session';

const sampleSnapshot: MushafUrlSnapshot = {
  pageNumber: 12,
  ayah: '2:25',
  word: '2:25:3',
  segment: '2:25:3:1',
  panel: 'word',
  ayahTab: 'translation',
  wordTab: 'morphology',
  sources: {
    tafsirSource: 'ar-muyassar',
    translationSource: 'en-sahih-international',
    fullI3rabSource: 'muyassar',
  },
};

describe('mushaf-reader-session', () => {
  beforeEach(() => {
    sessionStorage.clear();
  });

  afterEach(() => {
    sessionStorage.clear();
  });

  it('detects a bare mushaf entry when the query string is empty', () => {
    expect(isBareMushafEntry(convertToParamMap({}))).toBe(true);
    expect(isBareMushafEntry(convertToParamMap({ page: '5' }))).toBe(false);
  });

  it('maps a snapshot to query params while omitting v1 defaults', () => {
    expect(mushafSnapshotToQueryParams(sampleSnapshot)).toEqual({
      page: 12,
      ayah: '2:25',
      word: '2:25:3',
      segment: '2:25:3:1',
      panel: 'word',
      ayahTab: 'translation',
      wordTab: 'morphology',
      tafsirSource: 'ar-muyassar',
      translationSource: 'en-sahih-international',
      fullI3rabSource: 'muyassar',
    });
  });

  it('omits default panel and tabs from restored query params', () => {
    const defaultsOnly: MushafUrlSnapshot = {
      pageNumber: DEFAULT_MUSHAF_READER_STATE.pageNumber,
      ayah: null,
      word: null,
      segment: null,
      panel: DEFAULT_MUSHAF_READER_STATE.panel,
      ayahTab: DEFAULT_MUSHAF_READER_STATE.ayahTab,
      wordTab: DEFAULT_MUSHAF_READER_STATE.wordTab,
      sources: {
        tafsirSource: null,
        translationSource: null,
        fullI3rabSource: null,
      },
    };

    expect(mushafSnapshotToQueryParams(defaultsOnly)).toEqual({});
  });

  it('round-trips a snapshot through sessionStorage', () => {
    saveMushafReaderSession(sampleSnapshot);

    expect(sessionStorage.getItem(SESSION_STORAGE_KEY)).not.toBeNull();
    expect(loadMushafReaderSession()).toEqual(sampleSnapshot);
  });

  it('returns null for missing or invalid stored session payloads', () => {
    expect(loadMushafReaderSession()).toBeNull();

    sessionStorage.setItem(SESSION_STORAGE_KEY, '{not-json');
    expect(loadMushafReaderSession()).toBeNull();

    sessionStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify({ pageNumber: 5 }));
    expect(loadMushafReaderSession()).toBeNull();
  });
});
