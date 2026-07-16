import { Component, computed, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';

import { DetailOverlayLinkDirective } from '../../../../core/navigation/detail-overlay/detail-overlay-link.directive';
import {
  LemmaDetailFrame,
  RootDetailFrame,
  StemDetailFrame,
  UniqueDetailFrame,
  WordTypeDetailFrame,
} from '../../../../core/navigation/detail-overlay/detail-overlay.models';
import { ResourceLoadState, WordAnalysisViewModel } from '../../models/mushaf.models';
import { wordTypeDetailFrameFromAnalysis } from '../../utils/word-type-detail-frame.adapter';
import { SegmentDataRowsComponent } from '../segment-data-rows/segment-data-rows.component';
import { SegmentRenderedWordComponent } from '../segment-rendered-word/segment-rendered-word.component';
import { WordMorphologySummaryComponent } from '../word-morphology-summary/word-morphology-summary.component';

@Component({
  selector: 'qd-selected-word-section',
  standalone: true,
  imports: [
    CommonModule,
    DetailOverlayLinkDirective,
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

  // Detail-overlay frames (Feature 029, Change B): every identity link is a
  // real anchor that opens a one-frame overlay stack over the Mushaf base.
  protected readonly rootFrame = computed<RootDetailFrame | null>(() => {
    const rootId = this.analysis()?.morphology.root?.id;

    if (!rootId) {
      return null;
    }

    return { kind: 'root', id: rootId, view: 'words', wordView: 'simple', surahView: 'mentioned', detailPage: 1 };
  });

  protected readonly lemmaFrame = computed<LemmaDetailFrame | null>(() => {
    const lemmaId = this.analysis()?.morphology.lemma?.id;

    if (!lemmaId) {
      return null;
    }

    return {
      kind: 'lemma',
      id: lemmaId,
      view: 'words',
      wordView: 'simple',
      surahView: 'mentioned',
      detailPage: 1,
      typeCode: null,
    };
  });

  protected readonly stemFrame = computed<StemDetailFrame | null>(() => {
    const stemId = this.analysis()?.morphology.stem?.id;

    if (!stemId) {
      return null;
    }

    return {
      kind: 'stem',
      id: stemId,
      view: 'words',
      wordView: 'simple',
      surahView: 'mentioned',
      detailPage: 1,
      typeCode: null,
    };
  });

  protected readonly tashkeelIdentityFrame = computed<UniqueDetailFrame | null>(() => {
    const analysis = this.analysis();

    if (!analysis) {
      return null;
    }

    return { kind: 'unique', mode: 'tashkeel', id: analysis.identity.uniqueTashkeel.id, view: 'ayahs', ayahPage: 1 };
  });

  protected readonly simpleIdentityFrame = computed<UniqueDetailFrame | null>(() => {
    const analysis = this.analysis();

    if (!analysis) {
      return null;
    }

    return { kind: 'unique', mode: 'simple', id: analysis.identity.uniqueSimple.id, view: 'ayahs', ayahPage: 1 };
  });

  protected readonly wordTypeFrame = computed<WordTypeDetailFrame | null>(() => {
    const analysis = this.analysis();

    return analysis === null ? null : wordTypeDetailFrameFromAnalysis(analysis);
  });
}
