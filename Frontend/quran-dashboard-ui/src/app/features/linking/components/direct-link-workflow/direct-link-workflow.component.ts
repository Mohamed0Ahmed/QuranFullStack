import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';

import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { QdErrorStateComponent } from '../../../../shared/ui/error-state/error-state.component';
import { ExplorerPanelSkeletonComponent } from '../../../../shared/ui/explorer-panel-skeleton/explorer-panel-skeleton.component';
import { QdNoticeComponent } from '../../../../shared/ui/notice/notice.component';
import { LINKING_LABELS } from '../../models/linking.labels';
import { LinkingManualLinkShape } from '../../models/linking-manual-mushaf.models';
import { LinkingWorkflowFacade, LinkingWorkflowStep } from '../../state/linking-workflow.facade';
import { LinkingDoorStepComponent } from '../linking-door-step/linking-door-step.component';
import { LinkingManualShapeSelectorComponent } from '../linking-manual-shape-selector/linking-manual-shape-selector.component';
import { LinkingPreflightStepComponent } from '../linking-preflight-step/linking-preflight-step.component';
import {
  LinkingVirtualAyahListComponent,
  LinkingVirtualWordToggle,
} from '../linking-virtual-ayah-list/linking-virtual-ayah-list.component';

@Component({
  selector: 'qd-direct-link-workflow',
  standalone: true,
  imports: [
    QdActionDirective,
    QdErrorStateComponent,
    ExplorerPanelSkeletonComponent,
    QdNoticeComponent,
    LinkingDoorStepComponent,
    LinkingManualShapeSelectorComponent,
    LinkingPreflightStepComponent,
    LinkingVirtualAyahListComponent,
  ],
  templateUrl: './direct-link-workflow.component.html',
  styleUrl: './direct-link-workflow.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DirectLinkWorkflowComponent {
  protected readonly workflow = inject(LinkingWorkflowFacade);
  protected readonly labels = LINKING_LABELS;
  protected readonly state = this.workflow.state;
  protected readonly currentStep = this.workflow.step;
  protected readonly canAdvanceDoor = this.workflow.canAdvanceDoor;
  protected readonly canSubmit = this.workflow.canSubmit;
  protected readonly directDraft = this.workflow.directDraft;
  protected readonly directRequest = this.workflow.directSourceRequest;
  protected readonly directSelectedCount = this.workflow.directSelectedCount;
  protected readonly directManualGrouped = this.workflow.directManualGrouped;
  protected readonly canAdvanceSource = this.workflow.canAdvanceSource;
  protected readonly execution = this.workflow.executionState;
  protected readonly isAutomatic = computed(() =>
    this.directDraft()?.automaticWordMatchesEnabled !== null,
  );
  protected readonly steps: readonly LinkingWorkflowStep[] = [
    'configure-source',
    'door',
    'preflighting',
    'ready',
  ];

  protected next(): void { this.workflow.next(); }
  protected cancel(): void { this.workflow.dismiss(); }
  protected retryPreflight(): void { this.workflow.retryPreflight(); }
  protected submit(): void { this.workflow.submit(); }
  protected acknowledge(): void { void this.workflow.acknowledgeSuccess(); }
  protected cancelExecution(): void { this.workflow.cancelExecution(); }
  protected canNavigateTo(step: LinkingWorkflowStep): boolean { return this.workflow.canNavigateTo(step); }
  protected navigateTo(step: LinkingWorkflowStep): void { this.workflow.navigateTo(step); }

  protected toggleManualWord(toggle: LinkingVirtualWordToggle): void {
    this.workflow.toggleDirectManualWord(toggle.ayahId, toggle.quranWordId);
  }

  protected setManualLinkShape(linkShape: LinkingManualLinkShape): void {
    this.workflow.setDirectManualLinkShape(linkShape);
  }

  protected stepLabel(step: LinkingWorkflowStep): string {
    return this.labels.operationSteps[step];
  }
}
