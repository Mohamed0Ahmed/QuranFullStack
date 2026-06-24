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
  ROOTS_CLOSE_PANEL_LABEL,
  ROOTS_EMPTY_SELECTION_LABEL,
  ROOTS_LOADING_LABEL,
  ROOTS_PANEL_LABEL,
  ROOTS_PANEL_TAB_ARIA,
  ROOTS_PANEL_TAB_LABELS,
} from '../../models/roots.labels';
import { ROOT_VIEW_KEYS, RootView } from '../../models/roots.models';

/** Viewport width below which the panel becomes a dismissible drawer. */
const NARROW_VIEWPORT_QUERY = '(max-width: 60rem)';

/**
 * Roots Explorer persistent detail panel shell (Feature 015, T020 + T069/T070).
 *
 * Shell only: no view content, no data calls. It renders its own independent
 * scroll container, a `role="tablist"` strip with EXACTLY the 5 named tabs (no
 * "نظرة عامة" tab), and the empty-selection state.
 *
 * Accessibility (T070): roving `tabindex` + RTL-aware arrow keys on the tablist;
 * each tab is linked to the single `role="tabpanel"` surface via
 * `aria-controls`/`aria-labelledby`; the surface is focusable so keyboard users
 * can scroll it; load status is announced by the page via `role="status"`.
 *
 * Drawer (T069): on narrow viewports a selected root opens as a dismissible
 * drawer — focus is trapped inside it, `Esc` and a backdrop click close it, and
 * focus returns to the control that opened it. Desktop renders the panel inline.
 * Viewport detection is `matchMedia`-guarded and defaults to desktop where it is
 * unavailable (SSR / the jsdom test runner).
 */
@Component({
  selector: 'qd-root-details-panel',
  standalone: true,
  templateUrl: './root-details-panel.component.html',
  styleUrl: './root-details-panel.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RootDetailsPanelComponent {
  /** Active panel view (drives `aria-selected`/roving tabindex). */
  readonly view = input.required<RootView>();

  /** True when no root is selected → render the empty-selection state. */
  readonly emptySelection = input(false);

  /** Optional loading flag for the panel surface (drives `aria-busy`). */
  readonly loading = input(false);

  /** A selected root may be missing/invalid → controlled not-found flag. */
  readonly notFound = input(false);

  readonly viewChange = output<RootView>();

  /** Drawer dismissed (narrow screens): the page clears the selection. */
  readonly close = output<void>();

  protected readonly panelLabel = ROOTS_PANEL_LABEL;
  protected readonly closeLabel = ROOTS_CLOSE_PANEL_LABEL;
  protected readonly emptySelectionLabel = ROOTS_EMPTY_SELECTION_LABEL;
  protected readonly loadingLabel = ROOTS_LOADING_LABEL;

  /** Stable DOM id for the single tabpanel surface (aria-controls target). */
  protected readonly surfaceDomId = 'root-details-panel-surface';

  /** Ordered, stable tab definitions (exactly 5; no overview tab). */
  protected readonly tabs = ROOT_VIEW_KEYS.map((key) => ({
    key,
    label: ROOTS_PANEL_TAB_LABELS[key],
    aria: ROOTS_PANEL_TAB_ARIA[key],
  }));

  private readonly panelRoot = viewChild<ElementRef<HTMLElement>>('panelRoot');
  private readonly tabList = viewChild<ElementRef<HTMLElement>>('tabList');

  /** Whether a root is selected (panel surface shown vs empty state). */
  protected readonly hasSelection = computed(() => !this.emptySelection());

  /** Narrow viewport → the panel renders as a dismissible drawer. */
  private readonly isNarrow = signal(false);
  protected readonly drawerMode = computed(() => this.isNarrow() && this.hasSelection());

  private readonly focusTrapFactory = inject(FocusTrapFactory);
  private readonly destroyRef = inject(DestroyRef);
  private focusTrap?: FocusTrap;
  private previouslyFocused: HTMLElement | null = null;

  constructor() {
    this.observeViewport();
    // Trap focus while the drawer is open; restore it to the opener on close.
    effect(() => (this.drawerMode() ? this.enterDrawer() : this.exitDrawer()));
    this.destroyRef.onDestroy(() => this.exitDrawer());
  }

  /** DOM id for a tab button (aria-labelledby target for the tabpanel). */
  protected tabDomId(key: RootView): string {
    return `root-details-tabbtn-${key}`;
  }

  protected isActive(key: RootView): boolean {
    return this.view() === key;
  }

  protected selectView(key: RootView): void {
    if (key === this.view()) {
      return;
    }
    this.viewChange.emit(key);
  }

  protected onEscape(): void {
    if (this.drawerMode()) {
      this.close.emit();
    }
  }

  /**
   * RTL-aware roving-tabindex arrow navigation for the tablist. The DOM order is
   * already RTL (Arabic-first), so ArrowLeft moves forward and ArrowRight moves
   * backward in reading order, mirroring the visual order.
   */
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
    // jsdom (test runner) and SSR lack matchMedia → stay on the desktop layout.
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
