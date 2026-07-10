import { MutashabihatOccurrenceDto } from '../../models/mushaf.models';

export function mutashabihatOccurrenceKey(occurrence: MutashabihatOccurrenceDto): string {
  return `${occurrence.verseKey}:${occurrence.wordFrom}`;
}

export function buildCollapsedOccurrencePreview(
  occurrences: MutashabihatOccurrenceDto[],
  previewCount: number,
): MutashabihatOccurrenceDto[] {
  if (occurrences.length <= previewCount) {
    return occurrences;
  }

  const preview = occurrences.slice(0, previewCount);
  const previewKeys = new Set(preview.map(mutashabihatOccurrenceKey));

  const pinnedSelected = occurrences.filter(
    (occurrence) => occurrence.isSelectedAyah && !previewKeys.has(mutashabihatOccurrenceKey(occurrence)),
  );

  if (pinnedSelected.length === 0) {
    return preview;
  }

  return [...preview, ...pinnedSelected];
}
