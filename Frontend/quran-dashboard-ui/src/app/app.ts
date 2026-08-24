import { Component, computed, inject } from '@angular/core';
import { AppShellComponent } from './core/layout/app-shell/app-shell.component';
import { DetailOverlayHistoryService } from './core/navigation/detail-overlay/detail-overlay-history.service';
import { LinkingWorkspaceHostComponent } from './features/linking/components/linking-workspace-host/linking-workspace-host.component';
import { LinkingAddFeedbackToastComponent } from './features/linking/components/linking-add-feedback-toast/linking-add-feedback-toast.component';
import { LinkingWorkspaceStore } from './features/linking/state/linking-workspace.store';
import { EntityDetailOverlayHostComponent } from './features/words/entity-detail-overlay/entity-detail-overlay-host.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [AppShellComponent, EntityDetailOverlayHostComponent, LinkingAddFeedbackToastComponent, LinkingWorkspaceHostComponent],
  template: `
    <qd-app-shell
      [attr.inert]="hasForegroundDialog() ? '' : null"
      [attr.aria-hidden]="hasForegroundDialog() ? true : null"
    />
    @defer (when overlayOpen(); prefetch on idle) {
      <qd-entity-detail-overlay-host
        [attr.inert]="linkingOpen() ? '' : null"
        [attr.aria-hidden]="linkingOpen() ? true : null"
      />
    }
    @defer (when linkingOpen(); prefetch on idle) {
      <qd-linking-workspace-host />
    }
    <qd-linking-add-feedback-toast />
  `,
})
export class App {
  private readonly overlay = inject(DetailOverlayHistoryService);
  private readonly linkingWorkspace = inject(LinkingWorkspaceStore);

  protected readonly overlayOpen = this.overlay.isOpen;
  protected readonly linkingOpen = this.linkingWorkspace.isOpen;
  protected readonly hasForegroundDialog = computed(() => this.overlayOpen() || this.linkingOpen());

  constructor() {
    this.overlay.start();
  }
}
