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
import { CLOSE_LABEL } from '../../models/unique-words.labels';
import {
  WORD_TYPES_DETAILS_PANEL_LABEL,
  WORD_TYPES_EMPTY_SELECTION_LABEL,
  WORD_TYPES_NOT_FOUND_LABEL,
  WORD_TYPE_DETAIL_TAB_ARIA,
  WORD_TYPE_DETAIL_TAB_LABELS,
} from '../../models/word-types.labels';
import { WORD_TYPE_DETAIL_VIEW_KEYS, WordTypeDetailView } from '../../models/word-types.models';

@Component({
  selector: 'qd-word-type-details-panel',
  standalone: true,
  imports: [A11yModule, ModalScrollLockDirective, NgTemplateOutlet],
  templateUrl: './word-type-details-panel.component.html',
  styleUrl: './word-type-details-panel.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WordTypeDetailsPanelComponent {
  readonly view = input.required<WordTypeDetailView>();
  readonly inline = input(true);
  readonly emptySelection = input(false);
  readonly selectionTitle = input('');
  readonly loading = input(false);
  readonly notFound = input(false);

  readonly viewChange = output<WordTypeDetailView>();
  readonly close = output<void>();

  protected readonly panelLabel = WORD_TYPES_DETAILS_PANEL_LABEL;
  protected readonly closeLabel = CLOSE_LABEL;
  protected readonly emptySelectionLabel = WORD_TYPES_EMPTY_SELECTION_LABEL;
  protected readonly notFoundLabel = WORD_TYPES_NOT_FOUND_LABEL;
  protected readonly surfaceDomId = 'word-type-details-panel-surface';

  protected readonly tabs = WORD_TYPE_DETAIL_VIEW_KEYS.map((key) => ({
    key,
    label: WORD_TYPE_DETAIL_TAB_LABELS[key],
    aria: WORD_TYPE_DETAIL_TAB_ARIA[key],
  }));

  private readonly tabList = viewChild<ElementRef<HTMLElement>>('tabList');
  protected readonly hasSelection = computed(() => !this.emptySelection());

  protected tabDomId(key: WordTypeDetailView): string {
    return `word-type-details-tabbtn-${key}`;
  }

  protected isActive(key: WordTypeDetailView): boolean {
    return this.view() === key;
  }

  protected selectView(key: WordTypeDetailView): void {
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

  protected onTabKeydown(event: KeyboardEvent, currentKey: WordTypeDetailView): void {
    const order = WORD_TYPE_DETAIL_VIEW_KEYS;
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
    const nextKey = order[nextIndex!];
    this.selectView(nextKey);
    this.focusTab(nextKey);
  }

  private focusTab(key: WordTypeDetailView): void {
    const list = this.tabList()?.nativeElement;
    const tab = list?.querySelector<HTMLElement>(`[data-word-type-tab="${key}"]`);
    tab?.focus();
  }
}
