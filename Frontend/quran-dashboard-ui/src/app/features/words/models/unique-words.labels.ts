import { explorerSortOptions } from './explorer-sort';
import {
  UNIQUE_WORD_SORT_COLUMN_LIST,
  UniqueWordKind,
  WordDrilldownView,
} from './unique-words.models';
import { lemmasRoutePath, rootsRoutePath, stemsRoutePath, wordTypesRoutePath } from '../../../core/navigation/route-paths';
import {
  ROW_NUMBER_HEADER,
  WORDS_SHARED_HEADERS,
  WORDS_SHARED_LIST_HEADERS,
  WORDS_SHARED_SURAH_VIEWS,
  WORDS_SHARED_WORD_VIEWS,
} from './words-shared.labels';

export interface WordSectionCardLabel {
  labelAr: string;
}

export const ACTIVE_HUB_SECTION: WordSectionCardLabel = {
  labelAr: 'الكلمات الفريدة',
};

export interface ActiveHubSection extends WordSectionCardLabel {
  route: string;
}

export const ADDITIONAL_ACTIVE_HUB_SECTIONS: readonly ActiveHubSection[] = [
  { labelAr: 'الجذور', route: rootsRoutePath() },
  { labelAr: 'الصيغ المعجمية', route: lemmasRoutePath() },
  { labelAr: 'الأصول الصرفية', route: stemsRoutePath() },
  { labelAr: 'أنواع الكلمات', route: wordTypesRoutePath() },
];

export const WORDS_HUB_SECTIONS_LABEL = 'أقسام الكلمات';

export const UNIQUE_WORD_KIND_LABELS: Record<UniqueWordKind, string> = {
  tashkeel: WORDS_SHARED_WORD_VIEWS.tashkeel,
  simple: WORDS_SHARED_WORD_VIEWS.simple,
};

export const UNIQUE_WORDS_MUSHAF_ORDER_LABEL = 'ترتيب المصحف';

export const UNIQUE_WORD_SORT_OPTIONS = explorerSortOptions(
  UNIQUE_WORD_SORT_COLUMN_LIST,
  UNIQUE_WORDS_MUSHAF_ORDER_LABEL,
);

export const WORD_DRILLDOWN_VIEW_LABELS: Record<WordDrilldownView, string> = {
  surahs: WORDS_SHARED_HEADERS.surahs,
  missing: WORDS_SHARED_SURAH_VIEWS.missing,
  ayahs: WORDS_SHARED_HEADERS.ayahs,
};

export const UNIQUE_WORD_TABLE_LABEL = 'جدول الكلمات الفريدة';
export const UNIQUE_WORD_TABLE_BODY_LABEL = 'قائمة الكلمات الفريدة';
export const UNIQUE_WORD_TABS_LABEL = 'أنماط الكلمات الفريدة';
export const UNIQUE_WORD_LIST_PAGINATION_LABEL = 'تصفّح الكلمات';
export const UNIQUE_WORD_PANEL_SURFACE_LABEL = 'التفاصيل';

export const UNIQUE_WORD_WORD_HEADER = WORDS_SHARED_LIST_HEADERS.word;
export const UNIQUE_WORD_TYPE_HEADER = 'نوع الكلمة';
export const UNIQUE_WORD_ROOT_HEADER = WORDS_SHARED_HEADERS.root;
export const UNIQUE_WORD_NULL_PLACEHOLDER = '—';

export { ROW_NUMBER_HEADER };
export const OCCURRENCES_CHIP_LABEL = WORDS_SHARED_HEADERS.occurrences;
export const SURAH_OCCURRENCES_COUNT_HEADER = WORDS_SHARED_LIST_HEADERS.occurrences;
export const SURAH_NAME_HEADER = WORDS_SHARED_LIST_HEADERS.surahName;
export const AYAH_REF_LABEL = 'آية';
export const MUSHAF_PAGE_REF_LABEL = 'ص';

export const UNIQUE_WORDS_RESULT_COUNT_LABEL = 'عدد الكلمات';

export const UNIQUE_WORDS_PRIMARY_TYPE_FILTER_LABEL = 'النوع الأساسي';
export const UNIQUE_WORDS_PRIMARY_TYPE_FILTER_PLACEHOLDER = 'اختر نوعًا…';
export const UNIQUE_WORDS_PRIMARY_ROOT_FILTER_LABEL = 'الجذر الأساسي';
export const UNIQUE_WORDS_PRIMARY_ROOT_FILTER_PLACEHOLDER = 'ابحث عن جذر…';

export const EMPTY_LIST_LABEL = 'لا توجد نتائج';
export const SEARCH_LABEL = 'بحث';
export const SEARCH_PLACEHOLDER = 'ابحث في الكلمات…';
export const SORT_LABEL = 'ترتيب';
export const LOADING_LABEL = 'جارٍ التحميل...';
export const CLOSE_LABEL = 'إغلاق';
export const DRILLDOWN_EMPTY_SURAHS_LABEL = 'لا توجد سور';
export const DRILLDOWN_EMPTY_MISSING_LABEL = 'لا توجد سور مفقودة';
export const DRILLDOWN_EMPTY_AYAHS_LABEL = 'لا توجد آيات';
export const DRILLDOWN_ERROR_LABEL = 'تعذّر تحميل التفاصيل';
export const DRILLDOWN_PANEL_TITLE = 'تفاصيل الكلمة';
export const DRILLDOWN_PANEL_EMPTY_LABEL = 'اختر كلمة لعرض تفاصيلها';
export const DRILLDOWN_VIEW_TABLIST_LABEL = 'عرض التفاصيل';

export const OPEN_AYAH_IN_MUSHAF_LABEL = 'فتح الآية في المصحف';

export const RESTORED_WORD_NOT_FOUND_LABEL = 'الكلمة المحددة غير موجودة';

export const RESTORED_WORD_LOAD_ERROR_LABEL =
  'تعذّر تحميل الكلمة المحددة. تحقّق من الاتصال ثم أعد المحاولة.';
