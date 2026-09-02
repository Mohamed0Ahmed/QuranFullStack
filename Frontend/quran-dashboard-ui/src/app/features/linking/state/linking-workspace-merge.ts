import {
  LinkingWorkspaceItem,
  LinkingWorkspaceSnapshot,
} from '../models/linking-workspace.models';
import { parseQuranVerseKey, type QuranVerseKey } from '../../../shared/quran/quran-location';

export function mergeWorkspaceSnapshot(
  snapshot: LinkingWorkspaceSnapshot,
  previous: readonly LinkingWorkspaceItem[],
): LinkingWorkspaceSnapshot {
  const previousBySourceKey = new Map(previous.map((item) => [item.sourceKey, item]));
  return {
    workspaceVersion: snapshot.workspaceVersion,
    items: snapshot.items.map((item) => {
      const known = previousBySourceKey.get(item.sourceKey);
      if (known === undefined) {
        return item;
      }
      const ayahIdByVerseKey =
        Object.keys(item.ayahIdByVerseKey).length > 0
          ? item.ayahIdByVerseKey
          : known.ayahIdByVerseKey;
      return {
        ...item,
        selectedWordIdsByAyahId:
          Object.keys(item.selectedWordIdsByAyahId).length > 0
            ? item.selectedWordIdsByAyahId
            : known.selectedWordIdsByAyahId,
        ayahIdByVerseKey,
        configurationRevision: known.configurationRevision + 1,
        linkingDataRevision: known.linkingDataRevision,
        lastResolvedCount: item.lastResolvedCount ?? known.lastResolvedCount,
        configuration: {
          ...item.configuration,
          ayahInclusion: {
            mode: item.configuration.ayahInclusion.mode,
            verseKeys: toVerseKeys(item.ayahOverrideIds, ayahIdByVerseKey),
          },
        },
      };
    }),
  };
}

export function toVerseKeys(
  ayahIds: readonly number[],
  ayahIdByVerseKey: Readonly<Record<string, number>>,
): readonly QuranVerseKey[] {
  const verseKeyByAyahId = new Map(
    Object.entries(ayahIdByVerseKey).flatMap(([verseKey, ayahId]) => {
      const parsed = parseQuranVerseKey(verseKey);
      return parsed ? [[ayahId, parsed.key] as const] : [];
    }),
  );
  return ayahIds
    .map((ayahId) => verseKeyByAyahId.get(ayahId))
    .filter((verseKey): verseKey is QuranVerseKey => verseKey !== undefined);
}

export function toAyahIds(
  verseKeys: readonly QuranVerseKey[],
  ayahIdByVerseKey: Readonly<Record<string, number>>,
): readonly number[] {
  return verseKeys
    .map((verseKey) => ayahIdByVerseKey[verseKey])
    .filter((ayahId): ayahId is number => ayahId !== undefined);
}
