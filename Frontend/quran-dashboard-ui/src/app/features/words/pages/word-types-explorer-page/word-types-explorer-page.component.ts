import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, computed, inject, signal, viewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, Subscription, debounceTime } from 'rxjs';

import { PaginationComponent } from '../../../../shared/ui/pagination/pagination.component';
import { QD_BP_DESKTOP_MIN_QUERY } from '../../../../shared/layout/breakpoints';
import { AyahMatchesListComponent } from '../../components/ayah-matches-list/ayah-matches-list.component';
import { MissingSurahsListComponent } from '../../components/missing-surahs-list/missing-surahs-list.component';
import { SurahOccurrencesListComponent } from '../../components/surah-occurrences-list/surah-occurrences-list.component';
import { WordTypeDetailsPanelComponent } from '../../components/word-type-details-panel/word-type-details-panel.component';
import { WordTypeFilterComponent, WordTypeScopeSelectedEvent } from '../../components/word-type-filter/word-type-filter.component';
import { WordTypeGroupedWordsListComponent } from '../../components/word-type-grouped-words-list/word-type-grouped-words-list.component';
import { WordTypeTableViewTabsComponent } from '../../components/word-type-table-view-tabs/word-type-table-view-tabs.component';
import {
  WordTypePresenceFlagChange,
  WordTypesPresenceFilterComponent,
} from '../../components/word-types-presence-filter/word-types-presence-filter.component';
import {
  WordTypeCountColumn,
  WordTypeCountOpenedEvent,
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
import {
  DEFAULT_WORD_TYPES_DETAIL_PAGE,
  LemmaTableRowDto,
  RootTableRowDto,
  StemTableRowDto,
  WORD_TYPES_DETAIL_PAGE_SIZE,
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
} from '../../models/word-types.models';
import {
  WordTypeDetailScope,
  WordTypeDetailSelection,
  WordTypeDetailSelectionKind,
  WordTypeGroupedMemberWordDto,
} from '../../models/word-types-detail.models';
import { AyahMatchDto, PagedResultDto as SharedPagedResultDto } from '../../models/unique-words.models';
import { WordTypesDetailFacade } from '../../state/word-types-detail.facade';
import { WordTypesExplorerFacade } from '../../state/word-types-explorer.facade';
import {
  buildWordTypesQueryParams,
  buildWordTypesDetailScopeQuery,
  canonicalWordTypesDetailPage,
  clearWordTypesSelection,
  parseWordTypesQueryParams,
} from '../../state/word-types-url-sync';
import { mapWordTypeAyahMatchToShared } from '../../utils/word-type-ayah-match.mapper';

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
    MissingSurahsListComponent,
    PaginationComponent,
    SurahOccurrencesListComponent,
    WordTypeDetailsPanelComponent,
    WordTypeFilterComponent,
    WordTypeGroupedWordsListComponent,
    WordTypeTableViewTabsComponent,
    WordTypesPresenceFilterComponent,
    WordTypesTableComponent,
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

  private desktopQuery?: MediaQueryList;
  private readonly onDesktopChange = (event: MediaQueryListEvent): void => this.isDesktop.set(event.matches);

  // Debounced word-identity search: user input echoes into searchDraft immediately and only settles to
  // the URL after 300ms; the route sync mirrors the restored search back into the input on refresh/Back.
  private readonly searchInput = new Subject<string>();
  private searchSub?: Subscription;
  private querySyncSub?: Subscription;

  protected readonly pageSize = WORD_TYPES_PAGE_SIZE;
  protected readonly listState = this.explorerFacade.listState;
  protected readonly panelState = this.detailFacade.panelState;
  protected readonly isDesktop = signal(true);
  protected readonly searchDraft = signal('');
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

  // A word summary and a grouped summary share the same measure shape; the panel renders whichever
  // one the active selection produced.
  protected readonly activeSummary = computed(() => {
    const panel = this.panelState();
    const summary = panel.summary ?? panel.groupedSummary;
    return summary
      ? {
          label: summary.displayText,
          occurrences: summary.occurrencesCount,
          ayahs: summary.ayahsCount,
          surahs: summary.surahsCount,
        }
      : null;
  });

  protected readonly emptyAyahsPage: SharedPagedResultDto<AyahMatchDto> = {
    page: 1,
    pageSize: WORD_TYPES_DETAIL_PAGE_SIZE,
    totalCount: 0,
    items: [],
  };

  protected readonly emptyMemberWordsPage: SharedPagedResultDto<WordTypeGroupedMemberWordDto> = {
    page: 1,
    pageSize: WORD_TYPES_DETAIL_PAGE_SIZE,
    totalCount: 0,
    items: [],
  };

  protected readonly memberWordsForView = computed(() => this.panelState().words ?? this.emptyMemberWordsPage);

  protected readonly ayahsPageForView = computed(() => {
    const page = this.panelState().ayahs;
    return page ? { ...page, items: page.items.map(mapWordTypeAyahMatchToShared) } : this.emptyAyahsPage;
  });

  protected get pageTitle() { return WORD_TYPES_PAGE_TITLE; }
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

  ngOnInit(): void {
    this.explorerFacade.bindToRoute(this.route);
    this.detailFacade.bindToRoute(this.route);

    // Search is list-scope: a change resets the list page and keeps the identity-loaded detail selection.
    this.searchSub = this.searchInput.pipe(debounceTime(300))
      .subscribe((value) => this.updateQueryParams({ search: value || null, page: null }));
    this.querySyncSub = this.route.queryParamMap
      .subscribe((params) => this.searchDraft.set(parseWordTypesQueryParams(params).search ?? ''));

    if (typeof window !== 'undefined' && typeof window.matchMedia === 'function') {
      this.desktopQuery = window.matchMedia(QD_BP_DESKTOP_MIN_QUERY);
      this.isDesktop.set(this.desktopQuery.matches);
      this.desktopQuery.addEventListener('change', this.onDesktopChange);
    }
  }

  ngOnDestroy(): void {
    this.explorerFacade.unbindFromRoute();
    this.detailFacade.unbindFromRoute();
    this.searchSub?.unsubscribe();
    this.querySyncSub?.unsubscribe();
    this.desktopQuery?.removeEventListener('change', this.onDesktopChange);
  }

  protected onSearchInput(value: string): void {
    this.searchDraft.set(value);
    this.searchInput.next(value);
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
    const scope = this.currentScope();
    let keyChange: { word: number; contextCode: string } | { root: number } | { stem: number } | { lemma: number };

    if (event.row.kind === 'word') {
      const identity = normalizeWordTableRow(event.row);
      keyChange = { word: event.row.tashkeelWordId, contextCode: event.row.contextCode };
      this.detailFacade.selectRow(identity, scope, event.view);
    } else {
      this.detailFacade.selectGroupedRow(event.row, scope, event.view);
      keyChange = event.row.kind === 'root'
        ? { root: event.row.rootId }
        : event.row.kind === 'stem'
          ? { stem: event.row.stemId }
          : { lemma: event.row.lemmaId };
    }

    this.updateQueryParams(
      {
        ...clearWordTypesSelection(),
        ...buildWordTypesQueryParams({
          ...keyChange,
          ...buildWordTypesDetailScopeQuery({ scope }),
          view: event.view,
          detailPage: canonicalWordTypesDetailPage(event.view, DEFAULT_WORD_TYPES_DETAIL_PAGE),
          location: null,
          column: event.row.kind === 'word' ? event.column : null,
        }),
      },
    );
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
    const column = this.toCountColumn(this.listState().query.column);
    this.detailFacade.clearSelection();
    this.updateQueryParams(clearWordTypesSelection());

    if (selectedRow) {
      this.table()?.focusStatistic(selectedRow, view, column);
      return;
    }

    this.filter()?.focusSelectedType();
  }

  protected changeSort(event: Event): void {
    this.explorerFacade.changeSort((event.target as HTMLSelectElement).value as WordTypeSort);
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

  private toCountColumn(column: string | null): WordTypeCountColumn | null {
    return column === 'occurrences' || column === 'ayahs' || column === 'surahs' ? column : null;
  }

  protected mentionedSurahs() {
    const surahs = this.panelState().surahs?.surahs ?? [];
    return surahs.map((surah) => ({
      surahNumber: surah.surahNumber,
      nameArabic: surah.nameArabic,
      occurrencesInSurah: surah.occurrencesCount,
    }));
  }

  protected missingSurahs() {
    const surahs = this.panelState().surahs?.missingSurahs ?? [];
    return surahs.map((surah) => ({
      surahNumber: surah.surahNumber,
      nameArabic: surah.nameArabic,
    }));
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
