import { describe, expect, it } from 'vitest';
import { convertToParamMap } from '@angular/router';

import {
  buildUrlEnumCorrections,
  clampMushafPageNumber,
  normalizeAyahTab,
  normalizePanelMode,
  normalizeWordTab,
  parseMushafUrlParams,
} from './mushaf-url-sync';

describe('mushaf-url-sync', () => {
  it('clamps page numbers to 1–604', () => {
    expect(clampMushafPageNumber('0')).toBe(1);
    expect(clampMushafPageNumber('999')).toBe(604);
    expect(clampMushafPageNumber('not-a-number')).toBe(1);
    expect(clampMushafPageNumber('5')).toBe(5);
  });

  it('normalizes out-of-scope enum values to the nearest valid v1 value', () => {
    expect(normalizePanelMode('sources')).toBe('none');
    expect(normalizePanelMode('word')).toBe('word');
    expect(normalizeAyahTab('links')).toBe('tafsir');
    expect(normalizeAyahTab('full-i3rab')).toBe('full-i3rab');
    expect(normalizeWordTab('unknown')).toBe('segments');
    expect(normalizeWordTab('identity')).toBe('identity');
  });

  it('parses a full deep-link URL snapshot with natural Quran keys', () => {
    const snapshot = parseMushafUrlParams(
      convertToParamMap({
        page: '5',
        ayah: '2:25',
        word: '2:25:3',
        segment: '2:25:3:1',
        panel: 'word',
        ayahTab: 'tafsir',
        wordTab: 'segments',
        tafsirSource: 'ar-muyassar',
        translationSource: 'en-sahih-international',
        fullI3rabSource: 'muyassar',
      }),
    );

    expect(snapshot).toEqual({
      pageNumber: 5,
      ayah: '2:25',
      word: '2:25:3',
      segment: '2:25:3:1',
      panel: 'word',
      ayahTab: 'tafsir',
      wordTab: 'segments',
      sources: {
        tafsirSource: 'ar-muyassar',
        translationSource: 'en-sahih-international',
        fullI3rabSource: 'muyassar',
      },
    });
  });

  it('derives ayah from word when the URL has word but omits ayah', () => {
    const snapshot = parseMushafUrlParams(
      convertToParamMap({
        page: '5',
        word: '2:25:3',
      }),
    );

    expect(snapshot.ayah).toBe('2:25');
    expect(snapshot.word).toBe('2:25:3');
  });

  it('builds URL corrections when raw enum values are out of scope', () => {
    const snapshot = parseMushafUrlParams(
      convertToParamMap({
        page: '700',
        panel: 'sources',
        ayahTab: 'links',
        wordTab: 'bad-tab',
      }),
    );

    const corrections = buildUrlEnumCorrections(
      convertToParamMap({
        page: '700',
        panel: 'sources',
        ayahTab: 'links',
        wordTab: 'bad-tab',
      }),
      snapshot,
    );

    expect(corrections).toEqual({
      page: 604,
      panel: null,
      ayahTab: 'tafsir',
      wordTab: 'segments',
    });
  });
});
