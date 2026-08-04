import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
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

import { CONTEXT_MENU_LABELS } from './context-menu.labels';

const VIEWPORT_MARGIN = 8;

interface MenuPlacement {
  readonly left: number;
  readonly top: number;
}

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
  readonly menuAriaLabel = input(CONTEXT_MENU_LABELS.menuAriaLabel);

  readonly dismissed = output<void>();

  private readonly elementRef = inject(ElementRef<HTMLElement>);
  private readonly menu = viewChild.required<ElementRef<HTMLElement>>('menu');
  private readonly invoker = resolveInvoker();
  private isDestroyed = false;

  protected readonly placement = signal<MenuPlacement | null>(null);

  constructor() {
    inject(DestroyRef).onDestroy(() => {
      this.isDestroyed = true;
      this.returnFocusToInvoker();
    });
    setTimeout(() => {
      if (!this.isDestroyed) {
        this.items()[0]?.focus();
      }
    });

    afterRenderEffect(() => {
      const anchor = this.position();
      const rect = this.menu().nativeElement.getBoundingClientRect();
      if (rect.width === 0 && rect.height === 0) {
        return;
      }
      const next = this.place(anchor, rect.width, rect.height);
      untracked(() => {
        const current = this.placement();
        if (current === null || current.left !== next.left || current.top !== next.top) {
          this.placement.set(next);
        }
      });
    });
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.dismissed.emit();
  }

  protected onMenuKeydown(event: KeyboardEvent): void {
    const step = event.key === 'ArrowDown' ? 1 : event.key === 'ArrowUp' ? -1 : 0;
    if (step === 0) {
      return;
    }
    event.preventDefault();
    const items = this.items();
    if (items.length === 0) {
      return;
    }
    const current = items.indexOf(document.activeElement as HTMLElement);
    const next = (current + step + items.length) % items.length;
    items[next].focus();
  }

  private items(): HTMLElement[] {
    return Array.from(this.menu().nativeElement.querySelectorAll<HTMLElement>('[role="menuitem"]'));
  }

  private returnFocusToInvoker(): void {
    const active = document.activeElement;
    const focusStayedInsideTheMenu =
      active === null || active === document.body || this.elementRef.nativeElement.contains(active);
    if (focusStayedInsideTheMenu) {
      this.invoker?.focus();
    }
  }

  private place(anchor: { x: number; y: number }, width: number, height: number): MenuPlacement {
    const rtl = this.resolveDirection() === 'rtl';
    const viewportWidth = window.innerWidth;
    const viewportHeight = window.innerHeight;

    let left = rtl ? anchor.x - width : anchor.x;
    if (rtl ? left < VIEWPORT_MARGIN : left + width > viewportWidth - VIEWPORT_MARGIN) {
      left = rtl ? anchor.x : anchor.x - width;
    }

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

function clamp(value: number, min: number, max: number): number {
  return Math.max(min, Math.min(value, max));
}

function resolveInvoker(): HTMLElement | null {
  const active = document.activeElement;
  return active instanceof HTMLElement && active !== document.body ? active : null;
}
