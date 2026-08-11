import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';

import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { LINKING_LABELS } from '../../models/linking.labels';
import { LinkingSourceDescriptor } from '../../models/linking-source.models';
import { LinkingAccessService } from '../../state/linking-access.service';
import { LinkingWorkflowFacade } from '../../state/linking-workflow.facade';
import { LinkingWorkspaceStore } from '../../state/linking-workspace.store';

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

  readonly source = input.required<LinkingSourceDescriptor>();

  protected readonly labels = LINKING_LABELS;
  protected readonly canUseLinking = this.access.canUseLinking;
  protected readonly feedback = signal<string | null>(null);
  protected readonly feedbackId = computed(() => `linking-actions-feedback-${this.source().kind}-${this.source().label}`);

  protected addToWorkspace(): void {
    const existing = this.workspace.itemCount();
    const sourceKey = this.workspace.addOrFocus(this.source());
    if (sourceKey === null) {
      return;
    }
    this.feedback.set(this.workspace.itemCount() === existing ? this.labels.alreadyInWorkspace : this.labels.addedToWorkspace);
  }

  protected startDirectLink(): void {
    this.feedback.set(null);
    this.workflow.startFromSource(this.source());
  }
}
