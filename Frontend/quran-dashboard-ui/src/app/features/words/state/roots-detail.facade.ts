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
    ayahTypeCode?: string | null,
  ): void {
    this.controller.selectRootWithPanel(summary, view, wordView, surahView, detailPage, ayahTypeCode);
  }

  setAyahTypeCode(typeCode: string | null): void {
    this.controller.setAyahTypeCode(typeCode);
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
      typeCode: parsed.typeCode,
    };
  }
}
