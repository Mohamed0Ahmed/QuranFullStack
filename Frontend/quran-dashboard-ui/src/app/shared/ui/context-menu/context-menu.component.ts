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

  readonly dismissed = output<void>();

  private readonly elementRef = inject(ElementRef<HTMLElement>);
  private readonly menu = viewChild.required<ElementRef<HTMLElement>>('menu');

  protected readonly placement = signal<MenuPlacement | null>(null);

  constructor() {
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
