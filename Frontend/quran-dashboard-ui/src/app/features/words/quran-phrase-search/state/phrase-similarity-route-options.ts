import { PhraseSearchCapabilitiesResponse } from '../../../../core/api/generated/models/phrase-search-capabilities-response';
import { PhraseSimilarityUrlState } from '../models/phrase-similarity.models';

export function supportsSimilarityRoute(
  capabilities: PhraseSearchCapabilitiesResponse | null,
  route: PhraseSimilarityUrlState,
): boolean {
  const mode = capabilities?.modes.find((item) => item.mode === route.mode);
  return Boolean(
    mode?.supportedLengths.includes(route.length) &&
    route.length >= 2 &&
    route.min >= 50 &&
    route.min <= 100,
  );
}
