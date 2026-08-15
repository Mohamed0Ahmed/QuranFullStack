import { ChangeDetectionStrategy, Component, ElementRef, computed, effect, inject, viewChild } from '@angular/core';

import { QdModalShellComponent } from '../../../../shared/ui/modal-shell/modal-shell.component';
import { LINKING_LABELS } from '../../models/linking.labels';
import { LinkingWorkspaceStore } from '../../state/linking-workspace.store';
import { LinkingFocusCoordinator } from '../../state/linking-focus.coordinator';
import { LinkingWorkspaceComponent } from '../linking-workspace/linking-workspace.component';
import { DirectLinkWorkflowComponent } from '../direct-link-workflow/direct-link-workflow.component';
import { LinkingWorkflowFacade } from '../../state/linking-workflow.facade';
import { LinkingSourceAyahEditorComponent } from '../linking-source-ayah-editor/linking-source-ayah-editor.component';
import { ConfirmDialogComponent } from '../../../../shared/ui/confirm-dialog/confirm-dialog.component';
import { QdErrorStateComponent } from '../../../../shared/ui/error-state/error-state.component';

@Component({
  selector: 'qd-linking-workspace-host',
  standalone: true,
  imports: [
    QdModalShellComponent,
    LinkingWorkspaceComponent,
    DirectLinkWorkflowComponent,
    LinkingSourceAyahEditorComponent,
    ConfirmDialogComponent,
    QdErrorStateComponent,
  ],
  templateUrl: './linking-workspace-host.component.html',
  styleUrl: './linking-workspace-host.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LinkingWorkspaceHostComponent {
  private readonly workspace = inject(LinkingWorkspaceStore);
  private readonly workflow = inject(LinkingWorkflowFacade);
  private readonly focus = inject(LinkingFocusCoordinator);
  private readonly surfaceEntry = viewChild<ElementRef<HTMLElement>>('surfaceEntry');

  protected readonly labels = LINKING_LABELS;
  protected readonly isOpen = this.workspace.isOpen;
  protected readonly isWorkspace = computed(() => this.workspace.activeSurface() === 'workspace');
  protected readonly isSourceAyahEditor = computed(
    () => this.workspace.activeSurface() === 'source-ayah-editor',
  );
  protected readonly isLinkingFlow = computed(() => this.workspace.activeSurface() === 'linking-flow');
  protected readonly modalTitle = computed(() => {
    if (this.isSourceAyahEditor()) {
      return this.labels.sourceEditor;
    }
    return this.isLinkingFlow() ? this.labels.directLink : this.labels.workspace;
  });
  protected readonly editorSourceKey = this.workspace.editorSourceKey;
  protected readonly clearAllRequested = this.workspace.clearAllRequested;
  protected readonly itemCount = this.workspace.itemCount;
  protected readonly persistenceWarning = this.workspace.persistenceWarning;
  protected readonly editorExitPending = this.workspace.editorExitPending;

  constructor() {
    effect(() => {
      if (!this.isOpen()) {
        return;
      }
      const activeSurface = this.workspace.activeSurface();
      if (this.focus.origin() === null) {
        this.focus.capture(activeSurface === 'linking-flow' ? 'inline-source-action' : 'navbar');
      }
      this.focus.focusAfterRender(() => this.surfaceEntry()?.nativeElement ?? null);
    });
  }

  protected close(): void {
    if (this.isLinkingFlow()) {
      this.workflow.dismiss();
      return;
    }
    this.workspace.close();
    this.focus.restore();
  }

  protected async closeSourceEditor(): Promise<void> {
    if (await this.workspace.returnToWorkspace()) {
      this.focus.restore(() => this.surfaceEntry()?.nativeElement ?? null);
    }
  }

  protected dismissPersistenceWarning(): void { this.workspace.dismissPersistenceWarning(); }
  protected confirmClearAll(): void { this.workspace.confirmClearAll(); }
  protected cancelClearAll(): void { this.workspace.cancelClearAll(); }
}
