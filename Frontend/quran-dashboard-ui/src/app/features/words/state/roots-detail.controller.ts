import { Injectable } from '@angular/core';
import { Observable, Subscription } from 'rxjs';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { RootsApi } from '../data-access/roots.api';
import {
  ROOTS_ERROR_LABEL,
  ROOTS_NOT_FOUND_LABEL,
} from '../models/roots.labels';
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
  isPaginatedRootView,
} from '../models/roots.models';
import { AbstractDetailController } from './abstract-detail.controller';
import { RootsCache, RootsCacheKeys } from './roots-cache';
import {
  buildAyahsPanelUpdate,
  buildDetailErrorUpdate,
  buildLemmasPanelUpdate,
  buildMentionedSurahsPanelUpdate,
  buildMissingSurahsPanelUpdate,
  buildStemsPanelUpdate,
  buildWordsPanelUpdate,
  extractPanelErrorMessage,
  restoredRootNotFoundUpdate,
} from './roots-detail-panel.updates';
import { RootsDetailViewHandlers, RootsDetailViewLoader } from './roots-detail-view.loader';

const INITIAL_PANEL: RootsPanelState = {
  selectedRootId: null,
  summary: null,
  view: DEFAULT_ROOT_VIEW,
  wordView: DEFAULT_ROOT_WORD_VIEW,
  surahView: DEFAULT_ROOT_SURAHS_VIEW,
  ayahTypeCode: null,
  detailPage: DEFAULT_ROOT_DETAIL_PAGE,
  ayahs: null,
  words: null,
  mentionedSurahs: null,
  missingSurahs: null,
  lemmas: null,
  stems: null,
  status: 'idle',
  errorMessage: '',
};

export interface RootsDetailUrlState {
  readonly rootId: number;
  readonly view: RootView;
  readonly wordView: RootWordView;
  readonly surahView: RootSurahView;
  readonly detailPage: number;
  readonly typeCode: string | null;
}

export function rootsDetailUrlStatesEqual(
  current: RootsDetailUrlState | null,
  next: RootsDetailUrlState | null,
): boolean {
  if (current === null || next === null) {
    return current === next;
  }

  return (
    current.rootId === next.rootId &&
    current.view === next.view &&
    current.wordView === next.wordView &&
    current.surahView === next.surahView &&
    current.detailPage === next.detailPage &&
    current.typeCode === next.typeCode
  );
}

@Injectable()
export class RootsDetailController extends AbstractDetailController<
  RootsPanelState,
  RootsDetailUrlState,
  RootSummaryDto,
  RootsDetailViewHandlers
