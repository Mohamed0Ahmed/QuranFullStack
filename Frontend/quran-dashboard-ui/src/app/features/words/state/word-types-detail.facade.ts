import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, ParamMap } from '@angular/router';
import { Observable, Subscription, of } from 'rxjs';
import { catchError, distinctUntilChanged, map, switchMap, tap } from 'rxjs/operators';

import { ApiResponse } from '../../../core/data-access/api-response.model';
import { WordTypeGroupedSummaryDto, WordTypesApi } from '../data-access/word-types.api';
import { WORD_TYPES_ERROR_LABEL, WORD_TYPES_NOT_FOUND_LABEL } from '../models/word-types.labels';
import {
  DEFAULT_GROUPED_WORD_TYPES_DETAIL_VIEW,
  DEFAULT_WORD_TYPE_CASE,
  DEFAULT_WORD_TYPE_TENSE,
  DEFAULT_WORD_TYPE_VOICE,
  DEFAULT_WORD_TYPES_DETAIL_PAGE,
  DEFAULT_WORD_TYPES_DETAIL_VIEW,
  ParsedWordTypesQuery,
  WordTypeDetailView,
  WordTypeRowDto,
  WordTypeRowIdentity,
  WordTypeSummaryDto,
} from '../models/word-types.models';
import {
  WordTypeDetailScope,
  WordTypeDetailSelection,
  WordTypesDetailState,
} from '../models/word-types-detail.models';
import { parseWordTypesQueryParams } from './word-types-url-sync';
import { WordTypesCache, WordTypesCacheKeys } from './word-types-cache';
import {
  buildAyahsPanelUpdate,
  buildDetailErrorUpdate,
  buildSurahsPanelUpdate,
  buildWordsPanelUpdate,
  extractPanelErrorMessage,
  isPaginatedWordTypeView,
} from './word-types-detail-panel.updates';
import { WordTypesDetailViewLoader, toGroupedRequest } from './word-types-detail-view.loader';

const INITIAL_PANEL: WordTypesDetailState = {
  status: 'idle',
  selection: null,
  kind: 'word',
  selectedRow: null,
  view: DEFAULT_WORD_TYPES_DETAIL_VIEW,
  detailPage: DEFAULT_WORD_TYPES_DETAIL_PAGE,
  location: null,
  summary: null,
  groupedSummary: null,
  words: null,
  ayahs: null,
  surahs: null,
  errorMessage: '',
};

interface PanelUrlState {
  readonly selection: WordTypeDetailSelection;
  readonly view: WordTypeDetailView;
  readonly detailPage: number;
  readonly location: string | null;
}

interface SummaryDescriptor {
  readonly key: string;
  readonly load: () => Observable<ApiResponse<WordTypeSummaryDto | WordTypeGroupedSummaryDto>>;
  readonly apply: (data: WordTypeSummaryDto | WordTypeGroupedSummaryDto) => Partial<WordTypesDetailState>;
}

@Injectable({ providedIn: 'root' })
export class WordTypesDetailFacade {
  private readonly api = inject(WordTypesApi);
  private readonly cache = inject(WordTypesCache);
  private readonly viewLoader = inject(WordTypesDetailViewLoader);

  private readonly _panel = signal<WordTypesDetailState>(INITIAL_PANEL);
  private routeSub?: Subscription;
  private detailSub?: Subscription;
  private summarySub?: Subscription;
  private activeUrlState: PanelUrlState | null = null;
  // Monotonic guard: only the newest summary/detail load may write state, so a late non-cancellable
  // response cannot overwrite a newer kind/scope/view/page.
  private generation = 0;

  readonly panelState = computed(() => this._panel());
  readonly selection = computed(() => this._panel().selection);
  readonly kind = computed(() => this._panel().kind);
  readonly view = computed(() => this._panel().view);
  readonly detailPage = computed(() => this._panel().detailPage);
  readonly status = computed(() => this._panel().status);

  bindToRoute(route: ActivatedRoute): void {
    this.unbindFromRoute();
    this.routeSub = route.queryParamMap
      .pipe(
        map((params) => this.toPanelUrlState(params)),
        distinctUntilChanged((a, b) => this.isSamePanelUrlState(a, b)),
        switchMap((state) => this.syncFromUrlState(state)),
      )
      .subscribe();
  }

