import { Injectable, computed, inject } from '@angular/core';
import { ParamMap } from '@angular/router';

import { RootsApi } from '../data-access/roots.api';
import {
  DEFAULT_ROOT_VIEW,
  RootSummaryDto,
  RootSurahView,
  RootView,
  RootWordView,
} from '../models/roots.models';
import { parseRootsQueryParams } from './roots-url-sync';
import { RootsCache } from './roots-cache';
import { AbstractRouteDetailFacade } from './abstract-route-detail.facade';
import {
  RootsDetailController,
  RootsDetailUrlState,
  rootsDetailUrlStatesEqual,
} from './roots-detail.controller';
import { RootsDetailViewLoader } from './roots-detail-view.loader';

/**
 * Thin route adapter over `RootsDetailController` (Feature 029, Change B4;
 * consolidated onto `AbstractRouteDetailFacade` in Feature 033, decision 5
 * (DRY)).
 *
 * The facade keeps the roots explorer page contract — bind/unbind to the
 * page's `ActivatedRoute` query state plus the direct selection methods — and
 * delegates all panel state and load orchestration to its own private
 * controller instance. The global overlay adapters use their own
 * component-scoped `RootsDetailController` instances, so overlay activity can
 * never mutate this page facade's state.
 */
@Injectable({ providedIn: 'root' })
export class RootsDetailFacade extends AbstractRouteDetailFacade<
  RootsDetailUrlState,
  RootView,
  RootWordView,
  RootSurahView
> {
  protected readonly controller = new RootsDetailController(
    inject(RootsApi),
    inject(RootsCache),
    inject(RootsDetailViewLoader),
  );

  readonly panelState = this.controller.panelState;

  readonly selectedRootId = computed(() => this.panelState().selectedRootId);
  readonly view = computed(() => this.panelState().view);
  readonly wordView = computed(() => this.panelState().wordView);
  readonly surahView = computed(() => this.panelState().surahView);
  readonly status = computed(() => this.panelState().status);
  readonly ayahs = computed(() => this.panelState().ayahs);
  readonly words = computed(() => this.panelState().words);
  readonly mentionedSurahs = computed(() => this.panelState().mentionedSurahs);
  readonly missingSurahs = computed(() => this.panelState().missingSurahs);
  readonly lemmas = computed(() => this.panelState().lemmas);
  readonly stems = computed(() => this.panelState().stems);
  readonly detailPage = computed(() => this.panelState().detailPage);

  selectRoot(summary: RootSummaryDto, view: RootView = DEFAULT_ROOT_VIEW): void {
    this.controller.selectRoot(summary, view);
  }

  selectRootWithPanel(
    summary: RootSummaryDto,
    view: RootView,
    wordView?: RootWordView,
    surahView?: RootSurahView,
    detailPage?: number,
  ): void {
    this.controller.selectRootWithPanel(summary, view, wordView, surahView, detailPage);
  }

  protected urlStatesEqual(a: RootsDetailUrlState | null, b: RootsDetailUrlState | null): boolean {
    return rootsDetailUrlStatesEqual(a, b);
  }

  protected toPanelUrlState(params: ParamMap): RootsDetailUrlState | null {
    const parsed = parseRootsQueryParams(params);
    if (parsed.rootId === null) {
      return null;
    }

    return {
      rootId: parsed.rootId,
      view: parsed.view,
      wordView: parsed.wordView,
      surahView: parsed.surahView,
      detailPage: parsed.detailPage,
    };
  }
}
