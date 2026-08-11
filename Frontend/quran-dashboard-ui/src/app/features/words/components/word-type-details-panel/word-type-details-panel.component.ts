import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { QdDetailsPanelShellComponent } from '../details-panel-shell/details-panel-shell.component';
import { CLOSE_LABEL } from '../../models/unique-words.labels';
import { WORD_TYPE_DETAIL_PRESENTATIONS } from '../../models/word-types.labels';
import {
  WORD_TYPE_DETAIL_VIEWS,
  WORD_TYPE_DETAIL_VIEW_KEYS,
  WordTypeDetailView,
} from '../../models/word-types.models';
import { WordTypeDetailSelectionKind } from '../../models/word-types-detail.models';

@Component({
  selector: 'qd-word-type-details-panel',
  standalone: true,
  imports: [QdDetailsPanelShellComponent],
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
  readonly loading = input(false);
  readonly notFound = input(false);

  readonly viewChange = output<WordTypeDetailView>();
  readonly close = output<void>();

  protected get panelLabel() { return this.presentation.panelLabel; }
  protected get closeLabel() { return CLOSE_LABEL; }
  protected get emptySelectionLabel() { return this.presentation.emptySelectionLabel; }
  protected get notFoundLabel() { return this.presentation.notFoundLabel; }

  private get presentation() { return WORD_TYPE_DETAIL_PRESENTATIONS[this.kind()]; }

  // Word selections expose ayahs/surahs; grouped selections add the leading related-words tab.
  protected readonly tabKeys = computed<readonly WordTypeDetailView[]>(() =>
    this.kind() === 'word' ? WORD_TYPE_DETAIL_VIEW_KEYS : WORD_TYPE_DETAIL_VIEWS,
  );

  protected readonly tabs = computed(() =>
    this.tabKeys().map((key) => ({
      key,
      ...this.presentation.tabs[key],
    })),
  );
}
