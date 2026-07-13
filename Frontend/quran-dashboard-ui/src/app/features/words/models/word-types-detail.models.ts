import type {
  WordTypeGroupedMemberWordDto,
  WordTypeGroupedSummaryDto,
} from '../data-access/word-types.api';
import type {
  PagedResultDto,
  WordTypeAyahMatchDto,
  WordTypeCase,
  WordTypeDetailView,
  WordTypeMainType,
  WordTypeRowIdentity,
  WordTypeSummaryDto,
  WordTypeSurahsResponseDto,
  WordTypeTense,
  WordTypeVoice,
  WordTypesLoadStatus,
} from './word-types.models';

export interface WordTypeDetailScope {
  type: WordTypeMainType;
  childCode: string | null;
  case: WordTypeCase;
  tense: WordTypeTense;
  voice: WordTypeVoice;
}

export type WordTypeDetailSelection =
  | { kind: 'word'; identity: WordTypeRowIdentity }
  | { kind: 'root'; rootId: number; scope: WordTypeDetailScope }
  | { kind: 'stem'; stemId: number; scope: WordTypeDetailScope }
  | { kind: 'lemma'; lemmaId: number; scope: WordTypeDetailScope };

export type WordTypeDetailSelectionKind = WordTypeDetailSelection['kind'];

// The grouped selections that carry a scoped numeric dimension (everything but the word selection).
export type WordTypeGroupedDetailSelection = Extract<WordTypeDetailSelection, { kind: 'root' | 'stem' | 'lemma' }>;

export interface WordTypesDetailState {
  status: WordTypesLoadStatus;
  // Kind-aware active selection and its discriminant. `selectedRow` stays the word-only identity used
  // for row focus restoration; grouped selections carry their numeric dimension in `selection`/`kind`.
  selection: WordTypeDetailSelection | null;
  kind: WordTypeDetailSelectionKind;
  selectedRow: WordTypeRowIdentity | null;
  view: WordTypeDetailView;
  detailPage: number;
  location: string | null;
  // Word summaries populate `summary`; grouped summaries populate `groupedSummary`. Exactly one is
  // non-null for an active selection.
  summary: WordTypeSummaryDto | null;
  groupedSummary: WordTypeGroupedSummaryDto | null;
  words: PagedResultDto<WordTypeGroupedMemberWordDto> | null;
  ayahs: PagedResultDto<WordTypeAyahMatchDto> | null;
  surahs: WordTypeSurahsResponseDto | null;
  errorMessage: string;
}
