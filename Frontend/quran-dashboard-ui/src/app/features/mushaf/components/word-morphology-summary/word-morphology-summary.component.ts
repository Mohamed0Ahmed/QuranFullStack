import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';

import { DetailOverlayLinkDirective } from '../../../../core/navigation/detail-overlay/detail-overlay-link.directive';
import {
  LemmaDetailFrame,
  RootDetailFrame,
  StemDetailFrame,
  WordTypeDetailFrame,
} from '../../../../core/navigation/detail-overlay/detail-overlay.models';
import { WordMorphologyDto } from '../../models/mushaf.models';
import { morphologyTextOrDash } from '../../utils/morphology-display.labels';

@Component({
  selector: 'qd-word-morphology-summary',
  standalone: true,
  imports: [CommonModule, DetailOverlayLinkDirective],
  templateUrl: './word-morphology-summary.component.html',
  styleUrls: ['./word-morphology-summary.component.scss'],
})
export class WordMorphologySummaryComponent {
  readonly morphology = input.required<WordMorphologyDto>();
  /** Detail-overlay frames (Feature 029, Change B); a null frame keeps its column non-interactive. */
  readonly wordTypeFrame = input<WordTypeDetailFrame | null>(null);
  readonly rootFrame = input<RootDetailFrame | null>(null);
  readonly lemmaFrame = input<LemmaDetailFrame | null>(null);
  readonly stemFrame = input<StemDetailFrame | null>(null);

  protected readonly morphologyTextOrDash = morphologyTextOrDash;
}
