import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { FocusTrap, FocusTrapFactory } from '@angular/cdk/a11y';

import {
  ROOTS_EMPTY_SELECTION_LABEL,
  ROOTS_PANEL_LABEL,
  ROOTS_PANEL_TAB_ARIA,
  ROOTS_PANEL_TAB_LABELS,
} from '../../models/roots.labels';
import { CLOSE_LABEL } from '../../models/unique-words.labels';
import { ROOT_VIEW_KEYS, RootView } from '../../models/roots.models';

const NARROW_VIEWPORT_QUERY = '(max-width: 60rem)';

@Component({
  selector: 'qd-root-details-panel',
  standalone: true,
  templateUrl: './root-details-panel.component.html',
  styleUrl: './root-details-panel.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RootDetailsPanelComponent {
  readonly view = input.required<RootView>();

  readonly emptySelection = input(false);

  readonly selectionTitle = input('');

  readonly loading = input(false);

  readonly notFound = input(false);

  readonly viewChange = output<RootView>();

  readonly close = output<void>();

  protected get rootsPanelLabel() {
    return ROOTS_PANEL_LABEL;
  }

  protected get closeLabel() {
    return CLOSE_LABEL;
  }

  protected get emptySelectionLabel() {
    return ROOTS_EMPTY_SELECTION_LABEL;
  }

  protected readonly surfaceDomId = 'root-details-panel-surface';

  protected readonly tabs = ROOT_VIEW_KEYS.map((key) => ({
    key,
    label: ROOTS_PANEL_TAB_LABELS[key],
    aria: ROOTS_PANEL_TAB_ARIA[key],
  }));

  private readonly panelRoot = viewChild<ElementRef<HTMLElement>>('panelRoot');
  private readonly tabList = viewChild<ElementRef<HTMLElement>>('tabList');

  protected readonly hasSelection = computed(() => !this.emptySelection());

  private readonly isNarrow = signal(false);
  protected readonly drawerMode = computed(() => this.isNarrow() && this.hasSelection());

  private readonly focusTrapFactory = inject(FocusTrapFactory);
  private readonly destroyRef = inject(DestroyRef);
  private focusTrap?: FocusTrap;
  private previouslyFocused: HTMLElement | null = null;

  constructor() {
    this.observeViewport();
    effect(() => (this.drawerMode() ? this.enterDrawer() : this.exitDrawer()));
    this.destroyRef.onDestroy(() => this.exitDrawer());
  }

  protected tabDomId(key: RootView): string {
    return `root-details-tabbtn-${key}`;
  }

  protected isActive(key: RootView): boolean {
    return this.view() === key;
  }

  protected selectView(key: RootView): void {
    if (this.emptySelection() || key === this.view()) {
      return;
    }
    this.viewChange.emit(key);
  }

  protected onEscape(): void {
    if (this.drawerMode() || this.hasSelection()) {
      this.close.emit();
    }
  }

  protected onTabKeydown(event: KeyboardEvent, currentKey: RootView): void {
    const order = ROOT_VIEW_KEYS;
    const index = order.indexOf(currentKey);
    let nextIndex: number | null = null;

    switch (event.key) {
      case 'ArrowLeft':
        nextIndex = (index + 1) % order.length;
        break;
      case 'ArrowRight':
        nextIndex = (index - 1 + order.length) % order.length;
        break;
      case 'Home':
        nextIndex = 0;
        break;
      case 'End':
        nextIndex = order.length - 1;
        break;
      default:
        return;
    }

    event.preventDefault();
    if (nextIndex === null) {
      return;
    }

    const nextKey = order[nextIndex];
    this.selectView(nextKey);
    this.focusTab(nextKey);
  }

  private focusTab(key: RootView): void {
    const list = this.tabList()?.nativeElement;
    const tab = list?.querySelector<HTMLElement>(`[data-root-tab="${key}"]`);
    tab?.focus();
  }

  private observeViewport(): void {
    if (typeof window === 'undefined' || typeof window.matchMedia !== 'function') {
      return;
    }

    const query = window.matchMedia(NARROW_VIEWPORT_QUERY);
    this.isNarrow.set(query.matches);

    const onChange = (event: MediaQueryListEvent) => this.isNarrow.set(event.matches);
    query.addEventListener('change', onChange);
    this.destroyRef.onDestroy(() => query.removeEventListener('change', onChange));
  }

  private enterDrawer(): void {
    if (this.focusTrap || typeof document === 'undefined') {
      return;
    }
    const root = this.panelRoot()?.nativeElement;
    if (!root) {
      return;
    }
    this.previouslyFocused = document.activeElement as HTMLElement | null;
    this.focusTrap = this.focusTrapFactory.create(root);
    void this.focusTrap.focusInitialElementWhenReady();
  }

  private exitDrawer(): void {
    if (!this.focusTrap) {
      return;
    }
    this.focusTrap.destroy();
    this.focusTrap = undefined;
    this.previouslyFocused?.focus?.();
    this.previouslyFocused = null;
  }
}
