import { Injectable, computed, inject } from '@angular/core';
import { ActivatedRoute, ParamMap } from '@angular/router';
import { Subscription } from 'rxjs';
import { distinctUntilChanged, map } from 'rxjs/operators';

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
import {
  RootsDetailController,
  RootsDetailUrlState,
  rootsDetailUrlStatesEqual,
} from './roots-detail.controller';
import { RootsDetailViewLoader } from './roots-detail-view.loader';

/**
 * Thin route adapter over `RootsDetailController` (Feature 029, Change B4).
 *
 * The facade keeps the roots explorer page contract — bind/unbind to the
 * page's `ActivatedRoute` query state plus the direct selection methods — and
 * delegates all panel state and load orchestration to its own private
 * controller instance. The global overlay adapters use their own
 * component-scoped `RootsDetailController` instances, so overlay activity can
 * never mutate this page facade's state.
 */
@Injectable({ providedIn: 'root' })
export class RootsDetailFacade {
  private readonly controller = new RootsDetailController(
    inject(RootsApi),
    inject(RootsCache),
    inject(RootsDetailViewLoader),
  );

  private routeSub?: Subscription;

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

  bindToRoute(route: ActivatedRoute): void {
    this.unbindFromRoute();

    this.routeSub = route.queryParamMap
      .pipe(
        map((params) => this.toPanelUrlState(params)),
        distinctUntilChanged((a, b) => rootsDetailUrlStatesEqual(a, b)),
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

  clearSelection(): void {
    this.controller.clearSelection();
  }

  setView(view: RootView): void {
    this.controller.setView(view);
  }

  setWordView(wordView: RootWordView): void {
    this.controller.setWordView(wordView);
  }

  setSurahView(surahView: RootSurahView): void {
    this.controller.setSurahView(surahView);
  }

  setDetailPage(page: number): void {
    this.controller.setDetailPage(page);
  }

  private toPanelUrlState(params: ParamMap): RootsDetailUrlState | null {
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
