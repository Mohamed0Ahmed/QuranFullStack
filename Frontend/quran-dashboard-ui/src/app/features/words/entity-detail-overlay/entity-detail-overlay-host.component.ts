import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';

import { DetailOverlayHistoryService } from '../../../core/navigation/detail-overlay/detail-overlay-history.service';
import { DetailModalShellComponent } from '../../../shared/ui/detail-modal-shell/detail-modal-shell.component';
import {
  ENTITY_DETAIL_BACK_LABEL,
  ENTITY_DETAIL_CAP_STATUS_MESSAGE,
  ENTITY_DETAIL_CLOSE_LABEL,
  ENTITY_DETAIL_KIND_TITLES,
  ENTITY_DETAIL_RESTORE_LABEL,
  entityDetailRestoreAriaLabel,
} from './entity-detail-overlay.labels';

/**
 * Persistent host of the global entity-detail overlay (Feature 029, Change B).
 * Mounted once beside `qd-app-shell` at the application composition root, so it
 * survives every route change. It binds the URL-authoritative history
 * coordinator to the accessible dialog shell; Words owns which entities exist
 * and how their details render (lazy adapters).
 */
@Component({
  selector: 'qd-entity-detail-overlay-host',
  standalone: true,
  imports: [DetailModalShellComponent],
  templateUrl: './entity-detail-overlay-host.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EntityDetailOverlayHostComponent {
  protected readonly overlay = inject(DetailOverlayHistoryService);

  constructor() {
    this.overlay.start();
  }

  protected readonly hasStack = computed(() => this.overlay.state().stack.length > 0);
  protected readonly depth = computed(() => this.overlay.state().stack.length);
  protected readonly visibility = computed(() => (this.overlay.isOpen() ? 'open' : 'closed') as 'open' | 'closed');
  protected readonly topFrame = this.overlay.topFrame;

  protected readonly title = computed(() => {
    const top = this.topFrame();
    return top === null ? '' : ENTITY_DETAIL_KIND_TITLES[top.kind];
  });

  /** Announced once through the shell's polite live region when the cap refuses an append. */
  protected readonly capStatus = computed(() =>
    this.overlay.capRejectionCount() > 0 ? ENTITY_DETAIL_CAP_STATUS_MESSAGE : '',
  );

  protected get backLabel() {
    return ENTITY_DETAIL_BACK_LABEL;
  }

  protected get closeLabel() {
    return ENTITY_DETAIL_CLOSE_LABEL;
  }

  protected get restoreLabel() {
    return ENTITY_DETAIL_RESTORE_LABEL;
  }

  protected restoreAriaLabel(): string {
    return entityDetailRestoreAriaLabel(this.title());
  }
}
