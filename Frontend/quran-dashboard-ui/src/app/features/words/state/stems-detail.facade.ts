import { Injectable, computed, inject } from '@angular/core';
import { ActivatedRoute, ParamMap } from '@angular/router';
import { Subscription } from 'rxjs';
import { distinctUntilChanged, map } from 'rxjs/operators';

import { StemsApi } from '../data-access/stems.api';
import {
  DEFAULT_STEM_VIEW,
  StemSummaryDto,
  StemSurahView,
  StemView,
  StemWordView,
} from '../models/stems.models';
import { parseStemsQueryParams } from './stems-url-sync';
import { StemsCache } from './stems-cache';
import {
  StemsDetailController,
  StemsDetailUrlState,
  stemsDetailUrlStatesEqual,
} from './stems-detail.controller';
import { StemsDetailViewLoader } from './stems-detail-view.loader';

/**
 * Thin route adapter over `StemsDetailController` (Feature 029, Change B4).
 *
 * The facade keeps the stems explorer page contract — bind/unbind to the
 * page's `ActivatedRoute` query state plus the direct selection methods — and
 * delegates all panel state and load orchestration to its own private
 * controller instance. The global overlay adapters use their own
 * component-scoped `StemsDetailController` instances, so overlay activity can
 * never mutate this page facade's state.
 */
@Injectable({ providedIn: 'root' })
export class StemsDetailFacade {
  private readonly controller = new StemsDetailController(
    inject(StemsApi),
    inject(StemsCache),
    inject(StemsDetailViewLoader),
  );

  private routeSub?: Subscription;

  readonly panelState = this.controller.panelState;

  readonly selectedStemId = computed(() => this.panelState().selectedStemId);
  readonly view = computed(() => this.panelState().view);
  readonly wordView = computed(() => this.panelState().wordView);
  readonly surahView = computed(() => this.panelState().surahView);
  readonly status = computed(() => this.panelState().status);
  readonly ayahs = computed(() => this.panelState().ayahs);
  readonly words = computed(() => this.panelState().words);
  readonly mentionedSurahs = computed(() => this.panelState().mentionedSurahs);
  readonly missingSurahs = computed(() => this.panelState().missingSurahs);
  readonly lemmas = computed(() => this.panelState().lemmas);
  readonly detailPage = computed(() => this.panelState().detailPage);

  bindToRoute(route: ActivatedRoute): void {
    this.unbindFromRoute();

    this.routeSub = route.queryParamMap
      .pipe(
        map((params) => this.toPanelUrlState(params)),
        distinctUntilChanged((a, b) => stemsDetailUrlStatesEqual(a, b)),
      )
      .subscribe((state) => this.controller.applyUrlState(state));
  }

  unbindFromRoute(): void {
    this.routeSub?.unsubscribe();
    this.routeSub = undefined;
    this.controller.cancelPendingLoads();
  }

  selectStem(summary: StemSummaryDto, view: StemView = DEFAULT_STEM_VIEW): void {
    this.controller.selectStem(summary, view);
  }

  selectStemWithPanel(
    summary: StemSummaryDto,
    view: StemView,
    wordView?: StemWordView,
    surahView?: StemSurahView,
    detailPage?: number,
  ): void {
    this.controller.selectStemWithPanel(summary, view, wordView, surahView, detailPage);
  }

  clearSelection(): void {
    this.controller.clearSelection();
  }

  setView(view: StemView): void {
    this.controller.setView(view);
  }

  setWordView(wordView: StemWordView): void {
    this.controller.setWordView(wordView);
  }

  setSurahView(surahView: StemSurahView): void {
    this.controller.setSurahView(surahView);
  }

  setDetailPage(page: number): void {
    this.controller.setDetailPage(page);
  }

  setAyahTypeCode(typeCode: string | null): void {
    this.controller.setAyahTypeCode(typeCode);
  }

  private toPanelUrlState(params: ParamMap): StemsDetailUrlState | null {
    const parsed = parseStemsQueryParams(params);
    if (parsed.stemId === null) {
      return null;
    }

    return {
      stemId: parsed.stemId,
      view: parsed.view,
      wordView: parsed.wordView,
      surahView: parsed.surahView,
      detailPage: parsed.detailPage,
      typeCode: parsed.typeCode,
    };
  }
}
