import { ChangeDetectionStrategy, Component, ElementRef, computed, effect, inject, viewChild } from '@angular/core';

import { QdModalShellComponent } from '../../../../shared/ui/modal-shell/modal-shell.component';
import { LINKING_LABELS } from '../../models/linking.labels';
import { LinkingWorkspaceStore } from '../../state/linking-workspace.store';
import { LinkingFocusCoordinator } from '../../state/linking-focus.coordinator';
import { LinkingWorkspaceComponent } from '../linking-workspace/linking-workspace.component';
import { DirectLinkWorkflowComponent } from '../direct-link-workflow/direct-link-workflow.component';
import { LinkingWorkflowFacade } from '../../state/linking-workflow.facade';
import { LinkingSourceAyahEditorComponent } from '../linking-source-ayah-editor/linking-source-ayah-editor.component';
import { LinkingManualWordEditorComponent } from '../linking-manual-word-editor/linking-manual-word-editor.component';
import { ConfirmDialogComponent } from '../../../../shared/ui/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'qd-linking-workspace-host',
  standalone: true,
  imports: [
    QdModalShellComponent,
    LinkingWorkspaceComponent,
    DirectLinkWorkflowComponent,
    LinkingSourceAyahEditorComponent,
    LinkingManualWordEditorComponent,
    ConfirmDialogComponent,
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
  protected readonly isManualWordEditor = computed(
    () => this.workspace.activeSurface() === 'manual-word-editor',
  );
  protected readonly isLinkingFlow = computed(() => this.workspace.activeSurface() === 'linking-flow');
  protected readonly modalTitle = computed(() => {
    if (this.isSourceAyahEditor()) {
      return this.labels.sourceEditor;
    }
    if (this.isManualWordEditor()) {
      return this.labels.manualWords;
    }
    return this.isLinkingFlow() ? this.labels.directLink : this.labels.workspace;
  });
  protected readonly activeSourceKey = this.workspace.activeSourceKey;
  protected readonly clearAllRequested = this.workspace.clearAllRequested;
  protected readonly itemCount = this.workspace.itemCount;

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

  protected closeSourceEditor(): void {
    this.workspace.returnToWorkspace();
    this.focus.restore(() => this.surfaceEntry()?.nativeElement ?? null);
  }

  protected openManualWordEditor(): void {
    const sourceKey = this.activeSourceKey();
    if (sourceKey !== null) {
      this.workspace.openManualWordEditor(sourceKey);
    }
  }

  protected closeManualWordEditor(): void {
    this.workspace.returnToWorkspace();
    this.focus.restore(() => this.surfaceEntry()?.nativeElement ?? null);
  }

  protected confirmClearAll(): void { this.workspace.confirmClearAll(); }
  protected cancelClearAll(): void { this.workspace.cancelClearAll(); }
}
