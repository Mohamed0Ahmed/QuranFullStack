export type LinkingSourceClassification = 'NEW_SOURCE' | 'UNCHANGED' | 'UPDATE' | 'INVALID';

export type LinkingAyahClassification =
  | 'NEW_AYAH'
  | 'OVERLAP_OTHER_SOURCE'
  | 'UNCHANGED'
  | 'UPDATE'
  | 'REMOVE'
  | 'INVALID';

export interface LinkingPreflightCounts {
  requested: number;
  new: number;
  overlapping: number;
  unchanged: number;
  updated: number;
  removed: number;
  invalid: number;
}

export interface LinkingOverlappingSource {
  sourceIdentity: string;
  label: string;
  sourceKind: string;
}

export interface LinkingWordChanges {
  added: readonly number[];
  removed: readonly number[];
  unchanged: readonly number[];
}

export interface LinkingDescriptionChanges {
  added: readonly string[];
  removed: readonly string[];
  changed: readonly string[];
  unchanged: readonly string[];
}

export interface LinkingAyahPreflight {
  ayahId: number;
  verseKey: string;
  surahNumber: number;
  ayahNumber: number;
  classification: LinkingAyahClassification;
  overlappingSources: readonly LinkingOverlappingSource[];
  wordChanges: LinkingWordChanges;
  descriptionChanges: LinkingDescriptionChanges;
  invalidReason: string | null;
}

export interface LinkingSourcePreflight {
  sourceIdentity: string;
  label: string;
  sourceKind: string;
  contributionMode: string;
  classification: LinkingSourceClassification;
  automaticWordMatchesEnabled: boolean | null;
  existingContributionId: number | null;
  existingContributionVersion: number | null;
  counts: LinkingPreflightCounts;
  ayahs: readonly LinkingAyahPreflight[];
}

export interface LinkingPreflightResult {
  doorId: number;
  doorName: string;
  isNoOp: boolean;
  isBlocked: boolean;
  preflightToken: string;
  totals: LinkingPreflightCounts;
  sources: readonly LinkingSourcePreflight[];
}
