import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { QdDetailsPanelShellComponent } from '../details-panel-shell/details-panel-shell.component';

import {
  ROOTS_EMPTY_SELECTION_LABEL,
  ROOTS_NOT_FOUND_LABEL,
  ROOTS_PANEL_LABEL,
  ROOTS_PANEL_TAB_ARIA,
  ROOTS_PANEL_TAB_LABELS,
} from '../../models/roots.labels';
import { CLOSE_LABEL } from '../../models/unique-words.labels';
import { ROOT_VIEW_KEYS, RootView } from '../../models/roots.models';

@Component({
  selector: 'qd-root-details-panel',
  standalone: true,
  imports: [QdDetailsPanelShellComponent],
  templateUrl: './root-details-panel.component.html',
  styleUrl: './root-details-panel.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RootDetailsPanelComponent {
  readonly view = input.required<RootView>();
  readonly inline = input(true);
  readonly frameless = input(false);
  readonly emptySelection = input(false);
  readonly selectionTitle = input('');
  readonly loading = input(false);
  readonly notFound = input(false);
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

  protected readonly tabs = ROOT_VIEW_KEYS.map((key) => ({
    key,
    label: ROOTS_PANEL_TAB_LABELS[key],
    aria: ROOTS_PANEL_TAB_ARIA[key],
  }));
}
