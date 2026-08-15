import { DoorLinkAyahDto } from '../../../core/api/generated/models/door-link-ayah-dto';
import { DoorLinkRecordSummaryDto } from '../../../core/api/generated/models/door-link-record-summary-dto';
import { DoorLinkSnapshotDto } from '../../../core/api/generated/models/door-link-snapshot-dto';
import { DoorLinkSnapshotRecordDto } from '../../../core/api/generated/models/door-link-snapshot-record-dto';
import { AbwabDoorLinkRecordView } from '../models/abwab-door-links.models';

export function mapDoorLinkSnapshot(snapshot: DoorLinkSnapshotDto): readonly AbwabDoorLinkRecordView[] {
  const ayahsById = new Map(snapshot.ayahs.map((ayah) => [ayah.ayahId, ayah]));
  const unitIds = new Set<number>();
  if (ayahsById.size !== snapshot.ayahs.length) {
    throw new Error('Invalid door-link snapshot ayah catalog.');
  }
  return snapshot.records.map((record) => {
    if (unitIds.has(record.unitId)) {
      throw new Error('Invalid duplicate door-link record.');
    }
    unitIds.add(record.unitId);
    const ayahs = record.ayahs.map((reference) => {
      const ayah = ayahsById.get(reference.ayahId);
      if (ayah === undefined) {
        throw new Error('Invalid door-link snapshot ayah reference.');
      }
      return {
        ...ayah,
        selectedWordIds: [...reference.selectedWordIds],
        descriptions: [...reference.descriptions],
        words: [...ayah.words],
      } satisfies DoorLinkAyahDto;
    });
    return { summary: toSummary(record, ayahs), ayahs };
  });
}

function toSummary(
  record: DoorLinkSnapshotRecordDto,
  ayahs: readonly DoorLinkAyahDto[],
): DoorLinkRecordSummaryDto {
  return {
    unitId: record.unitId,
    isGrouped: record.isGrouped,
    ayahCount: ayahs.length,
    selectedWordCount: ayahs.reduce((count, ayah) => count + ayah.selectedWordIds.length, 0),
    descriptionCount: ayahs.reduce((count, ayah) => count + ayah.descriptions.length, 0),
    firstVerseKey: ayahs[0]?.verseKey ?? '',
    lastVerseKey: ayahs.at(-1)?.verseKey ?? '',
  };
}
