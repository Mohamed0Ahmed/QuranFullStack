import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  computed,
  inject,
  input,
  output,
  viewChild,
} from '@angular/core';

import {
  ROOTS_EMPTY_SELECTION_LABEL,
  ROOTS_LOADING_LABEL,
  ROOTS_PANEL_LABEL,
  ROOTS_PANEL_TAB_ARIA,
  ROOTS_PANEL_TAB_LABELS,
} from '../../models/roots.labels';
import { ROOT_VIEW_KEYS, RootView } from '../../models/roots.models';

/**
 * Roots Explorer persistent detail panel shell (Feature 015, T020).
 *
 * Shell only: no view content, no data calls. It renders:
 *  - its own scroll container (the panel scrolls independently from the table);
 *  - a `role="tablist"` strip with EXACTLY the 5 named tabs (الكلمات · الآيات ·
 *    السور · الصيغ المعجمية · الأصول الصرفية) and NO "نظرة عامة" tab;
 *  - the empty-selection state (`اختر جذرًا لعرض تفاصيله`) when nothing is
 *    selected;
 *  - drawer scaffolding for narrow screens (focus-trap/Esc/focus-return polished
 *    in T069).
 *
 * Story phases (US1–US5) plug per-view content into the `tabpanel` projection
 * slot. Tab selection is emitted to the page, which reflects `view` in the URL
 * (query param) so the active tab survives refresh/back-forward.
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

  protected readonly panelLabel = ROOTS_PANEL_LABEL;
  protected readonly emptySelectionLabel = ROOTS_EMPTY_SELECTION_LABEL;
  protected readonly loadingLabel = ROOTS_LOADING_LABEL;

  /** Ordered, stable tab definitions (exactly 5; no overview tab). */
  protected readonly tabs = ROOT_VIEW_KEYS.map((key) => ({
    key,
    label: ROOTS_PANEL_TAB_LABELS[key],
    aria: ROOTS_PANEL_TAB_ARIA[key],
  }));

  private readonly tabList =
    viewChild<ElementRef<HTMLElement>>('tabList');

  /** Stable tab keys for the template. */
  protected readonly tabKeys = ROOT_VIEW_KEYS;

  /** Whether a root is selected (panel surface shown vs empty state). */
  protected readonly hasSelection = computed(() => !this.emptySelection());

  protected isActive(key: RootView): boolean {
    return this.view() === key;
  }

  protected selectView(key: RootView): void {
    if (key === this.view()) {
      return;
    }
    this.viewChange.emit(key);
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
    if (!list) {
      return;
    }
    const tab = list.querySelector<HTMLElement>(`[data-root-tab="${key}"]`);
    tab?.focus();
  }
}
