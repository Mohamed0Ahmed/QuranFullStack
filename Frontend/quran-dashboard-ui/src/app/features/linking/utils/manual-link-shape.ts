import { LinkingManualMushafAyahSource, LinkingManualLinkShape } from '../models/linking-manual-mushaf.models';
import { orderedUniqueLinkingVerseKeys } from './linking-verse-order';

export function manualMushafVerseKeys(source: LinkingManualMushafAyahSource): readonly string[] {
  return orderedUniqueLinkingVerseKeys(source.manualAyahs.map((ayah) => ayah.verseKey));
}

export function effectiveManualLinkShape(
  preference: LinkingManualLinkShape,
  includedVerseKeys: readonly string[],
): LinkingManualLinkShape {
  return includedVerseKeys.length > 1 ? preference : 'independent';
}
