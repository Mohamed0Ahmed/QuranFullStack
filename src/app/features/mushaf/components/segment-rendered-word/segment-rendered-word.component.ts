import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';

import { RenderedSegmentViewModel } from '../../models/mushaf.models';

@Component({
  selector: 'qd-segment-rendered-word',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './segment-rendered-word.component.html',
  styleUrls: ['./segment-rendered-word.component.scss'],
})
export class SegmentRenderedWordComponent {
  readonly segments = input.required<RenderedSegmentViewModel[]>();
  readonly fullWordText = input.required<string>();
}
