import { Injectable, computed, signal } from '@angular/core';

import {
  DEFAULT_ROOT_DETAIL_PAGE,
  DEFAULT_ROOT_SURAHS_VIEW,
  DEFAULT_ROOT_VIEW,
  DEFAULT_ROOT_WORD_VIEW,
  RootSummaryDto,
  RootSurahView,
  RootView,
  RootWordView,
  RootsPanelState,
} from '../models/roots.models';

const INITIAL_PANEL: RootsPanelState = {
  selectedRootId: null,
  summary: null,
  view: DEFAULT_ROOT_VIEW,
  wordView: DEFAULT_ROOT_WORD_VIEW,
  surahView: DEFAULT_ROOT_SURAHS_VIEW,
  detailPage: DEFAULT_ROOT_DETAIL_PAGE,
  status: 'idle',
  errorMessage: '',
};

/**
 * Roots Explorer (Feature 015) persistent detail-panel facade. Modeled on
 * `UniqueWordsDrilldownFacade`, but the detail surface is a **persistent side
 * panel**, not a modal: there is no `isOpen`/modal-close. Selection drives
 * visibility — when `selectedRootId` is null the panel shows the empty-selection
 * state (`اختر جذرًا لعرض تفاصيله`).
 *
 * Foundational skeleton: owns the panel signals and exposes per-view load stubs;
 * the ayahs load lands in US2 (T039), words in US3 (T049), surahs in US4 (T058),
 * lemmas/stems in US5 (T066).
 */
@Injectable({ providedIn: 'root' })
export class RootsDetailFacade {
  private readonly _panel = signal<RootsPanelState>(INITIAL_PANEL);

  readonly panelState = computed(() => this._panel());

  readonly selectedRootId = computed(() => this._panel().selectedRootId);
  readonly view = computed(() => this._panel().view);
  readonly status = computed(() => this._panel().status);

  // --- Selection + URL restore (US1/US2) ---

  /** Selects a root, setting the default view (ayahs). Implemented across stories. */
  selectRoot(_summary: RootSummaryDto, _view: RootView = DEFAULT_ROOT_VIEW): void {
    // US1/T039: set selection + default view, load the active view.
  }

  /** Restores panel state from parsed URL params. Implemented across stories. */
  restoreFromUrl(
    _rootId: number | null,
    _view: RootView,
    _wordView: RootWordView,
    _surahView: RootSurahView,
    _detailPage: number,
  ): void {
    // US1/T039: load summary + active view from URL.
  }

  /** Clears the selection, returning to the empty-selection state. */
  clearSelection(): void {
    this._panel.set(INITIAL_PANEL);
  }

  /** Sets the active panel tab. Implemented across stories. */
  setView(_view: RootView): void {
    // US2+: switch view + lazy-load.
  }
}
