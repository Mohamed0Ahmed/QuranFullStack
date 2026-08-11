import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { QdDetailsPanelShellComponent } from '../details-panel-shell/details-panel-shell.component';

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
  imports: [QdDetailsPanelShellComponent],
  templateUrl: './stem-details-panel.component.html',
  styleUrl: './stem-details-panel.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StemDetailsPanelComponent {
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

  protected readonly tabs = STEM_VIEW_KEYS.map((key) => ({
    key,
    label: STEMS_PANEL_TAB_LABELS[key],
    aria: STEMS_PANEL_TAB_ARIA[key],
  }));
}
