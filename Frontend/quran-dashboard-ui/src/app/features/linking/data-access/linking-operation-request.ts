import { LinkingOperationUnitBody } from '../../../core/api/generated/models/linking-operation-unit-body';
import { LinkingPreflightSourceBody } from '../../../core/api/generated/models/linking-preflight-source-body';
import { LinkingSourceIntent } from '../models/linking-merge.models';
import { toLinkingSourceDescriptorBody } from '../utils/linking-source-descriptor-body';

export function toPreflightSourceBodies(
  sourceIntents: readonly LinkingSourceIntent[],
): LinkingPreflightSourceBody[] {
  return sourceIntents.map((intent, index) => ({
    automaticWordMatchesEnabled:
      intent.contributionMode === 'automatic' ? intent.automaticWordMatchesEnabled : null,
    contributionMode: toWireContributionMode(intent.contributionMode),
    descriptor: toLinkingSourceDescriptorBody(intent.source),
    orderValue: index + 1,
    units: toUnitBodies(intent),
  }));
}

function toUnitBodies(intent: LinkingSourceIntent): LinkingOperationUnitBody[] {
  const isManual = intent.contributionMode !== 'automatic';
  return intent.units.map((unit) => ({
    ayahs: unit.ayahs.map((ayah) => ({
      ayahId: ayah.ayahId,
      descriptions: [],
      selectedWordIds: isManual
        ? ayah.wordContributions.map((contribution) => contribution.quranWordId)
        : null,
    })),
  }));
}

function toWireContributionMode(contributionMode: LinkingSourceIntent['contributionMode']): string {
  switch (contributionMode) {
    case 'automatic':
      return 'automatic';
    case 'manual-single':
      return 'manual_single';
    case 'manual-independent':
      return 'manual_independent';
    case 'manual-grouped':
      return 'manual_grouped';
  }
}
