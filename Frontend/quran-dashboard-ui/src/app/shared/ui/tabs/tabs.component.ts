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

import { QD_TABS_INSTANCE, QdTabDirective } from './tab.directive';

export type QdTabsOrientation = 'horizontal' | 'vertical';
export type QdTabsLayout = 'inline' | 'grid';

export const QD_TABS_SEGMENTED_MAX = 3;

let nextTabsId = 0;

@Component({
  selector: 'qd-tabs',
  standalone: true,
  templateUrl: './tabs.component.html',
  styleUrl: './tabs.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [{ provide: QD_TABS_INSTANCE, useExisting: QdTabsComponent }],
  host: {
    '(keydown)': 'onKeydown($event)',
  },
})
export class QdTabsComponent {
  readonly ariaLabel = input.required<string>();
  readonly orientation = input<QdTabsOrientation>('horizontal');
  readonly layout = input<QdTabsLayout>('inline');

  readonly instanceId = `qd-tabs-${nextTabsId++}`;

  private readonly elementRef = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly tabs = contentChildren(QdTabDirective, { descendants: true });

  private readonly manualFocusIndex = signal<number | null>(null);

  protected readonly segmented = computed(
    () => this.layout() === 'inline' && this.tabs().length <= QD_TABS_SEGMENTED_MAX,
  );

  protected readonly scrollable = computed(
    () => this.layout() === 'inline' && this.tabs().length > QD_TABS_SEGMENTED_MAX,
  );

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

    effect(() => {
      const selected = this.tabs().find((tab) => tab.selected());
      selected?.scrollIntoView();
    });
  }

  protected onKeydown(event: KeyboardEvent): void {
    if (!this.originatesOnOwnTab(event.target)) {
      return;
    }

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
    const tab = this.tabs()[index];
    tab?.focus();
    tab?.scrollIntoView();
  }

  private originatesOnOwnTab(target: EventTarget | null): boolean {
    if (!(target instanceof HTMLElement)) {
      return false;
    }
    const tablist = target.closest('[role="tab"]')?.closest('[role="tablist"]') ?? null;
    return tablist !== null && tablist.parentElement === this.elementRef.nativeElement;
  }

  private resolveDirection(): 'ltr' | 'rtl' {
    const dirHost = this.elementRef.nativeElement.closest('[dir]');
    return dirHost?.getAttribute('dir') === 'rtl' ? 'rtl' : 'ltr';
  }
}
