import type {
  WordTypeGroupedMemberWordDto as WordTypeGroupedMemberWordWireDto,
  WordTypeGroupedSummaryDto as WordTypeGroupedSummaryWireDto,
} from '../../../core/api/generated/models';
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

export type WordTypeGroupedKind = 'root' | 'stem' | 'lemma';

export interface WordTypeGroupedRequestParams extends WordTypeDetailScope {
  kind: WordTypeGroupedKind;
  dimensionId: number;
}

export interface WordTypeGroupedSummaryDto extends Omit<WordTypeGroupedSummaryWireDto, 'kind'> {
  kind: WordTypeGroupedKind;
}

export interface WordTypeGroupedMemberWordDto
  extends Omit<WordTypeGroupedMemberWordWireDto, 'case' | 'tense' | 'voice'> {
  case: WordTypeCase | null;
  tense: WordTypeTense | null;
  voice: WordTypeVoice | null;
}

export type WordTypeDetailSelection =
  | { kind: 'word'; identity: WordTypeRowIdentity; scope: WordTypeDetailScope }
  | { kind: 'root'; rootId: number; scope: WordTypeDetailScope }
  | { kind: 'stem'; stemId: number; scope: WordTypeDetailScope }
  | { kind: 'lemma'; lemmaId: number; scope: WordTypeDetailScope };

export type WordTypeDetailSelectionKind = WordTypeDetailSelection['kind'];

export type WordTypeGroupedDetailSelection = Extract<WordTypeDetailSelection, { kind: 'root' | 'stem' | 'lemma' }>;

export interface WordTypesDetailState {
  status: WordTypesLoadStatus;
  selection: WordTypeDetailSelection | null;
  kind: WordTypeDetailSelectionKind;
  selectedRow: WordTypeRowIdentity | null;
  view: WordTypeDetailView;
  detailPage: number;
  location: string | null;
  summary: WordTypeSummaryDto | null;
  groupedSummary: WordTypeGroupedSummaryDto | null;
  words: PagedResultDto<WordTypeGroupedMemberWordDto> | null;
  ayahs: PagedResultDto<WordTypeAyahMatchDto> | null;
  surahs: WordTypeSurahsResponseDto | null;
  errorMessage: string;
}
