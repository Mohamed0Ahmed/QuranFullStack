import { Directive, computed, inject, input } from '@angular/core';
import { Params } from '@angular/router';

import { DetailOverlayHistoryService } from './detail-overlay-history.service';
import { DetailFrame } from './detail-overlay.models';

export interface DetailOverlayBaseTarget {
  readonly basePath: string;
  readonly queryParams: Params;
}

// Real anchor with a canonical href so copy-link, modifier/middle clicks, and the
// context menu keep native browser behavior; only an unmodified primary click is
// intercepted into an in-app continuity navigation (see navigateBaseWithOverlay).
@Directive({
  selector: 'a[qdAyahOverlayLink]',
  standalone: true,
  host: {
    '[attr.href]': 'href()',
    '(click)': 'onClick($event)',
  },
})
export class DetailOverlayAyahLinkDirective {
  private readonly history = inject(DetailOverlayHistoryService);

  readonly qdAyahOverlayLink = input.required<DetailOverlayBaseTarget>();
  readonly qdAyahLinkParentFrame = input<DetailFrame | null>(null);

  protected readonly href = computed(() => {
    this.history.urlEpoch();
    const target = this.qdAyahOverlayLink();
    return this.history.buildBaseWithOverlayHref(target.basePath, target.queryParams, this.overlayOptions());
  });

  protected onClick(event: MouseEvent): void {
    if (event.button !== 0 || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) {
      return;
    }
    event.preventDefault();

    const target = this.qdAyahOverlayLink();
    this.history.navigateBaseWithOverlay(target.basePath, target.queryParams, this.overlayOptions());
  }

  private overlayOptions(): { promoteFrame?: DetailFrame } {
    return { promoteFrame: this.qdAyahLinkParentFrame() ?? undefined };
  }
}
