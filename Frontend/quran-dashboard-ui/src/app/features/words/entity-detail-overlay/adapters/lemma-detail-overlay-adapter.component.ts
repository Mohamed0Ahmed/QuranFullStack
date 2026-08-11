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
import { NgTemplateOutlet } from '@angular/common';

import { DetailOverlayHistoryService } from '../../../../core/navigation/detail-overlay/detail-overlay-history.service';
import { DETAIL_OVERLAY_LINK_MODE } from '../../../../core/navigation/detail-overlay/detail-overlay-link.directive';
import { LemmaDetailFrame } from '../../../../core/navigation/detail-overlay/detail-overlay.models';
import { AyahMatchesListComponent } from '../../components/ayah-matches-list/ayah-matches-list.component';
import { LemmaAyahTypeFiltersComponent } from '../../components/lemma-ayah-type-filters/lemma-ayah-type-filters.component';
import { LemmaDetailsPanelComponent } from '../../components/lemma-details-panel/lemma-details-panel.component';
import { LemmaStemsListComponent } from '../../components/lemma-stems-list/lemma-stems-list.component';
import { LemmaWordsListComponent } from '../../components/lemma-words-list/lemma-words-list.component';
import { MissingSurahsListComponent } from '../../components/missing-surahs-list/missing-surahs-list.component';
import { SurahOccurrencesListComponent } from '../../components/surah-occurrences-list/surah-occurrences-list.component';
import {
  LEMMAS_EMPTY_VIEW_LABEL,
  LEMMAS_LOADING_LABEL,
  LEMMAS_NOT_FOUND_LABEL,
  LEMMAS_SURAHS_TABLIST_LABEL,
  LEMMAS_SURAHS_VIEW_LABELS,
  LEMMAS_WORDS_TABLIST_LABEL,
  LEMMAS_WORD_VIEW_LABELS,
} from '../../models/lemmas.labels';
import {
  DEFAULT_LEMMA_DETAIL_PAGE,
  DEFAULT_LEMMA_SURAHS_VIEW,
  DEFAULT_LEMMA_WORD_VIEW,
  LEMMA_DETAIL_PAGE_SIZE,
  LemmaSurahView,
  LemmaView,
  LemmaWordItemDto,
  LemmaWordView,
  PagedResultDto,
} from '../../models/lemmas.models';
import { AyahMatchDto } from '../../models/unique-words.models';
import { LemmasDetailController } from '../../state/lemmas-detail.controller';
import { mapLemmaAyahMatchToShared } from '../../utils/lemma-ayah-match.mapper';
import { WORDS_DETAIL_RETRY_LABEL } from '../../models/words-shared.labels';
import { QdErrorStateComponent } from '../../../../shared/ui/error-state/error-state.component';
import { QdTabDirective } from '../../../../shared/ui/tabs/tab.directive';
import { QdTabsComponent } from '../../../../shared/ui/tabs/tabs.component';
import { EntityDetailOverlayTitleStore } from '../entity-detail-overlay-title.store';

// Overlay adapter for lemma frames (Feature 029, B4). Owns a component-scoped LemmasDetailController
// (never the page facade) and maps every `frame` input change onto applyUrlState. All view/sub-view/
// page/type changes route through DetailOverlayHistoryService.replaceTopFrame — never the Router or
// controller state directly — and the URL sync feeds the new frame back in, re-driving the controller.
// Unlike roots, the ayahs view's typeCode is part of the frame identity.
let nextSubViewInstance = 0;

