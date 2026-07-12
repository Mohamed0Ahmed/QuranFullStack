import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, computed, inject, signal, viewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { PaginationComponent } from '../../../../shared/ui/pagination/pagination.component';
import { QD_BP_DESKTOP_MIN_QUERY } from '../../../../shared/layout/breakpoints';
import { AyahMatchesListComponent } from '../../components/ayah-matches-list/ayah-matches-list.component';
import { MissingSurahsListComponent } from '../../components/missing-surahs-list/missing-surahs-list.component';
import { SurahOccurrencesListComponent } from '../../components/surah-occurrences-list/surah-occurrences-list.component';
import { WordTypeDetailSummaryComponent } from '../../components/word-type-detail-summary/word-type-detail-summary.component';
import { WordTypeDetailsPanelComponent } from '../../components/word-type-details-panel/word-type-details-panel.component';
import { WordTypeFilterComponent } from '../../components/word-type-filter/word-type-filter.component';
import { WordTypeGroupedWordsListComponent } from '../../components/word-type-grouped-words-list/word-type-grouped-words-list.component';
import { WordTypeTableViewTabsComponent } from '../../components/word-type-table-view-tabs/word-type-table-view-tabs.component';
import { WordTypeCountOpenedEvent, WordTypesTableComponent } from '../../components/word-types-table/word-types-table.component';
import {
  WORD_TYPE_SORT_OPTIONS,
  WORD_TYPE_TABLE_VIEW_EMPTY_LABELS,
  WORD_TYPE_TABLE_VIEW_TABLE_LABELS,
  WORD_TYPES_ERROR_LABEL,
  WORD_TYPES_PAGE_TITLE,
  WORD_TYPES_RETRY_LABEL,
  WORD_TYPES_SELECT_SUBTYPE_LABEL,
  WORD_TYPES_SORT_LABEL,
} from '../../models/word-types.labels';
import {
  DEFAULT_GROUPED_WORD_TYPES_DETAIL_VIEW,
  DEFAULT_WORD_TYPES_DETAIL_PAGE,
  DEFAULT_WORD_TYPES_DETAIL_VIEW,
  LemmaTableRowDto,
  RootTableRowDto,
  StemTableRowDto,
  WORD_TYPES_DETAIL_PAGE_SIZE,
  WORD_TYPES_PAGE_SIZE,
  WordTableRowDto,
  WordTypeCase,
  WordTypeDetailView,
  WordTypeMainType,
  WordTypeSort,
  WordTypeTableRowDto,
  WordTypeTableView,
  WordTypeTense,
  WordTypeVoice,
  normalizeWordTableRow,
} from '../../models/word-types.models';
import { WordTypeGroupedMemberWordDto } from '../../data-access/word-types.api';
import { WordTypeDetailScope, WordTypeGroupedDetailSelection } from '../../models/word-types-detail.models';
import { AyahMatchDto, PagedResultDto as SharedPagedResultDto } from '../../models/unique-words.models';
import { WordTypesDetailFacade } from '../../state/word-types-detail.facade';
import { WordTypesExplorerFacade } from '../../state/word-types-explorer.facade';
import {
  buildWordTypesQueryParams,
  canonicalWordTypesDetailPage,
  clearWordTypesSelection,
} from '../../state/word-types-url-sync';
import { mapWordTypeAyahMatchToShared } from '../../utils/word-type-ayah-match.mapper';

