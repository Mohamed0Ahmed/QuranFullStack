import { UniqueWordKind, UniqueWordSort, WordDrilldownView } from './unique-words.models';
import { rootsRoutePath } from '../../../core/navigation/route-paths';

export interface WordSectionCardLabel {
  labelAr: string;
  descriptionAr: string;
}

export const ACTIVE_HUB_SECTION: WordSectionCardLabel = {
  labelAr: 'الكلمات الفريدة',
  descriptionAr: 'استعراض الكلمات القرآنية الفريدة وتوزيعها',
};

/**
 * Additional active Words-hub sections that have landed routes. Feature 015
 * (Roots Explorer, FR-047) promotes الجذور from coming-soon to active.
 * Each entry carries its stable route (a route key, not a translated label).
 */
export interface ActiveHubSection extends WordSectionCardLabel {
  route: string;
}

export const ADDITIONAL_ACTIVE_HUB_SECTIONS: readonly ActiveHubSection[] = [
  { labelAr: 'الجذور', descriptionAr: 'استكشاف جذور الكلمات القرآنية', route: rootsRoutePath() },
];

export const COMING_SOON_HUB_SECTIONS: readonly WordSectionCardLabel[] = [
  { labelAr: 'الصيغة المعجمية', descriptionAr: 'استكشاف الصيغ المعجمية للكلمات' },
  { labelAr: 'الأصل الصرفي', descriptionAr: 'استكشاف الأصول الصرفية للكلمات' },
  { labelAr: 'أنواع الكلمة', descriptionAr: 'استكشاف أنواع الكلمات من حيث الاسم والفعل والحرف' },
];

export const COMING_SOON_BADGE = 'قريبًا';
export const WORDS_HUB_TITLE = 'الكلمات';
export const WORDS_HUB_SUBTITLE = 'أقسام دراسة الكلمات القرآنية';

export const UNIQUE_WORD_KIND_LABELS: Record<UniqueWordKind, string> = {
  tashkeel: 'بالتشكيل',
  simple: 'إملائي (بدون تشكيل)',
};

export const UNIQUE_WORD_SORT_LABELS: Record<UniqueWordSort, string> = {
  'mushaf-order': 'ترتيب المصحف',
  occurrences: 'الأكثر تكرارًا',
  alpha: 'أبجدي',
};

export const WORD_DRILLDOWN_VIEW_LABELS: Record<WordDrilldownView, string> = {
  surahs: 'السور',
  missing: 'لم يذكر في',
  ayahs: 'الآيات',
};

export const OCCURRENCES_CHIP_LABEL = 'المواضع';
export const SURAH_OCCURRENCES_COUNT_HEADER = 'عدد مرات الظهور';
export const ROW_NUMBER_HEADER = 'م';
export const SURAH_NAME_HEADER = 'السورة';
export const AYAH_REF_LABEL = 'آية';
export const MUSHAF_PAGE_REF_LABEL = 'ص';

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

export const OPEN_AYAH_IN_MUSHAF_LABEL = 'فتح الآية في المصحف';

export const RESTORED_WORD_NOT_FOUND_LABEL = 'الكلمة المحددة غير موجودة';

export const RESTORED_WORD_LOAD_ERROR_LABEL =
  'تعذّر تحميل الكلمة المحددة. تحقّق من الاتصال ثم أعد المحاولة.';
