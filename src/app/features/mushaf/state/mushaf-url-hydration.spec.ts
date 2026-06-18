import { describe, expect, it, vi } from 'vitest';

import { applyAuthoritativeUrlSnapshot } from './mushaf-url-hydration';

describe('applyAuthoritativeUrlSnapshot', () => {
  it('clears segment when the snapshot has a word but no segment', () => {
    const setUiState = vi.fn();
    const setWord = vi.fn();
    const setSources = vi.fn();
    const setAyah = vi.fn();

    applyAuthoritativeUrlSnapshot(
      {
        panel: 'word',
        ayah: null,
        word: '2:25:4',
        segment: null,
        ayahTab: 'tafsir',
        wordTab: 'segments',
        sources: { tafsirSource: null, translationSource: null, fullI3rabSource: null },
      },
      {
        selectedAyahKey: null,
        selectedWordLocation: '2:25:3',
        urlExplicitSources: { tafsirSource: null, translationSource: null, fullI3rabSource: null },
      },
      {
        setUiState,
        clearWordSelection: vi.fn(),
        setWord,
        setUrlExplicitSources: setSources,
        clearAyahSelection: vi.fn(),
        setAyah,
      },
    );

    expect(setUiState).toHaveBeenCalledWith('word', 'tafsir', 'segments', null);
    expect(setWord).toHaveBeenCalledWith('2:25:4', true);
  });

  it('does not carry over omitted source params from previous state', () => {
    const setSources = vi.fn();
    const setAyah = vi.fn();

    applyAuthoritativeUrlSnapshot(
      {
        panel: 'ayah',
        ayah: '2:25',
        word: null,
        segment: null,
        ayahTab: 'tafsir',
        wordTab: 'segments',
        sources: { tafsirSource: null, translationSource: null, fullI3rabSource: null },
      },
      {
        selectedAyahKey: '2:25',
        selectedWordLocation: null,
        urlExplicitSources: {
          tafsirSource: 'ar-muyassar',
          translationSource: 'en-sahih-international',
          fullI3rabSource: 'muyassar',
        },
      },
      {
        setUiState: vi.fn(),
        clearWordSelection: vi.fn(),
        setWord: vi.fn(),
        setUrlExplicitSources: setSources,
        clearAyahSelection: vi.fn(),
        setAyah,
      },
    );

    expect(setSources).toHaveBeenCalledWith({
      tafsirSource: null,
      translationSource: null,
      fullI3rabSource: null,
    });
    expect(setAyah).toHaveBeenCalledWith('2:25', true);
  });
});
