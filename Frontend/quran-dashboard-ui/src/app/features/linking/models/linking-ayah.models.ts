export interface LinkingAyah {
  verseKey: string;
  ayahId: number | null;
  surahNumber: number | null;
  surahNameArabic: string | null;
  ayahNumber: number;
  pageNumber: number;
  words: readonly LinkingAyahWord[];
}

export interface LinkingAyahWord {
  renderPosition: number;
  canonicalQuranWordId: number | null;
  textUthmani: string;
  isAyahMarker: boolean;
  isSourceMatch: boolean;
}
