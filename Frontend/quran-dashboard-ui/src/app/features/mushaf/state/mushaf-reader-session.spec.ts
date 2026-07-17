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
  focusAyah: null,
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

  it('treats a URL whose only params are overlay-owned keys as bare (Feature 029 B7)', () => {
    expect(
      isBareMushafEntry(
        convertToParamMap({
          qdDetail: 'v1~root~999~ayahs~simple~mentioned~1',
          qdDetailOpen: '1',
        }),
      ),
    ).toBe(true);
    expect(isBareMushafEntry(convertToParamMap({ qdDetail: 'v1~root~999~ayahs~simple~mentioned~1' }))).toBe(true);
    // Any reader-owned key alongside overlay keys means the entry is not bare.
    expect(
      isBareMushafEntry(convertToParamMap({ page: '5', qdDetail: 'v1~root~999~ayahs~simple~mentioned~1' })),
    ).toBe(false);
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
      focusAyah: null,
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

  it('does not persist transient focusAyah in session storage', () => {
    saveMushafReaderSession({ ...sampleSnapshot, focusAyah: '4:57' });

    expect(loadMushafReaderSession()?.focusAyah).toBeNull();
    expect(mushafSnapshotToQueryParams({ ...sampleSnapshot, focusAyah: '4:57' })).toEqual(
      mushafSnapshotToQueryParams(sampleSnapshot),
    );
  });

  it('serializes widened similarity ayah tabs to query params', () => {
    const similarAyahsSnapshot: MushafUrlSnapshot = {
      pageNumber: 5,
      ayah: '2:25',
      focusAyah: null,
      word: null,
      segment: null,
      panel: 'ayah',
      ayahTab: 'similar-ayahs',
      wordTab: DEFAULT_MUSHAF_READER_STATE.wordTab,
      sources: {
        tafsirSource: null,
        translationSource: null,
        fullI3rabSource: null,
      },
    };
    const mutashabihatSnapshot: MushafUrlSnapshot = {
      ...similarAyahsSnapshot,
      ayahTab: 'mutashabihat',
    };

    expect(mushafSnapshotToQueryParams(similarAyahsSnapshot)).toEqual({
      page: 5,
      ayah: '2:25',
      panel: 'ayah',
      ayahTab: 'similar-ayahs',
    });
    expect(mushafSnapshotToQueryParams(mutashabihatSnapshot)).toEqual({
      page: 5,
      ayah: '2:25',
      panel: 'ayah',
      ayahTab: 'mutashabihat',
    });
  });

  it('round-trips widened similarity ayah tabs through sessionStorage', () => {
    for (const ayahTab of ['similar-ayahs', 'mutashabihat'] as const) {
      const snapshot: MushafUrlSnapshot = {
        pageNumber: 5,
        ayah: '2:25',
        focusAyah: null,
        word: null,
        segment: null,
        panel: 'ayah',
        ayahTab,
        wordTab: DEFAULT_MUSHAF_READER_STATE.wordTab,
        sources: {
          tafsirSource: null,
          translationSource: null,
          fullI3rabSource: null,
        },
      };

      saveMushafReaderSession(snapshot);
      expect(loadMushafReaderSession()).toEqual(snapshot);
      sessionStorage.clear();
    }
  });

  it('returns null for missing or invalid stored session payloads', () => {
    expect(loadMushafReaderSession()).toBeNull();

    sessionStorage.setItem(SESSION_STORAGE_KEY, '{not-json');
    expect(loadMushafReaderSession()).toBeNull();

    sessionStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify({ pageNumber: 5 }));
    expect(loadMushafReaderSession()).toBeNull();
  });
});
