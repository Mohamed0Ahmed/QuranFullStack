import { AfterViewInit, Directive, ElementRef, InjectionToken, inject, input, signal } from '@angular/core';

export interface QdTabsInstance {
  readonly instanceId: string;
}

export const QD_TABS_INSTANCE = new InjectionToken<QdTabsInstance>('QD_TABS_INSTANCE');

let nextTabId = 0;

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
export class QdTabDirective implements AfterViewInit {
  readonly selected = input(false);
  readonly disabled = input(false);
  readonly panelId = input<string | null>(null);
  readonly disabledReasonId = input<string | null>(null);

  private readonly elementRef = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly tabs = inject(QD_TABS_INSTANCE, { optional: true });

  readonly roving = signal(false);
  readonly tabId = `${this.tabs?.instanceId ?? 'qd-tabs'}-tab-${nextTabId++}`;

  ngAfterViewInit(): void {
    const element = this.elementRef.nativeElement;
    if (!element.hasAttribute('id')) {
      element.setAttribute('id', this.tabId);
    }
    this.applyFallbackAttribute('aria-controls', this.panelId());
    this.applyFallbackAttribute('aria-describedby', this.disabledReasonId());
  }

  setRoving(isRoving: boolean): void {
    this.roving.set(isRoving);
  }

  focus(): void {
    this.elementRef.nativeElement.focus();
  }

  scrollIntoView(): void {
    this.elementRef.nativeElement.scrollIntoView?.({ block: 'nearest', inline: 'nearest' });
  }

  private applyFallbackAttribute(name: string, value: string | null): void {
    const element = this.elementRef.nativeElement;
    if (value !== null && value !== '' && !element.hasAttribute(name)) {
      element.setAttribute(name, value);
    }
  }
}
