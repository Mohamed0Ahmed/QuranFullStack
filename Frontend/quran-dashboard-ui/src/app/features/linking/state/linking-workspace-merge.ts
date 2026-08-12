import { LinkingWorkspaceConfigurationRequest } from '../data-access/linking-workspace.repository';
import {
  LinkingWorkspaceItem,
  LinkingWorkspaceSnapshot,
} from '../models/linking-workspace.models';

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
        ayahIdByVerseKey,
        configurationRevision: known.configurationRevision + 1,
        lastResolvedCount: item.lastResolvedCount ?? known.lastResolvedCount,
        lastResolvedCountIsStale: known.lastResolvedCountIsStale,
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
): readonly string[] {
  const verseKeyByAyahId = new Map(
    Object.entries(ayahIdByVerseKey).map(([verseKey, ayahId]) => [ayahId, verseKey]),
  );
  return ayahIds
    .map((ayahId) => verseKeyByAyahId.get(ayahId))
    .filter((verseKey): verseKey is string => verseKey !== undefined);
}

export function toAyahIds(
  verseKeys: readonly string[],
  ayahIdByVerseKey: Readonly<Record<string, number>>,
): readonly number[] {
  return verseKeys
    .map((verseKey) => ayahIdByVerseKey[verseKey])
    .filter((ayahId): ayahId is number => ayahId !== undefined);
}

export function toConfigurationRequest(item: LinkingWorkspaceItem): LinkingWorkspaceConfigurationRequest | null {
  if (item.sourceVersion === null) {
    return null;
  }
  const configuration = item.configuration;
  const isManual = configuration.kind === 'manual';
  return {
    sourceVersion: item.sourceVersion,
    label: item.source.label,
    inclusionMode: configuration.ayahInclusion.mode,
    ayahOverrideIds: item.ayahOverrideIds,
    selectedWords: isManual ? toSelectedWords(item) : [],
    automaticWordMatchesEnabled: isManual ? null : configuration.automaticWordMatchesEnabled,
    manualLinkShape: isManual ? configuration.linkShape : null,
    descriptions: item.descriptions,
  };
}

function toSelectedWords(
  item: LinkingWorkspaceItem,
): readonly { ayahId: number; quranWordId: number }[] {
  if (item.configuration.kind !== 'manual') {
    return [];
  }
  const selectedWords: { ayahId: number; quranWordId: number }[] = [];
  for (const [verseKey, quranWordIds] of Object.entries(item.configuration.quranWordIdsByVerseKey)) {
    const ayahId = item.ayahIdByVerseKey[verseKey];
    if (ayahId === undefined) {
      continue;
    }
    for (const quranWordId of quranWordIds) {
      selectedWords.push({ ayahId, quranWordId });
    }
  }
  return selectedWords;
}
