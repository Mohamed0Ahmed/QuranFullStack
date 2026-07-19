import { Injectable, computed, inject } from '@angular/core';
import { ParamMap } from '@angular/router';

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
import { AbstractRouteDetailFacade } from './abstract-route-detail.facade';
import {
  StemsDetailController,
  StemsDetailUrlState,
  stemsDetailUrlStatesEqual,
} from './stems-detail.controller';
import { StemsDetailViewLoader } from './stems-detail-view.loader';

@Injectable({ providedIn: 'root' })
export class StemsDetailFacade extends AbstractRouteDetailFacade<
  StemsDetailUrlState,
  StemView,
  StemWordView,
  StemSurahView
> {
  // Per-facade controller instance keeps this page's panel state isolated from the
  // component-scoped controllers the global overlay adapters use.
  protected readonly controller = new StemsDetailController(
    inject(StemsApi),
    inject(StemsCache),
    inject(StemsDetailViewLoader),
  );

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

  setAyahTypeCode(typeCode: string | null): void {
    this.controller.setAyahTypeCode(typeCode);
  }

  protected urlStatesEqual(a: StemsDetailUrlState | null, b: StemsDetailUrlState | null): boolean {
    return stemsDetailUrlStatesEqual(a, b);
  }

  protected toPanelUrlState(params: ParamMap): StemsDetailUrlState | null {
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
