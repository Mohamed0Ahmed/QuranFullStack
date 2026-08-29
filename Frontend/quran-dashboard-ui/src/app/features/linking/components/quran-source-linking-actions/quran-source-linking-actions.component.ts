import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';

import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { LINKING_LABELS } from '../../models/linking.labels';
import {
  createLinkingSourceLaunch,
  LinkingSourceLaunch,
} from '../../models/linking-source-launch.models';
import { LinkingSourceDescriptor } from '../../models/linking-source.models';
import { LinkingAccessService } from '../../state/linking-access.service';
import { LinkingFocusCoordinator } from '../../state/linking-focus.coordinator';
import { LinkingWorkflowFacade } from '../../state/linking-workflow.facade';
import { LinkingWorkspaceStore } from '../../state/linking-workspace.store';
import { linkingSourceKey } from '../../utils/linking-source-key';
import { linkingSourcePresentation } from '../../utils/linking-source-presentation';

@Component({
  selector: 'qd-quran-source-linking-actions',
  standalone: true,
  imports: [QdActionDirective],
  templateUrl: './quran-source-linking-actions.component.html',
  styleUrl: './quran-source-linking-actions.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class QuranSourceLinkingActionsComponent {
  private readonly access = inject(LinkingAccessService);
  private readonly workspace = inject(LinkingWorkspaceStore);
  private readonly workflow = inject(LinkingWorkflowFacade);
  private readonly focus = inject(LinkingFocusCoordinator);

  readonly source = input<LinkingSourceDescriptor | null>(null);
  readonly launch = input<LinkingSourceLaunch | null>(null);
  readonly alignment = input<'start' | 'center'>('start');

  protected readonly labels = LINKING_LABELS;
  protected readonly canUseLinking = this.access.canUseLinking;
  private readonly sourceLaunch = computed(() => {
    const launch = this.launch();
    const source = this.source();
    return launch ?? (source === null ? null : createLinkingSourceLaunch(source));
  });
  protected readonly sourceKey = computed(() => {
    const launch = this.sourceLaunch();
    return launch === null ? null : linkingSourceKey(launch.source);
  });
  protected readonly sourcePresentation = computed(() => {
    const launch = this.sourceLaunch();
    return launch === null ? '' : linkingSourcePresentation(launch.source);
  });
  protected readonly sourceDescription = computed(
    () => {
      const launch = this.sourceLaunch();
      return launch === null ? '' : `${this.sourcePresentation()}: ${launch.source.label}`;
    },
  );
  protected readonly addActionLabel = computed(
    () => `${this.labels.addToWorkspace}: ${this.sourceDescription()}`,
  );
  protected readonly directActionLabel = computed(
    () => `${this.labels.directLink}: ${this.sourceDescription()}`,
  );

  protected addToWorkspace(): void {
    if (!this.access.canUseLinking()) {
      return;
    }

    const launch = this.sourceLaunch();
    if (launch === null) {
      return;
    }
    this.focus.capture('inline-source-action');
    this.workspace.addSource(launch);
  }

  protected startDirectLink(): void {
    if (!this.access.canUseLinking()) {
      return;
    }

    const launch = this.sourceLaunch();
    if (launch === null) {
      return;
    }
    this.focus.capture('inline-source-action');
    this.workflow.startFromSource(launch);
  }
}
