import { LinkingAyah } from '../models/linking-ayah.models';
import { LinkingSourceConfiguration } from '../models/linking-workspace.models';

export function applyLinkingSourceConfiguration(
  configuration: LinkingSourceConfiguration,
  ayah: LinkingAyah,
): LinkingAyah {
  const selectedWordIds =
    configuration.kind === 'manual'
      ? new Set(configuration.quranWordIdsByVerseKey[ayah.verseKey] ?? [])
      : null;

  return {
    ...ayah,
    words: ayah.words.map((word) => ({
      ...word,
      isSourceMatch:
        !word.isAyahMarker &&
        (configuration.kind === 'automatic'
          ? configuration.automaticWordMatchesEnabled && word.isSourceMatch
          : selectedWordIds?.has(word.canonicalQuranWordId) === true),
    })),
  };
}
