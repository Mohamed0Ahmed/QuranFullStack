import { RootView, RootWordView, RootSurahView } from './roots.models';

/**
 * Roots Explorer (Feature 015) Arabic labels — verbatim from the spec.
 * Centralized here so templates stay clean and labels stay consistent.
 */

// Page header + table column headers (exact wording per spec).
export const ROOTS_PAGE_TITLE = 'الجذور';
export const ROOTS_SEARCH_LABEL = 'بحث في الجذور';
export const ROOTS_SEARCH_PLACEHOLDER = 'اكتب جذرًا…';

export const ROOTS_COLUMN_HEADERS = {
  rowNumber: '#',
  root: 'الجذر',
  occurrences: 'المواضع',
  ayahs: 'الآيات',
  surahs: 'السور',
  simpleWords: 'كلمات بدون تشكيل',
  tashkeelWords: 'كلمات بالتشكيل',
  lemmas: 'الصيغ المعجمية',
  stems: 'الأصول الصرفية',
} as const;

// Panel tab labels — exactly 5 tabs, NO overview (نظرة عامة) tab.
export const ROOTS_PANEL_TAB_LABELS: Record<RootView, string> = {
  words: 'الكلمات',
  ayahs: 'الآيات',
  surahs: 'السور',
  lemmas: 'الصيغ المعجمية',
  stems: 'الأصول الصرفية',
};

export const ROOTS_PANEL_TAB_ARIA: Record<RootView, string> = {
  words: 'الكلمات (بدون تشكيل / بالتشكيل)',
  ayahs: 'الآيات',
  surahs: 'السور (ورد فيها / لم يذكر فيها)',
  lemmas: 'الصيغ المعجمية',
  stems: 'الأصول الصرفية',
};

export const ROOTS_WORD_VIEW_LABELS: Record<RootWordView, string> = {
  simple: 'بدون تشكيل',
  tashkeel: 'بالتشكيل',
};

export const ROOTS_SURAHS_VIEW_LABELS: Record<RootSurahView, string> = {
  mentioned: 'ورد فيها',
  missing: 'لم يذكر فيها',
};

// Panel states.
export const ROOTS_EMPTY_SELECTION_LABEL = 'اختر جذرًا لعرض تفاصيله';
export const ROOTS_PANEL_LABEL = 'تفاصيل الجذر';
export const ROOTS_CLOSE_PANEL_LABEL = 'إغلاق لوحة التفاصيل';
export const ROOTS_LOADING_LABEL = 'جارٍ التحميل…';
export const ROOTS_EMPTY_VIEW_LABEL = 'لا توجد نتائج';
export const ROOTS_NOT_FOUND_LABEL = 'الجذر غير موجود';
export const ROOTS_ERROR_LABEL = 'تعذّر تحميل تفاصيل الجذر. تحقّق من الاتصال ثم أعد المحاولة.';
export const ROOTS_LIST_ERROR_LABEL = 'تعذّر تحميل الجذور. تحقّق من الاتصال ثم أعد المحاولة.';
export const ROOTS_NO_RESULTS_LABEL = 'لا توجد جذور مطابقة لبحثك';

export const ROOTS_WORD_OCCURRENCES_HEADER = 'عدد مرات الظهور في الجذر';
export const ROOTS_WORD_DISPLAY_HEADER = 'الكلمة';
export const ROOTS_LEMMA_TEXT_HEADER = 'الصيغة المعجمية';
export const ROOTS_STEM_TEXT_HEADER = 'الأصل الصرفي';
export const ROOTS_OPEN_UNIQUE_WORD_LABEL = 'فتح تفاصيل الكلمة في مستكشف الكلمات الفريدة';

// Sort control labels.
export const ROOTS_SORT_LABELS = {
  label: 'الترتيب',
  'mushaf-order': 'ترتيب المصحف',
  occurrences: 'الأكثر ورودًا',
  alpha: 'أبجدي',
} as const;
