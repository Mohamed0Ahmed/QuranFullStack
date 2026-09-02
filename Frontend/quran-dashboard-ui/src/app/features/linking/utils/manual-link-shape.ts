import { LinkingManualLinkShape } from '../models/linking-manual-mushaf.models';

export function effectiveManualLinkShape(
  preference: LinkingManualLinkShape,
  includedVerseKeys: readonly string[],
): LinkingManualLinkShape {
  return includedVerseKeys.length > 1 ? preference : 'independent';
}
