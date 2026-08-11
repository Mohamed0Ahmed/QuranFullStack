import { LinkingAyah } from './linking-ayah.models';
import { LinkingManualWordLocationsByVerseKey } from './linking-manual-mushaf.models';
import { LinkingSourceDescriptor } from './linking-source.models';

export interface MergedLinkingSelection {
  ayahs: readonly MergedAyahSelection[];
}

export interface MergedAyahSelection {
  verseKey: string;
  ayah: LinkingAyah;
  sourceKeys: readonly string[];
  words: readonly MergedLinkingWordSelection[];
}

export interface MergedLinkingWordSelection {
  renderPosition: number;
  textUthmani: string;
  sourceKeys: readonly string[];
  canonicalQuranWordId: number | null;
  wordLocation: string | null;
}

export interface LinkingSourceIntent {
  sourceKey: string;
  source: LinkingSourceDescriptor;
  contributionMode: 'automatic' | 'manual-single' | 'manual-independent' | 'manual-grouped';
  units: readonly LinkingIntentUnit[];
}

export interface LinkingIntentUnit {
  ayahs: readonly LinkingIntentAyah[];
}

export interface LinkingIntentAyah {
  verseKey: string;
  wordContributions: readonly LinkingWordContribution[];
}

export type LinkingWordContribution =
  | { identity: 'canonical-quran-word-id'; quranWordId: number }
  | { identity: 'presentation-occurrence'; verseKey: string; renderPosition: number }
  | { identity: 'manual-word-location'; wordLocation: string };

export interface LinkingManualWordContributionSource {
  wordLocationsByVerseKey: LinkingManualWordLocationsByVerseKey;
}
