import { ChangeDetectionStrategy, Component, inject } from '@angular/core';

import { QdModalShellComponent } from '../../../../shared/ui/modal-shell/modal-shell.component';
import { LINKING_LABELS } from '../../models/linking.labels';
import { LinkingWorkspaceStore } from '../../state/linking-workspace.store';
import { LinkingWorkspaceComponent } from '../linking-workspace/linking-workspace.component';

@Component({
  selector: 'qd-linking-workspace-host',
  standalone: true,
  imports: [QdModalShellComponent, LinkingWorkspaceComponent],
  templateUrl: './linking-workspace-host.component.html',
  styleUrl: './linking-workspace-host.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LinkingWorkspaceHostComponent {
  private readonly workspace = inject(LinkingWorkspaceStore);

  protected readonly labels = LINKING_LABELS;
  protected readonly isOpen = this.workspace.isOpen;

  protected close(): void {
    this.workspace.close();
  }
}
