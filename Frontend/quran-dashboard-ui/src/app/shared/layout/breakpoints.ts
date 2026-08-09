import contract from './breakpoints.contract.json';

export const QD_BREAKPOINTS = {
  compactMax: contract.compactMax,
  mediumMin: contract.mediumMin,
  mediumMax: contract.mediumMax,
  wideMin: contract.wideMin,
  widePlusMin: contract.widePlusMin,
} as const;

export type QdBand = 'compact' | 'medium' | 'wide';

export const QD_BP_COMPACT_QUERY = `(max-width: ${QD_BREAKPOINTS.compactMax}px)`;
export const QD_BP_MEDIUM_QUERY =
  `(min-width: ${QD_BREAKPOINTS.mediumMin}px) and (max-width: ${QD_BREAKPOINTS.mediumMax}px)`;
export const QD_BP_MEDIUM_MAX_QUERY = `(max-width: ${QD_BREAKPOINTS.mediumMax}px)`;
export const QD_BP_WIDE_QUERY = `(min-width: ${QD_BREAKPOINTS.wideMin}px)`;
export const QD_BP_WIDE_PLUS_QUERY = `(min-width: ${QD_BREAKPOINTS.widePlusMin}px)`;

export function qdBandForWidth(width: number): QdBand {
  if (width <= QD_BREAKPOINTS.compactMax) {
    return 'compact';
  }
  if (width <= QD_BREAKPOINTS.mediumMax) {
    return 'medium';
  }
  return 'wide';
}

export function qdIsWidePlus(width: number): boolean {
  return width >= QD_BREAKPOINTS.widePlusMin;
}

export const QD_BP_PHONE_MAX_QUERY = QD_BP_COMPACT_QUERY;
export const QD_BP_TABLET_MAX_QUERY = QD_BP_MEDIUM_MAX_QUERY;
export const QD_BP_DESKTOP_MIN_QUERY = QD_BP_WIDE_QUERY;
