import { LemmaView, LemmaWordView, LemmaSurahView } from './lemmas.models';
import { ROW_NUMBER_HEADER } from './unique-words.labels';

export const LEMMAS_PAGE_TITLE = 'الصيغ المعجمية';
export const LEMMAS_SEARCH_LABEL = 'بحث في الصيغ المعجمية';
export const LEMMAS_SEARCH_PLACEHOLDER = 'اكتب صيغة معجمية…';

export const LEMMAS_COLUMN_HEADERS = {
  rowNumber: ROW_NUMBER_HEADER,
  lemma: 'الصيغة المعجمية',
  root: 'الجذر',
  type: 'النوع',
  occurrences: 'المواضع',
  ayahs: 'الآيات',
  surahs: 'السور',
  simpleWords: 'كلمات بدون تشكيل',
  tashkeelWords: 'كلمات بالتشكيل',
  stems: 'الأصول الصرفية',
} as const;

export const LEMMAS_COLUMN_COUNT_LABELS = {
  occurrences: 'المواضع',
  ayahs: 'الآيات',
  surahs: 'السور',
  simpleWords: 'كلمات بدون تشكيل',
  tashkeelWords: 'كلمات بالتشكيل',
  stems: 'الأصول الصرفية',
} as const;

export const LEMMAS_PANEL_TAB_LABELS: Record<LemmaView, string> = {
  words: 'الكلمات',
  ayahs: 'الآيات',
  surahs: 'السور',
  stems: 'الأصول الصرفية',
};

export const LEMMAS_PANEL_TAB_ARIA: Record<LemmaView, string> = {
  words: 'الكلمات (بدون تشكيل / بالتشكيل)',
  ayahs: 'الآيات',
  surahs: 'السور (ورد فيها / لم يذكر فيها)',
  stems: 'الأصول الصرفية',
};

export const LEMMAS_WORD_VIEW_LABELS: Record<LemmaWordView, string> = {
  simple: 'بدون تشكيل',
  tashkeel: 'بالتشكيل',
};

export const LEMMAS_SURAHS_VIEW_LABELS: Record<LemmaSurahView, string> = {
  mentioned: 'ورد فيها',
  missing: 'لم يذكر فيها',
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

export const LEMMAS_WORD_OCCURRENCES_HEADER = 'عدد مرات الظهور في الصيغة المعجمية';
export const LEMMAS_WORD_DISPLAY_HEADER = 'الكلمة';
export const LEMMAS_STEM_TEXT_HEADER = 'الأصل الصرفي';
export const LEMMAS_OPEN_UNIQUE_WORD_LABEL = 'فتح تفاصيل الكلمة في مستكشف الكلمات الفريدة';

export const LEMMAS_SORT_LABELS = {
  label: 'الترتيب',
  'mushaf-order': 'ترتيب المصحف',
  occurrences: 'الأكثر ورودًا',
  alpha: 'أبجدي',
} as const;
