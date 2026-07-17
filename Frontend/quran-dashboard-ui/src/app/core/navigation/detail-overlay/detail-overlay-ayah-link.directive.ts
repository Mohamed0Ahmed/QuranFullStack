import { Directive, computed, inject, input } from '@angular/core';
import { Params } from '@angular/router';

import { DetailOverlayHistoryService } from './detail-overlay-history.service';
import { DetailFrame } from './detail-overlay.models';

/** Base-route destination of an ayah link: a path plus its complete own query params. */
export interface DetailOverlayBaseTarget {
  readonly basePath: string;
  readonly queryParams: Params;
}

/**
 * Real, copyable ayah link that keeps the detail overlay alive across a base
 * navigation (Feature 029, Change B7).
 *
 * The anchor carries a canonical href to the destination base (the Mushaf
 * reader), so copy-link, modifier clicks (Ctrl/Cmd/Shift), middle-click, and
 * the context menu keep ordinary browser behavior. Only an unmodified primary
 * click is intercepted and turned into an in-app continuity navigation:
 *
 * - With the overlay open, the current stack rides along and the base change
 *   uses replace semantics (no extra history entry between modal frames).
 * - With the overlay closed, `qdAyahLinkParentFrame` (the source side panel's
 *   detail context) is promoted to a one-frame stack over the destination.
 * - Without a parent frame, the click is a plain page navigation with all
 *   overlay keys stripped.
 */
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
