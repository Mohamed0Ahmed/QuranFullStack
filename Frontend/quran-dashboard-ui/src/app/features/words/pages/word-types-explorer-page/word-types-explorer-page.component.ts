import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, computed, inject, signal, viewChild } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { Subscription } from 'rxjs';

import { PaginationComponent } from '../../../../shared/ui/pagination/pagination.component';
import { QD_BP_DESKTOP_MIN_QUERY } from '../../../../shared/layout/breakpoints';
import { AyahMatchesListComponent } from '../../components/ayah-matches-list/ayah-matches-list.component';
import { MissingSurahsListComponent } from '../../components/missing-surahs-list/missing-surahs-list.component';
import { SurahOccurrencesListComponent } from '../../components/surah-occurrences-list/surah-occurrences-list.component';
import { WordTypeDetailsPanelComponent } from '../../components/word-type-details-panel/word-type-details-panel.component';
import { WordTypeFilterComponent, WordTypeScopeSelectedEvent } from '../../components/word-type-filter/word-type-filter.component';
import { WordTypeGroupedWordsListComponent } from '../../components/word-type-grouped-words-list/word-type-grouped-words-list.component';
import { WordTypeScopeCountsComponent } from '../../components/word-type-scope-counts/word-type-scope-counts.component';
import { WordTypeTableViewTabsComponent } from '../../components/word-type-table-view-tabs/word-type-table-view-tabs.component';
import { ExplorerToolbarComponent } from '../../components/explorer-toolbar/explorer-toolbar.component';
import { WordsExplainerComponent } from '../../components/words-explainer/words-explainer.component';
import { WordsLocalNavComponent } from '../../components/words-local-nav/words-local-nav.component';
import {
  WordTypePresenceFlagChange,
  WordTypesPresenceFilterComponent,
} from '../../components/word-types-presence-filter/word-types-presence-filter.component';
import {
  WordTypeCountOpenedEvent,
  WordTypeTableFocusColumn,
  WordTypesTableComponent,
} from '../../components/word-types-table/word-types-table.component';
import {
  WORD_TYPE_SORT_OPTIONS,
  WORD_TYPE_DETAIL_PRESENTATIONS,
  WORD_TYPE_TABLE_VIEW_EMPTY_LABELS,
  WORD_TYPE_TABLE_VIEW_TABLE_LABELS,
  WORD_TYPES_ERROR_LABEL,
  WORD_TYPES_PAGE_TITLE,
  WORD_TYPES_RETRY_LABEL,
  WORD_TYPES_SEARCH_LABEL,
  WORD_TYPES_SEARCH_PLACEHOLDER,
  WORD_TYPES_SELECT_SUBTYPE_LABEL,
  WORD_TYPES_SORT_LABEL,
} from '../../models/word-types.labels';
import { sortQueryValue } from '../../models/explorer-sort';
import { WORDS_EXPLAINER_CONTENT } from '../../models/words-explainer.content';
import { WordsExplainerPreference } from '../../state/words-explainer-preference';
import { WordsDebouncedSearchDraftController } from '../../state/words-debounced-search-draft.controller';
import {
  DEFAULT_WORD_TYPES_DETAIL_PAGE,
  DEFAULT_WORD_TYPE_SORT,
  LemmaTableRowDto,
  RootTableRowDto,
  StemTableRowDto,
  WORD_TYPES_PAGE_SIZE,
  WordTableRowDto,
  WordTypeCase,
  WordTypeDetailView,
  WordTypeSort,
  WordTypeTableRowDto,
  WordTypeTableView,
  WordTypeTense,
  WordTypeVoice,
  normalizeWordTableRow,
  normalizeWordTypeSort,
} from '../../models/word-types.models';
import {
  WordTypeDetailScope,
  WordTypeDetailSelection,
  WordTypeDetailSelectionKind,
} from '../../models/word-types-detail.models';
import { WordTypesDetailFacade } from '../../state/word-types-detail.facade';
import { WordTypesExplorerFacade } from '../../state/word-types-explorer.facade';
import {
  buildWordTypesQueryParams,
  canonicalWordTypesDetailPage,
  clearWordTypesSelection,
  parseWordTypesQueryParams,
} from '../../state/word-types-url-sync';
import {
  EMPTY_WORD_TYPE_AYAHS_PAGE,
  EMPTY_WORD_TYPE_MEMBER_WORDS_PAGE,
  wordTypeAyahParentFrame,
  wordTypeAyahsPageView,
  wordTypeDetailSummaryView,
  wordTypeLinkingSource,
  wordTypeMemberWordsPageView,
  wordTypeMentionedSurahViews,
  wordTypeMissingSurahViews,
} from './word-types-detail-panel.view-model';
import {
  buildWordTypeTableDetailQuery,
  defaultWordTypeTableDetailView,
} from './word-types-table-detail-selection';

