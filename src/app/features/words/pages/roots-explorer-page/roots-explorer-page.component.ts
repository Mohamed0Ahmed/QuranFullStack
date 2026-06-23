import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';

import { RootDetailsPanelComponent } from '../../components/root-details-panel/root-details-panel.component';
import { ROOTS_EMPTY_SELECTION_LABEL, ROOTS_PAGE_TITLE } from '../../models/roots.labels';
import { DEFAULT_ROOT_VIEW } from '../../models/roots.models';
import { RootsDetailFacade } from '../../state/roots-detail.facade';
import { RootsExplorerFacade } from '../../state/roots-explorer.facade';

/**
 * Roots Explorer routeable smart page (Feature 015). Route:
 * `/dashboard/words/roots`. Sibling of the Unique Words explorer.
 *
 * Foundational shell (T018): renders the split-screen layout with a roots table
 * placeholder (filled by US1 T030) and the persistent detail panel shell
 * (T020) showing the empty-selection state. The table content, search/sort/
 * pagination, and per-view panel content are added by the user-story phases.
 */
@Component({
  selector: 'qd-roots-explorer-page',
  standalone: true,
  imports: [RootDetailsPanelComponent],
  templateUrl: './roots-explorer-page.component.html',
  styleUrl: './roots-explorer-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RootsExplorerPageComponent {
  private readonly listFacade = inject(RootsExplorerFacade);
  private readonly detailFacade = inject(RootsDetailFacade);

  protected readonly pageTitle = ROOTS_PAGE_TITLE;
  protected readonly emptySelectionLabel = ROOTS_EMPTY_SELECTION_LABEL;

  protected readonly panelState = this.detailFacade.panelState;

  /** No root selected yet → panel shows the empty-selection state. */
  protected readonly emptySelection = computed(
    () => this.panelState().selectedRootId === null,
  );

  protected readonly activeView = computed(() => this.panelState().view);

  protected readonly defaultView = DEFAULT_ROOT_VIEW;
}
