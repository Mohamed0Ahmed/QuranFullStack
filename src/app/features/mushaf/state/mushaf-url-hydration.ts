import {
  AyahStudyTab,
  MushafReaderSources,
  PanelMode,
  WordAnalysisTab,
} from '../models/mushaf.models';
import { MushafUrlSnapshot } from './mushaf-url-sync';

export interface MushafUrlHydrationCurrent {
  selectedAyahKey: string | null;
  selectedWordLocation: string | null;
  urlExplicitSources: MushafReaderSources;
}

export interface MushafUrlHydrationHandlers {
  setUiState(
    panel: PanelMode,
    ayahTab: AyahStudyTab,
    wordTab: WordAnalysisTab,
    segmentLocation: string | null,
  ): void;
  clearWordSelection(): void;
  setWord(wordLocation: string, reload: boolean): void;
  setUrlExplicitSources(sources: MushafReaderSources): void;
  clearAyahSelection(): void;
  setAyah(verseKey: string, reload: boolean): void;
}

type AuthoritativeUrlSlice = Pick<
  MushafUrlSnapshot,
  'panel' | 'ayah' | 'word' | 'segment' | 'ayahTab' | 'wordTab' | 'sources'
>;

/**
 * Applies a URL snapshot as the sole source of truth for selection/UI state.
 * Omitted query params clear their corresponding state (no carry-over).
 */
export function applyAuthoritativeUrlSnapshot(
  snapshot: AuthoritativeUrlSlice,
  current: MushafUrlHydrationCurrent,
  handlers: MushafUrlHydrationHandlers,
): void {
  handlers.setUiState(snapshot.panel, snapshot.ayahTab, snapshot.wordTab, snapshot.segment);

  if (snapshot.word) {
    const wordChanged = snapshot.word !== current.selectedWordLocation;
    handlers.setWord(snapshot.word, wordChanged);
  } else {
    handlers.clearWordSelection();
  }

  const nextSources: MushafReaderSources = {
    tafsirSource: snapshot.sources.tafsirSource,
    translationSource: snapshot.sources.translationSource,
    fullI3rabSource: snapshot.sources.fullI3rabSource,
  };

  const ayahChanged = snapshot.ayah !== current.selectedAyahKey;
  const sourcesChanged =
    nextSources.tafsirSource !== current.urlExplicitSources.tafsirSource ||
    nextSources.translationSource !== current.urlExplicitSources.translationSource ||
    nextSources.fullI3rabSource !== current.urlExplicitSources.fullI3rabSource;

  handlers.setUrlExplicitSources(nextSources);

  if (snapshot.ayah) {
    handlers.setAyah(snapshot.ayah, ayahChanged || sourcesChanged);
  } else {
    handlers.clearAyahSelection();
  }
}