  unbindFromRoute(): void {
    this.routeSub?.unsubscribe();
    this.routeSub = undefined;
    this.summarySub?.unsubscribe();
    this.detailSub?.unsubscribe();
    this.summarySub = undefined;
    this.detailSub = undefined;
  }

  // Optimistic word selection from a table row: the row already carries its summary, so only the
  // active detail view is fetched.
  selectRow(
    row: WordTypeRowDto,
    scope: WordTypeDetailScope,
    view: WordTypeDetailView = DEFAULT_WORD_TYPES_DETAIL_VIEW,
  ): void {
    const identity = toIdentity(row);
    const selection: WordTypeDetailSelection = { kind: 'word', identity, scope };
    this.activeUrlState = { selection, view, detailPage: DEFAULT_WORD_TYPES_DETAIL_PAGE, location: null };
    this._panel.set({
      ...INITIAL_PANEL,
      selection,
      kind: 'word',
      selectedRow: identity,
      summary: row,
      view,
      status: 'loading',
    });
    this.loadActiveView(selection, view, DEFAULT_WORD_TYPES_DETAIL_PAGE);
  }

  // General selection (word or grouped): the summary is fetched for the selected identity/scope.
  select(selection: WordTypeDetailSelection, view?: WordTypeDetailView): void {
    const resolvedView = view ?? defaultViewForSelection(selection);
    const state: PanelUrlState = { selection, view: resolvedView, detailPage: DEFAULT_WORD_TYPES_DETAIL_PAGE, location: null };
    this.activeUrlState = state;
    this._panel.set({
      ...INITIAL_PANEL,
      selection,
      kind: selection.kind,
      selectedRow: selectedRowOf(selection),
      view: resolvedView,
      status: 'loading',
    });
    this.summarySub?.unsubscribe();
    this.summarySub = this.loadSummaryAndRestore(state).subscribe();
  }

  clearSelection(): void {
    this.summarySub?.unsubscribe();
    this.detailSub?.unsubscribe();
    this.summarySub = undefined;
    this.detailSub = undefined;
    this.activeUrlState = null;
    this.generation++;
    this._panel.set(INITIAL_PANEL);
  }

  setView(view: WordTypeDetailView): void {
    const current = this._panel();
    if (current.selection === null || !hasSummary(current) || view === current.view) {
      return;
    }
    if (view === 'words' && current.selection.kind === 'word') {
      return;
    }

    const detailPage = DEFAULT_WORD_TYPES_DETAIL_PAGE;
    this.activeUrlState = { selection: current.selection, view, detailPage, location: null };
    this._panel.update((state) => ({ ...state, view, detailPage, status: 'loading', errorMessage: '' }));
    this.loadActiveView(current.selection, view, detailPage);
  }

  setDetailPage(page: number): void {
    const current = this._panel();
    if (current.selection === null || !hasSummary(current) || page < 1 || !isPaginatedWordTypeView(current.view)) {
      return;
    }

    this.activeUrlState = {
      selection: current.selection,
      view: current.view,
      detailPage: page,
      location: current.location,
    };
    this._panel.update((state) => ({ ...state, detailPage: page, status: 'loading', errorMessage: '' }));
    this.loadActiveView(current.selection, current.view, page);
  }

  // Reloads the summary when it never arrived, otherwise reloads the active view. Failed reads are
  // never cached, so a retry always re-issues the request.
  retry(): void {
    const current = this._panel();
    const state = this.activeUrlState;
    if (current.selection === null || state === null) {
      return;
    }

    this._panel.update((panel) => ({ ...panel, status: 'loading', errorMessage: '' }));

    if (!hasSummary(current)) {
      this.summarySub?.unsubscribe();
      this.summarySub = this.loadSummaryAndRestore(state).subscribe();
      return;
    }

    this.loadActiveView(state.selection, state.view, state.detailPage);
  }

  private toPanelUrlState(params: ParamMap): PanelUrlState | null {
    const parsed = parseWordTypesQueryParams(params);
    const selection = toSelection(parsed);
    if (selection === null) {
      return null;
    }

    return {
      selection,
      view: parsed.view,
      detailPage: parsed.detailPage,
      location: parsed.location,
    };
  }

