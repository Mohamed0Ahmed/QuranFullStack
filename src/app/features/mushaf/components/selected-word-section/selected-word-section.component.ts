import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';

import { ResourceLoadState, WordAnalysisViewModel } from '../../models/mushaf.models';
import { SegmentDataRowsComponent } from '../segment-data-rows/segment-data-rows.component';
import { SegmentRenderedWordComponent } from '../segment-rendered-word/segment-rendered-word.component';
import { WordMorphologySummaryComponent } from '../word-morphology-summary/word-morphology-summary.component';

@Component({
  selector: 'qd-selected-word-section',
  standalone: true,
  imports: [
    CommonModule,
    SegmentRenderedWordComponent,
    WordMorphologySummaryComponent,
    SegmentDataRowsComponent,
  ],
  templateUrl: './selected-word-section.component.html',
  styleUrls: ['./selected-word-section.component.scss'],
  host: {
    '[class.qd-selected-word-section--embedded]': 'embedded()',
  },
})
export class SelectedWordSectionComponent {
  readonly analysis = input<WordAnalysisViewModel | null>(null);
  readonly loadState = input.required<ResourceLoadState>();
  readonly selectedWordLocation = input<string | null>(null);
  readonly embedded = input(false);

  readonly sectionFocus = output<void>();

  protected readonly loadingSegmentPlaceholders = [0, 1, 2] as const;
}
