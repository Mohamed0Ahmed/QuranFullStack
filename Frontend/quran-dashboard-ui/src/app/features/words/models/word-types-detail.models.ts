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

export interface WordTypeGroupedMemberWordDto extends Omit<
  WordTypeGroupedMemberWordWireDto,
  'case' | 'tense' | 'voice'
> {
  case: WordTypeCase | null;
  tense: WordTypeTense | null;
  voice: WordTypeVoice | null;
}

export type WordTypeDetailSelection =
  | { kind: 'word'; identity: WordTypeRowIdentity }
  | { kind: 'root'; rootId: number; scope: WordTypeDetailScope }
  | { kind: 'stem'; stemId: number; scope: WordTypeDetailScope }
  | { kind: 'lemma'; lemmaId: number; scope: WordTypeDetailScope };

export type WordTypeDetailSelectionKind = WordTypeDetailSelection['kind'];

export type WordTypeGroupedDetailSelection = Extract<
  WordTypeDetailSelection,
  { kind: 'root' | 'stem' | 'lemma' }
>;

export type WordTypesDetailSeed =
  | {
      readonly kind: 'word';
      readonly selection: Extract<WordTypeDetailSelection, { kind: 'word' }>;
      readonly summary: WordTypeSummaryDto;
    }
  | {
      readonly kind: 'grouped';
      readonly selection: WordTypeGroupedDetailSelection;
      readonly summary: WordTypeGroupedSummaryDto;
    };

export interface WordTypesDetailTarget {
  readonly selection: WordTypeDetailSelection;
  readonly view: WordTypeDetailView;
  readonly detailPage: number;
  readonly seed?: WordTypesDetailSeed;
}

export interface WordTypesDetailState {
  readonly status: WordTypesLoadStatus;
  readonly selection: WordTypeDetailSelection | null;
  readonly view: WordTypeDetailView;
  readonly detailPage: number;
  readonly summary: WordTypeSummaryDto | null;
  readonly groupedSummary: WordTypeGroupedSummaryDto | null;
  readonly words: PagedResultDto<WordTypeGroupedMemberWordDto> | null;
  readonly ayahs: PagedResultDto<WordTypeAyahMatchDto> | null;
  readonly surahs: WordTypeSurahsResponseDto | null;
  readonly errorMessage: string;
}
