import { LinkingOperationSourceDraft } from '../models/linking-operation-draft.models';
import { LinkingWorkspaceItem } from '../models/linking-workspace.models';

export function toLinkingOperationDraft(item: LinkingWorkspaceItem): LinkingOperationSourceDraft {
  if (item.linkingDataRevision === null) {
    throw new Error('مراجعة بيانات الربط غير متاحة للمصدر.');
  }
  return {
    sourceKey: item.sourceKey,
    sourceId: item.sourceId,
    sourceVersion: item.sourceVersion,
    linkingDataRevision: item.linkingDataRevision,
    descriptor: item.source,
    label: item.source.label,
    selection: {
      mode: item.configuration.ayahInclusion.mode,
      ayahIds: item.ayahOverrideIds,
    },
    selectedWordIdsByAyahId: item.selectedWordIdsByAyahId,
    descriptions: [],
    automaticWordMatchesEnabled:
      item.configuration.kind === 'automatic'
        ? item.configuration.automaticWordMatchesEnabled
        : null,
    manualLinkShape:
      item.configuration.kind === 'manual' ? item.configuration.linkShape : null,
  };
}
