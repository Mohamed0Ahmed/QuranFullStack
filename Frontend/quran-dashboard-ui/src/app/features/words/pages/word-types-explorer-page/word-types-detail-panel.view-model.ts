import { WordTypeDetailFrame } from '../../../../core/navigation/detail-overlay/detail-overlay.models';
import {
  AyahMatchDto,
  MissingSurahItemDto,
  PagedResultDto as SharedPagedResultDto,
  UniqueWordSurahItemDto,
} from '../../models/unique-words.models';
import {
  WordTypeGroupedMemberWordDto,
  WordTypesDetailState,
} from '../../models/word-types-detail.models';
import { WORD_TYPES_DETAIL_PAGE_SIZE } from '../../models/word-types.models';
import { mapWordTypeAyahMatchToShared } from '../../utils/word-type-ayah-match.mapper';
import { LinkingSourceDescriptor } from '../../../linking/models/linking-source.models';

export interface WordTypeDetailSummaryView {
  readonly label: string;
  readonly occurrences: number;
  readonly ayahs: number;
  readonly surahs: number;
}

export const EMPTY_WORD_TYPE_AYAHS_PAGE: SharedPagedResultDto<AyahMatchDto> = {
  page: 1,
  pageSize: WORD_TYPES_DETAIL_PAGE_SIZE,
  totalCount: 0,
  items: [],
};

export const EMPTY_WORD_TYPE_MEMBER_WORDS_PAGE: SharedPagedResultDto<WordTypeGroupedMemberWordDto> = {
  page: 1,
  pageSize: WORD_TYPES_DETAIL_PAGE_SIZE,
  totalCount: 0,
  items: [],
};

export function wordTypeDetailSummaryView(panel: WordTypesDetailState): WordTypeDetailSummaryView | null {
  const summary = panel.summary ?? panel.groupedSummary;
  return summary
    ? {
        label: summary.displayText,
        occurrences: summary.occurrencesCount,
        ayahs: summary.ayahsCount,
        surahs: summary.surahsCount,
      }
    : null;
}

export function wordTypeLinkingSource(panel: WordTypesDetailState): LinkingSourceDescriptor | null {
  const selection = panel.selection;
  const summary = wordTypeDetailSummaryView(panel);
  if (selection === null || summary === null) {
    return null;
  }
  const scope = { ...selection.scope };
  switch (selection.kind) {
    case 'word':
      return {
        kind: 'word-type',
        selection: { kind: 'word', ...selection.identity, scope },
        label: summary.label,
      };
    case 'root':
      return { kind: 'word-type', selection: { kind: 'root', rootId: selection.rootId, scope }, label: summary.label };
    case 'stem':
      return { kind: 'word-type', selection: { kind: 'stem', stemId: selection.stemId, scope }, label: summary.label };
    case 'lemma':
      return { kind: 'word-type', selection: { kind: 'lemma', lemmaId: selection.lemmaId, scope }, label: summary.label };
  }
}

export function wordTypeMemberWordsPageView(
  panel: WordTypesDetailState,
): SharedPagedResultDto<WordTypeGroupedMemberWordDto> {
  return panel.words ?? EMPTY_WORD_TYPE_MEMBER_WORDS_PAGE;
}

export function wordTypeAyahsPageView(panel: WordTypesDetailState): SharedPagedResultDto<AyahMatchDto> {
  const page = panel.ayahs;
  return page
    ? { ...page, items: page.items.map(mapWordTypeAyahMatchToShared).filter(isAyahMatch) }
    : EMPTY_WORD_TYPE_AYAHS_PAGE;
}

function isAyahMatch(value: AyahMatchDto | null): value is AyahMatchDto {
  return value !== null;
}

export function wordTypeAyahParentFrame(panel: WordTypesDetailState): WordTypeDetailFrame | null {
  if (panel.selection === null || panel.selection.kind !== 'word') {
    return null;
  }

  const identity = panel.selection.identity;
  return {
    kind: 'wordType',
    tashkeelWordId: identity.tashkeelWordId,
    contextCode: identity.contextCode,
    case: identity.case,
    tense: identity.tense,
    voice: identity.voice,
    view: panel.view,
    detailPage: panel.detailPage,
  };
}

export function wordTypeMentionedSurahViews(panel: WordTypesDetailState): UniqueWordSurahItemDto[] {
  return (panel.surahs?.surahs ?? []).map((surah) => ({
    surahNumber: surah.surahNumber,
    nameArabic: surah.nameArabic,
    occurrencesInSurah: surah.occurrencesCount,
  }));
}

export function wordTypeMissingSurahViews(panel: WordTypesDetailState): MissingSurahItemDto[] {
  return (panel.surahs?.missingSurahs ?? []).map((surah) => ({
    surahNumber: surah.surahNumber,
    nameArabic: surah.nameArabic,
  }));
}
