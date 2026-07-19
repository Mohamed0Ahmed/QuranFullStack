import { Component, inject } from '@angular/core';
import { AppShellComponent } from './core/layout/app-shell/app-shell.component';
import { DetailOverlayHistoryService } from './core/navigation/detail-overlay/detail-overlay-history.service';
import { EntityDetailOverlayHostComponent } from './features/words/entity-detail-overlay/entity-detail-overlay-host.component';

// The shell goes inert while the overlay dialog is open so the sibling dialog is the only
// interactive surface (Feature 029, Change B). This is the one sanctioned composition point of
// core layout and the Words-owned overlay host.
@Component({
  selector: 'app-root',
  standalone: true,
  imports: [AppShellComponent, EntityDetailOverlayHostComponent],
  template: `
    <qd-app-shell [attr.inert]="overlayOpen() ? '' : null" [attr.aria-hidden]="overlayOpen() ? true : null" />
    <qd-entity-detail-overlay-host />
  `,
})
export class App {
  private readonly overlay = inject(DetailOverlayHistoryService);
  protected readonly overlayOpen = this.overlay.isOpen;
}
