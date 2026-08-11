import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { QdTabDirective } from '../../../../shared/ui/tabs/tab.directive';
import { QdTabsComponent } from '../../../../shared/ui/tabs/tabs.component';
import {
  WORD_TYPE_TABLE_VIEW_OPTIONS,
  WORD_TYPE_TABLE_VIEW_TABS_LABEL,
} from '../../models/word-types.labels';
import { WordTypeTableView } from '../../models/word-types.models';

@Component({
  selector: 'qd-word-type-table-view-tabs',
  standalone: true,
  imports: [QdTabDirective, QdTabsComponent],
  templateUrl: './word-type-table-view-tabs.component.html',
  styleUrl: './word-type-table-view-tabs.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WordTypeTableViewTabsComponent {
  readonly selectedView = input.required<WordTypeTableView>();
  readonly disabled = input(false);
  readonly panelId = input<string | null>(null);
  readonly viewSelected = output<WordTypeTableView>();

  protected get tabsLabel() { return WORD_TYPE_TABLE_VIEW_TABS_LABEL; }
  protected get options() { return WORD_TYPE_TABLE_VIEW_OPTIONS; }

  tabId(view: WordTypeTableView): string {
    return `word-type-table-view-tab-${view}`;
  }

  protected isSelected(view: WordTypeTableView): boolean {
    return this.selectedView() === view;
  }

  protected selectView(view: WordTypeTableView): void {
    if (this.disabled() || this.isSelected(view)) {
      return;
    }

    this.viewSelected.emit(view);
  }
}