> {
  constructor(
    private readonly api: RootsApi,
    private readonly cache: RootsCache,
    private readonly viewLoader: RootsDetailViewLoader,
  ) {
    super(INITIAL_PANEL);
  }

  selectRoot(summary: RootSummaryDto, view: RootView = DEFAULT_ROOT_VIEW): void {
    this.selectRootWithPanel(summary, view);
  }

  selectRootWithPanel(
    summary: RootSummaryDto,
    view: RootView,
    wordView: RootWordView = DEFAULT_ROOT_WORD_VIEW,
    surahView: RootSurahView = DEFAULT_ROOT_SURAHS_VIEW,
    detailPage: number = DEFAULT_ROOT_DETAIL_PAGE,
    ayahTypeCode: string | null = null,
  ): void {
    const token = this.requests.beginTransition();
    const nextState: RootsDetailUrlState = {
      rootId: summary.id,
      view,
      wordView,
      surahView,
      detailPage,
      typeCode: ayahTypeCode,
    };
    this.activeUrlState = nextState;
    this._panel.set({
      ...INITIAL_PANEL,
      selectedRootId: summary.id,
      summary,
      view,
      wordView,
      surahView,
      ayahTypeCode,
      detailPage,
      status: 'loading',
    });
    this.loadActiveView(nextState, token);
  }

  setAyahTypeCode(typeCode: string | null): void {
    const current = this._panel();
    if (
      current.selectedRootId === null ||
      current.summary === null ||
      (current.view !== 'ayahs' && current.view !== 'words')
    ) {
      return;
    }

    const normalizedTypeCode = this.normalizeTypeCode(typeCode);
    if (normalizedTypeCode === current.ayahTypeCode && current.detailPage === DEFAULT_ROOT_DETAIL_PAGE) {
      return;
    }

    const token = this.requests.beginTransition();
    const nextState: RootsDetailUrlState = {
      rootId: current.selectedRootId,
      view: current.view,
      wordView: current.wordView,
      surahView: current.surahView,
      detailPage: DEFAULT_ROOT_DETAIL_PAGE,
      typeCode: normalizedTypeCode,
    };
    this.activeUrlState = nextState;
    this._panel.update((state) => ({
      ...state,
      ayahTypeCode: normalizedTypeCode,
      detailPage: DEFAULT_ROOT_DETAIL_PAGE,
      status: 'loading',
      errorMessage: '',
    }));
    this.loadActiveView(nextState, token);
  }

  setView(view: RootView): void {
    const current = this._panel();
    if (current.selectedRootId === null || current.summary === null || view === current.view) {
      return;
    }

    const detailPage = DEFAULT_ROOT_DETAIL_PAGE;
    const wordView = view === 'words' ? current.wordView : DEFAULT_ROOT_WORD_VIEW;
    const surahView = view === 'surahs' ? current.surahView : DEFAULT_ROOT_SURAHS_VIEW;
    const typeCode = view === 'ayahs' || view === 'words' ? current.ayahTypeCode : null;

    const token = this.requests.beginTransition();
    const nextState: RootsDetailUrlState = {
      rootId: current.selectedRootId,
      view,
      wordView,
      surahView,
      detailPage,
      typeCode,
    };
    this.activeUrlState = nextState;
    this._panel.update((s) => ({
      ...s,
      view,
      wordView,
      surahView,
      ayahTypeCode: typeCode,
      detailPage,
      status: 'loading',
      errorMessage: '',
    }));
    this.loadActiveView(nextState, token);
  }

  setWordView(wordView: RootWordView): void {
    const current = this._panel();
    if (
      current.selectedRootId === null ||
      current.summary === null ||
      current.view !== 'words' ||
      wordView === current.wordView
    ) {
      return;
    }

    const token = this.requests.beginTransition();
    const nextState: RootsDetailUrlState = {
      rootId: current.selectedRootId,
      view: 'words',
      wordView,
      surahView: current.surahView,
      detailPage: DEFAULT_ROOT_DETAIL_PAGE,
      typeCode: current.ayahTypeCode,
    };
    this.activeUrlState = nextState;
    this._panel.update((s) => ({
      ...s,
      wordView,
      detailPage: DEFAULT_ROOT_DETAIL_PAGE,
      status: 'loading',
      errorMessage: '',
    }));
    this.loadActiveView(nextState, token);
  }

  setSurahView(surahView: RootSurahView): void {
    const current = this._panel();
    if (
      current.selectedRootId === null ||
      current.summary === null ||
      current.view !== 'surahs' ||
      surahView === current.surahView
    ) {
      return;
    }

    const token = this.requests.beginTransition();
    const nextState: RootsDetailUrlState = {
      rootId: current.selectedRootId,
      view: 'surahs',
      wordView: current.wordView,
      surahView,
      detailPage: current.detailPage,
      typeCode: null,
    };
    this.activeUrlState = nextState;
    this._panel.update((s) => ({
      ...s,
      surahView,
      status: 'loading',
      errorMessage: '',
    }));
    this.loadActiveView(nextState, token);
  }

  setDetailPage(page: number): void {
    const current = this._panel();
    if (current.selectedRootId === null || current.summary === null || page < 1) {
      return;
    }

    if (!isPaginatedRootView(current.view)) {
      return;
    }

    const token = this.requests.beginTransition();
    const nextState: RootsDetailUrlState = {
      rootId: current.selectedRootId,
      view: current.view,
      wordView: current.wordView,
      surahView: current.surahView,
      detailPage: page,
      typeCode: current.view === 'ayahs' || current.view === 'words' ? current.ayahTypeCode : null,
    };
    this.activeUrlState = nextState;
    this._panel.update((s) => ({
      ...s,
      detailPage: page,
      status: 'loading',
      errorMessage: '',
    }));
    this.loadActiveView(nextState, token);
  }

  protected override readonly notFoundLabel = ROOTS_NOT_FOUND_LABEL;
  protected override readonly errorLabel = ROOTS_ERROR_LABEL;

  protected override urlStatesEqual(a: RootsDetailUrlState | null, b: RootsDetailUrlState | null): boolean {
    return rootsDetailUrlStatesEqual(a, b);
  }

  protected override sameIdentity(current: RootsPanelState, state: RootsDetailUrlState): boolean {
    return current.selectedRootId === state.rootId && current.summary !== null;
  }

  protected override applyUrlStateFields(panel: RootsPanelState, state: RootsDetailUrlState): RootsPanelState {
    return {
      ...panel,
      selectedRootId: state.rootId,
      view: state.view,
      wordView: state.wordView,
      surahView: state.surahView,
      ayahTypeCode: state.typeCode,
      detailPage: state.detailPage,
    };
  }

  protected override applySummary(state: RootsDetailUrlState, data: RootSummaryDto): Partial<RootsPanelState> {
    return { summary: data, ayahTypeCode: state.typeCode };
  }

  protected override loadSummary(state: RootsDetailUrlState): Observable<ApiResponse<RootSummaryDto>> {
    return this.cache.getOrLoad(RootsCacheKeys.summary(state.rootId), () => this.api.getRootSummary(state.rootId));
  }

  protected override notFoundPanel(state: RootsDetailUrlState, message: string): RootsPanelState {
    return restoredRootNotFoundUpdate(message, ROOTS_NOT_FOUND_LABEL, state.rootId);
  }

  protected override errorPanel(state: RootsDetailUrlState, message: string): RootsPanelState {
    return {
      ...INITIAL_PANEL,
      selectedRootId: state.rootId,
      status: 'error',
      errorMessage: message || ROOTS_ERROR_LABEL,
    };
  }

  protected override extractErrorMessage(err: unknown, fallback: string): string {
    return extractPanelErrorMessage(err, fallback);
  }

  protected override requestActiveView(
    state: RootsDetailUrlState,
    handlers: RootsDetailViewHandlers,
  ): Subscription | undefined {
    const current = this._panel();
    return this.viewLoader.loadActiveView(
      {
        rootId: state.rootId,
        view: state.view,
        wordView: state.wordView,
        surahView: state.surahView,
        ayahTypeCode: state.typeCode,
        detailPage: state.detailPage,
        cachedMissingSurahs: current.missingSurahs,
      },
      handlers,
    );
  }

  protected override buildViewHandlers(token: number): RootsDetailViewHandlers {
    return {
      onAyahs: (response) => this.applyIfCurrent(token, (s) => ({ ...s, ...buildAyahsPanelUpdate(response) })),
      onWords: (response) => this.applyIfCurrent(token, (s) => ({ ...s, ...buildWordsPanelUpdate(response) })),
      onMentionedSurahs: (response) =>
        this.applyIfCurrent(token, (s) => ({ ...s, ...buildMentionedSurahsPanelUpdate(response) })),
      onMissingSurahs: (response) =>
        this.applyIfCurrent(token, (s) => ({ ...s, ...buildMissingSurahsPanelUpdate(response) })),
      onLemmas: (response) => this.applyIfCurrent(token, (s) => ({ ...s, ...buildLemmasPanelUpdate(response) })),
      onStems: (response) => this.applyIfCurrent(token, (s) => ({ ...s, ...buildStemsPanelUpdate(response) })),
      onError: (err) =>
        this.applyIfCurrent(token, (s) => ({ ...s, ...buildDetailErrorUpdate(err, ROOTS_ERROR_LABEL) })),
    };
  }

  private normalizeTypeCode(typeCode: string | null): string | null {
    return typeCode === null || typeCode.trim().length === 0 ? null : typeCode.trim();
  }
}
