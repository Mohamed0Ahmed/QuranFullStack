import { NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { A11yModule } from '@angular/cdk/a11y';
import { ScrollingModule } from '@angular/cdk/scrolling';

import { DetailOverlayHistoryService } from '../../../../core/navigation/detail-overlay/detail-overlay-history.service';
import { DetailFrame } from '../../../../core/navigation/detail-overlay/detail-overlay.models';
import { ModalScrollLockDirective } from '../../../../shared/ui/modal-scroll-lock/modal-scroll-lock.directive';

import { ExplorerPanelSkeletonComponent } from '../../../../shared/ui/explorer-panel-skeleton/explorer-panel-skeleton.component';
import { SurahOccurrencesListComponent } from '../surah-occurrences-list/surah-occurrences-list.component';
import { MissingSurahsListComponent } from '../missing-surahs-list/missing-surahs-list.component';
import { AyahMatchesListComponent } from '../ayah-matches-list/ayah-matches-list.component';
import {
  CLOSE_LABEL,
  DRILLDOWN_EMPTY_AYAHS_LABEL,
  DRILLDOWN_EMPTY_MISSING_LABEL,
  DRILLDOWN_EMPTY_SURAHS_LABEL,
  DRILLDOWN_PANEL_EMPTY_LABEL,
  DRILLDOWN_PANEL_TITLE,
  DRILLDOWN_VIEW_TABLIST_LABEL,
  LOADING_LABEL,
  WORD_DRILLDOWN_VIEW_LABELS,
} from '../../models/unique-words.labels';
import { WordDrilldownState, WordDrilldownView } from '../../models/unique-words.models';
import { WORDS_DETAIL_RETRY_LABEL } from '../../models/words-shared.labels';
import { QdStateComponent } from '../../../../shared/ui/state/state.component';
import { mapUniqueWordSummaryDisplayText } from '../../utils/unique-words-display.mapper';

@Component({
  selector: 'qd-word-drilldown-modal',
  standalone: true,
  imports: [
    A11yModule,
    ModalScrollLockDirective,
    NgTemplateOutlet,
    ScrollingModule,
    ExplorerPanelSkeletonComponent,
    QdStateComponent,
    SurahOccurrencesListComponent,
    MissingSurahsListComponent,
    AyahMatchesListComponent,
  ],
  templateUrl: './word-drilldown-modal.component.html',
  styleUrl: './word-drilldown-modal.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WordDrilldownModalComponent {
  private readonly detailOverlayHistory = inject(DetailOverlayHistoryService);

  /**
   * Only the top layer may trap focus (Feature 029 §5.9). While the global
   * detail overlay is open this drawer sits inside the inert app shell, so its
   * own trap stands down and the dialog's trap is the only enabled one.
   */
  protected readonly drawerTrapEnabled = computed(() => !this.detailOverlayHistory.isOpen());

  readonly state = input.required<WordDrilldownState>();
  readonly inline = input(false);
  /**
   * Content-only mode (Feature 029, Change B4): render just the view tablist +
   * drilldown body in a plain wrapper — no card section, no dialog/backdrop, no
   * header/close. Used inside the global detail overlay shell, which owns the
   * dialog chrome. When false, the inline/modal branches behave as before.
   */
  readonly frameless = input(false);
  /**
   * The drilldown's own typed unique frame (Feature 029, B7), threaded into the
   * ayah list so an ayah click keeps/promotes the detail context over the
   * Mushaf. The render site builds it: the page knows its route mode, the
   * overlay adapter passes its frame.
   */
  readonly parentFrame = input<DetailFrame | null>(null);

  readonly closeModal = output<void>();
  readonly viewChange = output<WordDrilldownView>();
  readonly ayahPageChange = output<number>();

  /**
   * The failed drilldown's single recovery action (Feature 030, M3). This
   * component is presentation-only: each render site re-drives its own
   * controller's current identity.
   */
  readonly retry = output<void>();

  protected readonly retryLabel = WORDS_DETAIL_RETRY_LABEL;

  protected get closeLabel() {
    return CLOSE_LABEL;
  }

  protected get loadingLabel() {
    return LOADING_LABEL;
  }

  protected get emptySurahsLabel() {
    return DRILLDOWN_EMPTY_SURAHS_LABEL;
  }

  protected get emptyMissingLabel() {
    return DRILLDOWN_EMPTY_MISSING_LABEL;
  }

  protected get emptyAyahsLabel() {
    return DRILLDOWN_EMPTY_AYAHS_LABEL;
  }

  protected get drilldownPanelTitle() {
    return DRILLDOWN_PANEL_TITLE;
  }

  protected get drilldownViewTablistLabel() {
    return DRILLDOWN_VIEW_TABLIST_LABEL;
  }

  protected get panelEmptyLabel() {
    return DRILLDOWN_PANEL_EMPTY_LABEL;
  }

  protected readonly entityTitle = computed(() => {
    if (!this.state().isOpen) {
      return '';
    }
    const summary = this.state().summary;
    return summary ? mapUniqueWordSummaryDisplayText(summary).displayText : '';
  });

  protected readonly drilldownViews: readonly WordDrilldownView[] = ['surahs', 'missing', 'ayahs'];

  protected drilldownLabel(view: WordDrilldownView): string {
    return WORD_DRILLDOWN_VIEW_LABELS[view];
  }

  protected onBackdropClick(event: MouseEvent): void {
    if (event.target === event.currentTarget) {
      this.closeModal.emit();
    }
  }
}
