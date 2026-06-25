import { StemView, StemWordView, StemSurahView } from './stems.models';
import { ROW_NUMBER_HEADER } from './unique-words.labels';

export const STEMS_PAGE_TITLE = 'الأصول الصرفية';
export const STEMS_SEARCH_LABEL = 'بحث في الأصول الصرفية';
export const STEMS_SEARCH_PLACEHOLDER = 'اكتب أصلًا صرفيًا…';

export const STEMS_COLUMN_HEADERS = {
  rowNumber: ROW_NUMBER_HEADER,
  stem: 'الأصل الصرفي',
  lemma: 'الصيغة المعجمية',
  root: 'الجذر',
  type: 'النوع',
  occurrences: 'المواضع',
  ayahs: 'الآيات',
  surahs: 'السور',
  simpleWords: 'كلمات بدون تشكيل',
  tashkeelWords: 'كلمات بالتشكيل',
} as const;

export const STEMS_COLUMN_COUNT_LABELS = {
  occurrences: 'المواضع',
  ayahs: 'الآيات',
  surahs: 'السور',
  simpleWords: 'كلمات بدون تشكيل',
  tashkeelWords: 'كلمات بالتشكيل',
} as const;

export const STEMS_PANEL_TAB_LABELS: Record<StemView, string> = {
  words: 'الكلمات',
  ayahs: 'الآيات',
  surahs: 'السور',
  lemmas: 'الصيغ المعجمية',
};

export const STEMS_PANEL_TAB_ARIA: Record<StemView, string> = {
  words: 'الكلمات (بدون تشكيل / بالتشكيل)',
  ayahs: 'الآيات',
  surahs: 'السور (ورد فيها / لم يذكر فيها)',
  lemmas: 'الصيغ المعجمية',
};

export const STEMS_WORD_VIEW_LABELS: Record<StemWordView, string> = {
  simple: 'بدون تشكيل',
  tashkeel: 'بالتشكيل',
};

export const STEMS_SURAHS_VIEW_LABELS: Record<StemSurahView, string> = {
  mentioned: 'ورد فيها',
  missing: 'لم يذكر فيها',
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

export const STEMS_WORD_OCCURRENCES_HEADER = 'عدد مرات الظهور في الأصل الصرفي';
export const STEMS_WORD_DISPLAY_HEADER = 'الكلمة';
export const STEMS_LEMMA_TEXT_HEADER = 'الصيغة المعجمية';
export const STEMS_OPEN_UNIQUE_WORD_LABEL = 'فتح تفاصيل الكلمة في مستكشف الكلمات الفريدة';

export const STEMS_SORT_LABELS = {
  label: 'الترتيب',
  'mushaf-order': 'ترتيب المصحف',
  occurrences: 'الأكثر ورودًا',
  alpha: 'أبجدي',
} as const;