@Component({
  selector: 'qd-word-types-explorer-page',
  standalone: true,
  imports: [
    AyahMatchesListComponent,
    MissingSurahsListComponent,
    PaginationComponent,
    SurahOccurrencesListComponent,
    WordTypeDetailSummaryComponent,
    WordTypeDetailsPanelComponent,
    WordTypeFilterComponent,
    WordTypeGroupedWordsListComponent,
    WordTypeTableViewTabsComponent,
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

  protected readonly pageSize = WORD_TYPES_PAGE_SIZE;
  protected readonly listState = this.explorerFacade.listState;
  protected readonly panelState = this.detailFacade.panelState;
  protected readonly isDesktop = signal(true);
  private readonly filter = viewChild(WordTypeFilterComponent);
  private readonly table = viewChild(WordTypesTableComponent);

  protected readonly selectedRow = computed<WordTypeTableRowDto | null>(() => {
    const state = this.listState();
    const rows = state.rows;
    if (!rows) {
      return null;
    }

    const query = state.query;
    switch (query.tableView) {
      case 'words':
        return query.word === null
          ? null
          : rows.items.find((row): row is WordTableRowDto => row.kind === 'word' && this.matchesWordQuery(row)) ?? null;
      case 'roots':
        return query.root === null
          ? null
          : rows.items.find((row): row is RootTableRowDto => row.kind === 'root' && row.rootId === query.root) ?? null;
      case 'stems':
        return query.stem === null
          ? null
          : rows.items.find((row): row is StemTableRowDto => row.kind === 'stem' && row.stemId === query.stem) ?? null;
      case 'lemmas':
        return query.lemma === null
          ? null
          : rows.items.find((row): row is LemmaTableRowDto => row.kind === 'lemma' && row.lemmaId === query.lemma) ?? null;
    }
  });

  protected readonly emptySelection = computed(() => this.panelState().selection === null);

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

  ngOnInit(): void {
    this.explorerFacade.bindToRoute(this.route);
    this.detailFacade.bindToRoute(this.route);

    if (typeof window !== 'undefined' && typeof window.matchMedia === 'function') {
      this.desktopQuery = window.matchMedia(QD_BP_DESKTOP_MIN_QUERY);
      this.isDesktop.set(this.desktopQuery.matches);
      this.desktopQuery.addEventListener('change', this.onDesktopChange);
    }
  }

  ngOnDestroy(): void {
    this.explorerFacade.unbindFromRoute();
    this.detailFacade.unbindFromRoute();
    this.desktopQuery?.removeEventListener('change', this.onDesktopChange);
  }

  protected selectType(type: WordTypeMainType): void {
    this.explorerFacade.selectType(type);
  }

  protected selectChild(childCode: string | null): void {
    this.explorerFacade.selectChild(childCode);
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

  protected selectRow(row: WordTypeTableRowDto): void {
    if (row.kind === 'word') {
      this.selectWordRow(row);
      return;
    }

    this.selectGroupedRow(row);
  }

  private selectWordRow(row: WordTableRowDto): void {
    this.detailFacade.selectRow(normalizeWordTableRow(row), DEFAULT_WORD_TYPES_DETAIL_VIEW);
    this.updateQueryParams(
      buildWordTypesQueryParams({
        word: row.tashkeelWordId,
        contextCode: row.contextCode,
        view: DEFAULT_WORD_TYPES_DETAIL_VIEW,
        detailPage: canonicalWordTypesDetailPage(DEFAULT_WORD_TYPES_DETAIL_VIEW, DEFAULT_WORD_TYPES_DETAIL_PAGE),
        location: null,
      }),
    );
  }

  private selectGroupedRow(row: RootTableRowDto | StemTableRowDto | LemmaTableRowDto): void {
    const selection = this.toGroupedSelection(row);
    this.detailFacade.select(selection, DEFAULT_GROUPED_WORD_TYPES_DETAIL_VIEW);

    const keyChange =
      selection.kind === 'root'
        ? { root: selection.rootId }
        : selection.kind === 'stem'
          ? { stem: selection.stemId }
          : { lemma: selection.lemmaId };

    this.updateQueryParams(
      buildWordTypesQueryParams({
        ...keyChange,
        view: DEFAULT_GROUPED_WORD_TYPES_DETAIL_VIEW,
        detailPage: canonicalWordTypesDetailPage(DEFAULT_GROUPED_WORD_TYPES_DETAIL_VIEW, DEFAULT_WORD_TYPES_DETAIL_PAGE),
        location: null,
        column: null,
      }),
    );
  }

  private toGroupedSelection(row: RootTableRowDto | StemTableRowDto | LemmaTableRowDto): WordTypeGroupedDetailSelection {
    const scope = this.currentScope();
    switch (row.kind) {
      case 'root': return { kind: 'root', rootId: row.rootId, scope };
      case 'stem': return { kind: 'stem', stemId: row.stemId, scope };
      case 'lemma': return { kind: 'lemma', lemmaId: row.lemmaId, scope };
    }
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

  private matchesWordQuery(row: WordTableRowDto): boolean {
    const query = this.listState().query;
    const identity = normalizeWordTableRow(row);
    return identity.tashkeelWordId === query.word
      && identity.contextCode === query.contextCode
      && identity.case === query.case
      && identity.tense === query.tense
      && identity.voice === query.voice;
  }

  protected onCountOpened(event: WordTypeCountOpenedEvent): void {
    this.detailFacade.selectRow(normalizeWordTableRow(event.row), event.view);
    this.updateQueryParams(
      buildWordTypesQueryParams({
        word: event.row.tashkeelWordId,
        contextCode: event.row.contextCode,
        view: event.view,
        detailPage: canonicalWordTypesDetailPage(event.view, DEFAULT_WORD_TYPES_DETAIL_PAGE),
        location: null,
      }),
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
    this.detailFacade.clearSelection();
    this.updateQueryParams(clearWordTypesSelection());

    if (selectedRow) {
      this.table()?.focusRow(selectedRow);
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
