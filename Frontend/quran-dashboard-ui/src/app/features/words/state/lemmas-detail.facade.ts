import { Injectable, computed, inject } from '@angular/core';
import { ParamMap } from '@angular/router';

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
import { AbstractRouteDetailFacade } from './abstract-route-detail.facade';
import {
  LemmasDetailController,
  LemmasDetailUrlState,
  lemmasDetailUrlStatesEqual,
} from './lemmas-detail.controller';
import { LemmasDetailViewLoader } from './lemmas-detail-view.loader';

@Injectable({ providedIn: 'root' })
export class LemmasDetailFacade extends AbstractRouteDetailFacade<
  LemmasDetailUrlState,
  LemmaView,
  LemmaWordView,
  LemmaSurahView
> {
  // Per-facade controller instance keeps this page's panel state isolated from the
  // component-scoped controllers the global overlay adapters use.
  protected readonly controller = new LemmasDetailController(
    inject(LemmasApi),
    inject(LemmasCache),
    inject(LemmasDetailViewLoader),
  );

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

  setAyahTypeCode(typeCode: string | null): void {
    this.controller.setAyahTypeCode(typeCode);
  }

  protected urlStatesEqual(a: LemmasDetailUrlState | null, b: LemmasDetailUrlState | null): boolean {
    return lemmasDetailUrlStatesEqual(a, b);
  }

  protected toPanelUrlState(params: ParamMap): LemmasDetailUrlState | null {
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
