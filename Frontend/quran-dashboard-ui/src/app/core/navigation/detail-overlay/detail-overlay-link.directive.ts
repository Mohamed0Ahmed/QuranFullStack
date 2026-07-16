import { Directive, computed, inject, input } from '@angular/core';

import { DetailOverlayHistoryService } from './detail-overlay-history.service';
import { DetailFrame } from './detail-overlay.models';

/**
 * Real, copyable entity link into the detail overlay (Feature 029, Change B).
 *
 * The anchor carries a canonical href over the current base URL, so copy-link,
 * modifier clicks (Ctrl/Cmd/Shift), middle-click, and the context menu keep
 * ordinary browser behavior. Only an unmodified primary click is intercepted
 * and turned into an in-app overlay navigation.
 *
 * `start` mode (default) opens a new one-frame stack — explorer side panels and
 * Mushaf entity links. `append` mode pushes onto the open stack — cross-entity
 * links inside an overlay detail.
 */
@Directive({
  selector: 'a[qdDetailLink]',
  standalone: true,
  host: {
    '[attr.href]': 'href()',
    '(click)': 'onClick($event)',
  },
})
export class DetailOverlayLinkDirective {
  private readonly history = inject(DetailOverlayHistoryService);

  readonly qdDetailLink = input.required<DetailFrame>();
  readonly qdDetailLinkMode = input<'start' | 'append'>('start');

  protected readonly href = computed(() => {
    this.history.urlEpoch();
    return this.history.buildFrameHref(this.qdDetailLink(), this.qdDetailLinkMode());
  });

  protected onClick(event: MouseEvent): void {
    if (event.button !== 0 || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) {
      return;
    }
    event.preventDefault();

    if (this.qdDetailLinkMode() === 'append') {
      this.history.appendFrame(this.qdDetailLink());
      return;
    }
    this.history.startStack(this.qdDetailLink());
  }
}
