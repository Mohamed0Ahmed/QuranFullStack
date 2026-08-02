export const WORDS_SHARED_HEADERS = {
  rowNumber: 'م',
  word: 'الكلمة',
  lemma: 'الصيغة المعجمية',
  lemmas: 'الصيغ',
  root: 'الجذر',
  stem: 'الأصل الصرفي',
  stems: 'الأصول',
  type: 'النوع',
  occurrences: 'المواضع',
  ayahs: 'الآيات',
  surahs: 'السور',
  simpleWords: 'بدون تشكيل',
  tashkeelWords: 'بالتشكيل',
  missingSurahs: 'لم يذكر فيها',
} as const;

export const ROW_NUMBER_HEADER = WORDS_SHARED_HEADERS.rowNumber;

export const WORDS_SHARED_PANEL_TABS = {
  words: 'الكلمات',
  ayahs: WORDS_SHARED_HEADERS.ayahs,
  surahs: WORDS_SHARED_HEADERS.surahs,
  lemmas: WORDS_SHARED_HEADERS.lemmas,
  stems: WORDS_SHARED_HEADERS.stems,
} as const;

export const WORDS_SHARED_WORD_VIEWS = {
  simple: WORDS_SHARED_HEADERS.simpleWords,
  tashkeel: WORDS_SHARED_HEADERS.tashkeelWords,
} as const;

export const WORDS_SHARED_SURAH_VIEWS = {
  mentioned: 'ورد فيها',
  missing: WORDS_SHARED_HEADERS.missingSurahs,
} as const;

export const WORDS_SHARED_LIST_HEADERS = {
  word: WORDS_SHARED_HEADERS.word,
  occurrences: 'عدد مرات الظهور',
  surahName: 'السورة',
} as const;

export const WORDS_SHARED_COUNT_COLUMNS = {
  occurrences: WORDS_SHARED_HEADERS.occurrences,
  ayahs: WORDS_SHARED_HEADERS.ayahs,
  surahs: WORDS_SHARED_HEADERS.surahs,
  simpleWords: 'كلمات بدون تشكيل',
  tashkeelWords: 'كلمات بالتشكيل',
  lemmas: 'الصيغ المعجمية',
  stems: 'الأصول الصرفية',
} as const;

// UI labels for the shared count-range filter (Feature 026, US5; chips reshaped in Feature 030, N4).
// greaterThan/lessThan prefix the metric's threshold to form the two shortcut chip labels; apply is the
// touch-friendly twin of pressing Enter in the مخصّص inputs.
export const WORDS_RANGE_FILTER_LABELS = {
  sectionLabel: 'تصفية حسب الأعداد',
  greaterThan: 'أكثر من',
  lessThan: 'أقل من',
  custom: 'مخصّص',
  apply: 'تطبيق',
  min: 'من',
  max: 'إلى',
  clearAll: 'مسح كل عوامل التصفية',
} as const;

// UI labels for the shared association filter picker (Feature 026, US7). activeFilter is the
// neutral badge shown while a URL-restored selection's real label has not resolved yet.
export const WORDS_ASSOCIATION_FILTER_LABELS = {
  activeFilter: 'مرشَّح نشط',
  clear: 'مسح التصفية',
  loading: 'جارٍ التحميل…',
  // M32/M43 + M74: a load failure must read as a calm error hint, distinct from both the
  // loading state and a genuine zero-match empty result.
  error: 'تعذّر تحميل الخيارات',
  noResults: 'لا توجد نتائج',
} as const;

// The result-count stat's own labels moved with the component to shared/ui/result-count/
// (Slice B2, T1001 — it is now a shared primitive, not words-only). See
// shared/ui/result-count/result-count.labels.ts.

/**
 * The single recovery action offered on a failed detail load (Feature 030, M3),
 * shared by all five detail surfaces (roots, lemmas, stems, word types, and the
 * unique-word drilldown) in both the side panel and the global overlay.
 * Retrying re-drives the current complete identity, so a transient
 * network/server failure no longer leaves the retained detail unusable until the
 * user changes identity or reloads the page.
 */
export const WORDS_DETAIL_RETRY_LABEL = 'إعادة المحاولة';
