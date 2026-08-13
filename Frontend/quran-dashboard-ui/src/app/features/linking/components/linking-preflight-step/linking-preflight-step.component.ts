import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';

import { QdErrorStateComponent } from '../../../../shared/ui/error-state/error-state.component';
import { ExplorerPanelSkeletonComponent } from '../../../../shared/ui/explorer-panel-skeleton/explorer-panel-skeleton.component';
import { QdNoticeComponent } from '../../../../shared/ui/notice/notice.component';
import { LINKING_LABELS } from '../../models/linking.labels';
import { LinkingSourcePreflight } from '../../models/linking-preflight.models';
import { LinkingPreflightPreviewFacade } from '../../state/linking-preflight-preview.facade';
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
  providers: [LinkingPreflightPreviewFacade],
  templateUrl: './linking-preflight-step.component.html',
  styleUrl: './linking-preflight-step.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LinkingPreflightStepComponent {
  private readonly workflow = inject(LinkingWorkflowFacade);
  private readonly preview = inject(LinkingPreflightPreviewFacade);

  protected readonly labels = LINKING_LABELS;
  protected readonly status = this.workflow.preflightStatus;
  protected readonly preflight = this.workflow.preflight;
  protected readonly operation = this.workflow.operation;
  protected readonly message = this.workflow.preflightMessage;
  protected readonly isBlocked = computed(() => this.preflight()?.isBlocked === true);
  protected readonly isNoOp = computed(() => this.preflight()?.isNoOp === true);
  protected readonly mergedAyahs = computed(() => this.operation()?.mergedSelection.ayahs ?? []);
  protected readonly mergedAyahsExpanded = signal(false);

  constructor() {
    effect(() => this.preview.synchronize(this.preflight(), this.operation()));
  }

  protected isExpanded(sourceIdentity: string): boolean {
    return this.preview.isExpanded(sourceIdentity);
  }

  protected toggleSource(source: LinkingSourcePreflight): void {
    this.preview.toggleSource(source);
  }

  protected toggleMergedAyahs(): void {
    this.mergedAyahsExpanded.update((expanded) => !expanded);
  }

  protected contributionModeLabel(contributionMode: string): string {
    switch (contributionMode) {
      case 'automatic':
        return this.labels.preflightContributionAutomatic;
      case 'manual_single':
        return this.labels.preflightContributionManualSingle;
      case 'manual_independent':
        return this.labels.preflightContributionManualIndependent;
      case 'manual_grouped':
        return this.labels.preflightContributionManualGrouped;
      default:
        return this.labels.preflightContributionUnknown;
    }
  }

  protected retry(): void {
    this.workflow.retryPreflight();
  }
}
