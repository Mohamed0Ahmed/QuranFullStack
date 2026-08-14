import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';

import { QdErrorStateComponent } from '../../../../shared/ui/error-state/error-state.component';
import { ExplorerPanelSkeletonComponent } from '../../../../shared/ui/explorer-panel-skeleton/explorer-panel-skeleton.component';
import { QdNoticeComponent } from '../../../../shared/ui/notice/notice.component';
import { LinkingPreparedSourceSummaryDto } from '../../../../core/api/generated/models/linking-prepared-source-summary-dto';
import { LINKING_LABELS } from '../../models/linking.labels';
import { LinkingWorkflowFacade } from '../../state/linking-workflow.facade';
import { LinkingPreflightAyahViewerComponent } from '../linking-preflight-ayah-viewer/linking-preflight-ayah-viewer.component';
import { LinkingPreflightMergedAyahViewerComponent } from '../linking-preflight-merged-ayah-viewer/linking-preflight-merged-ayah-viewer.component';

@Component({
  selector: 'qd-linking-preflight-step',
  standalone: true,
  imports: [
    QdErrorStateComponent,
    ExplorerPanelSkeletonComponent,
    QdNoticeComponent,
    LinkingPreflightAyahViewerComponent,
    LinkingPreflightMergedAyahViewerComponent,
  ],
  templateUrl: './linking-preflight-step.component.html',
  styleUrl: './linking-preflight-step.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LinkingPreflightStepComponent {
  protected readonly workflow = inject(LinkingWorkflowFacade);
  protected readonly labels = LINKING_LABELS;
  protected readonly status = this.workflow.preflightStatus;
  protected readonly preflight = this.workflow.prepared;
  protected readonly message = this.workflow.preflightMessage;
  protected readonly expandedSourceId = signal<number | null>(null);
  protected readonly mergedAyahsExpanded = signal(false);
  protected readonly stateGeneration = computed(() => this.workflow.state().operationGeneration);
  protected readonly isBlocked = computed(() => this.preflight()?.isBlocked === true);
  protected readonly isNoOp = computed(() => this.preflight()?.isNoOp === true);

  protected toggleSource(source: LinkingPreparedSourceSummaryDto): void {
    this.mergedAyahsExpanded.set(false);
    this.expandedSourceId.update((current) =>
      current === source.preparedSourceId ? null : source.preparedSourceId,
    );
  }

  protected toggleMergedAyahs(): void {
    this.expandedSourceId.set(null);
    this.mergedAyahsExpanded.update((expanded) => !expanded);
  }

  protected contributionModeLabel(contributionMode: string): string {
    switch (contributionMode) {
      case 'automatic': return this.labels.preflightContributionAutomatic;
      case 'manual_single': return this.labels.preflightContributionManualSingle;
      case 'manual_independent': return this.labels.preflightContributionManualIndependent;
      case 'manual_grouped': return this.labels.preflightContributionManualGrouped;
      default: return this.labels.preflightContributionUnknown;
    }
  }

  protected retry(): void {
    this.workflow.retryPreflight();
  }
}
