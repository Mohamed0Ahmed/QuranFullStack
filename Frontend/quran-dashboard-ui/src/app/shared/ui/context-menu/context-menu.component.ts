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
import { MenuPlacement, placeContextMenu, resolveMenuDirection } from './context-menu-placement';

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
      const next = placeContextMenu(
        anchor,
        { width: rect.width, height: rect.height },
        { width: window.innerWidth, height: window.innerHeight },
        resolveMenuDirection(this.elementRef.nativeElement),
      );
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

}

function resolveInvoker(): HTMLElement | null {
  const active = document.activeElement;
  return active instanceof HTMLElement && active !== document.body ? active : null;
}
