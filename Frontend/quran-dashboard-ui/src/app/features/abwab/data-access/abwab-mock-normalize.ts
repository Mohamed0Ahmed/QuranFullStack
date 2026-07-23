// UI-only mirror of the backend §5.1 Arabic normalization used for the core MOCK's conflict
// simulation. The backend `ArabicNormalizer` is the single source of truth for real uniqueness
// decisions; this mirrors only the mappings needed so the mock and HTTP adapter behave the same way
// for the parity suite (T063), not a re-certification of §5.1.
const TATWEEL = 'ـ';
const ALEF_VARIANTS = /[أإآٱ]/g;
const ALEF_MAQSURA = /ى/g;

export function normalizeArabicNameForUi(value: string): string {
  return value
    .replace(new RegExp(TATWEEL, 'g'), '')
    .replace(ALEF_VARIANTS, 'ا')
    .replace(ALEF_MAQSURA, 'ي')
    .trim()
    .replace(/\s+/g, ' ')
    .normalize('NFC');
}