@Component({
  selector: 'qd-lemma-detail-overlay-adapter',
  standalone: true,
  imports: [
    AyahMatchesListComponent,
    LemmaAyahTypeFiltersComponent,
    LemmaDetailsPanelComponent,
    LemmaStemsListComponent,
    LemmaWordsListComponent,
    MissingSurahsListComponent,
    NgTemplateOutlet,
    QdErrorStateComponent,
    QdTabDirective,
    QdTabsComponent,
    SurahOccurrencesListComponent,
  ],
  providers: [LemmasDetailController, { provide: DETAIL_OVERLAY_LINK_MODE, useValue: 'append' }],
  templateUrl: './lemma-detail-overlay-adapter.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LemmaDetailOverlayAdapterComponent {
  private readonly controller = inject(LemmasDetailController);
  private readonly overlay = inject(DetailOverlayHistoryService);
  private readonly titleStore = inject(EntityDetailOverlayTitleStore, { optional: true });

  readonly frame = input.required<LemmaDetailFrame>();

  private readonly subViewId = `overlay-lemmas-subview-${nextSubViewInstance++}`;
  protected readonly subViewPanelId = `${this.subViewId}-panel`;
  protected readonly activeSubViewTabId = computed(() => {
    const frame = this.frame();
    if (frame.view === 'words') {
      return this.subViewTabId(frame.wordView);
    }
    return frame.view === 'surahs' ? this.subViewTabId(frame.surahView) : null;
  });

  protected readonly retryLabel = WORDS_DETAIL_RETRY_LABEL;

  // Re-drives the current frame after a failed load (Feature 030, M3). The frame is unchanged, so this
  // never touches the URL — routing an identical frame through the history service would be a no-op.
  protected onRetry(): void {
    this.controller.retryCurrentIdentity();
  }

  protected readonly panelState = this.controller.panelState;

  readonly entityTitle = computed(() => this.panelState().summary?.lemmaText ?? '');

  readonly entityAyahCount = computed(() => this.panelState().summary?.ayahsCount ?? null);

  protected readonly wordViewOptions: readonly LemmaWordView[] = ['simple', 'tashkeel'];
  protected readonly surahViewOptions: readonly LemmaSurahView[] = ['mentioned', 'missing'];
  protected readonly emptyAyahsPage: PagedResultDto<AyahMatchDto> = { page: 1, pageSize: LEMMA_DETAIL_PAGE_SIZE, totalCount: 0, items: [] };
  protected readonly emptyWordsPage: PagedResultDto<LemmaWordItemDto> = { page: 1, pageSize: LEMMA_DETAIL_PAGE_SIZE, totalCount: 0, items: [] };

  protected readonly ayahsPageForView = computed(() => {
    const page = this.panelState().ayahs;
    return page ? { ...page, items: page.items.map(mapLemmaAyahMatchToShared) } : this.emptyAyahsPage;
  });

  protected get wordViewLabels() {
    return LEMMAS_WORD_VIEW_LABELS;
  }

  protected get surahViewLabels() {
    return LEMMAS_SURAHS_VIEW_LABELS;
  }

  protected get wordsTablistLabel() {
    return LEMMAS_WORDS_TABLIST_LABEL;
  }

  protected get surahsTablistLabel() {
    return LEMMAS_SURAHS_TABLIST_LABEL;
  }

  protected get emptyViewLabel() {
    return LEMMAS_EMPTY_VIEW_LABEL;
  }

  protected get notFoundLabel() {
    return LEMMAS_NOT_FOUND_LABEL;
  }

  protected get panelLoadingLabel() {
    return LEMMAS_LOADING_LABEL;
  }

  constructor() {
    // Track ONLY the frame input: applyUrlState reads/writes the controller's
    // panel signal internally, and tracking it would re-trigger this effect on
    // every load-state change (cancelling in-flight summary loads).
    effect(() => {
      const frame = this.frame();
      untracked(() =>
        this.controller.applyUrlState({
          lemmaId: frame.id,
          view: frame.view,
          wordView: frame.wordView,
          surahView: frame.surahView,
          detailPage: frame.detailPage,
          typeCode: frame.typeCode,
        }),
      );
    });

    effect(() => this.titleStore?.setTitle(this.entityTitle()));
    effect(() => this.titleStore?.setAyahCount(this.entityAyahCount()));
    inject(DestroyRef).onDestroy(() => this.titleStore?.clear());
  }

  protected onViewChange(view: LemmaView): void {
    const frame = this.frame();
    if (view === frame.view) {
      return;
    }

    this.overlay.replaceTopFrame({
      ...frame,
      view,
      wordView: view === 'words' ? frame.wordView : DEFAULT_LEMMA_WORD_VIEW,
      surahView: view === 'surahs' ? frame.surahView : DEFAULT_LEMMA_SURAHS_VIEW,
      detailPage: DEFAULT_LEMMA_DETAIL_PAGE,
      typeCode: null,
    });
  }

  protected subViewTabId(option: string): string {
    return `${this.subViewId}-tab-${option}`;
  }

  protected onWordViewChange(wordView: LemmaWordView): void {
    const frame = this.frame();
    if (frame.view !== 'words' || wordView === frame.wordView) {
      return;
    }

    this.overlay.replaceTopFrame({ ...frame, wordView, detailPage: DEFAULT_LEMMA_DETAIL_PAGE, typeCode: null });
  }

  protected onSurahViewChange(surahView: LemmaSurahView): void {
    const frame = this.frame();
    if (frame.view !== 'surahs' || surahView === frame.surahView) {
      return;
    }

    this.overlay.replaceTopFrame({ ...frame, surahView, typeCode: null });
  }

  protected onDetailPageChange(page: number): void {
    const frame = this.frame();
    if (page === frame.detailPage) {
      return;
    }

    this.overlay.replaceTopFrame({
      ...frame,
      detailPage: page,
      typeCode: frame.view === 'ayahs' ? frame.typeCode : null,
    });
  }

  protected onAyahTypeCodeChange(typeCode: string | null): void {
    const frame = this.frame();
    if (frame.view !== 'ayahs') {
      return;
    }

    const normalizedTypeCode = this.normalizeTypeCode(typeCode);
    if (normalizedTypeCode === frame.typeCode && frame.detailPage === DEFAULT_LEMMA_DETAIL_PAGE) {
      return;
    }

    this.overlay.replaceTopFrame({
      ...frame,
      typeCode: normalizedTypeCode,
      detailPage: DEFAULT_LEMMA_DETAIL_PAGE,
    });
  }

  private normalizeTypeCode(typeCode: string | null): string | null {
    return typeCode === null || typeCode.trim().length === 0 ? null : typeCode.trim();
  }
}