  private syncFromUrlState(state: PanelUrlState | null): Observable<void> {
    if (state === null) {
      this.clearSelection();
      return of(undefined);
    }

    if (this.isSamePanelUrlState(this.activeUrlState, state)) {
      return of(undefined);
    }

    this.activeUrlState = state;
    const current = this._panel();

    if (isSameSelection(current.selection, state.selection) && hasSummary(current)) {
      this._panel.update((panel) => ({
        ...panel,
        selection: state.selection,
        view: state.view,
        detailPage: state.detailPage,
        location: state.location,
        status: 'loading',
        errorMessage: '',
      }));
      this.loadActiveView(state.selection, state.view, state.detailPage);
      return of(undefined);
    }

    this._panel.set({
      ...INITIAL_PANEL,
      selection: state.selection,
      kind: state.selection.kind,
      selectedRow: selectedRowOf(state.selection),
      view: state.view,
      detailPage: state.detailPage,
      location: state.location,
      status: 'loading',
    });

    return this.loadSummaryAndRestore(state);
  }

  private loadSummaryAndRestore(state: PanelUrlState): Observable<void> {
    const generation = ++this.generation;
    const descriptor = this.summaryDescriptor(state.selection);

    return this.cache.getOrLoad(descriptor.key, descriptor.load).pipe(
      tap((response) => {
        if (generation !== this.generation) {
          return;
        }
        if (!response.isSuccess || !response.data) {
          this.applySelectionNotFound(state, response.message ?? '');
          return;
        }

        this._panel.update((panel) => ({
          ...panel,
          ...descriptor.apply(response.data!),
          status: 'loading',
          errorMessage: '',
        }));
        this.loadActiveView(state.selection, state.view, state.detailPage);
      }),
      catchError((err) => {
        if (generation === this.generation) {
          if (err instanceof HttpErrorResponse && err.status === 404) {
            this.applySelectionNotFound(state, extractPanelErrorMessage(err, WORD_TYPES_NOT_FOUND_LABEL));
          } else {
            this.applySelectionError(state, extractPanelErrorMessage(err, WORD_TYPES_ERROR_LABEL));
          }
        }
        return of(undefined);
      }),
      map(() => undefined),
    );
  }

  private summaryDescriptor(selection: WordTypeDetailSelection): SummaryDescriptor {
    if (selection.kind === 'word') {
      const identity = selection.identity;
      return {
        key: WordTypesCacheKeys.summary(identity),
        load: () => this.api.getSummary(identity),
        apply: (data) => ({ summary: data as WordTypeSummaryDto, groupedSummary: null }),
      };
    }

    const request = toGroupedRequest(selection);
    return {
      key: WordTypesCacheKeys.groupedSummary(request),
      load: () => this.api.getGroupedSummary(request),
      apply: (data) => ({ groupedSummary: data as WordTypeGroupedSummaryDto, summary: null }),
    };
  }

  private loadActiveView(selection: WordTypeDetailSelection, view: WordTypeDetailView, detailPage: number): void {
    this.detailSub?.unsubscribe();
    const generation = ++this.generation;

    this.detailSub = this.viewLoader.loadActiveView(
      { selection, view, detailPage },
      {
        onWords: (response) => this.applyIfCurrent(generation, (panel) => ({ ...panel, ...buildWordsPanelUpdate(response) })),
        onAyahs: (response) => this.applyIfCurrent(generation, (panel) => ({ ...panel, ...buildAyahsPanelUpdate(response) })),
        onSurahs: (response) => this.applyIfCurrent(generation, (panel) => ({ ...panel, ...buildSurahsPanelUpdate(response) })),
        onError: (err) => this.applyIfCurrent(generation, (panel) => ({ ...panel, ...buildDetailErrorUpdate(err, WORD_TYPES_ERROR_LABEL) })),
      },
    );
  }

  private applyIfCurrent(
    generation: number,
    update: (panel: WordTypesDetailState) => WordTypesDetailState,
  ): void {
    if (generation !== this.generation) {
      return;
    }
    this._panel.update(update);
  }

  private applySelectionNotFound(state: PanelUrlState, message: string): void {
    this._panel.set({
      ...INITIAL_PANEL,
      selection: state.selection,
      kind: state.selection.kind,
      selectedRow: selectedRowOf(state.selection),
      view: state.view,
      detailPage: state.detailPage,
      location: state.location,
      status: 'notFound',
      errorMessage: message || WORD_TYPES_NOT_FOUND_LABEL,
    });
  }

