import {
  LinkingManualLinkShape,
  LinkingManualWordIdsByVerseKey,
  isCanonicalQuranWordId,
} from '../models/linking-manual-mushaf.models';
import { isLinkingSourceDescriptor, isVerseKey } from '../models/linking-source.models';
import {
  LinkingSelection,
  LinkingSourceConfiguration,
  LinkingWorkspaceItem,
  LinkingWorkspacePersistedEnvelope,
  LinkingWorkspacePersistedItem,
} from '../models/linking-workspace.models';
import { linkingSourceKey } from '../utils/linking-source-key';
import { orderedUniqueLinkingVerseKeys } from '../utils/linking-verse-order';

const WORKSPACE_VERSION = 3;
const MAX_WORKSPACE_ITEMS = 100;
const MAX_SELECTION_KEYS = 10_000;
const MAX_WORD_IDS_PER_AYAH = 500;
const MAX_STRING_LENGTH = 1_000;
const MAX_RESULT_COUNT = 1_000_000;

export function encodeLinkingWorkspace(
  actorSub: string,
  revision: number,
  items: readonly LinkingWorkspaceItem[],
): string {
  const envelope: LinkingWorkspacePersistedEnvelope = {
    version: WORKSPACE_VERSION,
    actorSub,
    revision,
    items: items.map(toPersistedItem),
  };
  return JSON.stringify(envelope);
}

export function decodeLinkingWorkspace(actorSub: string, raw: string): {
  items: readonly LinkingWorkspaceItem[];
  invalidPayload: boolean;
} {
  const envelope = parseEnvelope(actorSub, raw);
  if (envelope === null) {
    return { items: [], invalidPayload: true };
  }

  const sourceKeys = new Set<string>();
  const items: LinkingWorkspaceItem[] = [];
  for (const candidate of envelope.items.slice(0, MAX_WORKSPACE_ITEMS)) {
    const item = decodeItem(candidate);
    if (item === null || sourceKeys.has(item.sourceKey)) {
      continue;
    }
    sourceKeys.add(item.sourceKey);
    items.push(item);
  }

  return { items, invalidPayload: false };
}

function parseEnvelope(actorSub: string, raw: string): LinkingWorkspacePersistedEnvelope | null {
  let value: unknown;
  try {
    value = JSON.parse(raw);
  } catch {
    return null;
  }

  if (
    !isRecord(value) ||
    value['version'] !== WORKSPACE_VERSION ||
    value['actorSub'] !== actorSub ||
    !isBoundedString(value['actorSub']) ||
    !isNonNegativeSafeInteger(value['revision']) ||
    !Array.isArray(value['items'])
  ) {
    return null;
  }

  return {
    version: WORKSPACE_VERSION,
    actorSub: value['actorSub'],
    revision: value['revision'],
    items: value['items'] as readonly LinkingWorkspacePersistedItem[],
  };
}

function decodeItem(value: unknown): LinkingWorkspaceItem | null {
  if (!isRecord(value) || !isLinkingSourceDescriptor(value['source'])) {
    return null;
  }

  const source = normalizeSource(value['source']);
  if (!hasBoundedDescriptorStrings(source)) {
    return null;
  }

  const configuration = decodeConfiguration(source, value['configuration']);
  const lastResolvedCount = decodeCount(value['lastResolvedCount']);
  if (configuration === null || lastResolvedCount === undefined) {
    return null;
  }

  const sourceKey = linkingSourceKey(source);
  return toWorkspaceItem(sourceKey, source, configuration, lastResolvedCount, true);
}

function decodeConfiguration(source: LinkingWorkspaceItem['source'], value: unknown): LinkingSourceConfiguration | null {
  if (!isRecord(value) || !isLinkingSelection(value['ayahInclusion'])) {
    return null;
  }

  if (source.kind === 'manual-mushaf-ayahs') {
    if (
      value['kind'] !== 'manual' ||
      !isManualLinkShape(value['linkShape']) ||
      !isManualWordIdsByVerseKey(value['quranWordIdsByVerseKey'])
    ) {
      return null;
    }
    const ayahInclusion = normalizeSelection(value['ayahInclusion']);
    const quranWordIdsByVerseKey = normalizeWordIds(value['quranWordIdsByVerseKey']);
    const manualVerseKeys = new Set(source.manualAyahs.map((ayah) => ayah.verseKey));
    if (
      ayahInclusion.verseKeys.some((verseKey) => !manualVerseKeys.has(verseKey)) ||
      Object.keys(quranWordIdsByVerseKey).some((verseKey) => !manualVerseKeys.has(verseKey))
    ) {
      return null;
    }
    return {
      kind: 'manual',
      ayahInclusion,
      quranWordIdsByVerseKey,
      linkShape: value['linkShape'],
    };
  }

  if (value['kind'] !== 'automatic' || typeof value['automaticWordMatchesEnabled'] !== 'boolean') {
    return null;
  }
  return {
    kind: 'automatic',
    ayahInclusion: normalizeSelection(value['ayahInclusion']),
    automaticWordMatchesEnabled: value['automaticWordMatchesEnabled'],
  };
}

