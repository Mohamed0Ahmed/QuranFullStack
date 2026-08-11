import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';

import { QdDetailsWorkspaceComponent } from '../../../../shared/ui/details-workspace/details-workspace.component';
import { QdEmptyStateComponent } from '../../../../shared/ui/empty-state/empty-state.component';
import { LINKING_LABELS } from '../../models/linking.labels';
import { LinkingWorkspaceItem } from '../../models/linking-workspace.models';
import { LinkingWorkspaceStore } from '../../state/linking-workspace.store';
import { LinkingWorkspaceItemComponent } from '../linking-workspace-item/linking-workspace-item.component';

@Component({
  selector: 'qd-linking-workspace',
  standalone: true,
  imports: [QdDetailsWorkspaceComponent, QdEmptyStateComponent, LinkingWorkspaceItemComponent],
  templateUrl: './linking-workspace.component.html',
  styleUrl: './linking-workspace.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LinkingWorkspaceComponent {
  private readonly workspace = inject(LinkingWorkspaceStore);

  protected readonly labels = LINKING_LABELS;
  protected readonly items = this.workspace.items;
  protected readonly hasItems = computed(() => this.items().length > 0);

  protected remove(item: LinkingWorkspaceItem): void {
    this.workspace.remove(item.sourceKey);
  }

  protected editSelection(item: LinkingWorkspaceItem): void {
    this.workspace.addOrFocus(item.source);
  }

  protected startDirectLink(item: LinkingWorkspaceItem): void {
    this.workspace.openDirectLink(item.sourceKey);
  }
}
