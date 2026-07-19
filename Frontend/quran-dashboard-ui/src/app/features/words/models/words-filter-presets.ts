// Count-range filter presets + URL grammar (Feature 026, US5). Presets are presentation config, not
// labels — the URL always stores the actual range (grammar `min..max`), so tuning thresholds later
// never breaks shared links (research R5/R6). This module is the single source of the grammar shared
// by every explorer's url-sync/api/cache layers.

export interface CountRange {
  readonly min: number | null;
  readonly max: number | null;
}

/** Metric families map to the metric groups from the spec Clarifications (one count scale each). */
export type RangeFamily = 'occurrences' | 'ayahsSurahs' | 'subCount';

/**
 * The "big vs small" chip threshold each family defaults to (Feature 030, N4-a). `ayahsSurahs` carries
 * the high-count 100 that ayahs needs; surahs overrides it per-metric because that metric is hard-capped
 * at 114, where a ">100" chip would match almost nothing.
 */
export const RANGE_FAMILY_THRESHOLDS: Record<RangeFamily, number> = {
  occurrences: 100,
  ayahsSurahs: 100,
  subCount: 10,
};

/**
 * The السور override, shared by every explorer that lists the metric so their chips cannot drift apart.
 * A surah count can never exceed 114, which is why it cannot take its family's 100.
 */
export const SURAHS_RANGE_THRESHOLD = 50;

/** The two shortcut chips a metric offers. The kind is the stable testid suffix and the `@for` track. */
export type RangeChipKind = 'gt' | 'lt';

/** A preset chip: presentation config that resolves to an ordinary range, never to a URL identity. */
export interface RangeChip {
  readonly kind: RangeChipKind;
  readonly threshold: number;
  readonly min: number | null;
  readonly max: number | null;
}

export function resolveRangeThreshold(family: RangeFamily, override?: number | null): number {
  return override ?? RANGE_FAMILY_THRESHOLDS[family];
}

/**
 * Builds the `أكثر من N` / `أقل من N` chips for a metric. Both bounds are strict, so exactly N stays
 * reachable only through the مخصّص panel (decision N4-b).
 */
export function buildRangeChips(family: RangeFamily, override?: number | null): readonly RangeChip[] {
  const threshold = resolveRangeThreshold(family, override);
  return [
    { kind: 'gt', threshold, min: threshold + 1, max: null },
    { kind: 'lt', threshold, min: null, max: threshold - 1 },
  ];
}

const RANGE_SEPARATOR = '..';
const NON_NEGATIVE_INT = /^\d+$/;

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

export function serializeCountRange(range: CountRange | null | undefined): string | null {
  if (!isRangeActive(range)) {
    return null;
  }
  return `${range.min ?? ''}${RANGE_SEPARATOR}${range.max ?? ''}`;
}

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
