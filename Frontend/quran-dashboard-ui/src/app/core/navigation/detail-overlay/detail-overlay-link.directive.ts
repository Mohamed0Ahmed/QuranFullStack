import { Directive, InjectionToken, computed, inject, input } from '@angular/core';

import { DetailOverlayHistoryService } from './detail-overlay-history.service';
import { DetailFrame } from './detail-overlay.models';

export type DetailOverlayLinkMode = 'start' | 'append';

// Context default for `qdDetailLinkMode`: overlay adapters provide `'append'` at their
// root so links inside the open overlay push onto the stack; the same lists in an explorer
// side panel (no provider) keep the `'start'` default. An explicit input always wins.
export const DETAIL_OVERLAY_LINK_MODE = new InjectionToken<DetailOverlayLinkMode>('DETAIL_OVERLAY_LINK_MODE');

// Real anchor with a canonical href so copy-link, modifier/middle clicks, and the context
// menu keep native browser behavior; only an unmodified primary click is intercepted into
// an in-app overlay navigation (start = new one-frame stack, append = push onto the stack).
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
  private readonly contextMode = inject(DETAIL_OVERLAY_LINK_MODE, { optional: true });

  readonly qdDetailLink = input.required<DetailFrame>();
  readonly qdDetailLinkMode = input<DetailOverlayLinkMode | null>(null);

  private readonly mode = computed<DetailOverlayLinkMode>(
    () => this.qdDetailLinkMode() ?? this.contextMode ?? 'start',
  );

  protected readonly href = computed(() => {
    this.history.urlEpoch();
    return this.history.buildFrameHref(this.qdDetailLink(), this.mode());
  });

  protected onClick(event: MouseEvent): void {
    if (event.button !== 0 || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) {
      return;
    }
    event.preventDefault();

    if (this.mode() === 'append') {
      this.history.appendFrame(this.qdDetailLink());
      return;
    }
    this.history.startStack(this.qdDetailLink());
  }
}
