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
  STEMS_EMPTY_SELECTION_LABEL,
  STEMS_NOT_FOUND_LABEL,
  STEMS_PANEL_LABEL,
  STEMS_PANEL_TAB_ARIA,
  STEMS_PANEL_TAB_LABELS,
} from '../../models/stems.labels';
import { CLOSE_LABEL } from '../../models/unique-words.labels';
import { STEM_VIEW_KEYS, StemView } from '../../models/stems.models';

@Component({
  selector: 'qd-stem-details-panel',
  standalone: true,
  imports: [A11yModule, ModalScrollLockDirective, NgTemplateOutlet],
  templateUrl: './stem-details-panel.component.html',
  styleUrl: './stem-details-panel.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StemDetailsPanelComponent {
  private readonly detailOverlayHistory = inject(DetailOverlayHistoryService);

  protected readonly drawerTrapEnabled = computed(() => !this.detailOverlayHistory.isOpen());

  readonly view = input.required<StemView>();
  readonly inline = input(true);
  readonly frameless = input(false);
  readonly emptySelection = input(false);
  readonly selectionTitle = input('');
  readonly loading = input(false);
  readonly notFound = input(false);
  readonly notFoundMessage = input('');

  readonly viewChange = output<StemView>();
  readonly close = output<void>();

  protected get panelLabel() {
    return STEMS_PANEL_LABEL;
  }

  protected get closeLabel() {
    return CLOSE_LABEL;
  }

  protected get emptySelectionLabel() {
    return STEMS_EMPTY_SELECTION_LABEL;
  }

  protected get notFoundLabel() {
    return STEMS_NOT_FOUND_LABEL;
  }

  protected readonly surfaceDomId = 'stem-details-panel-surface';

  protected readonly tabs = STEM_VIEW_KEYS.map((key) => ({
    key,
    label: STEMS_PANEL_TAB_LABELS[key],
    aria: STEMS_PANEL_TAB_ARIA[key],
  }));

  private readonly tabList = viewChild<ElementRef<HTMLElement>>('tabList');

  protected readonly hasSelection = computed(() => !this.emptySelection());

  protected tabDomId(key: StemView): string {
    return `stem-details-tabbtn-${key}`;
  }

  protected isActive(key: StemView): boolean {
    return this.view() === key;
  }

  protected selectView(key: StemView): void {
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

  protected onTabKeydown(event: KeyboardEvent, currentKey: StemView): void {
    const order = STEM_VIEW_KEYS;
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

  private focusTab(key: StemView): void {
    const list = this.tabList()?.nativeElement;
    const tab = list?.querySelector<HTMLElement>(`[data-stem-tab="${key}"]`);
    tab?.focus();
  }
}
