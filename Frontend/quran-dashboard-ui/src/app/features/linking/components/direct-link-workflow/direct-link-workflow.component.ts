import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';

import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { QdErrorStateComponent } from '../../../../shared/ui/error-state/error-state.component';
import { ExplorerPanelSkeletonComponent } from '../../../../shared/ui/explorer-panel-skeleton/explorer-panel-skeleton.component';
import { QdNoticeComponent } from '../../../../shared/ui/notice/notice.component';
import { PaginationComponent } from '../../../../shared/ui/pagination/pagination.component';
import { LINKING_LABELS } from '../../models/linking.labels';
import { LinkingWorkflowFacade, LinkingWorkflowStep } from '../../state/linking-workflow.facade';
import { LinkingDoorStepComponent } from '../linking-door-step/linking-door-step.component';
import { LinkingPreflightStepComponent } from '../linking-preflight-step/linking-preflight-step.component';
import { LinkingAyahCardComponent } from '../linking-ayah-card/linking-ayah-card.component';

const REVIEW_PAGE_SIZE = 12;

@Component({
  selector: 'qd-direct-link-workflow',
  standalone: true,
  imports: [QdActionDirective, QdErrorStateComponent, ExplorerPanelSkeletonComponent, QdNoticeComponent, PaginationComponent, LinkingDoorStepComponent, LinkingPreflightStepComponent, LinkingAyahCardComponent],
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
  protected readonly operation = this.workflow.operation;
  protected readonly canAdvanceDoor = this.workflow.canAdvanceDoor;
  protected readonly canAdvancePreflight = this.workflow.canAdvancePreflight;
  protected readonly canSubmit = this.workflow.canSubmit;
  protected readonly directSource = this.workflow.directSource;
  protected readonly directConfiguration = this.workflow.directConfiguration;
  protected readonly directAutomaticConfiguration = computed(() => {
    const configuration = this.directConfiguration();
    return configuration?.kind === 'automatic' ? configuration : null;
  });
  protected readonly reviewPage = signal(1);
  protected readonly reviewPageSize = REVIEW_PAGE_SIZE;
  protected readonly reviewAyahs = computed(() => this.operation()?.mergedSelection.ayahs ?? []);
  protected readonly visibleReviewAyahs = computed(() => this.reviewAyahs().slice((this.reviewPage() - 1) * REVIEW_PAGE_SIZE, this.reviewPage() * REVIEW_PAGE_SIZE));
  protected readonly steps: readonly LinkingWorkflowStep[] = ['configure-source', 'resolve', 'door', 'preflight', 'review'];

  constructor() {
    effect(() => {
      this.operation();
      this.reviewPage.set(1);
    });
  }

  protected next(): void { this.workflow.next(); }
  protected back(): void { this.workflow.back(); }
  protected retry(): void { this.workflow.retry(); }
  protected submit(): void { this.workflow.submit(); }
  protected acknowledge(): void { this.workflow.acknowledgeSuccess(); }
  protected setReviewPage(page: number): void { this.reviewPage.set(page); }
  protected setAutomaticWords(event: Event): void { this.workflow.setDirectAutomaticWords((event.target as HTMLInputElement).checked); }
  protected stepLabel(step: LinkingWorkflowStep): string { return this.labels.operationSteps[step]; }
}
