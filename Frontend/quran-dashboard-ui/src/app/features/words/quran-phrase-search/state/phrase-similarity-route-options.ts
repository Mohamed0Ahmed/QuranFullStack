import { PhraseSearchCapabilitiesResponse } from '../../../../core/api/generated/models/phrase-search-capabilities-response';
import { PhraseTextMode } from '../models/phrase-repetitions.models';
import { PhraseSimilarityUrlState } from '../models/phrase-similarity.models';

export function supportedGlobalSimilarityOptions(
  capabilities: PhraseSearchCapabilitiesResponse | null,
  mode: PhraseTextMode,
  currentLength: number,
  currentMinimum: number,
): { readonly length: number; readonly minimum: number } {
  const lengths =
    capabilities?.modes
      .find((item) => item.mode === mode)
      ?.supportedLengths.filter((length) => length >= 4) ?? [];
  const thresholds = capabilities?.similarityThresholds ?? [50];
  return {
    length: lengths.includes(currentLength) ? currentLength : (lengths[0] ?? 4),
    minimum: thresholds.includes(currentMinimum) ? currentMinimum : (thresholds[0] ?? 50),
  };
}

export function supportsSimilarityRoute(
  capabilities: PhraseSearchCapabilitiesResponse | null,
  route: PhraseSimilarityUrlState,
): boolean {
  const mode = capabilities?.modes.find((item) => item.mode === route.mode);
  if (!mode?.supportedLengths.includes(route.length) || route.min < 50 || route.min > 100) {
    return false;
  }
  return route.source === 'manual'
    ? route.length >= 2
    : route.length >= 4 && (capabilities?.similarityThresholds.includes(route.min) ?? false);
}
