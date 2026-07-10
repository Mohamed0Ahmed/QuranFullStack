import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';

import { WordMorphologyDto } from '../../models/mushaf.models';
import { morphologyTextOrDash } from '../../utils/morphology-display.labels';

@Component({
  selector: 'qd-word-morphology-summary',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './word-morphology-summary.component.html',
  styleUrls: ['./word-morphology-summary.component.scss'],
})
export class WordMorphologySummaryComponent {
  readonly morphology = input.required<WordMorphologyDto>();
  readonly rootExplorerHref = input('');
  readonly lemmaExplorerHref = input('');
  readonly stemExplorerHref = input('');

  protected readonly morphologyTextOrDash = morphologyTextOrDash;
}
