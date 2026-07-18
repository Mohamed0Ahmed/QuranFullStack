import { Injectable, computed, inject } from '@angular/core';
import { ActivatedRoute, ParamMap } from '@angular/router';
import { Subscription } from 'rxjs';
import { distinctUntilChanged, map } from 'rxjs/operators';

import { LemmasApi } from '../data-access/lemmas.api';
import {
  DEFAULT_LEMMA_VIEW,
  LemmaSummaryDto,
  LemmaSurahView,
  LemmaView,
  LemmaWordView,
} from '../models/lemmas.models';
import { parseLemmasQueryParams } from './lemmas-url-sync';
import { LemmasCache } from './lemmas-cache';
import {
  LemmasDetailController,
  LemmasDetailUrlState,
  lemmasDetailUrlStatesEqual,
} from './lemmas-detail.controller';
import { LemmasDetailViewLoader } from './lemmas-detail-view.loader';

/**
 * Thin route adapter over `LemmasDetailController` (Feature 029, Change B4).
 *
 * The facade keeps the lemmas explorer page contract — bind/unbind to the
 * page's `ActivatedRoute` query state plus the direct selection methods — and
 * delegates all panel state and load orchestration to its own private
 * controller instance. The global overlay adapters use their own
 * component-scoped `LemmasDetailController` instances, so overlay activity can
 * never mutate this page facade's state.
 */
@Injectable({ providedIn: 'root' })
export class LemmasDetailFacade {
  private readonly controller = new LemmasDetailController(
    inject(LemmasApi),
    inject(LemmasCache),
    inject(LemmasDetailViewLoader),
  );

  private routeSub?: Subscription;

  readonly panelState = this.controller.panelState;

  readonly selectedLemmaId = computed(() => this.panelState().selectedLemmaId);
  readonly view = computed(() => this.panelState().view);
  readonly wordView = computed(() => this.panelState().wordView);
  readonly surahView = computed(() => this.panelState().surahView);
  readonly status = computed(() => this.panelState().status);
  readonly ayahs = computed(() => this.panelState().ayahs);
  readonly words = computed(() => this.panelState().words);
  readonly mentionedSurahs = computed(() => this.panelState().mentionedSurahs);
  readonly missingSurahs = computed(() => this.panelState().missingSurahs);
  readonly stems = computed(() => this.panelState().stems);
  readonly detailPage = computed(() => this.panelState().detailPage);

  bindToRoute(route: ActivatedRoute): void {
    this.unbindFromRoute();

    this.routeSub = route.queryParamMap
      .pipe(
        map((params) => this.toPanelUrlState(params)),
        distinctUntilChanged((a, b) => lemmasDetailUrlStatesEqual(a, b)),
      )
      .subscribe((state) => this.controller.applyUrlState(state));
  }

  unbindFromRoute(): void {
    this.routeSub?.unsubscribe();
    this.routeSub = undefined;
    // Reset the active identity as well as cancelling in-flight loads: a bare
    // cancel leaves the controller's last identity set, so re-binding to the
    // SAME query params would short-circuit in applyUrlState() and strand the
    // panel in its cancelled `loading` state. Clearing forces a real reload on
    // re-entry.
    this.controller.clearSelection();
  }

  selectLemma(summary: LemmaSummaryDto, view: LemmaView = DEFAULT_LEMMA_VIEW): void {
    this.controller.selectLemma(summary, view);
  }

  selectLemmaWithPanel(
    summary: LemmaSummaryDto,
    view: LemmaView,
    wordView?: LemmaWordView,
    surahView?: LemmaSurahView,
    detailPage?: number,
    ayahTypeCode: string | null = null,
  ): void {
    this.controller.selectLemmaWithPanel(summary, view, wordView, surahView, detailPage, ayahTypeCode);
  }

  clearSelection(): void {
    this.controller.clearSelection();
  }

  setAyahTypeCode(typeCode: string | null): void {
    this.controller.setAyahTypeCode(typeCode);
  }

  setView(view: LemmaView): void {
    this.controller.setView(view);
  }

  setWordView(wordView: LemmaWordView): void {
    this.controller.setWordView(wordView);
  }

  setSurahView(surahView: LemmaSurahView): void {
    this.controller.setSurahView(surahView);
  }

  setDetailPage(page: number): void {
    this.controller.setDetailPage(page);
  }

  private toPanelUrlState(params: ParamMap): LemmasDetailUrlState | null {
    const parsed = parseLemmasQueryParams(params);
    if (parsed.lemmaId === null) {
      return null;
    }

    return {
      lemmaId: parsed.lemmaId,
      view: parsed.view,
      wordView: parsed.wordView,
      surahView: parsed.surahView,
      detailPage: parsed.detailPage,
      typeCode: parsed.typeCode,
    };
  }
}
