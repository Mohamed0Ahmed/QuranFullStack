import { ChangeDetectionStrategy, Component, computed, effect, inject, input } from '@angular/core';

import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { LinkingWorkspaceStore } from '../../state/linking-workspace.store';
import { LinkingWorkflowFacade } from '../../state/linking-workflow.facade';
import { LINKING_LABELS } from '../../models/linking.labels';
import { DirectLinkStep } from '../../models/linking-workflow.models';
import { LinkingDoorStepComponent } from '../linking-door-step/linking-door-step.component';

const WORKFLOW_STEPS: readonly DirectLinkStep[] = ['door', 'ayahs', 'highlight', 'review', 'result'];

@Component({
  selector: 'qd-direct-link-workflow',
  standalone: true,
  imports: [QdActionDirective, LinkingDoorStepComponent],
  templateUrl: './direct-link-workflow.component.html',
  styleUrl: './direct-link-workflow.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DirectLinkWorkflowComponent {
  private readonly workspace = inject(LinkingWorkspaceStore);
  private readonly workflow = inject(LinkingWorkflowFacade);

  readonly workspaceSourceKey = input<string | null>(null);

  protected readonly labels = LINKING_LABELS;
  protected readonly state = this.workflow.state;
  protected readonly currentStep = this.workflow.step;
  protected readonly selectedDoor = this.workflow.selectedDoor;
  protected readonly canAdvanceDoor = this.workflow.canAdvanceDoor;
  protected readonly sourceLoad = this.workflow.sourceLoad;
  protected readonly steps = WORKFLOW_STEPS;
  protected readonly sourceLoadProgress = computed(() => {
    const progress = this.sourceLoad().progress;
    return progress.total === null ? `${progress.loaded}` : `${progress.loaded} / ${progress.total}`;
  });

  constructor() {
    effect(() => {
      const sourceKey = this.workspaceSourceKey();
      if (this.workspace.activeSurface() === 'direct-link' && sourceKey !== null) {
        this.workflow.startFromWorkspace(sourceKey);
      }
    });
  }

  protected dismiss(): void {
    this.workflow.dismiss();
  }

  protected back(): void {
    this.workflow.back();
  }

  protected next(): void {
    this.workflow.next();
  }

  protected retrySource(): void {
    this.workflow.retrySource();
  }

  protected stepLabel(step: DirectLinkStep): string {
    return this.labels.directLinkSteps[step];
  }
}
