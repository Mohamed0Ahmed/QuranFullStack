import { LinkingOperationSourceDraft } from '../../linking/models/linking-operation-draft.models';
import { AbwabDoorLinkCopyRecord } from '../models/abwab-door-links.models';

interface CopySourceGroup {
  readonly records: AbwabDoorLinkCopyRecord[];
  readonly ayahIds: Set<number>;
  readonly isGrouped: boolean;
  readonly linkingDataRevision: number;
}

export function mapAbwabDoorLinkCopyRecords(
  records: readonly AbwabDoorLinkCopyRecord[],
  sourceLabel: string,
): readonly LinkingOperationSourceDraft[] {
  return buildSourceGroups(records).map((group) => toOperationDraft(group, sourceLabel));
}

function buildSourceGroups(records: readonly AbwabDoorLinkCopyRecord[]): readonly CopySourceGroup[] {
  const groups: CopySourceGroup[] = [];
  for (const record of records) {
    const recordAyahIds = new Set(record.ayahs.map((ayah) => ayah.ayahId));
    if (record.isGrouped) {
      groups.push({
        records: [record],
        ayahIds: recordAyahIds,
        isGrouped: true,
        linkingDataRevision: record.linkingDataRevision,
      });
      continue;
    }

    const target = groups.find(
      (group) =>
        !group.isGrouped &&
        group.linkingDataRevision === record.linkingDataRevision &&
        [...recordAyahIds].every((ayahId) => !group.ayahIds.has(ayahId)),
    );
    if (target === undefined) {
      groups.push({
        records: [record],
        ayahIds: recordAyahIds,
        isGrouped: false,
        linkingDataRevision: record.linkingDataRevision,
      });
      continue;
    }
    target.records.push(record);
    recordAyahIds.forEach((ayahId) => target.ayahIds.add(ayahId));
  }
  return groups;
}

function toOperationDraft(group: CopySourceGroup, sourceLabel: string): LinkingOperationSourceDraft {
  const ayahs = group.records.flatMap((record) => record.ayahs);
  const unitIds = group.records.map((record) => record.unitId);
  return {
    sourceKey: `abwab-copy:${group.isGrouped ? 'grouped' : 'independent'}:${unitIds.join('-')}`,
    sourceId: null,
    sourceVersion: null,
    linkingDataRevision: group.linkingDataRevision,
    descriptor: {
      kind: 'manual-mushaf-ayahs',
      label: sourceLabel,
      contextKey: null,
      manualAyahs: ayahs.map((ayah) => ({
        verseKey: ayah.verseKey,
        pageNumber: ayah.pageFrom,
        displayHint: null,
      })),
    },
    label: sourceLabel,
    selection: { mode: 'all-except', ayahIds: [] },
    selectedWordIdsByAyahId: Object.fromEntries(
      ayahs.map((ayah) => [ayah.ayahId, [...ayah.selectedWordIds]]),
    ),
    descriptions: ayahs.flatMap((ayah) =>
      ayah.descriptions.map((body, index) => ({ ayahId: ayah.ayahId, orderValue: index + 1, body })),
    ),
    automaticWordMatchesEnabled: null,
    manualLinkShape: group.isGrouped ? 'grouped' : 'independent',
  };
}
