import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { QdDetailsPanelShellComponent } from '../details-panel-shell/details-panel-shell.component';
import { QuranSourceLinkingActionsComponent } from '../../../linking/components/quran-source-linking-actions/quran-source-linking-actions.component';
import { LinkingSourceDescriptor } from '../../../linking/models/linking-source.models';
import { CLOSE_LABEL } from '../../models/unique-words.labels';
import { WORD_TYPE_DETAIL_PRESENTATIONS } from '../../models/word-types.labels';
import {
  WORD_TYPE_DETAIL_VIEWS,
  WORD_TYPE_DETAIL_VIEW_KEYS,
  WordTypeDetailView,
} from '../../models/word-types.models';
import { WordTypeDetailSelectionKind } from '../../models/word-types-detail.models';
import { wordTypeDetailTabCounts } from '../../utils/words-detail-tab-counts';

@Component({
  selector: 'qd-word-type-details-panel',
  standalone: true,
  imports: [QdDetailsPanelShellComponent, QuranSourceLinkingActionsComponent],
  templateUrl: './word-type-details-panel.component.html',
  styleUrl: './word-type-details-panel.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WordTypeDetailsPanelComponent {
  readonly view = input.required<WordTypeDetailView>();
  readonly kind = input<WordTypeDetailSelectionKind>('word');
  readonly inline = input(true);
  readonly frameless = input(false);
  readonly emptySelection = input(false);
  readonly selectionTitle = input('');
  readonly wordsCount = input<number | null>(null);
  readonly ayahsCount = input<number | null>(null);
  readonly surahsCount = input<number | null>(null);
  readonly loading = input(false);
  readonly notFound = input(false);
  readonly linkingSource = input<LinkingSourceDescriptor | null>(null);

  readonly viewChange = output<WordTypeDetailView>();
  readonly close = output<void>();

  protected get panelLabel() { return this.presentation.panelLabel; }
  protected get closeLabel() { return CLOSE_LABEL; }
  protected get emptySelectionLabel() { return this.presentation.emptySelectionLabel; }
  protected get notFoundLabel() { return this.presentation.notFoundLabel; }

  private get presentation() { return WORD_TYPE_DETAIL_PRESENTATIONS[this.kind()]; }

  protected readonly tabKeys = computed<readonly WordTypeDetailView[]>(() =>
    this.kind() === 'word' ? WORD_TYPE_DETAIL_VIEW_KEYS : WORD_TYPE_DETAIL_VIEWS,
  );

  protected readonly tabs = computed(() =>
    this.tabKeys().map((key) => ({
      key,
      ...this.presentation.tabs[key],
    })),
  );
  protected readonly tabCounts = computed(() =>
    wordTypeDetailTabCounts(this.wordsCount(), this.ayahsCount(), this.surahsCount()),
  );
}