function toPersistedItem(item: LinkingWorkspaceItem): LinkingWorkspacePersistedItem {
  return {
    sourceKey: item.sourceKey,
    source: item.source,
    configuration: item.configuration,
    lastResolvedCount: item.lastResolvedCount,
  };
}

export function toWorkspaceItem(
  sourceKey: string,
  source: LinkingWorkspaceItem['source'],
  configuration: LinkingSourceConfiguration,
  lastResolvedCount: number | null,
  lastResolvedCountIsStale: boolean,
  configurationRevision = 0,
): LinkingWorkspaceItem {
  return {
    sourceKey,
    source,
    configuration,
    configurationRevision,
    lastResolvedCount,
    lastResolvedCountIsStale,
  };
}

function isLinkingSelection(value: unknown): value is LinkingSelection {
  return (
    isRecord(value) &&
    (value['mode'] === 'all-except' || value['mode'] === 'only') &&
    Array.isArray(value['verseKeys']) &&
    value['verseKeys'].length <= MAX_SELECTION_KEYS &&
    value['verseKeys'].every(isVerseKey)
  );
}

function normalizeSelection(selection: LinkingSelection): LinkingSelection {
  return { mode: selection.mode, verseKeys: orderedUniqueLinkingVerseKeys(selection.verseKeys) };
}

function isManualWordIdsByVerseKey(value: unknown): value is LinkingManualWordIdsByVerseKey {
  if (!isRecord(value) || Object.keys(value).length > MAX_SELECTION_KEYS) {
    return false;
  }
  return Object.entries(value).every(
    ([verseKey, quranWordIds]) =>
      isVerseKey(verseKey) &&
      Array.isArray(quranWordIds) &&
      quranWordIds.length <= MAX_WORD_IDS_PER_AYAH &&
      quranWordIds.every(isCanonicalQuranWordId),
  );
}

function normalizeWordIds(
  quranWordIdsByVerseKey: LinkingManualWordIdsByVerseKey,
): LinkingManualWordIdsByVerseKey {
  return Object.fromEntries(
    Object.entries(quranWordIdsByVerseKey)
      .filter(([verseKey]) => isVerseKey(verseKey))
      .sort(([left], [right]) => left.localeCompare(right))
      .map(([verseKey, quranWordIds]) => [verseKey, [...new Set(quranWordIds)].sort((a, b) => a - b)]),
  );
}

function hasBoundedDescriptorStrings(source: LinkingWorkspaceItem['source']): boolean {
  if (!isBoundedString(source.label)) {
    return false;
  }
  if (source.kind === 'lemma' || source.kind === 'stem') {
    return source.typeCode === null || isBoundedString(source.typeCode);
  }
  if (source.kind === 'manual-mushaf-ayahs') {
    return source.manualAyahs.length <= MAX_SELECTION_KEYS && source.manualAyahs.every(
      (ayah) => ayah.displayHint === null || isBoundedString(ayah.displayHint),
    );
  }
  return true;
}

function decodeCount(value: unknown): number | null | undefined {
  return value === null || isNonNegativeSafeInteger(value) ? value : undefined;
}

function isManualLinkShape(value: unknown): value is LinkingManualLinkShape {
  return value === 'grouped' || value === 'independent';
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null;
}

function isBoundedString(value: unknown): value is string {
  return typeof value === 'string' && value.trim().length > 0 && value.length <= MAX_STRING_LENGTH;
}

function isNonNegativeSafeInteger(value: unknown): value is number {
  return typeof value === 'number' && Number.isSafeInteger(value) && value >= 0 && value <= MAX_RESULT_COUNT;
}

function normalizeSource(source: LinkingWorkspaceItem['source']): LinkingWorkspaceItem['source'] {
  if (source.kind !== 'manual-mushaf-ayahs') {
    return source;
  }
  const firstAyahByVerseKey = new Map<string, (typeof source.manualAyahs)[number]>();
  for (const ayah of source.manualAyahs) {
    if (!firstAyahByVerseKey.has(ayah.verseKey)) {
      firstAyahByVerseKey.set(ayah.verseKey, ayah);
    }
  }
  return {
    ...source,
    manualAyahs: orderedUniqueLinkingVerseKeys([...firstAyahByVerseKey.keys()]).map(
      (verseKey) => firstAyahByVerseKey.get(verseKey)!,
    ),
  };
}
