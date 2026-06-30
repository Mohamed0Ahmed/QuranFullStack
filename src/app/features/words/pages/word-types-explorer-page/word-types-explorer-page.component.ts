import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, computed, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { PaginationComponent } from '../../../../shared/ui/pagination/pagination.component';
import { WordTypeDetailsPanelComponent } from '../../components/word-type-details-panel/word-type-details-panel.component';
import { WordTypeFilterComponent } from '../../components/word-type-filter/word-type-filter.component';
import { WordTypesTableComponent } from '../../components/word-types-table/word-types-table.component';
import { WORD_TYPE_SORT_OPTIONS, WORD_TYPES_EMPTY_LABEL, WORD_TYPES_ERROR_LABEL, WORD_TYPES_LOADING_LABEL, WORD_TYPES_PAGE_TITLE, WORD_TYPES_SORT_LABEL, WORD_TYPES_TABLE_LABEL } from '../../models/word-types.labels';
import { WORD_TYPES_PAGE_SIZE, WordTypeMainType, WordTypeRowDto, WordTypeSort } from '../../models/word-types.models';
import { WordTypesDetailFacade } from '../../state/word-types-detail.facade';
import { WordTypesExplorerFacade } from '../../state/word-types-explorer.facade';

@Component({
  selector: 'qd-word-types-explorer-page',
  standalone: true,
  imports: [PaginationComponent, WordTypeDetailsPanelComponent, WordTypeFilterComponent, WordTypesTableComponent],
  templateUrl: './word-types-explorer-page.component.html',
  styleUrl: './word-types-explorer-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WordTypesExplorerPageComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly explorerFacade = inject(WordTypesExplorerFacade);
  private readonly detailFacade = inject(WordTypesDetailFacade);

  protected readonly pageSize = WORD_TYPES_PAGE_SIZE;
  protected readonly listState = this.explorerFacade.listState;
  protected readonly panelState = this.detailFacade.panelState;
  protected readonly selectedRow = computed(() => {
    const state = this.listState();
    const selectedId = state.query.word;
    if (selectedId === null || !state.rows) {
      return null;
    }

    return state.rows.items.find((row) => row.tashkeelWordId === selectedId && row.contextCode === state.query.contextCode) ?? null;
  });

  protected get pageTitle() { return WORD_TYPES_PAGE_TITLE; }
  protected get loadingLabel() { return WORD_TYPES_LOADING_LABEL; }
  protected get emptyLabel() { return WORD_TYPES_EMPTY_LABEL; }
  protected get errorLabel() { return WORD_TYPES_ERROR_LABEL; }
  protected get tableLabel() { return WORD_TYPES_TABLE_LABEL; }
  protected get sortLabel() { return WORD_TYPES_SORT_LABEL; }
  protected get sortOptions() { return WORD_TYPE_SORT_OPTIONS; }

  ngOnInit(): void {
    this.explorerFacade.bindToRoute(this.route);
    this.detailFacade.bindToRoute(this.route);
  }

  ngOnDestroy(): void {
    this.explorerFacade.unbindFromRoute();
    this.detailFacade.unbindFromRoute();
  }

  protected selectType(type: WordTypeMainType): void {
    this.explorerFacade.selectType(type);
  }

  protected selectRow(row: WordTypeRowDto): void {
    this.explorerFacade.selectRow(row);
  }

  protected changeSort(event: Event): void {
    this.explorerFacade.changeSort((event.target as HTMLSelectElement).value as WordTypeSort);
  }

  protected changePage(page: number): void {
    this.explorerFacade.changePage(page);
  }
}
