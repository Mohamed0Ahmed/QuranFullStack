import { ChangeDetectionStrategy, Component } from '@angular/core';

import { STEMS_PAGE_TITLE } from '../../models/stems.labels';

/**
 * Stems Explorer page shell (Feature 016). Thin routeable shell that resolves
 * `/dashboard/words/stems`. Search/sort/list/table/panel composition lands in
 * T048–T050 (US2); this Phase 2 shell guarantees the route resolves for CP-0.
 */
@Component({
  selector: 'qd-stems-explorer-page',
  standalone: true,
  templateUrl: './stems-explorer-page.component.html',
  styleUrl: './stems-explorer-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StemsExplorerPageComponent {
  protected readonly pageTitle = STEMS_PAGE_TITLE;
}
