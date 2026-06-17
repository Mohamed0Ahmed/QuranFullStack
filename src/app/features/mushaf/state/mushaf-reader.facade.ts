import { Injectable, signal, computed } from '@angular/core';

import {
  AyahStudyViewModel,
  DEFAULT_MUSHAF_READER_STATE,
  MushafPageViewModel,
  MushafReaderState,
  ResourceLoadState,
  WordAnalysisViewModel,
} from '../models/mushaf.models';

/**
 * Mushaf reader page-state facade.
 *
 * Owns all reader view state (selections, sources, tabs, and per-resource
 * loading/empty/error primitives). It is the single source of truth the shell
 * and child components render from; components never call APIs directly.
 *
 * Phase 2 skeleton: state holders only. Load methods (`loadPage`,
 * `loadAyahStudy`, `loadWordAnalysis`, source setters, and URL<->state sync)
 * are added by their stories (T025 / T035 / T044 / T048).
 */
@Injectable({ providedIn: 'root' })
export class MushafReaderFacade {
  private readonly _pageNumber = signal(DEFAULT_MUSHAF_READER_STATE.pageNumber);
  private readonly _selectedAyahKey = signal(DEFAULT_MUSHAF_READER_STATE.selectedAyahKey);
  private readonly _selectedWordLocation = signal(DEFAULT_MUSHAF_READER_STATE.selectedWordLocation);
  private readonly _selectedSegmentLocation = signal(DEFAULT_MUSHAF_READER_STATE.selectedSegmentLocation);
  private readonly _panel = signal(DEFAULT_MUSHAF_READER_STATE.panel);
  private readonly _ayahTab = signal(DEFAULT_MUSHAF_READER_STATE.ayahTab);
  private readonly _wordTab = signal(DEFAULT_MUSHAF_READER_STATE.wordTab);
  private readonly _sources = signal(DEFAULT_MUSHAF_READER_STATE.sources);

  private readonly _page = signal<MushafPageViewModel | null>(null);
  private readonly _ayahStudy = signal<AyahStudyViewModel | null>(null);
  private readonly _wordAnalysis = signal<WordAnalysisViewModel | null>(null);

  private readonly _pageLoadState = signal<ResourceLoadState>(DEFAULT_MUSHAF_READER_STATE.page);
  private readonly _ayahStudyLoadState = signal<ResourceLoadState>(DEFAULT_MUSHAF_READER_STATE.ayahStudy);
  private readonly _wordAnalysisLoadState = signal<ResourceLoadState>(DEFAULT_MUSHAF_READER_STATE.wordAnalysis);

  readonly pageNumber = this._pageNumber.asReadonly();
  readonly selectedAyahKey = this._selectedAyahKey.asReadonly();
  readonly selectedWordLocation = this._selectedWordLocation.asReadonly();
  readonly selectedSegmentLocation = this._selectedSegmentLocation.asReadonly();
  readonly panel = this._panel.asReadonly();
  readonly ayahTab = this._ayahTab.asReadonly();
  readonly wordTab = this._wordTab.asReadonly();
  readonly sources = this._sources.asReadonly();

  readonly page = this._page.asReadonly();
  readonly ayahStudy = this._ayahStudy.asReadonly();
  readonly wordAnalysis = this._wordAnalysis.asReadonly();

  readonly pageLoadState = this._pageLoadState.asReadonly();
  readonly ayahStudyLoadState = this._ayahStudyLoadState.asReadonly();
  readonly wordAnalysisLoadState = this._wordAnalysisLoadState.asReadonly();

  /** Aggregate reader state (mirrors the URL<->state contract). */
  readonly state = computed<MushafReaderState>(() => ({
    pageNumber: this._pageNumber(),
    selectedAyahKey: this._selectedAyahKey(),
    selectedWordLocation: this._selectedWordLocation(),
    selectedSegmentLocation: this._selectedSegmentLocation(),
    panel: this._panel(),
    ayahTab: this._ayahTab(),
    wordTab: this._wordTab(),
    sources: this._sources(),
    page: this._pageLoadState(),
    ayahStudy: this._ayahStudyLoadState(),
    wordAnalysis: this._wordAnalysisLoadState(),
  }));
}
