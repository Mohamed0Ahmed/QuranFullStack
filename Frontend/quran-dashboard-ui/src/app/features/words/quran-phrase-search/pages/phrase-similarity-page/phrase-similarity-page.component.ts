import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, computed, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { ExplorerPanelSkeletonComponent } from '../../../../../shared/ui/explorer-panel-skeleton/explorer-panel-skeleton.component';
import { QdEmptyStateComponent } from '../../../../../shared/ui/empty-state/empty-state.component';
import { QdErrorStateComponent } from '../../../../../shared/ui/error-state/error-state.component';
import { QdNoticeComponent } from '../../../../../shared/ui/notice/notice.component';
import { QdRefreshingIndicatorComponent } from '../../../../../shared/ui/refreshing-indicator/refreshing-indicator.component';
import { PhraseQueryResolutionComponent } from '../../components/phrase-query-resolution/phrase-query-resolution.component';
import { PhraseSimilarityListComponent } from '../../components/phrase-similarity-list/phrase-similarity-list.component';
import { PhraseTextModeToggleComponent } from '../../components/phrase-text-mode-toggle/phrase-text-mode-toggle.component';
import { PhraseResolutionViewState } from '../../models/phrase-query.models';
import { PhraseTextMode, isPhraseTextMode } from '../../models/phrase-repetitions.models';
import { PhraseSimilarityFacade } from '../../state/phrase-similarity.facade';

@Component({
  selector: 'qd-phrase-similarity-page',
  standalone: true,
  imports: [
    ExplorerPanelSkeletonComponent,
    PhraseQueryResolutionComponent,
    PhraseSimilarityListComponent,
    PhraseTextModeToggleComponent,
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
  private readonly route = inject(ActivatedRoute);

  protected readonly state = this.facade.state;
  protected readonly availableModes = computed<readonly PhraseTextMode[]>(() =>
    (this.state().capabilities?.modes ?? []).map((item) => item.mode).filter(isPhraseTextMode),
  );
  protected readonly globalLengths = computed<readonly number[]>(() =>
    (
      this.state().capabilities?.modes.find((item) => item.mode === this.state().route.mode)
        ?.supportedLengths ?? []
    ).filter((length) => length >= 4),
  );
  protected readonly thresholds = computed(
    () => this.state().capabilities?.similarityThresholds ?? [50, 60, 70, 80, 90],
  );
  protected readonly resolutionView = computed<PhraseResolutionViewState>(() => ({
    rawQuery: this.facade.draft(),
    mode: this.state().route.mode,
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
    const state = this.state();
    return state.resultsStatus === 'loading' || state.resultsStatus === 'refreshing';
  });
  protected readonly maximumDifferencesAllowed = computed(() =>
    Math.floor(this.state().route.length / 2),
  );
  protected readonly countAnnouncement = computed(() => {
    const state = this.state();
    if (state.resultsStatus !== 'success' && state.resultsStatus !== 'empty') {
      return '';
    }
    return `تم تحميل ${state.totalCount} نتيجة تشابه`;
  });

  ngOnInit(): void {
    this.facade.bindToRoute(this.route);
  }

  ngOnDestroy(): void {
    this.facade.unbindFromRoute();
  }

  protected onLength(value: string): void {
    const parsed = Number(value);
    if (Number.isSafeInteger(parsed) && this.globalLengths().includes(parsed)) {
      this.facade.setLength(parsed);
    }
  }

  protected onMinimum(value: string): void {
    const parsed = Number(value);
    if (Number.isFinite(parsed) && parsed >= 50 && parsed <= 100) {
      this.facade.setMinimumPercent(parsed);
    }
  }

  protected onDifferences(value: string): void {
    const parsed = Number(value);
    if (Number.isSafeInteger(parsed) && parsed >= 0) {
      this.facade.setMaximumDifferences(parsed);
    }
  }
}
