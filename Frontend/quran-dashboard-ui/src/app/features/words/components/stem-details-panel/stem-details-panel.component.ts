import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { QdDetailsPanelShellComponent } from '../details-panel-shell/details-panel-shell.component';
import { QuranSourceLinkingActionsComponent } from '../../../linking/components/quran-source-linking-actions/quran-source-linking-actions.component';
import { LinkingSourceDescriptor } from '../../../linking/models/linking-source.models';

import {
  STEMS_EMPTY_SELECTION_LABEL,
  STEMS_NOT_FOUND_LABEL,
  STEMS_PANEL_LABEL,
  STEMS_PANEL_TAB_ARIA,
  STEMS_PANEL_TAB_LABELS,
} from '../../models/stems.labels';
import { CLOSE_LABEL } from '../../models/unique-words.labels';
import { STEM_VIEW_KEYS, StemSummaryDto, StemView, StemWordView } from '../../models/stems.models';
import { stemDetailTabCounts } from '../../utils/words-detail-tab-counts';

@Component({
  selector: 'qd-stem-details-panel',
  standalone: true,
  imports: [QdDetailsPanelShellComponent, QuranSourceLinkingActionsComponent],
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
  readonly summary = input<StemSummaryDto | null>(null);
  readonly wordView = input<StemWordView>('simple');
  readonly lemmasCount = input<number | null>(null);
  readonly loading = input(false);
  readonly notFound = input(false);
  readonly notFoundMessage = input('');
  readonly linkingSource = input<LinkingSourceDescriptor | null>(null);

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
  protected readonly tabCounts = computed(() =>
    stemDetailTabCounts(this.summary(), this.wordView(), this.lemmasCount()),
  );
}
