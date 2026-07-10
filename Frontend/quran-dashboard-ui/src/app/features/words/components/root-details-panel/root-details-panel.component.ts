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
  ROOTS_EMPTY_SELECTION_LABEL,
  ROOTS_PANEL_LABEL,
  ROOTS_PANEL_TAB_ARIA,
  ROOTS_PANEL_TAB_LABELS,
} from '../../models/roots.labels';
import { CLOSE_LABEL } from '../../models/unique-words.labels';
import { ROOT_VIEW_KEYS, RootView } from '../../models/roots.models';

@Component({
  selector: 'qd-root-details-panel',
  standalone: true,
  imports: [A11yModule, ModalScrollLockDirective, NgTemplateOutlet],
  templateUrl: './root-details-panel.component.html',
  styleUrl: './root-details-panel.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RootDetailsPanelComponent {
  readonly view = input.required<RootView>();
  readonly inline = input(true);
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

  private readonly tabList = viewChild<ElementRef<HTMLElement>>('tabList');

  protected readonly hasSelection = computed(() => !this.emptySelection());

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
