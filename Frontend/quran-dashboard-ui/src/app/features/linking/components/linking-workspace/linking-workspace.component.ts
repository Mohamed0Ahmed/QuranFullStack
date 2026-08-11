import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';

import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { QdEmptyStateComponent } from '../../../../shared/ui/empty-state/empty-state.component';
import { QdResultListDirective } from '../../../../shared/ui/result-list/result-list.directive';
import { QdDetailsWorkspaceComponent } from '../../../../shared/ui/details-workspace/details-workspace.component';
import { LINKING_LABELS } from '../../models/linking.labels';
import { LinkingWorkspaceSourceRowView } from '../../models/linking-workspace-view.models';
import { LinkingFocusCoordinator } from '../../state/linking-focus.coordinator';
import { LinkingWorkflowFacade } from '../../state/linking-workflow.facade';
import { LinkingWorkspaceStore } from '../../state/linking-workspace.store';
import { LinkingWorkspaceSourceRowComponent } from '../linking-workspace-source-row/linking-workspace-source-row.component';

@Component({
  selector: 'qd-linking-workspace',
  standalone: true,
  imports: [QdActionDirective, QdDetailsWorkspaceComponent, QdEmptyStateComponent, QdResultListDirective, LinkingWorkspaceSourceRowComponent],
  templateUrl: './linking-workspace.component.html',
  styleUrl: './linking-workspace.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LinkingWorkspaceComponent {
  private readonly workspace = inject(LinkingWorkspaceStore);
  private readonly workflow = inject(LinkingWorkflowFacade);
  private readonly focus = inject(LinkingFocusCoordinator);
  protected readonly labels = LINKING_LABELS;
  protected readonly rows = computed<readonly LinkingWorkspaceSourceRowView[]>(() => {
    const checked = new Set(this.workspace.checkedSourceKeys());
    return this.workspace.items().map((item) => ({
      item,
      checked: checked.has(item.sourceKey),
      countStatus: item.lastResolvedCount === null ? 'unresolved' : item.lastResolvedCountIsStale ? 'stale' : 'ready',
      countLabel: item.lastResolvedCount === null ? this.labels.unresolvedResultCount : `${item.lastResolvedCount} ${this.labels.ayahUnit}`,
    }));
  });
  protected readonly selectedCount = computed(() => this.workspace.checkedSourceKeys().length);
  protected readonly hasRows = computed(() => this.rows().length > 0);
  protected readonly removedItem = this.workspace.removedItem;

  protected toggleMembership(sourceKey: string, checked: boolean): void {
    checked ? this.workspace.checkSource(sourceKey) : this.workspace.uncheckSource(sourceKey);
  }
  protected edit(sourceKey: string): void { this.focus.capture('workspace-row'); this.workspace.openAyahEditor(sourceKey); }
  protected automaticWords(sourceKey: string, enabled: boolean): void { this.workspace.setAutomaticWordMatchesEnabled(sourceKey, enabled); }
  protected manualWords(sourceKey: string): void { this.focus.capture('workspace-row'); this.workspace.openManualWordEditor(sourceKey); }
  protected remove(sourceKey: string): void { this.workspace.remove(sourceKey); }
  protected clearSelection(): void { this.workspace.clearCheckedSources(); }
  protected linkSelected(): void { this.workflow.startWorkspaceOperation(); }
  protected removeAll(): void { this.workspace.requestClearAll(); }
  protected undo(): void { this.workspace.undoRemove(); }
}
