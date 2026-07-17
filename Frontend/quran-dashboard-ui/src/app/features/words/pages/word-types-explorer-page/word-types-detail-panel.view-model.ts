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

/**
 * Derives everything the Word Types details panel renders from the detail facade's panel
 * state. The page component only wires these into computed signals; the shaping rules
 * (which summary wins, which page is shown while loading, what an ayah click promotes)
 * live here.
 */

/** The measure header a word summary and a grouped summary both reduce to. */
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

/**
 * A word summary and a grouped summary share the same measure shape; the panel renders
 * whichever one the active selection produced.
 */
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

/**
 * This panel's own typed frame (Feature 029 B7): an ayah click promotes it over the
 * Mushaf. Only a word-kind selection has a serializable overlay identity — grouped
 * root/stem/lemma selections have no frame grammar, so they yield null (plain page
 * navigation).
 */
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
