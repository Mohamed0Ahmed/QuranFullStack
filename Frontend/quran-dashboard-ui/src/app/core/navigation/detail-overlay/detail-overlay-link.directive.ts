import { Directive, InjectionToken, computed, inject, input } from '@angular/core';

import { DetailOverlayHistoryService } from './detail-overlay-history.service';
import { DetailFrame } from './detail-overlay.models';

export type DetailOverlayLinkMode = 'start' | 'append';

/**
 * Context default for `qdDetailLinkMode`. Overlay adapters provide `'append'`
 * once at their component root so every entity link rendered inside the open
 * overlay pushes onto the stack, while the same list components rendered in an
 * explorer side panel (no provider) keep the `'start'` default and open a new
 * one-frame stack. An explicit `qdDetailLinkMode` input always wins.
 */
export const DETAIL_OVERLAY_LINK_MODE = new InjectionToken<DetailOverlayLinkMode>('DETAIL_OVERLAY_LINK_MODE');

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
 * links inside an overlay detail. The effective mode is the explicit input if
 * set, else the component-tree `DETAIL_OVERLAY_LINK_MODE` token, else `start`.
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
