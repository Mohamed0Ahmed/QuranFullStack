import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  HostListener,
  afterRenderEffect,
  inject,
  input,
  output,
  signal,
  untracked,
  viewChild,
} from '@angular/core';

/** The `--qd-space-2` step, as a number: the placement math needs px, not a token. */
const VIEWPORT_MARGIN = 8;

interface MenuPlacement {
  readonly left: number;
  readonly top: number;
}

// Presentation-only row/node context menu shell (Slice A, plan §6 phase 6). Consumers
// project their own `role="menuitem"` items via `<ng-content>` — this component owns only
// the backdrop, positioning, and dismissal, never a door/template-node concern.
@Component({
  selector: 'qd-context-menu',
  standalone: true,
  templateUrl: './context-menu.component.html',
  styleUrl: './context-menu.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class QdContextMenuComponent {
  readonly position = input.required<{ x: number; y: number }>();
  readonly menuTestId = input.required<string>();
  readonly backdropTestId = input.required<string>();

  readonly dismissed = output<void>();

  private readonly elementRef = inject(ElementRef<HTMLElement>);
  private readonly menu = viewChild.required<ElementRef<HTMLElement>>('menu');

  protected readonly placement = signal<MenuPlacement | null>(null);

  constructor() {
    // The placement contract lives entirely here, so both consumers inherit it and neither can
    // drift: the menu extends toward the anchor's INLINE-START (RTL: right edge at `x`, growing
    // leftward; LTR: the mirror, which is the behaviour that shipped), flips at either viewport
    // edge, and is clamped after flipping. Measure-then-place is unavoidable — the box's own size
    // is what decides all three — so the first frame renders hidden rather than flashing at the
    // wrong side. jsdom has no layout engine and reports a zero-sized rect; there the menu keeps
    // the raw anchor and stays measurable by the specs, which is why the browser (e2e + the
    // recorded walk) is the verification tier for this contract.
    afterRenderEffect(() => {
      const anchor = this.position();
      const rect = this.menu().nativeElement.getBoundingClientRect();
      if (rect.width === 0 && rect.height === 0) {
        return;
      }
      const next = this.place(anchor, rect.width, rect.height);
      // Untracked on purpose: this effect WRITES `placement`, so a tracked read would make it
      // its own dependency and leave termination resting on the equality guard below rather
      // than on the dependency graph.
      untracked(() => {
        const current = this.placement();
        if (current === null || current.left !== next.left || current.top !== next.top) {
          this.placement.set(next);
        }
      });
    });
  }

  // Document-level, not `(keydown.escape)` on the menu element: none of the four open
  // paths (right-click, ⋯, the two workshop equivalents, or the tree's keyboard
  // ContextMenu/Shift+F10 path) puts focus inside the menu, so an element-bound handler
  // would never fire. Copies `top-navbar.component.ts`'s `document:keydown.escape` pattern.
  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.dismissed.emit();
  }

  private place(anchor: { x: number; y: number }, width: number, height: number): MenuPlacement {
    const rtl = this.resolveDirection() === 'rtl';
    const viewportWidth = window.innerWidth;
    const viewportHeight = window.innerHeight;

    let left = rtl ? anchor.x - width : anchor.x;
    if (rtl ? left < VIEWPORT_MARGIN : left + width > viewportWidth - VIEWPORT_MARGIN) {
      left = rtl ? anchor.x : anchor.x - width;
    }

    // Preferred block placement is below the anchor; open upward only if that would cross the
    // bottom edge.
    let top = anchor.y;
    if (top + height > viewportHeight - VIEWPORT_MARGIN) {
      top = anchor.y - height;
    }

    return {
      left: clamp(left, VIEWPORT_MARGIN, viewportWidth - width - VIEWPORT_MARGIN),
      top: clamp(top, VIEWPORT_MARGIN, viewportHeight - height - VIEWPORT_MARGIN),
    };
  }

  private resolveDirection(): 'ltr' | 'rtl' {
    const dirHost = (this.elementRef.nativeElement as HTMLElement).closest('[dir]');
    return dirHost?.getAttribute('dir') === 'rtl' ? 'rtl' : 'ltr';
  }
}

/** `max` first, so a menu taller or wider than the viewport keeps its START edge on screen
 * rather than its end — the items a user reaches first are the ones worth guaranteeing. */
function clamp(value: number, min: number, max: number): number {
  return Math.max(min, Math.min(value, max));
}
