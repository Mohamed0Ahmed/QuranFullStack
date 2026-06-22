import { UniqueWordKind, UniqueWordSort, WordDrilldownView } from './unique-words.models';

/**
 * Arabic labels and short descriptions for the Words feature. Route keys stay
 * stable in `unique-words.models.ts`; only display text lives here. No
 * Quranic/religious content is authored in this file.
 */

/** Hub section cards. The active v1 section plus four coming-soon sections. */
export interface WordSectionCardLabel {
  labelAr: string;
  descriptionAr: string;
}

export const ACTIVE_HUB_SECTION: WordSectionCardLabel = {
  labelAr: 'الكلمات الفريدة',
  descriptionAr: 'استعراض الكلمات القرآنية الفريدة وتوزيعها',
};

/** Coming-soon (disabled) hub cards. */
export const COMING_SOON_HUB_SECTIONS: readonly WordSectionCardLabel[] = [
  { labelAr: 'الجذور', descriptionAr: 'استكشاف جذور الكلمات القرآنية' },
  { labelAr: 'الصيغة المعجمية', descriptionAr: 'استكشاف الصيغ المعجمية للكلمات' },
  { labelAr: 'الأصل الصرفي', descriptionAr: 'استكشاف الأصول الصرفية للكلمات' },
  { labelAr: 'أنواع الكلمة', descriptionAr: 'استكشاف أنواع الكلمات من حيث الاسم والفعل والحرف' },
];

export const COMING_SOON_BADGE = 'قريبًا';
export const WORDS_HUB_TITLE = 'الكلمات';
export const WORDS_HUB_SUBTITLE = 'أقسام دراسة الكلمات القرآنية';

/** Mode labels keyed by stable route key. */
export const UNIQUE_WORD_KIND_LABELS: Record<UniqueWordKind, string> = {
  tashkeel: 'بالتشكيل',
  simple: 'إملائي (بدون تشكيل)',
};

/** Sort labels keyed by stable route key. */
export const UNIQUE_WORD_SORT_LABELS: Record<UniqueWordSort, string> = {
  'mushaf-order': 'ترتيب المصحف',
  occurrences: 'الأكثر تكرارًا',
  alpha: 'أبجدي',
};

/** Count chip / drill-down view labels keyed by stable view key. */
export const WORD_DRILLDOWN_VIEW_LABELS: Record<WordDrilldownView, string> = {
  surahs: 'السور',
  missing: 'لم يذكر في',
  ayahs: 'الآيات',
};

/** Count chip labels shown on each unique-word card. `occurrences` is summary-only in v1. */
export const OCCURRENCES_CHIP_LABEL = 'المواضع';

/** Common UI state labels. */
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
