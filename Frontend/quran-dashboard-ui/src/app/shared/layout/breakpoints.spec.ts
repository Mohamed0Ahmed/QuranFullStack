import { describe, expect, it } from 'vitest';

import contract from './breakpoints.contract.json';
import {
  QD_BP_COMPACT_QUERY,
  QD_BP_DESKTOP_MIN_QUERY,
  QD_BP_MEDIUM_MAX_QUERY,
  QD_BP_MEDIUM_QUERY,
  QD_BP_PHONE_MAX_QUERY,
  QD_BP_TABLET_MAX_QUERY,
  QD_BP_WIDE_PLUS_QUERY,
  QD_BP_WIDE_QUERY,
  QD_BREAKPOINTS,
  qdBandForWidth,
  qdIsWidePlus,
} from './breakpoints';

describe('Golden responsive bands', () => {
  it.each([
    [320, 'compact'],
    [767, 'compact'],
    [768, 'medium'],
    [1024, 'medium'],
    [1079, 'medium'],
    [1080, 'wide'],
    [1439, 'wide'],
    [1440, 'wide'],
  ])('classifies %ipx as the %s band', (width, band) => {
    expect(qdBandForWidth(width)).toBe(band);
  });

  it.each([
    [1079, false],
    [1080, false],
    [1439, false],
    [1440, true],
    [1920, true],
  ])('reports wide-plus at %ipx as %s', (width, expected) => {
    expect(qdIsWidePlus(width)).toBe(expected);
  });

  it('keeps wide-plus a measure enhancement rather than a fourth structural band', () => {
    expect(contract.widePlusIsStructural).toBe(false);
    expect(qdBandForWidth(QD_BREAKPOINTS.widePlusMin)).toBe('wide');
  });

  it('leaves no width uncovered between adjacent bands', () => {
    expect(QD_BREAKPOINTS.mediumMin).toBe(QD_BREAKPOINTS.compactMax + 1);
    expect(QD_BREAKPOINTS.wideMin).toBe(QD_BREAKPOINTS.mediumMax + 1);
  });

  it('derives every media query from the checked-in contract', () => {
    expect(QD_BREAKPOINTS).toEqual({
      compactMax: contract.compactMax,
      mediumMin: contract.mediumMin,
      mediumMax: contract.mediumMax,
      wideMin: contract.wideMin,
      widePlusMin: contract.widePlusMin,
    });
    expect(QD_BP_COMPACT_QUERY).toBe(`(max-width: ${contract.compactMax}px)`);
    expect(QD_BP_MEDIUM_QUERY).toBe(
      `(min-width: ${contract.mediumMin}px) and (max-width: ${contract.mediumMax}px)`,
    );
    expect(QD_BP_MEDIUM_MAX_QUERY).toBe(`(max-width: ${contract.mediumMax}px)`);
    expect(QD_BP_WIDE_QUERY).toBe(`(min-width: ${contract.wideMin}px)`);
    expect(QD_BP_WIDE_PLUS_QUERY).toBe(`(min-width: ${contract.widePlusMin}px)`);
  });

  it('keeps unmigrated consumers on the Golden bands through the legacy aliases', () => {
    expect(QD_BP_PHONE_MAX_QUERY).toBe(QD_BP_COMPACT_QUERY);
    expect(QD_BP_TABLET_MAX_QUERY).toBe(QD_BP_MEDIUM_MAX_QUERY);
    expect(QD_BP_DESKTOP_MIN_QUERY).toBe(QD_BP_WIDE_QUERY);
  });
});
