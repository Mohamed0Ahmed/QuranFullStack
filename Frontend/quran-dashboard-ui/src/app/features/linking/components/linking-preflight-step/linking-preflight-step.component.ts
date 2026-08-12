import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';

import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { QdErrorStateComponent } from '../../../../shared/ui/error-state/error-state.component';
import { ExplorerPanelSkeletonComponent } from '../../../../shared/ui/explorer-panel-skeleton/explorer-panel-skeleton.component';
import { QdNoticeComponent } from '../../../../shared/ui/notice/notice.component';
import { LINKING_LABELS } from '../../models/linking.labels';
import { LinkingSourcePreflight } from '../../models/linking-preflight.models';
import { LinkingWorkflowFacade } from '../../state/linking-workflow.facade';

@Component({
  selector: 'qd-linking-preflight-step',
  standalone: true,
  imports: [QdActionDirective, QdErrorStateComponent, ExplorerPanelSkeletonComponent, QdNoticeComponent],
  templateUrl: './linking-preflight-step.component.html',
  styleUrl: './linking-preflight-step.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LinkingPreflightStepComponent {
  private readonly workflow = inject(LinkingWorkflowFacade);
  private readonly expandedSourceIdentities = signal<readonly string[]>([]);

  protected readonly labels = LINKING_LABELS;
  protected readonly status = this.workflow.preflightStatus;
  protected readonly preflight = this.workflow.preflight;
  protected readonly message = this.workflow.preflightMessage;
  protected readonly isBlocked = computed(() => this.preflight()?.isBlocked === true);
  protected readonly isNoOp = computed(() => this.preflight()?.isNoOp === true);

  protected isExpanded(sourceIdentity: string): boolean {
    return this.expandedSourceIdentities().includes(sourceIdentity);
  }

  protected toggleSource(sourceIdentity: string): void {
    this.expandedSourceIdentities.update((identities) =>
      identities.includes(sourceIdentity)
        ? identities.filter((identity) => identity !== sourceIdentity)
        : [...identities, sourceIdentity],
    );
  }

  protected retry(): void {
    this.workflow.retryPreflight();
  }

  protected invalidAyahCount(source: LinkingSourcePreflight): number {
    return source.counts.invalid;
  }
}
