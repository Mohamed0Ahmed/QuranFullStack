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
  const descriptionsByAyahId = descriptionBodiesByAyahId(member);
  const intentAyahs = orderedAyahs.map((ayah) => ({
    ayahId: ayah.ayahId,
    verseKey: ayah.verseKey,
    wordContributions: wordContributionsFor(ayah),
    descriptions: descriptionsByAyahId.get(ayah.ayahId) ?? [],
  }));
  if (member.configuration.kind === 'automatic') {
    return {
      sourceKey: member.sourceKey,
      source: member.source,
      contributionMode: 'automatic',
      automaticWordMatchesEnabled: member.configuration.automaticWordMatchesEnabled,
      units: intentAyahs.map((ayah) => ({ ayahs: [ayah] })),
    };
  }
  const shape = effectiveManualLinkShape(member.configuration.linkShape, intentAyahs.map((ayah) => ayah.verseKey));
  return {
    sourceKey: member.sourceKey,
    source: member.source,
    contributionMode: shape === 'grouped' ? 'manual-grouped' : intentAyahs.length === 1 ? 'manual-single' : 'manual-independent',
    automaticWordMatchesEnabled: null,
    units: shape === 'grouped' ? [{ ayahs: intentAyahs }] : intentAyahs.map((ayah) => ({ ayahs: [ayah] })),
  };
}

function descriptionBodiesByAyahId(member: LinkingOperationMember): ReadonlyMap<number, readonly string[]> {
  const byAyahId = new Map<number, string[]>();
  for (const description of [...member.descriptions].sort((left, right) => left.orderValue - right.orderValue)) {
    (byAyahId.get(description.ayahId) ?? byAyahId.set(description.ayahId, []).get(description.ayahId)!).push(
      description.body,
    );
  }
  return byAyahId;
}

function wordContributionsFor(ayah: LinkingAyah): readonly LinkingWordContribution[] {
  return ayah.words
    .filter((word) => !word.isAyahMarker && word.isSourceMatch)
    .map((word) => ({ quranWordId: word.canonicalQuranWordId }));
}
