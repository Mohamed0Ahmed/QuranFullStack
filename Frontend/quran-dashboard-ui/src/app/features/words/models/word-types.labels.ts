import { explorerSortOptions } from './explorer-sort';
import { WORD_TYPE_SORT_COLUMN_LIST, WordTypeCase, WordTypeDetailView, WordTypeMainType, WordTypeTableView, WordTypeTense, WordTypeVoice } from './word-types.models';
import type { WordTypeDetailSelectionKind } from './word-types-detail.models';
import { ROW_NUMBER_HEADER } from './words-shared.labels';

export const WORD_TYPES_PAGE_TITLE = 'أنواع الكلمات';
export const WORD_TYPES_FILTER_LABEL = 'تصفية أنواع الكلمات';
export const WORD_TYPES_SEARCH_LABEL = 'بحث في الكلمات';
export const WORD_TYPES_SEARCH_PLACEHOLDER = 'ابحث في الكلمات';
export const WORD_TYPES_NO_SUBTYPES_LABEL = 'لا توجد أنواع فرعية لهذا النوع';
export const WORD_TYPES_SORT_LABEL = 'ترتيب';
export const WORD_TYPES_LOADING_LABEL = 'جارٍ التحميل…';
export const WORD_TYPES_SELECT_SUBTYPE_LABEL = 'اختر نوعًا فرعيًا لعرض الكلمات.';
export const WORD_TYPES_ERROR_LABEL = 'تعذّر تحميل أنواع الكلمات. تحقّق من الاتصال ثم أعد المحاولة.';
export const WORD_TYPES_RETRY_LABEL = 'إعادة المحاولة';
export const WORD_TYPES_NOT_FOUND_LABEL = 'الكلمة المحددة غير موجودة';
export const WORD_TYPES_NULL_PLACEHOLDER = '—';
export const WORD_TYPES_CASE_FILTER_LABEL = 'الحالة';
export const WORD_TYPES_TENSE_FILTER_LABEL = 'الزمن';
export const WORD_TYPES_VOICE_FILTER_LABEL = 'الصيغة';
export const WORD_TYPES_SUBTYPE_GROUP_LABEL = 'الأنواع الفرعية';
export const WORD_TYPES_CURRENT_FILTER_LABEL = 'الحالي';
export const WORD_TYPE_TABLE_VIEW_TABS_LABEL = 'عرض الجدول';

export const WORD_TYPES_SCOPE_COUNTS_LABEL = 'إحصاء النطاق';
export const WORD_TYPES_SCOPE_COUNTS_ERROR_LABEL = 'تعذّر تحميل إحصاء النطاق';

export const WORD_TYPE_TABLE_VIEW_OPTIONS = [
  { value: 'words', label: 'كلمات' },
  { value: 'roots', label: 'جذور' },
  { value: 'stems', label: 'أصول' },
  { value: 'lemmas', label: 'صيغ' },
] as const satisfies readonly { value: WordTypeTableView; label: string }[];

export const WORD_TYPE_TABLE_VIEW_TABLE_LABELS: Record<WordTypeTableView, string> = {
  words: 'جدول كلمات النوع',
  roots: 'جدول الجذور',
  stems: 'جدول الأصول',
  lemmas: 'جدول الصيغ',
};

export const WORD_TYPE_TABLE_VIEW_EMPTY_LABELS: Record<WordTypeTableView, string> = {
  words: 'لا توجد نتائج لهذا النوع',
  roots: 'لا توجد جذور لهذا النطاق',
  stems: 'لا توجد أصول لهذا النطاق',
  lemmas: 'لا توجد صيغ لهذا النطاق',
};

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

export const WORD_TYPES_MUSHAF_ORDER_LABEL = 'ترتيب المصحف';

export const WORD_TYPE_SORT_OPTIONS = explorerSortOptions(
  WORD_TYPE_SORT_COLUMN_LIST,
  WORD_TYPES_MUSHAF_ORDER_LABEL,
);

export interface WordTypeDetailPresentation {
  readonly panelLabel: string;
  readonly emptySelectionLabel: string;
  readonly notFoundLabel: string;
  readonly tabs: Record<WordTypeDetailView, { readonly label: string; readonly aria: string }>;
  readonly emptyViewLabels: Record<WordTypeDetailView, string>;
}

