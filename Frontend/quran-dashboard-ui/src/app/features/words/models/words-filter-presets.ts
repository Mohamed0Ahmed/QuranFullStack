// Count-range filter presets + URL grammar (Feature 026, US5). Presets are presentation config, not
// labels — the URL always stores the actual range (grammar `min..max`), so tuning thresholds later
// never breaks shared links (research R5/R6). This module is the single source of the grammar shared
// by every explorer's url-sync/api/cache layers.

/** An inclusive count range; either bound may be open (null). Absent filter = the whole thing is null. */
export interface CountRange {
  readonly min: number | null;
  readonly max: number | null;
}

/** Bucket families map to the metric groups from the spec Clarifications (disjoint boundaries). */
export type BucketFamily = 'occurrences' | 'ayahsSurahs' | 'subCount';

export interface RangeBucket {
  readonly labelAr: string;
  readonly min: number | null;
  readonly max: number | null;
}

export const RANGE_BUCKETS: Record<BucketFamily, readonly RangeBucket[]> = {
  occurrences: [
    { labelAr: '1', min: 1, max: 1 },
    { labelAr: '2–10', min: 2, max: 10 },
    { labelAr: '11–100', min: 11, max: 100 },
    { labelAr: '101–1000', min: 101, max: 1000 },
    { labelAr: '1001+', min: 1001, max: null },
  ],
  ayahsSurahs: [
    { labelAr: '1', min: 1, max: 1 },
    { labelAr: '2–10', min: 2, max: 10 },
    { labelAr: '11–50', min: 11, max: 50 },
    { labelAr: '51+', min: 51, max: null },
  ],
  subCount: [
    { labelAr: '1', min: 1, max: 1 },
    { labelAr: '2–5', min: 2, max: 5 },
    { labelAr: '6–20', min: 6, max: 20 },
    { labelAr: '21+', min: 21, max: null },
  ],
};

const RANGE_SEPARATOR = '..';
const NON_NEGATIVE_INT = /^\d+$/;

/** True when at least one bound narrows the result. */
export function isRangeActive(range: CountRange | null | undefined): range is CountRange {
  return !!range && (range.min !== null || range.max !== null);
}

/**
 * Parses the shared URL grammar `min..max` fail-closed. Either side may be omitted; anything malformed
 * (missing separator, non-numeric bound, min > max, or an empty range) yields `null` (filter absent).
 */
export function parseCountRange(raw: string | null | undefined): CountRange | null {
  if (raw === null || raw === undefined) {
    return null;
  }
  const separatorIndex = raw.indexOf(RANGE_SEPARATOR);
  if (separatorIndex === -1) {
    return null;
  }

  const minRaw = raw.slice(0, separatorIndex);
  const maxRaw = raw.slice(separatorIndex + RANGE_SEPARATOR.length);

  const min = parseBound(minRaw);
  const max = parseBound(maxRaw);
  if (min === 'invalid' || max === 'invalid') {
    return null;
  }
  if (min === null && max === null) {
    return null;
  }
  if (min !== null && max !== null && min > max) {
    return null;
  }

  return { min, max };
}

/** Serializes an active range to the `min..max` grammar; returns `null` for an absent/empty range. */
export function serializeCountRange(range: CountRange | null | undefined): string | null {
  if (!isRangeActive(range)) {
    return null;
  }
  return `${range.min ?? ''}${RANGE_SEPARATOR}${range.max ?? ''}`;
}

/** True when two ranges denote the same filter (used to toggle bucket selection). */
export function countRangesEqual(a: CountRange | null | undefined, b: CountRange | null | undefined): boolean {
  const left = isRangeActive(a) ? a : null;
  const right = isRangeActive(b) ? b : null;
  if (left === null || right === null) {
    return left === right;
  }
  return left.min === right.min && left.max === right.max;
}

function parseBound(raw: string): number | null | 'invalid' {
  if (raw.length === 0) {
    return null;
  }
  if (!NON_NEGATIVE_INT.test(raw)) {
    return 'invalid';
  }
  return Number.parseInt(raw, 10);
}
