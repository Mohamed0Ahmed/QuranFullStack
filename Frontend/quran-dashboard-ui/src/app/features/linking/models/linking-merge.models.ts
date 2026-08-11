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
}

export interface LinkingSourceIntent {
  sourceKey: string;
  source: LinkingSourceDescriptor;
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
