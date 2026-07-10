import { RootView, RootWordView, RootSurahView } from './roots.models';
import {
  ROW_NUMBER_HEADER,
  WORDS_SHARED_COUNT_COLUMNS,
  WORDS_SHARED_HEADERS,
  WORDS_SHARED_LIST_HEADERS,
  WORDS_SHARED_PANEL_TABS,
  WORDS_SHARED_SURAH_VIEWS,
  WORDS_SHARED_WORD_VIEWS,
} from './words-shared.labels';

export const ROOTS_PAGE_TITLE = 'الجذور';
export const ROOTS_SEARCH_LABEL = 'بحث في الجذور';
export const ROOTS_SEARCH_PLACEHOLDER = 'اكتب جذرًا…';

export const ROOTS_COLUMN_HEADERS = {
  rowNumber: ROW_NUMBER_HEADER,
  root: WORDS_SHARED_HEADERS.root,
  occurrences: WORDS_SHARED_HEADERS.occurrences,
  ayahs: WORDS_SHARED_HEADERS.ayahs,
  surahs: WORDS_SHARED_HEADERS.surahs,
  simpleWords: WORDS_SHARED_HEADERS.simpleWords,
  tashkeelWords: WORDS_SHARED_HEADERS.tashkeelWords,
  lemmas: WORDS_SHARED_HEADERS.lemmas,
  stems: WORDS_SHARED_HEADERS.stems,
} as const;

export const ROOTS_COLUMN_COUNT_LABELS = {
  occurrences: WORDS_SHARED_COUNT_COLUMNS.occurrences,
  ayahs: WORDS_SHARED_COUNT_COLUMNS.ayahs,
  surahs: WORDS_SHARED_COUNT_COLUMNS.surahs,
  simpleWords: WORDS_SHARED_COUNT_COLUMNS.simpleWords,
  tashkeelWords: WORDS_SHARED_COUNT_COLUMNS.tashkeelWords,
  lemmas: WORDS_SHARED_COUNT_COLUMNS.lemmas,
  stems: WORDS_SHARED_COUNT_COLUMNS.stems,
} as const;

export const ROOTS_PANEL_TAB_LABELS: Record<RootView, string> = {
  words: WORDS_SHARED_PANEL_TABS.words,
  ayahs: WORDS_SHARED_PANEL_TABS.ayahs,
  surahs: WORDS_SHARED_PANEL_TABS.surahs,
  lemmas: WORDS_SHARED_PANEL_TABS.lemmas,
  stems: WORDS_SHARED_PANEL_TABS.stems,
};

export const ROOTS_PANEL_TAB_ARIA: Record<RootView, string> = {
  words: `${WORDS_SHARED_PANEL_TABS.words} (${WORDS_SHARED_WORD_VIEWS.simple} / ${WORDS_SHARED_WORD_VIEWS.tashkeel})`,
  ayahs: WORDS_SHARED_PANEL_TABS.ayahs,
  surahs: `${WORDS_SHARED_PANEL_TABS.surahs} (${WORDS_SHARED_SURAH_VIEWS.mentioned} / ${WORDS_SHARED_SURAH_VIEWS.missing})`,
  lemmas: WORDS_SHARED_PANEL_TABS.lemmas,
  stems: WORDS_SHARED_PANEL_TABS.stems,
};

export const ROOTS_WORD_VIEW_LABELS: Record<RootWordView, string> = {
  simple: WORDS_SHARED_WORD_VIEWS.simple,
  tashkeel: WORDS_SHARED_WORD_VIEWS.tashkeel,
};

export const ROOTS_SURAHS_VIEW_LABELS: Record<RootSurahView, string> = {
  mentioned: WORDS_SHARED_SURAH_VIEWS.mentioned,
  missing: WORDS_SHARED_SURAH_VIEWS.missing,
};

export const ROOTS_EMPTY_SELECTION_LABEL = 'اختر جذرًا لعرض تفاصيله';
export const ROOTS_PANEL_LABEL = 'تفاصيل الجذر';
export const ROOTS_CLOSE_PANEL_LABEL = 'إغلاق لوحة التفاصيل';
export const ROOTS_EMPTY_VIEW_LABEL = 'لا توجد نتائج';
export const ROOTS_NOT_FOUND_LABEL = 'الجذر غير موجود';
export const ROOTS_ERROR_LABEL = 'تعذّر تحميل تفاصيل الجذر. تحقّق من الاتصال ثم أعد المحاولة.';
export const ROOTS_LIST_ERROR_LABEL = 'تعذّر تحميل الجذور. تحقّق من الاتصال ثم أعد المحاولة.';
export const ROOTS_NO_RESULTS_LABEL = 'لا توجد جذور مطابقة لبحثك';
export const ROOTS_TABLE_LABEL = 'جدول الجذور';
export const ROOTS_TABLE_BODY_LABEL = 'قائمة الجذور';

export const ROOTS_WORD_OCCURRENCES_HEADER = WORDS_SHARED_LIST_HEADERS.occurrences;
export const ROOTS_WORD_DISPLAY_HEADER = WORDS_SHARED_LIST_HEADERS.word;
export const ROOTS_LEMMA_TEXT_HEADER = WORDS_SHARED_HEADERS.lemma;
export const ROOTS_STEM_TEXT_HEADER = WORDS_SHARED_HEADERS.stem;
export const ROOTS_OPEN_UNIQUE_WORD_LABEL = 'فتح تفاصيل الكلمة في مستكشف الكلمات الفريدة';
export const ROOTS_OPEN_LEMMA_LABEL = 'فتح الصيغة المعجمية في مستكشف الصيغ المعجمية';
export const ROOTS_OPEN_STEM_LABEL = 'فتح الأصل الصرفي في مستكشف الأصول الصرفية';
export const ROOTS_LIST_PAGINATION_LABEL = 'تصفّح الجذور';
export const ROOTS_WORDS_TABLIST_LABEL = 'أنماط كلمات الجذر';
export const ROOTS_SURAHS_TABLIST_LABEL = 'عرض سور الجذر';
export const ROOTS_WORDS_PAGINATION_LABEL = 'تصفّح كلمات الجذر';

export const ROOTS_SORT_LABELS = {
  label: 'الترتيب',
  'mushaf-order': 'ترتيب المصحف',
  occurrences: 'الأكثر ورودًا',
  alpha: 'أبجدي',
} as const;
