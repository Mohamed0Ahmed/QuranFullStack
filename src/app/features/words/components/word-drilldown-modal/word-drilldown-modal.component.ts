import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { SurahOccurrencesListComponent } from '../surah-occurrences-list/surah-occurrences-list.component';
import { MissingSurahsListComponent } from '../missing-surahs-list/missing-surahs-list.component';
import { AyahMatchesListComponent } from '../ayah-matches-list/ayah-matches-list.component';
import {
  CLOSE_LABEL,
  DRILLDOWN_EMPTY_AYAHS_LABEL,
  DRILLDOWN_EMPTY_MISSING_LABEL,
  DRILLDOWN_EMPTY_SURAHS_LABEL,
  LOADING_LABEL,
  WORD_DRILLDOWN_VIEW_LABELS,
} from '../../models/unique-words.labels';
import { WordDrilldownState, WordDrilldownView } from '../../models/unique-words.models';

@Component({
  selector: 'qd-word-drilldown-modal',
  standalone: true,
  imports: [SurahOccurrencesListComponent, MissingSurahsListComponent, AyahMatchesListComponent],
  templateUrl: './word-drilldown-modal.component.html',
  styleUrl: './word-drilldown-modal.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WordDrilldownModalComponent {
  readonly state = input.required<WordDrilldownState>();

  readonly closeModal = output<void>();
  readonly viewChange = output<WordDrilldownView>();
  readonly ayahPageChange = output<number>();

  protected readonly closeLabel = CLOSE_LABEL;
  protected readonly loadingLabel = LOADING_LABEL;
  protected readonly emptySurahsLabel = DRILLDOWN_EMPTY_SURAHS_LABEL;
  protected readonly emptyMissingLabel = DRILLDOWN_EMPTY_MISSING_LABEL;
  protected readonly emptyAyahsLabel = DRILLDOWN_EMPTY_AYAHS_LABEL;

  protected readonly title = computed(() => this.state().summary?.displayTextUthmani ?? '');

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
