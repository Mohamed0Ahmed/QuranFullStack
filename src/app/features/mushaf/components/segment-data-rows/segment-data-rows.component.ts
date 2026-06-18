import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';

import { RenderedSegmentViewModel } from '../../models/mushaf.models';

@Component({
  selector: 'qd-segment-data-rows',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './segment-data-rows.component.html',
  styleUrls: ['./segment-data-rows.component.scss'],
})
export class SegmentDataRowsComponent {
  readonly segments = input.required<RenderedSegmentViewModel[]>();
}
