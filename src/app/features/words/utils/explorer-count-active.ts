export type MorphologyColumnKey =
  | 'occurrences'
  | 'ayahs'
  | 'surahs'
  | 'simple'
  | 'tashkeel'
  | 'lemmas'
  | 'stems';

export type UniqueWordsColumnKey = 'ayahs' | 'surahs' | 'missing';

export interface MorphologyActiveColumnState<
  View extends string,
  WordView extends string,
  SurahView extends string,
> {
  view: View | null;
  wordView?: WordView | null;
  surahView?: SurahView | null;
  activeColumn?: MorphologyColumnKey | null;
}

export function resolveMorphologyActiveColumn<View extends string, WordView extends string, SurahView extends string>(
  state: MorphologyActiveColumnState<View, WordView, SurahView>,
): MorphologyColumnKey | null {
  if (isMorphologyColumnCompatible(state.activeColumn, state.view, state.wordView)) {
    return state.activeColumn;
  }

  switch (state.view) {
    case 'words':
      return state.wordView === 'tashkeel' ? 'tashkeel' : 'simple';
    case 'surahs':
      return 'surahs';
    case 'lemmas':
      return 'lemmas';
    case 'stems':
      return 'stems';
    case 'ayahs':
      return 'occurrences';
    default:
      return null;
  }
}

export function parseMorphologyColumnKey(value: string | null): MorphologyColumnKey | null {
  switch (value) {
    case 'occurrences':
    case 'ayahs':
    case 'surahs':
    case 'simple':
    case 'tashkeel':
    case 'lemmas':
    case 'stems':
      return value;
    default:
      return null;
  }
}

export function parseUniqueWordsColumnKey(value: string | null): UniqueWordsColumnKey | null {
  switch (value) {
    case 'ayahs':
    case 'surahs':
    case 'missing':
      return value;
    default:
      return null;
  }
}

export function isMorphologyCountActive(options: {
  rowId: number;
  selectedRowId: number | null;
  column: MorphologyColumnKey;
  activeColumn: MorphologyColumnKey | null;
  disabled?: boolean;
}): boolean {
  if (options.disabled || options.selectedRowId !== options.rowId) {
    return false;
  }

  return options.activeColumn === options.column;
}

export function isUniqueWordCountActive(options: {
  rowId: number;
  selectedWordId: number | null;
  column: UniqueWordsColumnKey;
  activeColumn: UniqueWordsColumnKey | null;
  drilldownOpen: boolean;
  disabled?: boolean;
}): boolean {
  if (
    options.disabled ||
    !options.drilldownOpen ||
    options.selectedWordId !== options.rowId
  ) {
    return false;
  }

  return options.activeColumn === options.column;
}

function isMorphologyColumnCompatible<View extends string, WordView extends string>(
  column: MorphologyColumnKey | null | undefined,
  view: View | null,
  wordView: WordView | null | undefined,
): column is MorphologyColumnKey {
  if (column === null || column === undefined) {
    return false;
  }

  switch (view) {
    case 'words':
      return wordView === 'tashkeel' ? column === 'tashkeel' : column === 'simple';
    case 'surahs':
      return column === 'surahs';
    case 'lemmas':
      return column === 'lemmas';
    case 'stems':
      return column === 'stems';
    case 'ayahs':
      return column === 'occurrences' || column === 'ayahs';
    default:
      return false;
  }
}
