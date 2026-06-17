import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';

import { MushafReaderFacade } from '../../state/mushaf-reader.facade';

/**
 * Smart shell for the Mushaf reader at `/dashboard/mushaf`.
 *
 * Phase 2: renders an empty RTL two-column grid placeholder (study area on the
 * left, Mushaf area on the right). URL<->state hydration, page loading, and
 * composition of the study/word components are added by US1/US3/US4/US5
 * (T027 / T037 / T046 / T049).
 */
@Component({
  selector: 'qd-mushaf-reader-page',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './mushaf-reader-page.component.html',
  styleUrls: ['./mushaf-reader-page.component.scss'],
})
export class MushafReaderPageComponent {
  protected readonly facade = inject(MushafReaderFacade);
}