const DETAIL_KIND_BY_TABLE_VIEW: Record<WordTypeTableView, WordTypeDetailSelectionKind> = {
  words: 'word',
  roots: 'root',
  stems: 'stem',
  lemmas: 'lemma',
};

@Component({
  selector: 'qd-word-types-explorer-page',
  standalone: true,
  imports: [
    AyahMatchesListComponent,
    ExplorerToolbarComponent,
    MissingSurahsListComponent,
    NgTemplateOutlet,
    PaginationComponent,
    SurahOccurrencesListComponent,
    WordTypeDetailsPanelComponent,
    WordTypeFilterComponent,
    WordTypeGroupedWordsListComponent,
    WordTypeScopeCountsComponent,
    WordTypeTableViewTabsComponent,
    WordTypesPresenceFilterComponent,
    WordTypesTableComponent,
    WordsExplainerComponent,
    WordsLocalNavComponent,
  ],
  templateUrl: './word-types-explorer-page.component.html',
  styleUrl: './word-types-explorer-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WordTypesExplorerPageComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly explorerFacade = inject(WordTypesExplorerFacade);
  private readonly detailFacade = inject(WordTypesDetailFacade);
  private readonly explainerPreference = inject(WordsExplainerPreference);
  private readonly searchDraftController = WordsDebouncedSearchDraftController.create('types', (value) => this.updateQueryParams({ search: value || null, page: null }));

  private desktopQuery?: MediaQueryList;
  private readonly onDesktopChange = (event: MediaQueryListEvent): void => this.isDesktop.set(event.matches);

  private querySyncSub?: Subscription;

  protected readonly pageSize = WORD_TYPES_PAGE_SIZE;
  protected readonly listState = this.explorerFacade.listState;
  protected readonly scopeCountsState = this.explorerFacade.scopeCountsState;
  protected readonly panelState = this.detailFacade.panelState;
  protected readonly isDesktop = signal(true);
  protected readonly searchDraft = this.searchDraftController.draft;
  private readonly filter = viewChild(WordTypeFilterComponent);
  private readonly table = viewChild(WordTypesTableComponent);

  protected readonly selectedRow = computed<WordTypeTableRowDto | null>(() => {
    const state = this.listState();
    const rows = state.rows;
    const selection = this.panelState().selection;
    if (!rows || selection === null || !this.isSameScope(selection.scope, this.currentScope())) {
      return null;
    }

    switch (state.query.tableView) {
      case 'words':
        return selection.kind !== 'word'
          ? null
          : rows.items.find((row): row is WordTableRowDto => row.kind === 'word' && this.matchesWordIdentity(row, selection)) ?? null;
      case 'roots':
        return selection.kind !== 'root'
          ? null
          : rows.items.find((row): row is RootTableRowDto => row.kind === 'root' && row.rootId === selection.rootId) ?? null;
      case 'stems':
        return selection.kind !== 'stem'
          ? null
          : rows.items.find((row): row is StemTableRowDto => row.kind === 'stem' && row.stemId === selection.stemId) ?? null;
      case 'lemmas':
        return selection.kind !== 'lemma'
          ? null
          : rows.items.find((row): row is LemmaTableRowDto => row.kind === 'lemma' && row.lemmaId === selection.lemmaId) ?? null;
    }
  });

  protected readonly emptySelection = computed(() => this.panelState().selection === null);
  protected readonly detailKind = computed<WordTypeDetailSelectionKind>(() =>
    this.panelState().selection?.kind
      ?? DETAIL_KIND_BY_TABLE_VIEW[this.listState().query.tableView],
  );
  protected readonly detailEmptyLabel = computed(() =>
    WORD_TYPE_DETAIL_PRESENTATIONS[this.detailKind()].emptyViewLabels[this.panelState().view],
  );

  protected readonly emptyAyahsPage = EMPTY_WORD_TYPE_AYAHS_PAGE;
  protected readonly emptyMemberWordsPage = EMPTY_WORD_TYPE_MEMBER_WORDS_PAGE;
  protected readonly activeSummary = computed(() => wordTypeDetailSummaryView(this.panelState()));
  protected readonly memberWordsForView = computed(() => wordTypeMemberWordsPageView(this.panelState()));
  protected readonly ayahsPageForView = computed(() => wordTypeAyahsPageView(this.panelState()));
  protected readonly ayahParentFrame = computed(() => wordTypeAyahParentFrame(this.panelState()));
  protected readonly linkingSource = computed(() => wordTypeLinkingSource(this.panelState()));

  protected get pageTitle() { return WORD_TYPES_PAGE_TITLE; }
  protected get explainer() { return WORDS_EXPLAINER_CONTENT['word-types']; }
  protected readonly explainerExpanded = signal(this.explainerPreference.isExpanded('word-types'));
  protected get emptyLabel() { return WORD_TYPE_TABLE_VIEW_EMPTY_LABELS[this.listState().query.tableView]; }
  protected get selectSubtypeLabel() { return WORD_TYPES_SELECT_SUBTYPE_LABEL; }
  protected get errorLabel() { return WORD_TYPES_ERROR_LABEL; }
  protected get retryLabel() { return WORD_TYPES_RETRY_LABEL; }
  protected get tableLabel() { return WORD_TYPE_TABLE_VIEW_TABLE_LABELS[this.listState().query.tableView]; }
  protected get sortLabel() { return WORD_TYPES_SORT_LABEL; }
  protected get sortOptions() { return WORD_TYPE_SORT_OPTIONS; }
  protected get searchLabel() { return WORD_TYPES_SEARCH_LABEL; }
  protected get searchPlaceholder() { return WORD_TYPES_SEARCH_PLACEHOLDER; }

  protected onPresenceFlagChange(change: WordTypePresenceFlagChange): void {
    this.explorerFacade.selectPresenceFlag(change.dimension, change.value);
  }

  protected onExplainerToggled(expanded: boolean): void {
    this.explainerExpanded.set(expanded);
    this.explainerPreference.setExpanded('word-types', expanded);
  }

  protected onScopeCountsRetry(): void {
    this.explorerFacade.retryScopeCounts();
  }

  ngOnInit(): void {
    this.searchDraftController.start();
    this.explorerFacade.bindToRoute(this.route);
    this.detailFacade.bindToRoute(this.route);

    this.querySyncSub = this.route.queryParamMap
      .subscribe((params) => this.searchDraftController.syncCommitted(parseWordTypesQueryParams(params).search ?? ''));

    if (typeof window !== 'undefined' && typeof window.matchMedia === 'function') {
      this.desktopQuery = window.matchMedia(QD_BP_DESKTOP_MIN_QUERY);
      this.isDesktop.set(this.desktopQuery.matches);
      this.desktopQuery.addEventListener('change', this.onDesktopChange);
    }
  }

  ngOnDestroy(): void {
    this.explorerFacade.unbindFromRoute();
    this.detailFacade.unbindFromRoute();
    this.searchDraftController.destroy();
    this.querySyncSub?.unsubscribe();
    this.desktopQuery?.removeEventListener('change', this.onDesktopChange);
  }

  protected onSearchInput(value: string): void {
    this.searchDraftController.update(value);
  }

  protected selectScope(event: WordTypeScopeSelectedEvent): void {
    this.explorerFacade.selectScope(event.type, event.childCode);
  }

  protected selectCase(caseValue: WordTypeCase): void {
    this.explorerFacade.selectCase(caseValue);
  }

  protected selectTense(tense: WordTypeTense): void {
    this.explorerFacade.selectTense(tense);
  }

  protected selectVoice(voice: WordTypeVoice): void {
    this.explorerFacade.selectVoice(voice);
  }

  protected selectTableView(view: WordTypeTableView): void {
    this.explorerFacade.selectTableView(view);
  }

  private currentScope(): WordTypeDetailScope {
    const query = this.listState().query;
    return {
      type: query.type,
      childCode: query.childCode,
      case: query.case,
      tense: query.tense,
      voice: query.voice,
    };
  }

  private matchesWordIdentity(row: WordTableRowDto, selection: Extract<WordTypeDetailSelection, { kind: 'word' }>): boolean {
    const identity = normalizeWordTableRow(row);
    return identity.tashkeelWordId === selection.identity.tashkeelWordId
      && identity.contextCode === selection.identity.contextCode
      && identity.case === selection.identity.case
      && identity.tense === selection.identity.tense
      && identity.voice === selection.identity.voice;
  }

  private isSameScope(current: WordTypeDetailScope, next: WordTypeDetailScope): boolean {
    return current.type === next.type
      && current.childCode === next.childCode
      && current.case === next.case
      && current.tense === next.tense
      && current.voice === next.voice;
  }

  protected onCountOpened(event: WordTypeCountOpenedEvent): void {
    this.openTableDetail(event.row, event.view, event.column);
  }

  protected onRowSelected(row: WordTypeTableRowDto): void {
    this.openTableDetail(row, defaultWordTypeTableDetailView(row), 'identity');
  }

  private openTableDetail(
    row: WordTypeTableRowDto,
    view: WordTypeDetailView,
    column: WordTypeTableFocusColumn,
  ): void {
    const scope = this.currentScope();
    if (row.kind === 'word') {
      this.detailFacade.selectRow(normalizeWordTableRow(row), scope, view);
    } else {
      this.detailFacade.selectGroupedRow(row, scope, view);
    }
    this.updateQueryParams(buildWordTypeTableDetailQuery(row, scope, view, column));
  }

  protected onPanelViewChange(view: WordTypeDetailView): void {
    this.detailFacade.setView(view);
    this.updateQueryParams(buildWordTypesQueryParams({
      view,
      detailPage: canonicalWordTypesDetailPage(view, DEFAULT_WORD_TYPES_DETAIL_PAGE),
    }));
  }

  protected onDetailPageChange(page: number): void {
    this.detailFacade.setDetailPage(page);
    this.updateQueryParams(buildWordTypesQueryParams({
      detailPage: canonicalWordTypesDetailPage(this.panelState().view, page),
    }));
  }

  protected clearSelection(): void {
    const selectedRow = this.selectedRow();
    const view = this.panelState().view;
    const column = this.toFocusColumn(this.listState().query.column);
    this.detailFacade.clearSelection();
    this.updateQueryParams(clearWordTypesSelection());

    if (selectedRow) {
      this.table()?.focusTarget(selectedRow, view, column);
      return;
    }

    this.filter()?.focusSelectedType();
  }

  protected onSortChange(sort: WordTypeSort | null): void {
    this.explorerFacade.changeSort(sort);
  }

  protected onSortSelect(value: string): void {
    this.onSortChange(sortQueryValue(normalizeWordTypeSort(value), DEFAULT_WORD_TYPE_SORT));
  }

  protected changePage(page: number): void {
    this.explorerFacade.changePage(page);
  }

  protected retryList(): void {
    this.explorerFacade.retryList();
  }

  protected retryDetail(): void {
    this.detailFacade.retry();
  }

  protected selectionTitle(): string {
    return this.activeSummary()?.label ?? '';
  }

  private toFocusColumn(column: string | null): WordTypeTableFocusColumn | null {
    return column === 'identity' || column === 'occurrences' || column === 'ayahs' || column === 'surahs'
      ? column
      : null;
  }

  protected mentionedSurahs() {
    return wordTypeMentionedSurahViews(this.panelState());
  }

  protected missingSurahs() {
    return wordTypeMissingSurahViews(this.panelState());
  }

  private updateQueryParams(queryParams: Record<string, string | null>): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams,
      queryParamsHandling: 'merge',
      replaceUrl: false,
    });
  }
}
