import { ChangeDetectionStrategy, Component } from '@angular/core';

import { LEMMAS_PAGE_TITLE } from '../../models/lemmas.labels';

/**
 * Lemmas Explorer page shell (Feature 016). Thin routeable shell that resolves
 * `/dashboard/words/lemmas`. Search/sort/list/table/panel composition lands in
 * T037–T039 (US1); this Phase 2 shell guarantees the route resolves for CP-0.
 */
@Component({
  selector: 'qd-lemmas-explorer-page',
  standalone: true,
  templateUrl: './lemmas-explorer-page.component.html',
  styleUrl: './lemmas-explorer-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LemmasExplorerPageComponent {
  protected readonly pageTitle = LEMMAS_PAGE_TITLE;
}
