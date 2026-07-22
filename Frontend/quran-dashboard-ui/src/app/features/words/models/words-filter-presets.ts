export interface CountRange {
  readonly min: number | null;
  readonly max: number | null;
}

export type RangeFamily = 'occurrences' | 'ayahsSurahs' | 'subCount';

export const RANGE_FAMILY_THRESHOLDS: Record<RangeFamily, number> = {
  occurrences: 100,
  ayahsSurahs: 100,
  subCount: 10,
};

export const SURAHS_RANGE_THRESHOLD = 50;

export type RangeChipKind = 'gt' | 'lt';

export interface RangeChip {
  readonly kind: RangeChipKind;
  readonly threshold: number;
  readonly min: number | null;
  readonly max: number | null;
}

export function resolveRangeThreshold(family: RangeFamily, override?: number | null): number {
  return override ?? RANGE_FAMILY_THRESHOLDS[family];
}

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
