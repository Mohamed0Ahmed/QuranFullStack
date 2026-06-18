import {
  afterRenderEffect,
  Component,
  computed,
  DestroyRef,
  inject,
  input,
  viewChild,
  ElementRef,
} from '@angular/core';
import { CommonModule } from '@angular/common';

import { RenderedSegmentViewModel } from '../../models/mushaf.models';
import { buildUthmaniSegmentSlices } from '../../utils/segment-uthmani-slices';
import {
  applySegmentWordHighlights,
  clearSegmentHighlights,
  supportsCssCustomHighlights,
} from '../../utils/segment-word-highlights';

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

  private readonly wordHost = viewChild<ElementRef<HTMLElement>>('wordHost');
  private readonly destroyRef = inject(DestroyRef);

  protected readonly highlightSupported = supportsCssCustomHighlights();

  protected readonly slices = computed(() =>
    buildUthmaniSegmentSlices(this.fullWordText(), this.segments()),
  );

  protected readonly missingBadges = computed(() =>
    this.slices().filter((slice) => slice.isMissing),
  );

  constructor() {
    if (this.highlightSupported) {
      afterRenderEffect(() => {
        const host = this.wordHost()?.nativeElement;
        if (!host) {
          return;
        }

        applySegmentWordHighlights(host, this.fullWordText(), this.slices());
      });
    }

    this.destroyRef.onDestroy(() => clearSegmentHighlights());
  }
}
