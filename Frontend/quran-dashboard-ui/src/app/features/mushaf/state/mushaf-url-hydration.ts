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
  /**
   * True when the ayah-study resource is a stranded load that this hydration must recover
   * (F1). The caller MUST pass `true` only on the first hydration after a route (re)bind
   * where the resource is still loading — i.e. a prior load was interrupted by teardown and
   * the selection identity did not change, so it would never resolve unless reloaded here.
   * It must stay `false` for ordinary in-place URL patches on a live binding, otherwise an
   * unrelated patch (e.g. a study-tab switch) would cancel and restart an in-flight request.
   * Optional/defaults to `false` for callers that only care about identity-driven reloads.
   */
  ayahStudyIsLoading?: boolean;
  /** Same rebind-scoped stranded-load recovery signal as {@link ayahStudyIsLoading}, for word analysis. */
  wordAnalysisIsLoading?: boolean;
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

export function applyAuthoritativeUrlSnapshot(
  snapshot: AuthoritativeUrlSlice,
  current: MushafUrlHydrationCurrent,
  handlers: MushafUrlHydrationHandlers,
): void {
  handlers.setUiState(snapshot.panel, snapshot.ayahTab, snapshot.wordTab, snapshot.segment);

  if (snapshot.word) {
    const wordChanged = snapshot.word !== current.selectedWordLocation;
    handlers.setWord(snapshot.word, wordChanged || Boolean(current.wordAnalysisIsLoading));
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
    handlers.setAyah(
      snapshot.ayah,
      ayahChanged || sourcesChanged || Boolean(current.ayahStudyIsLoading),
    );
  } else {
    handlers.clearAyahSelection();
  }
}
