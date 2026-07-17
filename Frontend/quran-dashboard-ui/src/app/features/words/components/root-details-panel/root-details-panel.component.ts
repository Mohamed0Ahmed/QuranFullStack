import { NgTemplateOutlet } from '@angular/common';
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
import { A11yModule } from '@angular/cdk/a11y';

import { DetailOverlayHistoryService } from '../../../../core/navigation/detail-overlay/detail-overlay-history.service';
import { ModalScrollLockDirective } from '../../../../shared/ui/modal-scroll-lock/modal-scroll-lock.directive';

import {
  ROOTS_EMPTY_SELECTION_LABEL,
  ROOTS_NOT_FOUND_LABEL,
  ROOTS_PANEL_LABEL,
  ROOTS_PANEL_TAB_ARIA,
  ROOTS_PANEL_TAB_LABELS,
} from '../../models/roots.labels';
import { CLOSE_LABEL } from '../../models/unique-words.labels';
import { ROOT_VIEW_KEYS, RootView } from '../../models/roots.models';

/**
 * Per-instance id seed. A frameless overlay copy of this panel can be mounted at
 * the same time as the inert background panel; a shared id would make both tabs'
 * `aria-controls` and the surface `aria-labelledby` resolve ambiguously. Each
 * instance takes a distinct prefix so its ARIA relationships stay self-contained.
 */
let nextRootDetailsPanelInstanceId = 0;

@Component({
  selector: 'qd-root-details-panel',
  standalone: true,
  imports: [A11yModule, ModalScrollLockDirective, NgTemplateOutlet],
  templateUrl: './root-details-panel.component.html',
  styleUrl: './root-details-panel.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RootDetailsPanelComponent {
  private readonly detailOverlayHistory = inject(DetailOverlayHistoryService);

  /**
   * Only the top layer may trap focus (Feature 029 §5.9). While the global
   * detail overlay is open this drawer sits inside the inert app shell, so its
   * own trap stands down and the dialog's trap is the only enabled one.
   */
  protected readonly drawerTrapEnabled = computed(() => !this.detailOverlayHistory.isOpen());

  readonly view = input.required<RootView>();
  readonly inline = input(true);
  /**
   * Content-only mode (Feature 029, Change B4): render just the view tablist +
   * tabpanel body in a plain wrapper — no card section, no dialog/backdrop, no
   * header/close. Used inside the global detail overlay shell, which owns the
   * dialog chrome. When false, the inline/modal branches behave as before.
   */
  readonly frameless = input(false);
  readonly emptySelection = input(false);
  readonly selectionTitle = input('');
  readonly loading = input(false);
  readonly notFound = input(false);
  /** Server-supplied not-found text; falls back to the generic label when absent. */
  readonly notFoundMessage = input('');

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

  protected get notFoundLabel() {
    return ROOTS_NOT_FOUND_LABEL;
  }

  private readonly instanceIdPrefix = `root-details-panel-${nextRootDetailsPanelInstanceId++}`;
  protected readonly surfaceDomId = `${this.instanceIdPrefix}-surface`;

  protected readonly tabs = ROOT_VIEW_KEYS.map((key) => ({
    key,
    label: ROOTS_PANEL_TAB_LABELS[key],
    aria: ROOTS_PANEL_TAB_ARIA[key],
  }));

  private readonly tabList = viewChild<ElementRef<HTMLElement>>('tabList');

  protected readonly hasSelection = computed(() => !this.emptySelection());

  protected tabDomId(key: RootView): string {
    return `${this.instanceIdPrefix}-tabbtn-${key}`;
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
    if (!this.inline() || this.hasSelection()) {
      this.close.emit();
    }
  }

  protected onBackdropClick(event: MouseEvent): void {
    if (event.target === event.currentTarget) {
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
}