export const WORD_TYPE_DETAIL_PRESENTATIONS: Record<WordTypeDetailSelectionKind, WordTypeDetailPresentation> = {
  word: {
    panelLabel: 'تفاصيل كلمة النوع',
    emptySelectionLabel: 'اختر كلمة من الجدول لعرض تفاصيلها.',
    notFoundLabel: 'الكلمة المحددة غير موجودة',
    tabs: {
      words: { label: 'الكلمات المرتبطة', aria: 'الكلمات المرتبطة بالكلمة المحددة' },
      ayahs: { label: 'الآيات الخاصة بالكلمة', aria: 'الآيات الخاصة بالكلمة المحددة' },
      surahs: { label: 'سور الكلمة', aria: 'توزيع السور للكلمة المحددة' },
    },
    emptyViewLabels: {
      words: 'لا توجد كلمات مرتبطة بالكلمة المحددة',
      ayahs: 'لا توجد آيات مرتبطة بالكلمة المحددة',
      surahs: 'لا توجد سور مرتبطة بالكلمة المحددة',
    },
  },
  root: {
    panelLabel: 'تفاصيل الجذر',
    emptySelectionLabel: 'اختر جذرًا من الجدول لعرض تفاصيله.',
    notFoundLabel: 'الجذر المحدد غير موجود',
    tabs: {
      words: { label: 'كلمات الجذر', aria: 'الكلمات المرتبطة بالجذر المحدد' },
      ayahs: { label: 'آيات الجذر', aria: 'الآيات المرتبطة بالجذر المحدد' },
      surahs: { label: 'سور الجذر', aria: 'توزيع السور للجذر المحدد' },
    },
    emptyViewLabels: {
      words: 'لا توجد كلمات مرتبطة بالجذر المحدد',
      ayahs: 'لا توجد آيات مرتبطة بالجذر المحدد',
      surahs: 'لا توجد سور مرتبطة بالجذر المحدد',
    },
  },
  stem: {
    panelLabel: 'تفاصيل الأصل الصرفي',
    emptySelectionLabel: 'اختر أصلًا صرفيًا من الجدول لعرض تفاصيله.',
    notFoundLabel: 'الأصل الصرفي المحدد غير موجود',
    tabs: {
      words: { label: 'كلمات الأصل الصرفي', aria: 'الكلمات المرتبطة بالأصل الصرفي المحدد' },
      ayahs: { label: 'آيات الأصل الصرفي', aria: 'الآيات المرتبطة بالأصل الصرفي المحدد' },
      surahs: { label: 'سور الأصل الصرفي', aria: 'توزيع السور للأصل الصرفي المحدد' },
    },
    emptyViewLabels: {
      words: 'لا توجد كلمات مرتبطة بالأصل الصرفي المحدد',
      ayahs: 'لا توجد آيات مرتبطة بالأصل الصرفي المحدد',
      surahs: 'لا توجد سور مرتبطة بالأصل الصرفي المحدد',
    },
  },
  lemma: {
    panelLabel: 'تفاصيل الصيغة المعجمية',
    emptySelectionLabel: 'اختر صيغة معجمية من الجدول لعرض تفاصيلها.',
    notFoundLabel: 'الصيغة المعجمية المحددة غير موجودة',
    tabs: {
      words: { label: 'كلمات الصيغة المعجمية', aria: 'الكلمات المرتبطة بالصيغة المعجمية المحددة' },
      ayahs: { label: 'آيات الصيغة المعجمية', aria: 'الآيات المرتبطة بالصيغة المعجمية المحددة' },
      surahs: { label: 'سور الصيغة المعجمية', aria: 'توزيع السور للصيغة المعجمية المحددة' },
    },
    emptyViewLabels: {
      words: 'لا توجد كلمات مرتبطة بالصيغة المعجمية المحددة',
      ayahs: 'لا توجد آيات مرتبطة بالصيغة المعجمية المحددة',
      surahs: 'لا توجد سور مرتبطة بالصيغة المعجمية المحددة',
    },
  },
};

export const WORD_TYPES_DETAIL_SUMMARY_LABEL = 'ملخص التحديد';
export const WORD_TYPES_MEMBER_WORDS_PAGINATION_LABEL = 'تنقّل الكلمات المرتبطة';
export const WORD_TYPES_MEMBER_WORDS_EMPTY_LABEL = 'لا توجد كلمات مرتبطة بهذا التحديد';

export const WORD_TYPES_PRESENCE_FILTER_LABELS = {
  sectionLabel: 'تصفية حسب الارتباط',
  root: 'الجذر',
  stem: 'الأصل الصرفي',
  lemma: 'الصيغة المعجمية',
  any: 'الكل',
  present: 'موجود',
  missing: 'غير موجود',
} as const;

export const WORD_TYPES_TABLE_HEADERS = {
  rowNumber: ROW_NUMBER_HEADER,
  word: 'الكلمة',
  type: 'النوع',
  root: 'الجذر',
  stem: 'الأصل',
  lemma: 'الصيغة',
  occurrences: 'المواضع',
  ayahs: 'الآيات',
  surahs: 'السور',
} as const;
