import {
  Component,
  ElementRef,
  computed,
  effect,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { qdLoadingSizeReservation } from '../../../../shared/layout/loading-size-reservation';
import { QdEmptyStateComponent } from '../../../../shared/ui/empty-state/empty-state.component';
import { QdErrorStateComponent } from '../../../../shared/ui/error-state/error-state.component';
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
import type { QuranWordLocation } from '../../../../shared/quran/quran-location';

const FIRST_LOAD_SEGMENT_PLACEHOLDER_COUNT = 3;

@Component({
  selector: 'qd-selected-word-section',
  standalone: true,
  imports: [
    DetailOverlayLinkDirective,
    QdEmptyStateComponent,
    QdErrorStateComponent,
    SegmentRenderedWordComponent,
    WordMorphologySummaryComponent,
    SegmentDataRowsComponent,
  ],
  templateUrl: './selected-word-section.component.html',
  styleUrls: ['./selected-word-section.component.scss'],
  host: {
    '[class.qd-sws--embedded]': 'embedded()',
  },
})
export class SelectedWordSectionComponent {
  readonly analysis = input<WordAnalysisViewModel | null>(null);
  readonly loadState = input.required<ResourceLoadState>();
  readonly selectedWordLocation = input<QuranWordLocation | null>(null);
  readonly embedded = input(false);

  readonly sectionFocus = output<void>();

  private readonly sectionElement = viewChild<ElementRef<HTMLElement>>('wordSection');

  private readonly lastSegmentCount = signal<number | null>(null);

  protected readonly loadingSegmentPlaceholders = computed<readonly number[]>(() => {
    const count = this.lastSegmentCount() ?? FIRST_LOAD_SEGMENT_PLACEHOLDER_COUNT;
    return Array.from({ length: count }, (_, index) => index);
  });

  private readonly isLoadedSuccessfully = computed(() => {
    const state = this.loadState();
    return (
      !state.isLoading &&
      !state.isEmpty &&
      state.errorMessage === null &&
      this.selectedWordLocation() !== null &&
      this.analysis() !== null
    );
  });

  protected readonly reservedBlockSize = qdLoadingSizeReservation({
    host: this.sectionElement,
    isLoading: computed(() => this.loadState().isLoading),
    isSettled: this.isLoadedSuccessfully,
  }).reservedBlockSize;

  constructor() {
    effect(() => {
      if (this.isLoadedSuccessfully()) {
        this.lastSegmentCount.set(this.analysis()!.segments.length);
      }
    });
  }

  protected readonly rootFrame = computed<RootDetailFrame | null>(() => {
    const rootId = this.analysis()?.morphology.root?.id;

    if (!rootId) {
      return null;
    }

    return { kind: 'root', id: rootId, view: 'words', wordView: 'simple', surahView: 'mentioned', detailPage: 1, typeCode: null };
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

    return { kind: 'unique', mode: 'tashkeel', id: analysis.identity.uniqueTashkeel.id, view: 'ayahs', ayahPage: 1, typeCode: null };
  });

  protected readonly simpleIdentityFrame = computed<UniqueDetailFrame | null>(() => {
    const analysis = this.analysis();

    if (!analysis) {
      return null;
    }

    return { kind: 'unique', mode: 'simple', id: analysis.identity.uniqueSimple.id, view: 'ayahs', ayahPage: 1, typeCode: null };
  });

  protected readonly wordTypeFrame = computed<WordTypeDetailFrame | null>(() => {
    const analysis = this.analysis();

    return analysis === null ? null : wordTypeDetailFrameFromAnalysis(analysis);
  });

}
