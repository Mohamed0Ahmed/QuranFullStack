import { Injectable, inject } from '@angular/core';

import { UniqueWordsApi } from '../data-access/unique-words.api';
import {
  UniqueWordKind,
  UniqueWordListItemDto,
  WordDrilldownView,
} from '../models/unique-words.models';
import { UniqueWordsCache } from './unique-words-cache';
import { UniqueWordsDrilldownController } from './unique-words-drilldown.controller';

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

  setAyahTypeCode(typeCode: string | null): void {
    this.controller.setAyahTypeCode(typeCode);
  }

  closeDrilldown(): void {
    this.controller.closeDrilldown();
  }

  retryCurrentIdentity(): void {
    this.controller.retryCurrentIdentity();
  }

  cancelPendingWork(): void {
    this.controller.cancelPendingWork();
  }

  restoreFromUrl(
    mode: UniqueWordKind,
    wordId: number | null,
    view: WordDrilldownView | null,
    ayahPage: number | null,
    typeCode: string | null,
  ): void {
    this.controller.applyUrlState({ mode, wordId, view, ayahPage, typeCode });
  }
}
