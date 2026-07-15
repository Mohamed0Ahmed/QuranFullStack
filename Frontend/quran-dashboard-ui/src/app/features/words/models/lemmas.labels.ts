import { LemmaView, LemmaWordView, LemmaSurahView } from './lemmas.models';
import {
  ROW_NUMBER_HEADER,
  WORDS_SHARED_COUNT_COLUMNS,
  WORDS_SHARED_HEADERS,
  WORDS_SHARED_LIST_HEADERS,
  WORDS_SHARED_PANEL_TABS,
  WORDS_SHARED_SURAH_VIEWS,
  WORDS_SHARED_WORD_VIEWS,
} from './words-shared.labels';

export const LEMMAS_PAGE_TITLE = 'الصيغ المعجمية';
// Headline result-count stat (Feature 026, US4): label-prefix "عدد الـ…: N".
export const LEMMAS_RESULT_COUNT_LABEL = 'عدد الصيغ المعجمية';
// Association filter (Feature 026, US7): owned root (real FK belonging).
export const LEMMAS_ROOT_FILTER_LABEL = 'الجذر';
export const LEMMAS_ROOT_FILTER_PLACEHOLDER = 'ابحث عن جذر…';
export const LEMMAS_SEARCH_LABEL = 'بحث في الصيغ المعجمية';
export const LEMMAS_SEARCH_PLACEHOLDER = 'اكتب صيغة معجمية…';

export const LEMMAS_TABLE_LABEL = 'جدول الصيغ المعجمية';
export const LEMMAS_TABLE_BODY_LABEL = 'قائمة الصيغ المعجمية';
export const LEMMAS_LIST_PAGINATION_LABEL = 'تصفّح الصيغ المعجمية';
export const LEMMAS_WORDS_TABLIST_LABEL = 'أنماط كلمات الصيغة المعجمية';
export const LEMMAS_SURAHS_TABLIST_LABEL = 'عرض سور الصيغة المعجمية';
export const LEMMAS_WORDS_PAGINATION_LABEL = 'تصفّح كلمات الصيغة المعجمية';
export const LEMMAS_PANEL_SURFACE_LABEL = 'تفاصيل الصيغة المعجمية';

export const LEMMAS_COLUMN_HEADERS = {
  rowNumber: ROW_NUMBER_HEADER,
  lemma: WORDS_SHARED_HEADERS.lemma,
  root: WORDS_SHARED_HEADERS.root,
  occurrences: WORDS_SHARED_HEADERS.occurrences,
  ayahs: WORDS_SHARED_HEADERS.ayahs,
  surahs: WORDS_SHARED_HEADERS.surahs,
  simpleWords: WORDS_SHARED_HEADERS.simpleWords,
  tashkeelWords: WORDS_SHARED_HEADERS.tashkeelWords,
  stems: WORDS_SHARED_HEADERS.stems,
} as const;

export const LEMMAS_COLUMN_COUNT_LABELS = {
  occurrences: WORDS_SHARED_COUNT_COLUMNS.occurrences,
  ayahs: WORDS_SHARED_COUNT_COLUMNS.ayahs,
  surahs: WORDS_SHARED_COUNT_COLUMNS.surahs,
  simpleWords: WORDS_SHARED_COUNT_COLUMNS.simpleWords,
  tashkeelWords: WORDS_SHARED_COUNT_COLUMNS.tashkeelWords,
  stems: WORDS_SHARED_COUNT_COLUMNS.stems,
} as const;

export const LEMMAS_PANEL_TAB_LABELS: Record<LemmaView, string> = {
  words: WORDS_SHARED_PANEL_TABS.words,
  ayahs: WORDS_SHARED_PANEL_TABS.ayahs,
  surahs: WORDS_SHARED_PANEL_TABS.surahs,
  stems: WORDS_SHARED_PANEL_TABS.stems,
};

