import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  effect,
  inject,
  input,
  untracked,
} from '@angular/core';

import { DetailOverlayHistoryService } from '../../../../core/navigation/detail-overlay/detail-overlay-history.service';
import { DETAIL_OVERLAY_LINK_MODE } from '../../../../core/navigation/detail-overlay/detail-overlay-link.directive';
import { UniqueDetailFrame } from '../../../../core/navigation/detail-overlay/detail-overlay.models';
import { WordDrilldownModalComponent } from '../../components/word-drilldown-modal/word-drilldown-modal.component';
import { RESTORED_WORD_NOT_FOUND_LABEL } from '../../models/unique-words.labels';
import { DEFAULT_AYAH_PAGE, WordDrilldownView } from '../../models/unique-words.models';
import { UniqueWordsDrilldownController } from '../../state/unique-words-drilldown.controller';
import { mapUniqueWordSummaryDisplayText } from '../../utils/unique-words-display.mapper';
import { EntityDetailOverlayTitleStore } from '../entity-detail-overlay-title.store';
import { QuranSourceLinkingActionsComponent } from '../../../linking/components/quran-source-linking-actions/quran-source-linking-actions.component';
import { LinkingSourceDescriptor } from '../../../linking/models/linking-source.models';

@Component({
  selector: 'qd-unique-detail-overlay-adapter',
  standalone: true,
  imports: [WordDrilldownModalComponent, QuranSourceLinkingActionsComponent],
  providers: [UniqueWordsDrilldownController, { provide: DETAIL_OVERLAY_LINK_MODE, useValue: 'append' }],
  templateUrl: './unique-detail-overlay-adapter.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UniqueDetailOverlayAdapterComponent {
  private readonly controller = inject(UniqueWordsDrilldownController);
  private readonly overlay = inject(DetailOverlayHistoryService);
  private readonly titleStore = inject(EntityDetailOverlayTitleStore, { optional: true });

  readonly frame = input.required<UniqueDetailFrame>();

  protected readonly drilldownState = this.controller.drilldownState;

  readonly entityTitle = computed(() => {
    const summary = this.drilldownState().summary;
    return summary ? mapUniqueWordSummaryDisplayText(summary).displayText : '';
  });

  readonly entityAyahCount = computed(() => this.drilldownState().summary?.ayahsCount ?? null);

  protected readonly linkingSource = computed<LinkingSourceDescriptor | null>(() => {
    const state = this.drilldownState();
    if (!state.isOpen || state.summary === null || state.selectedWordId === null) {
      return null;
    }
    return {
      kind: 'unique-word',
      mode: state.summary.kind,
      wordId: state.selectedWordId,
      label: mapUniqueWordSummaryDisplayText(state.summary).displayText,
    };
  });

  protected get notFoundLabel() {
    return RESTORED_WORD_NOT_FOUND_LABEL;
  }

  constructor() {
    effect(() => {
      const frame = this.frame();
      untracked(() =>
        this.controller.applyUrlState({
          mode: frame.mode,
          wordId: frame.id,
          view: frame.view,
          ayahPage: frame.ayahPage,
        }),
      );
    });

    effect(() => this.titleStore?.setTitle(this.entityTitle()));
    effect(() => this.titleStore?.setAyahCount(this.entityAyahCount()));
    inject(DestroyRef).onDestroy(() => this.titleStore?.clear());
  }

  protected onRetry(): void {
    this.controller.retryCurrentIdentity();
  }

  protected onViewChange(view: WordDrilldownView): void {
    const frame = this.frame();
    if (view === frame.view) {
      return;
    }

    this.overlay.replaceTopFrame({ ...frame, view, ayahPage: DEFAULT_AYAH_PAGE });
  }

  protected onAyahPageChange(page: number): void {
    const frame = this.frame();
    if (page === frame.ayahPage) {
      return;
    }

    this.overlay.replaceTopFrame({ ...frame, ayahPage: page });
  }
}
