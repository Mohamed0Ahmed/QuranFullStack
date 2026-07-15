import { StemView, StemWordView, StemSurahView } from './stems.models';
import {
  ROW_NUMBER_HEADER,
  WORDS_SHARED_COUNT_COLUMNS,
  WORDS_SHARED_HEADERS,
  WORDS_SHARED_LIST_HEADERS,
  WORDS_SHARED_PANEL_TABS,
  WORDS_SHARED_SURAH_VIEWS,
  WORDS_SHARED_WORD_VIEWS,
} from './words-shared.labels';

export const STEMS_PAGE_TITLE = 'الأصول الصرفية';
// Headline result-count stat (Feature 026, US4): label-prefix "عدد الـ…: N".
export const STEMS_RESULT_COUNT_LABEL = 'عدد الأصول الصرفية';
// Association filters (Feature 026, US7): primary (dominant) root + lemma, labeled primary-not-sole.
export const STEMS_PRIMARY_ROOT_FILTER_LABEL = 'الجذر الأساسي';
export const STEMS_PRIMARY_ROOT_FILTER_PLACEHOLDER = 'ابحث عن جذر…';
export const STEMS_PRIMARY_LEMMA_FILTER_LABEL = 'الصيغة المعجمية الأساسية';
export const STEMS_PRIMARY_LEMMA_FILTER_PLACEHOLDER = 'ابحث عن صيغة معجمية…';
export const STEMS_SEARCH_LABEL = 'بحث في الأصول الصرفية';
export const STEMS_SEARCH_PLACEHOLDER = 'اكتب أصلًا صرفيًا…';

export const STEMS_TABLE_LABEL = 'جدول الأصول الصرفية';
export const STEMS_TABLE_BODY_LABEL = 'قائمة الأصول الصرفية';
export const STEMS_LIST_PAGINATION_LABEL = 'تصفّح الأصول الصرفية';
export const STEMS_WORDS_TABLIST_LABEL = 'أنماط كلمات الأصل الصرفي';
export const STEMS_SURAHS_TABLIST_LABEL = 'عرض سور الأصل الصرفي';
export const STEMS_WORDS_PAGINATION_LABEL = 'تصفّح كلمات الأصل الصرفي';
export const STEMS_PANEL_SURFACE_LABEL = 'تفاصيل الأصل الصرفي';
export const STEMS_LEMMAS_LIST_LABEL = 'الصيغ المعجمية المرتبطة';
export const STEMS_LEMMAS_LIST_LOADING_LABEL = 'جارٍ تحميل الصيغ المعجمية المرتبطة…';
export const STEMS_LEMMAS_LIST_EMPTY_LABEL = 'لا توجد صيغ معجمية مرتبطة';
export const STEMS_LEMMA_LINK_PREFIX = 'الصيغة المعجمية:';
export const STEMS_ROOT_LINK_PREFIX = 'الجذر:';

export const STEMS_COLUMN_HEADERS = {
  rowNumber: ROW_NUMBER_HEADER,
  stem: WORDS_SHARED_HEADERS.stem,
  lemma: WORDS_SHARED_HEADERS.lemmas,
  root: 'الجذور',
  occurrences: WORDS_SHARED_HEADERS.occurrences,
  ayahs: WORDS_SHARED_HEADERS.ayahs,
  surahs: WORDS_SHARED_HEADERS.surahs,
  simpleWords: WORDS_SHARED_HEADERS.simpleWords,
  tashkeelWords: WORDS_SHARED_HEADERS.tashkeelWords,
} as const;

export const STEMS_COLUMN_COUNT_LABELS = {
  occurrences: WORDS_SHARED_COUNT_COLUMNS.occurrences,
  ayahs: WORDS_SHARED_COUNT_COLUMNS.ayahs,
  surahs: WORDS_SHARED_COUNT_COLUMNS.surahs,
  simpleWords: WORDS_SHARED_COUNT_COLUMNS.simpleWords,
  tashkeelWords: WORDS_SHARED_COUNT_COLUMNS.tashkeelWords,
} as const;

