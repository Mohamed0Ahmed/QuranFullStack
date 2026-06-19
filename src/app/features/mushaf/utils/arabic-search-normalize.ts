const DIACRITICS_PATTERN = /[\u064B-\u065F\u0670]/g;

/** Normalizes Arabic text for case-insensitive search that ignores hamzas and diacritics. */
export function normalizeArabicForSearch(text: string): string {
  return text
    .toLowerCase()
    .replace(DIACRITICS_PATTERN, '')
    .replace(/[أإآء]/g, 'ا')
    .replace(/ؤ/g, 'و')
    .replace(/ئ/g, 'ي');
}

export function arabicSearchIncludes(target: string, query: string): boolean {
  const normalizedQuery = normalizeArabicForSearch(query.trim());
  if (!normalizedQuery) {
    return true;
  }

  return normalizeArabicForSearch(target).includes(normalizedQuery);
}
