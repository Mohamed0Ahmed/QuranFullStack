
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

export function toStudyAyahDisplayText(textUthmani: string): string {
  return textUthmani.replace(TRAILING_AYAH_END_MARKER, '').trimEnd();
}
