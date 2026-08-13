import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';

import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { QdErrorStateComponent } from '../../../../shared/ui/error-state/error-state.component';
import { ExplorerPanelSkeletonComponent } from '../../../../shared/ui/explorer-panel-skeleton/explorer-panel-skeleton.component';
import { QdNoticeComponent } from '../../../../shared/ui/notice/notice.component';
import { LINKING_LABELS } from '../../models/linking.labels';
import { LinkingWorkflowFacade, LinkingWorkflowStep } from '../../state/linking-workflow.facade';
import { LinkingDoorStepComponent } from '../linking-door-step/linking-door-step.component';
import { LinkingPreflightStepComponent } from '../linking-preflight-step/linking-preflight-step.component';

@Component({
  selector: 'qd-direct-link-workflow',
  standalone: true,
  imports: [
    QdActionDirective,
    QdErrorStateComponent,
    ExplorerPanelSkeletonComponent,
    QdNoticeComponent,
    LinkingDoorStepComponent,
    LinkingPreflightStepComponent,
  ],
  templateUrl: './direct-link-workflow.component.html',
  styleUrl: './direct-link-workflow.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DirectLinkWorkflowComponent {
  private readonly workflow = inject(LinkingWorkflowFacade);

  protected readonly labels = LINKING_LABELS;
  protected readonly state = this.workflow.state;
  protected readonly currentStep = this.workflow.step;
  protected readonly memberStates = this.workflow.memberStates;
  protected readonly canAdvanceDoor = this.workflow.canAdvanceDoor;
  protected readonly canSubmit = this.workflow.canSubmit;
  protected readonly directConfiguration = this.workflow.directConfiguration;
  protected readonly directAutomaticConfiguration = computed(() => {
    const configuration = this.directConfiguration();
    return configuration?.kind === 'automatic' ? configuration : null;
  });
  protected readonly steps: readonly LinkingWorkflowStep[] = ['configure-source', 'resolve', 'door', 'preflight'];

  protected next(): void { this.workflow.next(); }
  protected cancel(): void { this.workflow.dismiss(); }
  protected retry(): void { this.workflow.retry(); }
  protected submit(): void { this.workflow.submit(); }
  protected acknowledge(): void { this.workflow.acknowledgeSuccess(); }
  protected canNavigateTo(step: LinkingWorkflowStep): boolean { return this.workflow.canNavigateTo(step); }
  protected navigateTo(step: LinkingWorkflowStep): void { this.workflow.navigateTo(step); }

  protected setAutomaticWords(event: Event): void { this.workflow.setDirectAutomaticWords((event.target as HTMLInputElement).checked); }
  protected stepLabel(step: LinkingWorkflowStep): string { return this.labels.operationSteps[step]; }
}
