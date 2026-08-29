import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  OnInit,
  computed,
  effect,
  inject,
  untracked,
} from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { ExplorerPanelSkeletonComponent } from '../../../../../shared/ui/explorer-panel-skeleton/explorer-panel-skeleton.component';
import { QdEmptyStateComponent } from '../../../../../shared/ui/empty-state/empty-state.component';
import { QdErrorStateComponent } from '../../../../../shared/ui/error-state/error-state.component';
import { QdNoticeComponent } from '../../../../../shared/ui/notice/notice.component';
import { QdRefreshingIndicatorComponent } from '../../../../../shared/ui/refreshing-indicator/refreshing-indicator.component';
import { PhraseQueryResolutionComponent } from '../../components/phrase-query-resolution/phrase-query-resolution.component';
import { PhraseSimilarityListComponent } from '../../components/phrase-similarity-list/phrase-similarity-list.component';
import { PhraseResolutionViewState } from '../../models/phrase-query.models';
import { PhraseTextMode, isPhraseTextMode } from '../../models/phrase-repetitions.models';
import { isPhraseSimilarityResultSort } from '../../models/phrase-similarity.models';
import { PhraseSimilarityFacade } from '../../state/phrase-similarity.facade';
import { manualDifferenceOptions } from '../../state/phrase-similarity-threshold';
import {
  PhraseSimilarityAyahSelectionStore,
  phraseSimilarityResultSetKey,
} from '../../state/phrase-similarity-ayah-selection.store';

@Component({
  selector: 'qd-phrase-similarity-page',
  standalone: true,
  imports: [
    ExplorerPanelSkeletonComponent,
    PhraseQueryResolutionComponent,
    PhraseSimilarityListComponent,
    QdEmptyStateComponent,
    QdErrorStateComponent,
    QdNoticeComponent,
    QdRefreshingIndicatorComponent,
  ],
  templateUrl: './phrase-similarity-page.component.html',
  styleUrl: './phrase-similarity-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PhraseSimilarityPageComponent implements OnInit, OnDestroy {
  protected readonly facade = inject(PhraseSimilarityFacade);
  protected readonly ayahSelection = inject(PhraseSimilarityAyahSelectionStore);
  private readonly route = inject(ActivatedRoute);

  protected readonly state = this.facade.state;
  protected readonly availableModes = computed<readonly PhraseTextMode[]>(() =>
    (this.state().capabilities?.modes ?? []).map((item) => item.mode).filter(isPhraseTextMode),
  );
  protected readonly manualDifferences = computed(() =>
    manualDifferenceOptions(this.state().route.length),
  );
  protected readonly resolutionView = computed<PhraseResolutionViewState>(() => ({
    rawQuery: this.facade.draft(),
    mode: this.facade.draftMode(),
    status: this.state().resolutionStatus,
    candidates: this.state().candidates,
    selectedResolutionRef: this.state().route.resolution,
    message:
      this.state().resolutionStatus !== 'idle' &&
      this.state().resolutionStatus !== 'loading' &&
      this.state().resolutionStatus !== 'resolved'
        ? this.state().errorMessage
        : '',
  }));
  protected readonly busy = computed(() => {
    const status = this.state().resultsStatus;
    return status === 'loading' || status === 'refreshing';
  });
  protected readonly countAnnouncement = computed(() => {
    const state = this.state();
    if (state.resultsStatus !== 'success' && state.resultsStatus !== 'empty') {
      return '';
    }
    return `تم تحميل ${state.totalAyahCount} آية في ${state.totalOccurrenceCount} موضعًا`;
  });

  constructor() {
    effect(() => {
      const state = this.state();
      const resultSetKey = phraseSimilarityResultSetKey(
        state.route.build,
        state.route.resolution,
        this.facade.minimumMatchedWords(),
      );
      untracked(() => {
        this.ayahSelection.synchronizeResultSet(resultSetKey);
        this.ayahSelection.setTotalAyahCount(state.totalAyahCount);
      });
    });
  }

  ngOnInit(): void {
    this.facade.bindToRoute(this.route);
  }

  ngOnDestroy(): void {
    this.facade.unbindFromRoute();
  }

  protected onDifferences(value: string): void {
    const parsed = Number(value);
    if (Number.isSafeInteger(parsed) && this.manualDifferences().includes(parsed)) {
      this.facade.setMaximumDifferences(parsed);
    }
  }

  protected onSort(value: string): void {
    if (isPhraseSimilarityResultSort(value)) {
      this.facade.setSort(value);
    }
  }
}
