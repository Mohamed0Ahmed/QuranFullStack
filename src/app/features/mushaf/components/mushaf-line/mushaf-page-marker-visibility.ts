import { PageMarkerDto } from '../../models/mushaf.models';

/** Temporary presentation filter until division-marker UI is redesigned. */
export const HIDDEN_MUSHAF_PAGE_MARKER_TYPES = new Set<PageMarkerDto['markerType']>([
  'juz',
  'hizb',
  'rub',
  'sajda',
]);

export function isVisibleMushafPageMarker(marker: PageMarkerDto): boolean {
  return !HIDDEN_MUSHAF_PAGE_MARKER_TYPES.has(marker.markerType);
}
