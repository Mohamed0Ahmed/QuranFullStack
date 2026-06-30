import { WordTypeCase, WordTypeDetailView, WordTypeMainType, WordTypeSort, WordTypeTense, WordTypeVoice } from './word-types.models';

export const WORD_TYPES_PAGE_TITLE = 'أنواع الكلمات';
export const WORD_TYPES_TABLE_LABEL = 'جدول كلمات النوع';
export const WORD_TYPES_FILTER_LABEL = 'تصفية أنواع الكلمات';
export const WORD_TYPES_DETAILS_PANEL_LABEL = 'تفاصيل كلمة النوع';
export const WORD_TYPES_LOADING_LABEL = 'جارٍ التحميل…';
export const WORD_TYPES_EMPTY_LABEL = 'لا توجد نتائج لهذا النوع';
export const WORD_TYPES_ERROR_LABEL = 'تعذّر تحميل أنواع الكلمات. تحقّق من الاتصال ثم أعد المحاولة.';
export const WORD_TYPES_NOT_FOUND_LABEL = 'الكلمة المحددة غير موجودة';
export const WORD_TYPES_NULL_PLACEHOLDER = '—';

export const WORD_TYPE_MAIN_LABELS: Record<WordTypeMainType, string> = {
  noun: 'اسم',
  verb: 'فعل',
  particle: 'حرف وأداة',
  inl: 'حروف مقطّعة',
};

export const WORD_TYPE_CASE_LABELS: Record<WordTypeCase, string> = {
  all: 'كل الحالات',
  nominative: 'مرفوع',
  accusative: 'منصوب',
  genitive: 'مجرور',
  null: 'غير محدد',
};

export const WORD_TYPE_TENSE_LABELS: Record<WordTypeTense, string> = {
  all: 'كل الأزمنة',
  past: 'ماض',
  present: 'مضارع',
  imperative: 'أمر',
};

export const WORD_TYPE_VOICE_LABELS: Record<WordTypeVoice, string> = {
  all: 'كل الصيغ',
  active: 'معلوم',
  passive: 'مجهول',
};

export const WORD_TYPE_SORT_LABELS: Record<WordTypeSort, string> = {
  occurrences: 'الأكثر ورودًا',
  ayahs: 'الأكثر آيات',
  surahs: 'الأكثر سورًا',
  'mushaf-order': 'ترتيب المصحف',
  alpha: 'أبجدي',
};

export const WORD_TYPE_DETAIL_TAB_LABELS: Record<WordTypeDetailView, string> = {
  ayahs: 'الآيات الخاصة بالكلمة',
  surahs: 'السور',
  analysis: 'التحليل',
};

export const WORD_TYPES_TABLE_HEADERS = {
  word: 'الكلمة',
  type: 'النوع',
  root: 'الجذر',
  stem: 'الصيغة',
  lemma: 'الأصل',
  occurrences: 'المواضع',
  ayahs: 'الآيات',
  surahs: 'السور',
} as const;
