/** Derives `surah:ayah` from a readable word location `surah:ayah:word`. */
export function verseKeyFromWordLocation(wordLocation: string): string | null {
  const parts = wordLocation.split(':');
  if (parts.length < 3) {
    return null;
  }

  const surah = parts[0];
  const ayah = parts[1];
  if (!surah || !ayah) {
    return null;
  }

  return `${surah}:${ayah}`;
}
