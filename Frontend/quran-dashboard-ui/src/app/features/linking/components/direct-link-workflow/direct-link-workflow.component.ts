import { ChangeDetectionStrategy, Component, computed, effect, inject, input } from '@angular/core';

import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { QdEmptyStateComponent } from '../../../../shared/ui/empty-state/empty-state.component';
import { QdErrorStateComponent } from '../../../../shared/ui/error-state/error-state.component';
import { ExplorerPanelSkeletonComponent } from '../../../../shared/ui/explorer-panel-skeleton/explorer-panel-skeleton.component';
import { QdNoticeComponent } from '../../../../shared/ui/notice/notice.component';
import { LinkingAyah } from '../../models/linking-ayah.models';
import { LinkingWorkspaceStore } from '../../state/linking-workspace.store';
import { LinkingWorkflowFacade } from '../../state/linking-workflow.facade';
import { LINKING_LABELS } from '../../models/linking.labels';
import { DirectLinkStep } from '../../models/linking-workflow.models';
import { LinkingDoorStepComponent } from '../linking-door-step/linking-door-step.component';
import { LinkingAyahCardComponent } from '../linking-ayah-card/linking-ayah-card.component';
import { LinkingAyahSelectionComponent } from '../linking-ayah-selection/linking-ayah-selection.component';

const WORKFLOW_STEPS: readonly DirectLinkStep[] = ['door', 'ayahs', 'highlight', 'review', 'result'];

@Component({
  selector: 'qd-direct-link-workflow',
  standalone: true,
  imports: [
    QdActionDirective,
    QdEmptyStateComponent,
    QdErrorStateComponent,
    ExplorerPanelSkeletonComponent,
    QdNoticeComponent,
    LinkingDoorStepComponent,
    LinkingAyahCardComponent,
    LinkingAyahSelectionComponent,
  ],
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
  protected readonly allAyahs = this.workflow.allAyahs;
  protected readonly selectedCount = this.workflow.selectedCount;
  protected readonly selectedVerseKeys = this.workflow.selectedVerseKeys;
  protected readonly selection = computed(() => this.state().selection);
  protected readonly highlightSourceWords = this.workflow.highlightSourceWords;
  protected readonly canAdvanceAyahs = this.workflow.canAdvanceAyahs;
  protected readonly selectedAyahs = computed<readonly LinkingAyah[]>(() => {
    const selected = new Set(this.selectedVerseKeys());
    return this.allAyahs().filter((ayah) => selected.has(ayah.verseKey));
  });
  protected readonly sourceLoadProgress = computed(() => {
    const progress = this.sourceLoad().progress;
    return progress.total === null ? `${progress.loaded}` : `${progress.loaded} / ${progress.total}`;
  });
  protected readonly steps = WORKFLOW_STEPS;

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

  protected toggleAyah(verseKey: string): void {
    this.workflow.toggleAyah(verseKey);
  }

  protected selectAllAyahs(): void {
    this.workflow.selectAllAyahs();
  }

  protected clearAllAyahs(): void {
    this.workflow.clearAllAyahs();
  }

  protected setHighlightSourceWords(event: Event): void {
    this.workflow.setHighlightSourceWords((event.target as HTMLInputElement).checked);
  }

  protected confirm(): void {
    this.workflow.confirm();
  }

  protected stepLabel(step: DirectLinkStep): string {
    return this.labels.directLinkSteps[step];
  }
}