  private applySelectionError(state: PanelUrlState, message: string): void {
    this._panel.set({
      ...INITIAL_PANEL,
      selection: state.selection,
      kind: state.selection.kind,
      selectedRow: selectedRowOf(state.selection),
      view: state.view,
      detailPage: state.detailPage,
      location: state.location,
      status: 'error',
      errorMessage: message || WORD_TYPES_ERROR_LABEL,
    });
  }

  private isSamePanelUrlState(current: PanelUrlState | null, next: PanelUrlState | null): boolean {
    if (current === null || next === null) {
      return current === next;
    }

    return (
      isSameSelection(current.selection, next.selection)
      && current.view === next.view
      && current.detailPage === next.detailPage
      && current.location === next.location
    );
  }
}

function hasSummary(state: WordTypesDetailState): boolean {
  return state.summary !== null || state.groupedSummary !== null;
}

function toSelection(parsed: ParsedWordTypesQuery): WordTypeDetailSelection | null {
  const scope = scopeFrom(parsed);
  if (scope === null) {
    return null;
  }

  const selections: WordTypeDetailSelection[] = [];
  if (parsed.word !== null && parsed.contextCode.length > 0) {
    selections.push({
      kind: 'word',
      identity: {
        tashkeelWordId: parsed.word,
        contextCode: parsed.contextCode,
        case: scope.case,
        tense: scope.tense,
        voice: scope.voice,
      },
      scope,
    });
  }
  if (parsed.root !== null) selections.push({ kind: 'root', rootId: parsed.root, scope });
  if (parsed.stem !== null) selections.push({ kind: 'stem', stemId: parsed.stem, scope });
  if (parsed.lemma !== null) selections.push({ kind: 'lemma', lemmaId: parsed.lemma, scope });
  return selections.length === 1 ? selections[0] : null;
}

function scopeFrom(parsed: ParsedWordTypesQuery): WordTypeDetailScope | null {
  if (
    parsed.detailType === null
    || parsed.detailCase === null
    || parsed.detailTense === null
    || parsed.detailVoice === null
  ) {
    return null;
  }

  return {
    type: parsed.detailType,
    childCode: parsed.detailChildCode,
    case: parsed.detailCase,
    tense: parsed.detailTense,
    voice: parsed.detailVoice,
  };
}

function selectedRowOf(selection: WordTypeDetailSelection): WordTypeRowIdentity | null {
  return selection.kind === 'word' ? selection.identity : null;
}

function isSameSelection(current: WordTypeDetailSelection | null, next: WordTypeDetailSelection): boolean {
  if (current === null || current.kind !== next.kind) {
    return false;
  }

  if (current.kind === 'word' && next.kind === 'word') {
    return isSameIdentity(current.identity, next.identity) && isSameScope(current.scope, next.scope);
  }
  if (current.kind !== 'word' && next.kind !== 'word') {
    return dimensionIdOf(current) === dimensionIdOf(next) && isSameScope(current.scope, next.scope);
  }
  return false;
}

function dimensionIdOf(selection: WordTypeDetailSelection): number {
  switch (selection.kind) {
    case 'root':
      return selection.rootId;
    case 'stem':
      return selection.stemId;
    case 'lemma':
      return selection.lemmaId;
    case 'word':
      return selection.identity.tashkeelWordId;
  }
}

function isSameScope(current: WordTypeDetailScope, next: WordTypeDetailScope): boolean {
  return (
    current.type === next.type
    && current.childCode === next.childCode
    && current.case === next.case
    && current.tense === next.tense
    && current.voice === next.voice
  );
}

function isSameIdentity(current: WordTypeRowIdentity, next: WordTypeRowIdentity): boolean {
  return (
    current.tashkeelWordId === next.tashkeelWordId
    && current.contextCode === next.contextCode
    && current.case === next.case
    && current.tense === next.tense
    && current.voice === next.voice
  );
}

function defaultViewForSelection(selection: WordTypeDetailSelection): WordTypeDetailView {
  return selection.kind === 'word' ? DEFAULT_WORD_TYPES_DETAIL_VIEW : DEFAULT_GROUPED_WORD_TYPES_DETAIL_VIEW;
}

function toIdentity(row: WordTypeRowIdentity): WordTypeRowIdentity {
  return {
    tashkeelWordId: row.tashkeelWordId,
    contextCode: row.contextCode,
    case: row.case ?? DEFAULT_WORD_TYPE_CASE,
    tense: row.tense ?? DEFAULT_WORD_TYPE_TENSE,
    voice: row.voice ?? DEFAULT_WORD_TYPE_VOICE,
  };
}
