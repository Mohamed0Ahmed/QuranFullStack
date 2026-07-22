import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';

import { DetailOverlayHistoryService } from '../../../core/navigation/detail-overlay/detail-overlay-history.service';
import {
  LemmaDetailFrame,
  RootDetailFrame,
  StemDetailFrame,
  UniqueDetailFrame,
  WordTypeDetailFrame,
} from '../../../core/navigation/detail-overlay/detail-overlay.models';
import { DetailModalShellComponent } from '../../../shared/ui/detail-modal-shell/detail-modal-shell.component';
import { LemmaDetailOverlayAdapterComponent } from './adapters/lemma-detail-overlay-adapter.component';
import { RootDetailOverlayAdapterComponent } from './adapters/root-detail-overlay-adapter.component';
import { StemDetailOverlayAdapterComponent } from './adapters/stem-detail-overlay-adapter.component';
import { UniqueDetailOverlayAdapterComponent } from './adapters/unique-detail-overlay-adapter.component';
import { WordTypeDetailOverlayAdapterComponent } from './adapters/word-type-detail-overlay-adapter.component';
import { EntityDetailOverlayTitleStore } from './entity-detail-overlay-title.store';
import {
  ENTITY_DETAIL_BACK_LABEL,
  ENTITY_DETAIL_CAP_STATUS_MESSAGE,
  ENTITY_DETAIL_CLOSE_LABEL,
  ENTITY_DETAIL_KIND_LABELS,
  ENTITY_DETAIL_KIND_TITLES,
  ENTITY_DETAIL_RESTORE_LABEL,
  entityDetailAyahCountText,
  entityDetailRestoreAriaLabel,
} from './entity-detail-overlay.labels';

@Component({
  selector: 'qd-entity-detail-overlay-host',
  standalone: true,
  imports: [
    DetailModalShellComponent,
    LemmaDetailOverlayAdapterComponent,
    RootDetailOverlayAdapterComponent,
    StemDetailOverlayAdapterComponent,
    UniqueDetailOverlayAdapterComponent,
    WordTypeDetailOverlayAdapterComponent,
  ],
  providers: [EntityDetailOverlayTitleStore],
  templateUrl: './entity-detail-overlay-host.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EntityDetailOverlayHostComponent {
  protected readonly overlay = inject(DetailOverlayHistoryService);
  private readonly titleStore = inject(EntityDetailOverlayTitleStore);

  constructor() {
    this.overlay.start();
  }

  protected readonly hasStack = computed(() => this.overlay.state().stack.length > 0);

  protected readonly isOverlayOpen = this.overlay.isOpen;

  protected readonly depth = computed(() => this.overlay.state().stack.length);
  protected readonly visibility = computed(() => (this.overlay.isOpen() ? 'open' : 'closed') as 'open' | 'closed');
  protected readonly topFrame = this.overlay.topFrame;

  protected readonly rootFrame = computed<RootDetailFrame | null>(() => {
    const top = this.topFrame();
    return top !== null && top.kind === 'root' ? top : null;
  });

  protected readonly lemmaFrame = computed<LemmaDetailFrame | null>(() => {
    const top = this.topFrame();
    return top !== null && top.kind === 'lemma' ? top : null;
  });

  protected readonly stemFrame = computed<StemDetailFrame | null>(() => {
    const top = this.topFrame();
    return top !== null && top.kind === 'stem' ? top : null;
  });

  protected readonly uniqueFrame = computed<UniqueDetailFrame | null>(() => {
    const top = this.topFrame();
    return top !== null && top.kind === 'unique' ? top : null;
  });

  protected readonly wordTypeFrame = computed<WordTypeDetailFrame | null>(() => {
    const top = this.topFrame();
    return top !== null && top.kind === 'wordType' ? top : null;
  });

  protected readonly title = computed(() => {
    const top = this.topFrame();
    if (top === null) {
      return '';
    }
    const entityTitle = this.titleStore.title();
    return entityTitle !== '' ? entityTitle : ENTITY_DETAIL_KIND_TITLES[top.kind];
  });

  protected readonly kindLabel = computed(() => {
    const top = this.topFrame();
    return top === null ? '' : ENTITY_DETAIL_KIND_LABELS[top.kind];
  });

  protected readonly countText = computed(() => {
    const count = this.titleStore.ayahCount();
    return count === null ? '' : entityDetailAyahCountText(count);
  });

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
