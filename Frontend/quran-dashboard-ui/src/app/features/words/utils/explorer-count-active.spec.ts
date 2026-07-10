import { describe, expect, it } from 'vitest';

import {
  isMorphologyCountActive,
  isUniqueWordCountActive,
  resolveMorphologyActiveColumn,
} from './explorer-count-active';

describe('explorer-count-active', () => {
  it('maps words and surahs panel state to the matching active column', () => {
    expect(resolveMorphologyActiveColumn({ view: 'words', wordView: 'simple' })).toBe('simple');
    expect(resolveMorphologyActiveColumn({ view: 'words', wordView: 'tashkeel' })).toBe('tashkeel');
    expect(resolveMorphologyActiveColumn({ view: 'surahs', surahView: 'missing' })).toBe('surahs');
    expect(resolveMorphologyActiveColumn({ view: 'ayahs' })).toBe('occurrences');
    expect(resolveMorphologyActiveColumn({ view: 'lemmas' })).toBe('lemmas');
    expect(resolveMorphologyActiveColumn({ view: 'stems' })).toBe('stems');
  });

  it('prefers an explicit active column override', () => {
    expect(
      resolveMorphologyActiveColumn({
        view: 'ayahs',
        activeColumn: 'ayahs',
      }),
    ).toBe('ayahs');
  });

  it('ignores an explicit column override that does not match the active view', () => {
    expect(
      resolveMorphologyActiveColumn({
        view: 'words',
        wordView: 'simple',
        activeColumn: 'tashkeel',
      }),
    ).toBe('simple');
  });

  it('marks only the selected morphology chip as active', () => {
    expect(
      isMorphologyCountActive({
        rowId: 7,
        selectedRowId: 7,
        column: 'ayahs',
        activeColumn: 'ayahs',
      }),
    ).toBe(true);

    expect(
      isMorphologyCountActive({
        rowId: 7,
        selectedRowId: 7,
        column: 'occurrences',
        activeColumn: 'ayahs',
      }),
    ).toBe(false);
  });

  it('never marks disabled morphology chips as active', () => {
    expect(
      isMorphologyCountActive({
        rowId: 7,
        selectedRowId: 7,
        column: 'ayahs',
        activeColumn: 'ayahs',
        disabled: true,
      }),
    ).toBe(false);
  });

  it('matches unique words chips only when the drilldown is open on the same row', () => {
    expect(
      isUniqueWordCountActive({
        rowId: 9,
        selectedWordId: 9,
        column: 'surahs',
        activeColumn: 'surahs',
        drilldownOpen: true,
      }),
    ).toBe(true);

    expect(
      isUniqueWordCountActive({
        rowId: 9,
        selectedWordId: 9,
        column: 'ayahs',
        activeColumn: 'surahs',
        drilldownOpen: true,
      }),
    ).toBe(false);
  });
});
