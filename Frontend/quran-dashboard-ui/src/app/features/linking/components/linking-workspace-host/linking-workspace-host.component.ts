import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';

import { QdModalShellComponent } from '../../../../shared/ui/modal-shell/modal-shell.component';
import { LINKING_LABELS } from '../../models/linking.labels';
import { LinkingWorkspaceStore } from '../../state/linking-workspace.store';
import { LinkingWorkspaceComponent } from '../linking-workspace/linking-workspace.component';
import { DirectLinkWorkflowComponent } from '../direct-link-workflow/direct-link-workflow.component';
import { LinkingWorkflowFacade } from '../../state/linking-workflow.facade';

@Component({
  selector: 'qd-linking-workspace-host',
  standalone: true,
  imports: [QdModalShellComponent, LinkingWorkspaceComponent, DirectLinkWorkflowComponent],
  templateUrl: './linking-workspace-host.component.html',
  styleUrl: './linking-workspace-host.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LinkingWorkspaceHostComponent {
  private readonly workspace = inject(LinkingWorkspaceStore);
  private readonly workflow = inject(LinkingWorkflowFacade);

  protected readonly labels = LINKING_LABELS;
  protected readonly isOpen = this.workspace.isOpen;
  protected readonly isWorkspace = computed(() => this.workspace.activeSurface() === 'workspace');
  protected readonly isDirectLink = computed(() => this.workspace.activeSurface() === 'direct-link');
  protected readonly modalTitle = computed(() => (this.isDirectLink() ? this.labels.directLink : this.labels.workspace));
  protected readonly activeSourceKey = this.workspace.activeSourceKey;

  protected close(): void {
    if (this.isDirectLink()) {
      this.workflow.dismiss();
      return;
    }
    this.workspace.close();
  }
}
