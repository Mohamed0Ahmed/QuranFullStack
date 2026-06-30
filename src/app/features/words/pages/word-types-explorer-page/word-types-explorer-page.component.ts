import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { WordTypeDetailsPanelComponent } from '../../components/word-type-details-panel/word-type-details-panel.component';
import { WordTypeFilterComponent } from '../../components/word-type-filter/word-type-filter.component';
import { WordTypesTableComponent } from '../../components/word-types-table/word-types-table.component';
import { WORD_TYPES_PAGE_TITLE } from '../../models/word-types.labels';
import { WordTypesDetailFacade } from '../../state/word-types-detail.facade';
import { WordTypesExplorerFacade } from '../../state/word-types-explorer.facade';

@Component({
  selector: 'qd-word-types-explorer-page',
  standalone: true,
  imports: [WordTypeDetailsPanelComponent, WordTypeFilterComponent, WordTypesTableComponent],
  templateUrl: './word-types-explorer-page.component.html',
  styleUrl: './word-types-explorer-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WordTypesExplorerPageComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly explorerFacade = inject(WordTypesExplorerFacade);
  private readonly detailFacade = inject(WordTypesDetailFacade);

  protected readonly pageTitle = WORD_TYPES_PAGE_TITLE;
  protected readonly listState = this.explorerFacade.listState;
  protected readonly panelState = this.detailFacade.panelState;

  ngOnInit(): void {
    this.explorerFacade.bindToRoute(this.route);
    this.detailFacade.bindToRoute(this.route);
  }

  ngOnDestroy(): void {
    this.explorerFacade.unbindFromRoute();
    this.detailFacade.unbindFromRoute();
  }
}
