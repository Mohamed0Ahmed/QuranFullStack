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

export interface WordTypesDetailState {
  status: WordTypesLoadStatus;
  selectedRow: WordTypeRowIdentity | null;
  view: WordTypeDetailView;
  detailPage: number;
  location: string | null;
  summary: WordTypeSummaryDto | null;
  ayahs: PagedResultDto<WordTypeAyahMatchDto> | null;
  surahs: WordTypeSurahsResponseDto | null;
  errorMessage: string;
}
