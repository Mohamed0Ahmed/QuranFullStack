import { PhraseSearchCapabilitiesResponse } from '../../../../core/api/generated/models/phrase-search-capabilities-response';
import {
  DEFAULT_PHRASE_REPETITIONS_URL_STATE,
  PhraseRepetitionSort,
  PhraseRepetitionsUrlState,
  PhraseTextMode,
  isPhraseRepetitionSort,
  isPhraseTextMode,
} from '../models/phrase-repetitions.models';

type PhraseListRouteChanges = Partial<
  Pick<PhraseRepetitionsUrlState, 'length' | 'query' | 'page'>
> & {
  readonly mode?: PhraseTextMode;
  readonly sort?: PhraseRepetitionSort;
};

export function updatePhraseListRoute(
  current: PhraseRepetitionsUrlState,
  build: string | null,
  changes: PhraseListRouteChanges,
): PhraseRepetitionsUrlState {
  return {
    ...current,
    ...changes,
    build,
    page: changes.page ?? 1,
    phrase: null,
    occPage: 1,
  };
}

export function selectPhraseRoute(
  current: PhraseRepetitionsUrlState,
  build: string | null,
  phrase: number,
): PhraseRepetitionsUrlState {
  return { ...current, build, phrase, occPage: 1 };
}

export function clearPhraseRoute(
  current: PhraseRepetitionsUrlState,
): PhraseRepetitionsUrlState {
  return { ...current, phrase: null, occPage: 1 };
}

export function updatePhraseOccurrencePageRoute(
  current: PhraseRepetitionsUrlState,
  occPage: number,
): PhraseRepetitionsUrlState {
  return { ...current, occPage };
}

export function defaultPhraseRepetitionsRoute(
  capabilities: PhraseSearchCapabilitiesResponse | null,
): PhraseRepetitionsUrlState {
  return {
    build: capabilities?.activeBuildId ?? null,
    mode: isPhraseTextMode(capabilities?.defaultMode)
      ? capabilities.defaultMode
      : DEFAULT_PHRASE_REPETITIONS_URL_STATE.mode,
    length:
      capabilities?.defaultRepetitionLength ?? DEFAULT_PHRASE_REPETITIONS_URL_STATE.length,
    query: '',
    sort: isPhraseRepetitionSort(capabilities?.defaultRepetitionSort)
      ? capabilities.defaultRepetitionSort
      : DEFAULT_PHRASE_REPETITIONS_URL_STATE.sort,
    page: 1,
    phrase: null,
    occPage: 1,
  };
}
