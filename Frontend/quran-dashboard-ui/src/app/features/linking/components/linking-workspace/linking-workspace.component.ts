import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';

import { QdActionDirective } from '../../../../shared/ui/action/action.directive';
import { ConfirmDialogComponent } from '../../../../shared/ui/confirm-dialog/confirm-dialog.component';
import { QdDetailsWorkspaceComponent } from '../../../../shared/ui/details-workspace/details-workspace.component';
import { QdEmptyStateComponent } from '../../../../shared/ui/empty-state/empty-state.component';
import { QdResultListDirective } from '../../../../shared/ui/result-list/result-list.directive';
import { LINKING_LABELS } from '../../models/linking.labels';
import { LinkingWorkspaceSourceRowView } from '../../models/linking-workspace-view.models';
import { LinkingFocusCoordinator } from '../../state/linking-focus.coordinator';
import { LinkingWorkflowFacade } from '../../state/linking-workflow.facade';
import { LinkingWorkspaceStore } from '../../state/linking-workspace.store';
import { LinkingWorkspaceSourceRowComponent } from '../linking-workspace-source-row/linking-workspace-source-row.component';

@Component({
  selector: 'qd-linking-workspace',
  standalone: true,
  imports: [
    QdActionDirective,
    ConfirmDialogComponent,
    QdDetailsWorkspaceComponent,
    QdEmptyStateComponent,
    QdResultListDirective,
    LinkingWorkspaceSourceRowComponent,
  ],
  templateUrl: './linking-workspace.component.html',
  styleUrl: './linking-workspace.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LinkingWorkspaceComponent {
  private readonly workspace = inject(LinkingWorkspaceStore);
  private readonly workflow = inject(LinkingWorkflowFacade);
  private readonly focus = inject(LinkingFocusCoordinator);
  protected readonly labels = LINKING_LABELS;
  protected readonly removeSelectedRequested = signal(false);
  protected readonly bulkRemoval = signal(false);
  protected readonly clearAllRequested = this.workspace.clearAllRequested;
  protected readonly rows = computed<readonly LinkingWorkspaceSourceRowView[]>(() => {
    const checked = new Set(this.workspace.checkedSourceKeys());
    return this.workspace.items().map((item) => ({
      item,
      checked: checked.has(item.sourceKey),
      countLabel: item.lastResolvedCount === null ? this.labels.unresolvedResultCount : `${item.lastResolvedCount} ${this.labels.ayahUnit}`,
    }));
  });
  protected readonly selectedCount = computed(() => this.workspace.checkedSourceKeys().length);
  protected readonly hasRows = computed(() => this.rows().length > 0);
  protected readonly removedItem = this.workspace.removedItem;

  protected toggleMembership(sourceKey: string, checked: boolean): void {
    checked ? this.workspace.checkSource(sourceKey) : this.workspace.uncheckSource(sourceKey);
  }

  protected edit(sourceKey: string): void {
    this.focus.capture('workspace-row');
    this.workspace.openAyahEditor(sourceKey);
  }

  protected automaticWords(sourceKey: string, enabled: boolean): void {
    this.workspace.setAutomaticWordMatchesEnabled(sourceKey, enabled);
  }

  protected remove(sourceKey: string): void {
    this.bulkRemoval.set(false);
    this.workspace.remove(sourceKey);
  }

  protected clearSelection(): void {
    this.workspace.clearCheckedSources();
  }

  protected linkSelected(): void {
    this.workflow.startWorkspaceOperation();
  }

  protected requestRemoveSelected(): void {
    if (this.selectedCount() > 0) {
      this.removeSelectedRequested.set(true);
    }
  }

  protected confirmRemoveSelected(): void {
    const selectedKeys = this.rows()
      .filter((row) => row.checked)
      .map((row) => row.item.sourceKey);
    this.removeSelectedRequested.set(false);
    this.bulkRemoval.set(selectedKeys.length > 1);
    selectedKeys.forEach((sourceKey) => this.workspace.remove(sourceKey));
  }

  protected cancelRemoveSelected(): void {
    this.removeSelectedRequested.set(false);
  }

  protected requestClearAll(): void {
    this.workspace.requestClearAll();
  }

  protected confirmClearAll(): void {
    this.workspace.confirmClearAll();
  }

  protected cancelClearAll(): void {
    this.workspace.cancelClearAll();
  }

  protected undo(): void {
    this.workspace.undoRemove();
  }
}
