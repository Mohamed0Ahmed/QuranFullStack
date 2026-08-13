import { LinkingAyah } from './linking-ayah.models';
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
  canonicalQuranWordId: number;
}

export interface LinkingSourceIntent {
  sourceKey: string;
  source: LinkingSourceDescriptor;
  contributionMode: 'automatic' | 'manual-single' | 'manual-independent' | 'manual-grouped';
  automaticWordMatchesEnabled: boolean | null;
  units: readonly LinkingIntentUnit[];
}

export interface LinkingIntentUnit {
  ayahs: readonly LinkingIntentAyah[];
}

export interface LinkingIntentAyah {
  ayahId: number;
  verseKey: string;
  wordContributions: readonly LinkingWordContribution[];
}

export interface LinkingWordContribution {
  quranWordId: number;
}
