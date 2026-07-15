import { Directive, ElementRef, inject, input, signal } from '@angular/core';

/**
 * Marks a projected element as one tab inside a `qd-tabs` tablist.
 *
 * The host application owns selection state and navigation (routerLink or a
 * click handler) — this directive only supplies the ARIA `tab` role, the
 * `.qd-tabs__tab` / `.qd-is-selected` visual classes, and the roving
 * `tabindex` that `qd-tabs` assigns as the user moves focus with the
 * keyboard. It never changes `selected` itself.
 */
@Directive({
  selector: '[qdTab]',
  standalone: true,
  host: {
    role: 'tab',
    class: 'qd-tabs__tab',
    '[class.qd-is-selected]': 'selected()',
    '[attr.aria-selected]': 'selected()',
    '[attr.aria-disabled]': 'disabled() ? "true" : null',
    '[attr.tabindex]': 'roving() ? 0 : -1',
  },
})
export class QdTabDirective {
  /** Whether the host application considers this tab the active one. */
  readonly selected = input(false);
  /** Drops this tab out of the roving tabindex order when true. */
  readonly disabled = input(false);

  private readonly elementRef = inject<ElementRef<HTMLElement>>(ElementRef);

  /** Set by the parent `qd-tabs` — true for the single tab currently reachable via Tab. */
  readonly roving = signal(false);

  /** @internal called by `qd-tabs` to update the roving-tabindex flag. */
  setRoving(isRoving: boolean): void {
    this.roving.set(isRoving);
  }

  /** @internal called by `qd-tabs` to move real DOM focus during arrow-key navigation. */
  focus(): void {
    this.elementRef.nativeElement.focus();
  }
}
