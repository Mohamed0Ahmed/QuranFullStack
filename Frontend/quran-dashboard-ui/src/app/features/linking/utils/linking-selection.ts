import { LinkingSelection } from '../models/linking-workspace.models';
import { isVerseKey } from '../models/linking-source.models';

export const DEFAULT_LINKING_SELECTION: LinkingSelection = { mode: 'all-except', verseKeys: [] };

export function toggleLinkingSelection(
  selection: LinkingSelection,
  verseKey: string,
  universe: readonly string[],
): LinkingSelection {
  if (!isVerseKey(verseKey) || !new Set(universe).has(verseKey)) {
    return selection;
  }

  const overrides = new Set(selection.verseKeys);
  const isSelected = isVerseSelected(selection, verseKey);

  if (selection.mode === 'all-except') {
    if (isSelected) {
      overrides.add(verseKey);
    } else {
      overrides.delete(verseKey);
    }
  } else if (isSelected) {
    overrides.delete(verseKey);
  } else {
    overrides.add(verseKey);
  }

  return { mode: selection.mode, verseKeys: orderedVerseKeys(overrides, universe) };
}

export function selectAllLinkingAyahs(): LinkingSelection {
  return DEFAULT_LINKING_SELECTION;
}

export function clearLinkingAyahs(): LinkingSelection {
  return { mode: 'only', verseKeys: [] };
}

export function reconcileLinkingSelection(
  selection: LinkingSelection,
  universe: readonly string[],
): LinkingSelection {
  const validUniverse = uniqueVerseKeys(universe);
  const overrides = new Set(selection.verseKeys.filter((verseKey) => validUniverse.has(verseKey)));
  return { mode: selection.mode, verseKeys: orderedVerseKeys(overrides, universe) };
}

export function selectedLinkingVerseKeys(
  selection: LinkingSelection,
  universe: readonly string[],
): readonly string[] {
  const reconciled = reconcileLinkingSelection(selection, universe);
  const overrides = new Set(reconciled.verseKeys);

  return uniqueVerseKeyList(universe).filter((verseKey) =>
    reconciled.mode === 'all-except' ? !overrides.has(verseKey) : overrides.has(verseKey),
  );
}

export function selectedLinkingAyahCount(
  selection: LinkingSelection,
  universe: readonly string[],
): number {
  return selectedLinkingVerseKeys(selection, universe).length;
}

export function isVerseSelected(selection: LinkingSelection, verseKey: string): boolean {
  const overrides = new Set(selection.verseKeys);
  return selection.mode === 'all-except' ? !overrides.has(verseKey) : overrides.has(verseKey);
}

function orderedVerseKeys(overrides: ReadonlySet<string>, universe: readonly string[]): readonly string[] {
  return uniqueVerseKeyList(universe).filter((verseKey) => overrides.has(verseKey));
}

function uniqueVerseKeys(verseKeys: readonly string[]): ReadonlySet<string> {
  return new Set(uniqueVerseKeyList(verseKeys));
}

function uniqueVerseKeyList(verseKeys: readonly string[]): readonly string[] {
  const seen = new Set<string>();
  return verseKeys.filter((verseKey) => {
    if (!isVerseKey(verseKey) || seen.has(verseKey)) {
      return false;
    }

    seen.add(verseKey);
    return true;
  });
}
