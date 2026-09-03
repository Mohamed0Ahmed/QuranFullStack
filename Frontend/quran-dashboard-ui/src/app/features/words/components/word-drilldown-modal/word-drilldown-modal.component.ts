import { NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { ScrollingModule } from '@angular/cdk/scrolling';

import { DetailOverlayHistoryService } from '../../../../core/navigation/detail-overlay/detail-overlay-history.service';
import { DetailFrame } from '../../../../core/navigation/detail-overlay/detail-overlay.models';
import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { QdDetailsWorkspaceComponent } from '../../../../shared/ui/details-workspace/details-workspace.component';
import { QdErrorStateComponent } from '../../../../shared/ui/error-state/error-state.component';
import { QdModalShellComponent } from '../../../../shared/ui/modal-shell/modal-shell.component';
import { QdTabDirective } from '../../../../shared/ui/tabs/tab.directive';
import { QdTabsComponent } from '../../../../shared/ui/tabs/tabs.component';

import { ExplorerPanelSkeletonComponent } from '../../../../shared/ui/explorer-panel-skeleton/explorer-panel-skeleton.component';
import { SurahOccurrencesListComponent } from '../surah-occurrences-list/surah-occurrences-list.component';
import { MissingSurahsListComponent } from '../missing-surahs-list/missing-surahs-list.component';
import { AyahMatchesListComponent } from '../ayah-matches-list/ayah-matches-list.component';
import { AyahTypeFiltersComponent } from '../ayah-type-filters/ayah-type-filters.component';
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
import {
  AyahMatchDto,
  WordDrilldownState,
  WordDrilldownView,
} from '../../models/unique-words.models';
import { WORDS_DETAIL_RETRY_LABEL } from '../../models/words-shared.labels';
import { mapUniqueWordSummaryDisplayText } from '../../utils/unique-words-display.mapper';
import { QuranSourceLinkingActionsComponent } from '../../../linking/components/quran-source-linking-actions/quran-source-linking-actions.component';
import { LinkingSourceDescriptor } from '../../../linking/models/linking-source.models';
import { parseQuranVerseKey } from '../../../../shared/quran/quran-location';

@Component({
  selector: 'qd-word-drilldown-modal',
  standalone: true,
  imports: [
    QdModalShellComponent,
    NgTemplateOutlet,
    ScrollingModule,
    ExplorerPanelSkeletonComponent,
    QdActionDirective,
    QdDetailsWorkspaceComponent,
    QdErrorStateComponent,
    QdTabDirective,
    QdTabsComponent,
    SurahOccurrencesListComponent,
    MissingSurahsListComponent,
    AyahMatchesListComponent,
    AyahTypeFiltersComponent,
    QuranSourceLinkingActionsComponent,
  ],
  templateUrl: './word-drilldown-modal.component.html',
  styleUrl: './word-drilldown-modal.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WordDrilldownModalComponent {
  private readonly detailOverlayHistory = inject(DetailOverlayHistoryService);

  protected readonly drawerTrapEnabled = computed(() => !this.detailOverlayHistory.isOpen());

  readonly state = input.required<WordDrilldownState>();
  readonly inline = input(false);
  readonly frameless = input(false);
  readonly parentFrame = input<DetailFrame | null>(null);

  readonly closeModal = output<void>();
  readonly viewChange = output<WordDrilldownView>();
  readonly ayahPageChange = output<number>();
  readonly typeCodeChange = output<string | null>();

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

  protected readonly linkingSource = computed<LinkingSourceDescriptor | null>(() => {
    const state = this.state();
    if (!state.isOpen || state.summary === null || state.selectedWordId === null) {
      return null;
    }
    return {
      kind: 'unique-word',
      mode: state.summary.kind,
      wordId: state.selectedWordId,
      typeCodes: state.ayahTypeCode === null ? [] : [state.ayahTypeCode],
      label: mapUniqueWordSummaryDisplayText(state.summary).displayText,
    };
  });

  protected readonly drilldownViews: readonly WordDrilldownView[] = ['surahs', 'missing', 'ayahs'];

  protected readonly hasSelection = computed(() => (this.inline() ? this.state().isOpen : true));

  protected readonly ayahsPage = computed(() => {
    const page = this.state().ayahs;
    if (!page) {
      return null;
    }
    const items = page.items.flatMap((match): AyahMatchDto[] => {
      const verse = parseQuranVerseKey(match.verseKey);
      if (!verse) {
        return [];
      }
      return [{ ...match, verseKey: verse.key }];
    });
    return { ...page, items };
  });

  protected drilldownLabel(view: WordDrilldownView): string {
    return WORD_DRILLDOWN_VIEW_LABELS[view];
  }

  protected drilldownCount(view: WordDrilldownView): number | null {
    const summary = this.state().summary;
    if (summary === null) {
      return null;
    }

    switch (view) {
      case 'surahs':
        return summary.surahsCount;
      case 'missing':
        return summary.missingSurahsCount;
      case 'ayahs':
        return summary.ayahsCount;
    }
  }

  protected drilldownCountLabel(view: WordDrilldownView): string {
    const count = this.drilldownCount(view);
    return count === null ? '' : `${count}`;
  }

  protected drilldownTabAriaLabel(view: WordDrilldownView): string {
    const label = this.drilldownLabel(view);
    const count = this.drilldownCount(view);
    return count === null ? label : `${label}، ${count}`;
  }
}
