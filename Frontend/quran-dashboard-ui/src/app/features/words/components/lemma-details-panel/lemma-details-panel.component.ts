import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { QdDetailsPanelShellComponent } from '../details-panel-shell/details-panel-shell.component';
import { QuranSourceLinkingActionsComponent } from '../../../linking/components/quran-source-linking-actions/quran-source-linking-actions.component';
import { LinkingSourceDescriptor } from '../../../linking/models/linking-source.models';

import {
  LEMMAS_EMPTY_SELECTION_LABEL,
  LEMMAS_NOT_FOUND_LABEL,
  LEMMAS_PANEL_LABEL,
  LEMMAS_PANEL_TAB_ARIA,
  LEMMAS_PANEL_TAB_LABELS,
} from '../../models/lemmas.labels';
import { CLOSE_LABEL } from '../../models/unique-words.labels';
import { LEMMA_VIEW_KEYS, LemmaSummaryDto, LemmaView, LemmaWordView } from '../../models/lemmas.models';
import { lemmaDetailTabCounts } from '../../utils/words-detail-tab-counts';

@Component({
  selector: 'qd-lemma-details-panel',
  standalone: true,
  imports: [QdDetailsPanelShellComponent, QuranSourceLinkingActionsComponent],
  templateUrl: './lemma-details-panel.component.html',
  styleUrl: './lemma-details-panel.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LemmaDetailsPanelComponent {
  readonly view = input.required<LemmaView>();
  readonly inline = input(true);
  readonly frameless = input(false);
  readonly emptySelection = input(false);
  readonly selectionTitle = input('');
  readonly summary = input<LemmaSummaryDto | null>(null);
  readonly wordView = input<LemmaWordView>('simple');
  readonly loading = input(false);
  readonly notFound = input(false);
  readonly notFoundMessage = input('');
  readonly linkingSource = input<LinkingSourceDescriptor | null>(null);

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

  protected readonly tabs = LEMMA_VIEW_KEYS.map((key) => ({
    key,
    label: LEMMAS_PANEL_TAB_LABELS[key],
    aria: LEMMAS_PANEL_TAB_ARIA[key],
  }));
  protected readonly tabCounts = computed(() => lemmaDetailTabCounts(this.summary(), this.wordView()));
}
