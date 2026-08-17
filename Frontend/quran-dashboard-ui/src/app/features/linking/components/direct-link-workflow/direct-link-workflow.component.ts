import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';

import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { QdErrorStateComponent } from '../../../../shared/ui/error-state/error-state.component';
import { ExplorerPanelSkeletonComponent } from '../../../../shared/ui/explorer-panel-skeleton/explorer-panel-skeleton.component';
import { QdNoticeComponent } from '../../../../shared/ui/notice/notice.component';
import { LINKING_LABELS } from '../../models/linking.labels';
import { LinkingManualLinkShape } from '../../models/linking-manual-mushaf.models';
import { LinkingSourceTypeOption } from '../../models/linking-source.models';
import { LinkingInlineSourceWorkflowController } from '../../state/linking-inline-source-workflow.controller';
import { LinkingWorkflowFacade, LinkingWorkflowStep } from '../../state/linking-workflow.facade';
import { linkingSourceTypeCodes } from '../../utils/linking-source-types';
import { LinkingDoorStepComponent } from '../linking-door-step/linking-door-step.component';
import { LinkingManualShapeSelectorComponent } from '../linking-manual-shape-selector/linking-manual-shape-selector.component';
import { LinkingPreflightStepComponent } from '../linking-preflight-step/linking-preflight-step.component';
import { LinkingSourceTypeFiltersComponent } from '../linking-source-type-filters/linking-source-type-filters.component';
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
    LinkingSourceTypeFiltersComponent,
    LinkingVirtualAyahListComponent,
  ],
  templateUrl: './direct-link-workflow.component.html',
  styleUrl: './direct-link-workflow.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DirectLinkWorkflowComponent {
  protected readonly workflow = inject(LinkingWorkflowFacade);
  private readonly inlineSource = inject(LinkingInlineSourceWorkflowController);
  protected readonly labels = LINKING_LABELS;
  protected readonly state = this.workflow.state;
  protected readonly currentStep = this.workflow.step;
  protected readonly canAdvanceDoor = this.workflow.canAdvanceDoor;
  protected readonly canSubmit = this.workflow.canSubmit;
  protected readonly isNoOp = this.workflow.isNoOp;
  protected readonly directDraft = this.workflow.directDraft;
  protected readonly isCopy = this.workflow.isCopy;
  protected readonly directRequest = this.workflow.directSourceRequest;
  protected readonly directTotalAyahCount = this.workflow.directTotalAyahCount;
  protected readonly directAvailableTypes = this.inlineSource.availableTypes;
  protected readonly directSelectedTypeCodes = computed(() => {
    const draft = this.inlineSource.draft();
    return draft === null ? [] : linkingSourceTypeCodes(draft.descriptor);
  });
  protected readonly directSelectedCount = this.workflow.directSelectedCount;
  protected readonly directManualGrouped = this.workflow.directManualGrouped;
  protected readonly canAdvanceSource = this.workflow.canAdvanceSource;
  protected readonly execution = this.workflow.executionState;
  protected readonly showExecutionProgress = computed(() =>
    ['queued', 'running', 'finalizing'].includes(this.currentStep()),
  );
  protected readonly executionProgress = computed(() => calculateExecutionProgress(
    this.execution().job?.status ?? null,
    this.execution().job?.stage ?? null,
    this.execution().job?.processedItems ?? 0,
    this.execution().job?.totalItems ?? 0,
  ));
  protected readonly executionStageLabel = computed(() => {
    const job = this.execution().job;
    if (job?.status.toLowerCase() === 'queued') {
      return this.labels.executionStages.queued;
    }
    switch (job?.stage.toLowerCase()) {
      case 'loading-prepared': return this.labels.executionStages.loadingPrepared;
      case 'applying-unit-diff': return this.labels.executionStages.applyingUnitDiff;
      case 'synchronizing-door': return this.labels.executionStages.synchronizingDoor;
      case 'committing': return this.labels.executionStages.committing;
      default: return this.labels.executionStages.unknown;
    }
  });
  protected readonly isAutomatic = computed(() =>
    this.directDraft()?.automaticWordMatchesEnabled !== null,
  );
  protected readonly steps = computed<readonly LinkingWorkflowStep[]>(() =>
    this.isCopy()
      ? ['preflighting', 'ready']
      : ['configure-source', 'door', 'preflighting', 'ready'],
  );

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

  protected setDirectTypeCodes(typeCodes: readonly string[]): void {
    this.inlineSource.setTypeCodes(typeCodes);
  }

  protected directPageReady(page: {
    linkingDataRevision: number;
    totalItems: number;
    availableTypes: readonly LinkingSourceTypeOption[];
  }): void {
    this.inlineSource.pageReady(
      page.linkingDataRevision,
      page.totalItems,
      page.availableTypes,
      this.workflow.selectedDoorId(),
    );
  }

  protected stepLabel(step: LinkingWorkflowStep): string {
    return this.labels.operationSteps[step];
  }
}

function calculateExecutionProgress(
  status: string | null,
  stage: string | null,
  processedItems: number,
  totalItems: number,
): number {
  if (status?.toLowerCase() === 'queued') {
    return 5;
  }
  switch (stage?.toLowerCase()) {
    case 'loading-prepared': return 10;
    case 'applying-unit-diff': return 15 + Math.round(65 * progressRatio(processedItems, totalItems));
    case 'synchronizing-door': return 88;
    case 'committing': return 96;
    default: return 10;
  }
}

function progressRatio(processed: number, total: number): number {
  if (total <= 0) {
    return 0;
  }
  return Math.min(Math.max(processed / total, 0), 1);
}
