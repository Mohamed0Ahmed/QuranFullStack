import { LinkingAyah } from '../models/linking-ayah.models';
import { LinkingSourceIntent, LinkingWordContribution } from '../models/linking-merge.models';
import { LinkingOperationMember } from '../models/linking-operation.models';
import { effectiveManualLinkShape } from './manual-link-shape';
import { compareLinkingVerseKeys } from './linking-verse-order';

export function createLinkingSourceIntent(
  member: LinkingOperationMember,
  ayahs: readonly LinkingAyah[],
): LinkingSourceIntent {
  const orderedAyahs = [...ayahs].sort((left, right) => compareLinkingVerseKeys(left.verseKey, right.verseKey));
  const intentAyahs = orderedAyahs.map((ayah) => ({
    verseKey: ayah.verseKey,
    wordContributions: wordContributionsFor(member, ayah),
  }));
  if (member.configuration.kind === 'automatic') {
    return {
      sourceKey: member.sourceKey,
      source: member.source,
      contributionMode: 'automatic',
      units: intentAyahs.map((ayah) => ({ ayahs: [ayah] })),
    };
  }
  const shape = effectiveManualLinkShape(member.configuration.linkShape, intentAyahs.map((ayah) => ayah.verseKey));
  return {
    sourceKey: member.sourceKey,
    source: member.source,
    contributionMode: shape === 'grouped' ? 'manual-grouped' : intentAyahs.length === 1 ? 'manual-single' : 'manual-independent',
    units: shape === 'grouped' ? [{ ayahs: intentAyahs }] : intentAyahs.map((ayah) => ({ ayahs: [ayah] })),
  };
}

function wordContributionsFor(
  member: LinkingOperationMember,
  ayah: LinkingAyah,
): readonly LinkingWordContribution[] {
  return ayah.words
    .filter((word) => !word.isAyahMarker && word.isSourceMatch)
    .map((word) => {
      if (member.configuration.kind === 'manual' && word.wordLocation !== null) {
        return { identity: 'manual-word-location', wordLocation: word.wordLocation } as const;
      }
      if (word.canonicalQuranWordId !== null) {
        return { identity: 'canonical-quran-word-id', quranWordId: word.canonicalQuranWordId } as const;
      }
      return {
        identity: 'presentation-occurrence',
        verseKey: ayah.verseKey,
        renderPosition: word.renderPosition,
      } as const;
    });
}
