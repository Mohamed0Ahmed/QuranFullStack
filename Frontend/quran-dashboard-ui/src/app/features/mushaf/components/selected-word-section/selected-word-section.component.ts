import {
  Component,
  DestroyRef,
  ElementRef,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
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

const FIRST_LOAD_SEGMENT_PLACEHOLDER_COUNT = 3;
const RESERVATION_INLINE_SIZE_TOLERANCE_PX = 1;

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
  private readonly destroyRef = inject(DestroyRef);

  readonly analysis = input<WordAnalysisViewModel | null>(null);
  readonly loadState = input.required<ResourceLoadState>();
  readonly selectedWordLocation = input<string | null>(null);
  readonly embedded = input(false);

  readonly sectionFocus = output<void>();

  private readonly sectionElement = viewChild<ElementRef<HTMLElement>>('wordSection');

  // U1 (Feature 029): loading layout reservation. Only numeric geometry and the
  // previous segment count are retained — never prior text or Quran DOM.
  private readonly lastNaturalSize = signal<{ blockSize: number; inlineSize: number } | null>(null);
  private readonly lastSegmentCount = signal<number | null>(null);

  protected readonly loadingSegmentPlaceholders = computed<readonly number[]>(() => {
    const count = this.lastSegmentCount() ?? FIRST_LOAD_SEGMENT_PLACEHOLDER_COUNT;
    return Array.from({ length: count }, (_, index) => index);
  });

  protected readonly reservedBlockSize = computed<string | null>(() => {
    const natural = this.lastNaturalSize();
    return this.loadState().isLoading && natural !== null ? `${natural.blockSize}px` : null;
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

  constructor() {
    effect(() => {
      if (this.isLoadedSuccessfully()) {
        this.lastSegmentCount.set(this.analysis()!.segments.length);
      }
    });

    if (typeof ResizeObserver !== 'undefined') {
      const observer = new ResizeObserver((entries) => this.onSectionResize(entries));
      this.destroyRef.onDestroy(() => observer.disconnect());
      effect(() => {
        const element = this.sectionElement()?.nativeElement;
        if (element) {
          observer.disconnect();
          observer.observe(element);
        }
      });
    }
  }

  private onSectionResize(entries: ResizeObserverEntry[]): void {
    const target = entries[entries.length - 1]?.target;
    if (!target) {
      return;
    }
    const rect = target.getBoundingClientRect();

    if (this.isLoadedSuccessfully()) {
      this.lastNaturalSize.set({ blockSize: rect.height, inlineSize: rect.width });
      return;
    }

    const natural = this.lastNaturalSize();
    if (
      this.loadState().isLoading &&
      natural !== null &&
      Math.abs(natural.inlineSize - rect.width) > RESERVATION_INLINE_SIZE_TOLERANCE_PX
    ) {
      this.lastNaturalSize.set(null);
    }
  }

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