export const LEMMAS_PANEL_TAB_ARIA: Record<LemmaView, string> = {
  words: `${WORDS_SHARED_PANEL_TABS.words} (${WORDS_SHARED_WORD_VIEWS.simple} / ${WORDS_SHARED_WORD_VIEWS.tashkeel})`,
  ayahs: WORDS_SHARED_PANEL_TABS.ayahs,
  surahs: `${WORDS_SHARED_PANEL_TABS.surahs} (${WORDS_SHARED_SURAH_VIEWS.mentioned} / ${WORDS_SHARED_SURAH_VIEWS.missing})`,
  stems: WORDS_SHARED_PANEL_TABS.stems,
};

export const LEMMAS_WORD_VIEW_LABELS: Record<LemmaWordView, string> = {
  simple: WORDS_SHARED_WORD_VIEWS.simple,
  tashkeel: WORDS_SHARED_WORD_VIEWS.tashkeel,
};

export const LEMMAS_SURAHS_VIEW_LABELS: Record<LemmaSurahView, string> = {
  mentioned: WORDS_SHARED_SURAH_VIEWS.mentioned,
  missing: WORDS_SHARED_SURAH_VIEWS.missing,
};

export const LEMMAS_EMPTY_SELECTION_LABEL = 'اختر صيغة معجمية لعرض تفاصيلها';
export const LEMMAS_PANEL_LABEL = 'تفاصيل الصيغة المعجمية';
export const LEMMAS_CLOSE_PANEL_LABEL = 'إغلاق لوحة التفاصيل';
export const LEMMAS_LOADING_LABEL = 'جارٍ التحميل…';
export const LEMMAS_EMPTY_VIEW_LABEL = 'لا توجد نتائج';
export const LEMMAS_NOT_FOUND_LABEL = 'الصيغة المعجمية غير موجودة';
export const LEMMAS_ERROR_LABEL = 'تعذّر تحميل تفاصيل الصيغة المعجمية. تحقّق من الاتصال ثم أعد المحاولة.';
export const LEMMAS_LIST_ERROR_LABEL = 'تعذّر تحميل الصيغ المعجمية. تحقّق من الاتصال ثم أعد المحاولة.';
export const LEMMAS_NO_RESULTS_LABEL = 'لا توجد صيغ معجمية مطابقة لبحثك';
export const LEMMAS_AYAH_TYPE_FILTERS_LABEL = 'تصفية الأنواع في الآيات';
export const LEMMAS_AYAH_TYPE_ALL_LABEL = 'عرض الكل';

export const LEMMAS_WORD_OCCURRENCES_HEADER = WORDS_SHARED_LIST_HEADERS.occurrences;
export const LEMMAS_WORD_DISPLAY_HEADER = WORDS_SHARED_LIST_HEADERS.word;
export const LEMMAS_STEM_TEXT_HEADER = WORDS_SHARED_HEADERS.stem;
export const LEMMAS_OPEN_UNIQUE_WORD_LABEL = 'فتح تفاصيل الكلمة في مستكشف الكلمات الفريدة';
export const LEMMAS_STEMS_LIST_LABEL = 'الأصول الصرفية المرتبطة';
export const LEMMAS_STEMS_LIST_LOADING_LABEL = 'جارٍ تحميل الأصول الصرفية المرتبطة…';
export const LEMMAS_STEMS_LIST_EMPTY_LABEL = 'لا توجد أصول صرفية مرتبطة';
export const LEMMAS_ROOT_LINK_PREFIX = 'الجذر:';
export const LEMMAS_ROOT_MISSING_ARIA = 'لا يوجد جذر مرتبط';

export const LEMMAS_ROOT_MISSING_LABEL = '—';

export const LEMMAS_SORT_LABELS = {
  label: 'الترتيب',
  'mushaf-order': 'ترتيب المصحف',
  occurrences: 'الأكثر ورودًا',
  alpha: 'أبجدي',
} as const;
