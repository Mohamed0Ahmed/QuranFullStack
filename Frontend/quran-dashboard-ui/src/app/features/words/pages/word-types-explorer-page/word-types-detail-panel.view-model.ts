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

export function wordTypeMemberWordsPageView(
  panel: WordTypesDetailState,
): SharedPagedResultDto<WordTypeGroupedMemberWordDto> {
  return panel.words ?? EMPTY_WORD_TYPE_MEMBER_WORDS_PAGE;
}

export function wordTypeAyahsPageView(panel: WordTypesDetailState): SharedPagedResultDto<AyahMatchDto> {
  const page = panel.ayahs;
  return page ? { ...page, items: page.items.map(mapWordTypeAyahMatchToShared) } : EMPTY_WORD_TYPE_AYAHS_PAGE;
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
