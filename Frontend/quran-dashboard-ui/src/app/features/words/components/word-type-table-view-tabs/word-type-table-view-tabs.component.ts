import { ChangeDetectionStrategy, Component, ElementRef, input, output, viewChild } from '@angular/core';

import {
  WORD_TYPE_TABLE_VIEW_OPTIONS,
  WORD_TYPE_TABLE_VIEW_TABS_LABEL,
} from '../../models/word-types.labels';
import { WordTypeTableView } from '../../models/word-types.models';

@Component({
  selector: 'qd-word-type-table-view-tabs',
  standalone: true,
  templateUrl: './word-type-table-view-tabs.component.html',
  styleUrl: './word-type-table-view-tabs.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WordTypeTableViewTabsComponent {
  readonly selectedView = input.required<WordTypeTableView>();
  readonly disabled = input(false);
  readonly viewSelected = output<WordTypeTableView>();

  private readonly tabList = viewChild<ElementRef<HTMLElement>>('tabList');

  protected get tabsLabel() { return WORD_TYPE_TABLE_VIEW_TABS_LABEL; }
  protected get options() { return WORD_TYPE_TABLE_VIEW_OPTIONS; }

  protected isSelected(view: WordTypeTableView): boolean {
    return this.selectedView() === view;
  }

  protected selectView(view: WordTypeTableView): void {
    if (this.disabled() || this.isSelected(view)) {
      return;
    }

    this.viewSelected.emit(view);
  }

  protected onTabKeydown(event: KeyboardEvent, currentView: WordTypeTableView): void {
    if (this.disabled()) {
      return;
    }

    const order = this.options;
    const currentIndex = order.findIndex((option) => option.value === currentView);
    let nextIndex: number | null = null;

    switch (event.key) {
      case 'ArrowLeft':
        nextIndex = (currentIndex + 1) % order.length;
        break;
      case 'ArrowRight':
        nextIndex = (currentIndex - 1 + order.length) % order.length;
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
    const nextView = order[nextIndex].value;
    this.selectView(nextView);
    this.focusTab(nextView);
  }

  private focusTab(view: WordTypeTableView): void {
    const tab = this.tabList()?.nativeElement.querySelector<HTMLElement>(
      `[data-testid="word-type-table-view-tab--${view}"]`,
    );
    tab?.focus();
  }
}
