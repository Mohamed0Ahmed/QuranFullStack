import { Injectable, inject } from '@angular/core';

import { UniqueWordsApi } from '../data-access/unique-words.api';
import {
  UniqueWordKind,
  UniqueWordListItemDto,
  WordDrilldownView,
} from '../models/unique-words.models';
import { UniqueWordsCache } from './unique-words-cache';
import { UniqueWordsDrilldownController } from './unique-words-drilldown.controller';

/**
 * Thin adapter over `UniqueWordsDrilldownController` (Feature 029, Change B4).
 *
 * The facade keeps the Unique Words page contract — the same drilldown methods
 * plus `restoreFromUrl`, which forwards parsed page query state — and delegates
 * all drilldown state and load orchestration to its own private controller
 * instance. The global overlay adapter uses its own component-scoped
 * `UniqueWordsDrilldownController` instance, so overlay activity can never
 * mutate this page facade's state.
 */
@Injectable({ providedIn: 'root' })
export class UniqueWordsDrilldownFacade {
  private readonly controller = new UniqueWordsDrilldownController(
    inject(UniqueWordsApi),
    inject(UniqueWordsCache),
  );

  readonly drilldownState = this.controller.drilldownState;

  openDrilldown(word: UniqueWordListItemDto, view: WordDrilldownView): void {
    this.controller.openDrilldown(word, view);
  }

  setDrilldownView(view: WordDrilldownView): void {
    this.controller.setDrilldownView(view);
  }

  setAyahPage(page: number): void {
    this.controller.setAyahPage(page);
  }

  closeDrilldown(): void {
    this.controller.closeDrilldown();
  }

  /** See `UniqueWordsDrilldownController.retryCurrentIdentity` (Feature 030, M3). */
  retryCurrentIdentity(): void {
    this.controller.retryCurrentIdentity();
  }

  /** See `UniqueWordsDrilldownController.cancelPendingWork` (perf finding F3). */
  cancelPendingWork(): void {
    this.controller.cancelPendingWork();
  }

  restoreFromUrl(
    mode: UniqueWordKind,
    wordId: number | null,
    view: WordDrilldownView | null,
    ayahPage: number | null,
  ): void {
    this.controller.applyUrlState({ mode, wordId, view, ayahPage });
  }
}
