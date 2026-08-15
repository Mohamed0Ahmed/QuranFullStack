export interface LinkingQuranWordEntity {
  id: number;
  ayahId: number;
  wordNumber: number;
  textUthmani: string;
  isAyahMarker: boolean;
}

export interface LinkingQuranAyahEntity {
  id: number;
  verseKey: string;
  surahNumber: number;
  surahNameArabic: string;
  ayahNumber: number;
  pageFrom: number;
  pageTo: number;
}

export interface LinkingSourceAyahOverlay {
  ayahId: number;
  matchedQuranWordIds: readonly number[];
}

export function linkingEntityKey(linkingDataRevision: number, entityId: number): string {
  return `${linkingDataRevision}:${entityId}`;
}
