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
  LEMMAS_EMPTY_SELECTION_LABEL,
  LEMMAS_NOT_FOUND_LABEL,
  LEMMAS_PANEL_LABEL,
  LEMMAS_PANEL_TAB_ARIA,
  LEMMAS_PANEL_TAB_LABELS,
} from '../../models/lemmas.labels';
import { CLOSE_LABEL } from '../../models/unique-words.labels';
import { LEMMA_VIEW_KEYS, LemmaView } from '../../models/lemmas.models';

@Component({
  selector: 'qd-lemma-details-panel',
  standalone: true,
  imports: [A11yModule, ModalScrollLockDirective, NgTemplateOutlet],
  templateUrl: './lemma-details-panel.component.html',
  styleUrl: './lemma-details-panel.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LemmaDetailsPanelComponent {
  private readonly detailOverlayHistory = inject(DetailOverlayHistoryService);

  // Only the top layer may trap focus: while the global detail overlay is open this drawer sits in
  // the inert app shell, so its own trap stands down and the dialog's trap is the only one enabled.
  protected readonly drawerTrapEnabled = computed(() => !this.detailOverlayHistory.isOpen());

  readonly view = input.required<LemmaView>();
  readonly inline = input(true);
  // Content-only mode: renders just the tablist + tabpanel body; the global detail overlay shell
  // owns the dialog chrome (no card/backdrop/header here).
  readonly frameless = input(false);
  readonly emptySelection = input(false);
  readonly selectionTitle = input('');
  readonly loading = input(false);
  readonly notFound = input(false);
  // Server-supplied not-found text; falls back to the generic label when absent.
  readonly notFoundMessage = input('');

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
