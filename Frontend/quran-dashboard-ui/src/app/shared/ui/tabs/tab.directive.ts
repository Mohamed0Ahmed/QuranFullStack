import { Directive, ElementRef, inject, input, signal } from '@angular/core';

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
  readonly selected = input(false);
  readonly disabled = input(false);

  private readonly elementRef = inject<ElementRef<HTMLElement>>(ElementRef);

  readonly roving = signal(false);

  setRoving(isRoving: boolean): void {
    this.roving.set(isRoving);
  }

  focus(): void {
    this.elementRef.nativeElement.focus();
  }
}
