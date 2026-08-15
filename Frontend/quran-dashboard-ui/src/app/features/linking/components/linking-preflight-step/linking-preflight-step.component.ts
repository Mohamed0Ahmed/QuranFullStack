import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';

import { QdErrorStateComponent } from '../../../../shared/ui/error-state/error-state.component';
import { ExplorerPanelSkeletonComponent } from '../../../../shared/ui/explorer-panel-skeleton/explorer-panel-skeleton.component';
import { QdNoticeComponent } from '../../../../shared/ui/notice/notice.component';
import { LinkingPreparedPreflightStatusDto } from '../../../../core/api/generated/models/linking-prepared-preflight-status-dto';
import { LINKING_LABELS } from '../../models/linking.labels';
import { LinkingWorkflowFacade } from '../../state/linking-workflow.facade';
import { LinkingPreflightMergedAyahViewerComponent } from '../linking-preflight-merged-ayah-viewer/linking-preflight-merged-ayah-viewer.component';

@Component({
  selector: 'qd-linking-preflight-step',
  standalone: true,
  imports: [
    QdErrorStateComponent,
    ExplorerPanelSkeletonComponent,
    QdNoticeComponent,
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
  protected readonly stateGeneration = computed(() => this.workflow.state().operationGeneration);
  protected readonly isBlocked = computed(() => this.preflight()?.isBlocked === true);
  protected readonly isNoOp = computed(() => this.preflight()?.isNoOp === true);
  protected readonly progressValue = computed(() => calculateProgress(this.preflight()));
  protected readonly progressStageLabel = computed(() => {
    switch (this.preflight()?.stage.toLowerCase()) {
      case 'resolving': return this.labels.preflightStages.resolving;
      case 'classifying': return this.labels.preflightStages.classifying;
      case 'persisting': return this.labels.preflightStages.persisting;
      default: return this.labels.preflightStages.unknown;
    }
  });

  protected retry(): void {
    this.workflow.retryPreflight();
  }
}

function calculateProgress(resource: LinkingPreparedPreflightStatusDto | null): number {
  return resource === null
    ? 0
    : Math.round(100 * progressRatio(resource.processedAyahs, resource.totalAyahs));
}

function progressRatio(processed: number, total: number | null): number {
  if (total === null || total <= 0) {
    return 0;
  }
  return Math.min(Math.max(processed / total, 0), 1);
}
