import { NgTemplateOutlet } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  computed,
  input,
  output,
  viewChild,
} from '@angular/core';
import { A11yModule } from '@angular/cdk/a11y';

import { ModalScrollLockDirective } from '../../../../shared/ui/modal-scroll-lock/modal-scroll-lock.directive';

import {
  LEMMAS_EMPTY_SELECTION_LABEL,
  LEMMAS_NOT_FOUND_LABEL,
  LEMMAS_PANEL_LABEL,
  LEMMAS_PANEL_TAB_ARIA,
  LEMMAS_PANEL_TAB_LABELS,
} from '../../models/lemmas.labels';
import { CLOSE_LABEL } from '../../models/unique-words.labels';
import { LEMMA_VIEW_KEYS, LemmaView } from '../../models/lemmas.models';

/**
 * Lemmas Explorer persistent detail panel shell (Feature 016). Sibling of
 * `RootDetailsPanelComponent`. Renders exactly four tabs — الكلمات / الآيات /
 * السور / الأصول الصرفية — with no overview tab. Pure chrome: receives the
 * active view and emits `viewChange`; the active view content is projected via
 * `<ng-content />` by the explorer page.
 *
 * Responsive drawer + focus handling (T117): on desktop the page renders the
 * panel inline via `inline=true`; below the desktop breakpoint the page renders
 * the panel as a modal drawer (`inline=false`) with `cdkTrapFocus` +
 * `cdkTrapFocusAutoCapture`, `role="dialog"`, `aria-modal="true"`, an explicit
 * backdrop click handler, and Escape-to-close. RTL is honoured through logical
 * CSS properties (padding-inline/padding-block/border-block-end) and the
 * tablist arrow keys (ArrowLeft moves forward in Arabic reading order).
 */
@Component({
  selector: 'qd-lemma-details-panel',
  standalone: true,
  imports: [A11yModule, ModalScrollLockDirective, NgTemplateOutlet],
  templateUrl: './lemma-details-panel.component.html',
  styleUrl: './lemma-details-panel.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LemmaDetailsPanelComponent {
  readonly view = input.required<LemmaView>();
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

  readonly viewChange = output<LemmaView>();
  readonly close = output<void>();

  protected get panelLabel() {
    return LEMMAS_PANEL_LABEL;
  }

  protected get closeLabel() {
    return CLOSE_LABEL;
  }

  protected get emptySelectionLabel() {
    return LEMMAS_EMPTY_SELECTION_LABEL;
  }

  protected get notFoundLabel() {
    return LEMMAS_NOT_FOUND_LABEL;
  }

  protected readonly surfaceDomId = 'lemma-details-panel-surface';

  protected readonly tabs = LEMMA_VIEW_KEYS.map((key) => ({
    key,
    label: LEMMAS_PANEL_TAB_LABELS[key],
    aria: LEMMAS_PANEL_TAB_ARIA[key],
  }));

  private readonly tabList = viewChild<ElementRef<HTMLElement>>('tabList');

  protected readonly hasSelection = computed(() => !this.emptySelection());

  protected tabDomId(key: LemmaView): string {
    return `lemma-details-tabbtn-${key}`;
  }

  protected isActive(key: LemmaView): boolean {
    return this.view() === key;
  }

  protected selectView(key: LemmaView): void {
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

  protected onTabKeydown(event: KeyboardEvent, currentKey: LemmaView): void {
    const order = LEMMA_VIEW_KEYS;
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

  private focusTab(key: LemmaView): void {
    const list = this.tabList()?.nativeElement;
    const tab = list?.querySelector<HTMLElement>(`[data-lemma-tab="${key}"]`);
    tab?.focus();
  }
}
