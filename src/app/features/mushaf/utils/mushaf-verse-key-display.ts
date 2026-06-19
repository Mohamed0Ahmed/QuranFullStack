/** Formats grouped-coverage verse keys as ayah numbers only (e.g. `2:30, 2:31` → `30، 31`). */
export function formatCoverageAyahNumbers(verseKeys: string[]): string {
  return verseKeys
    .map((key) => {
      const colonIndex = key.indexOf(':');
      if (colonIndex === -1) {
        return key;
      }

      return key.slice(colonIndex + 1);
    })
    .join('، ');
}

const TRAILING_AYAH_END_MARKER =
  /\s*(?:\u06DD[\u06F0-\u06F9\u0660-\u0669]*)?[\u06F0-\u06F9\u0660-\u0669]+\s*$/u;

/** Study-card ayah line: full Uthmani text without the trailing ayah-end number. */
export function toStudyAyahDisplayText(textUthmani: string): string {
  return textUthmani.replace(TRAILING_AYAH_END_MARKER, '').trimEnd();
}