export const STEMS_PANEL_TAB_LABELS: Record<StemView, string> = {
  words: WORDS_SHARED_PANEL_TABS.words,
  ayahs: WORDS_SHARED_PANEL_TABS.ayahs,
  surahs: WORDS_SHARED_PANEL_TABS.surahs,
  lemmas: WORDS_SHARED_PANEL_TABS.lemmas,
};

export const STEMS_PANEL_TAB_ARIA: Record<StemView, string> = {
  words: `${WORDS_SHARED_PANEL_TABS.words} (${WORDS_SHARED_WORD_VIEWS.simple} / ${WORDS_SHARED_WORD_VIEWS.tashkeel})`,
  ayahs: WORDS_SHARED_PANEL_TABS.ayahs,
  surahs: `${WORDS_SHARED_PANEL_TABS.surahs} (${WORDS_SHARED_SURAH_VIEWS.mentioned} / ${WORDS_SHARED_SURAH_VIEWS.missing})`,
  lemmas: WORDS_SHARED_PANEL_TABS.lemmas,
};

export const STEMS_WORD_VIEW_LABELS: Record<StemWordView, string> = {
  simple: WORDS_SHARED_WORD_VIEWS.simple,
  tashkeel: WORDS_SHARED_WORD_VIEWS.tashkeel,
};

export const STEMS_SURAHS_VIEW_LABELS: Record<StemSurahView, string> = {
  mentioned: WORDS_SHARED_SURAH_VIEWS.mentioned,
  missing: WORDS_SHARED_SURAH_VIEWS.missing,
};

export const STEMS_EMPTY_SELECTION_LABEL = 'اختر أصلًا صرفيًا لعرض تفاصيله';
export const STEMS_PANEL_LABEL = 'تفاصيل الأصل الصرفي';
export const STEMS_CLOSE_PANEL_LABEL = 'إغلاق لوحة التفاصيل';
export const STEMS_LOADING_LABEL = 'جارٍ التحميل…';
export const STEMS_EMPTY_VIEW_LABEL = 'لا توجد نتائج';
export const STEMS_NOT_FOUND_LABEL = 'الأصل الصرفي غير موجود';
export const STEMS_ERROR_LABEL = 'تعذّر تحميل تفاصيل الأصل الصرفي. تحقّق من الاتصال ثم أعد المحاولة.';
export const STEMS_LIST_ERROR_LABEL = 'تعذّر تحميل الأصول الصرفية. تحقّق من الاتصال ثم أعد المحاولة.';
export const STEMS_NO_RESULTS_LABEL = 'لا توجد أصول صرفية مطابقة لبحثك';

export const STEMS_WORD_OCCURRENCES_HEADER = WORDS_SHARED_LIST_HEADERS.occurrences;
export const STEMS_WORD_DISPLAY_HEADER = WORDS_SHARED_LIST_HEADERS.word;
export const STEMS_LEMMA_TEXT_HEADER = WORDS_SHARED_HEADERS.lemma;
export const STEMS_OPEN_UNIQUE_WORD_LABEL = 'فتح تفاصيل الكلمة في مستكشف الكلمات الفريدة';

export const STEMS_LEMMA_MISSING_LABEL = '—';
export const STEMS_LEMMA_MISSING_ARIA = 'لا توجد صيغة معجمية مرتبطة';
export const STEMS_ROOT_MISSING_LABEL = '—';
export const STEMS_ROOT_MISSING_ARIA = 'لا يوجد جذر مرتبط';

export const STEMS_TYPE_DISTRIBUTION_LABEL = 'توزيع الأنواع';
export const STEMS_TYPE_DISTRIBUTION_LOADING_LABEL = 'جارٍ تحميل توزيع الأنواع…';
export const STEMS_TYPE_DISTRIBUTION_EMPTY_LABEL = 'لا توجد أنواع';
export const STEMS_AYAH_TYPE_FILTERS_LABEL = 'تصفية الأنواع في الآيات';
export const STEMS_AYAH_TYPE_ALL_LABEL = 'عرض الكل';

export const STEMS_SORT_LABELS = {
  label: 'الترتيب',
  'mushaf-order': 'ترتيب المصحف',
  occurrences: 'الأكثر ورودًا',
  alpha: 'أبجدي',
} as const;

export function stemsAdditionalTypesAria(count: number): string {
  return count === 1 ? 'نوع إضافي واحد' : `${count} أنواع إضافية`;
}
