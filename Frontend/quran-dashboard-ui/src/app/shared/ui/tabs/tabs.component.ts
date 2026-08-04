import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  computed,
  contentChildren,
  effect,
  inject,
  input,
  signal,
} from '@angular/core';

import { QdTabDirective } from './tab.directive';

export type QdTabsOrientation = 'horizontal' | 'vertical';
export type QdTabsLayout = 'inline' | 'grid';

// The app-wide tab-strip (UI_STYLE_SYSTEM.md §17 `qd-tabs`). It does not own
// selection: consumers project their own `qdTab` elements with their `[selected]`
// flag and click/routerLink handling. This only supplies the `role="tablist"`
// wrapper and RTL-aware roving-tabindex keyboard nav (Arrow/Home/End).
@Component({
  selector: 'qd-tabs',
  standalone: true,
  templateUrl: './tabs.component.html',
  styleUrl: './tabs.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '(keydown)': 'onKeydown($event)',
  },
})
export class QdTabsComponent {
  readonly ariaLabel = input.required<string>();
  readonly orientation = input<QdTabsOrientation>('horizontal');
  readonly layout = input<QdTabsLayout>('inline');

  private readonly elementRef = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly tabs = contentChildren(QdTabDirective, { descendants: true });

  private readonly manualFocusIndex = signal<number | null>(null);

  private readonly rovingIndex = computed(() => {
    const tabs = this.tabs();
    if (tabs.length === 0) {
      return 0;
    }

    const manual = this.manualFocusIndex();
    if (manual !== null && manual < tabs.length && !tabs[manual].disabled()) {
      return manual;
    }

    const selectedIndex = tabs.findIndex((tab) => tab.selected() && !tab.disabled());
    if (selectedIndex >= 0) {
      return selectedIndex;
    }

    const firstEnabled = tabs.findIndex((tab) => !tab.disabled());
    return firstEnabled >= 0 ? firstEnabled : 0;
  });

  constructor() {
    effect(() => {
      const tabs = this.tabs();
      const active = this.rovingIndex();
      tabs.forEach((tab, index) => tab.setRoving(index === active));
    });
  }

  protected onKeydown(event: KeyboardEvent): void {
    const tabs = this.tabs();
    const enabledIndexes = tabs.map((_, index) => index).filter((index) => !tabs[index].disabled());

    if (enabledIndexes.length === 0) {
      return;
    }

    const horizontal = this.orientation() === 'horizontal';
    const isRtl = this.resolveDirection() === 'rtl';

    let step = 0;
    switch (event.key) {
      case 'ArrowRight':
        if (!horizontal) return;
        step = isRtl ? -1 : 1;
        break;
      case 'ArrowLeft':
        if (!horizontal) return;
        step = isRtl ? 1 : -1;
        break;
      case 'ArrowDown':
        if (horizontal) return;
        step = 1;
        break;
      case 'ArrowUp':
        if (horizontal) return;
        step = -1;
        break;
      case 'Home':
        event.preventDefault();
        this.moveTo(enabledIndexes[0]);
        return;
      case 'End':
        event.preventDefault();
        this.moveTo(enabledIndexes[enabledIndexes.length - 1]);
        return;
      default:
        return;
    }

    event.preventDefault();
    const currentPosition = enabledIndexes.indexOf(this.rovingIndex());
    const basePosition = currentPosition >= 0 ? currentPosition : 0;
    const nextPosition = (basePosition + step + enabledIndexes.length) % enabledIndexes.length;
    this.moveTo(enabledIndexes[nextPosition]);
  }

  private moveTo(index: number): void {
    this.manualFocusIndex.set(index);
    this.tabs()[index]?.focus();
  }

  private resolveDirection(): 'ltr' | 'rtl' {
    const dirHost = this.elementRef.nativeElement.closest('[dir]');
    return dirHost?.getAttribute('dir') === 'rtl' ? 'rtl' : 'ltr';
  }
}
