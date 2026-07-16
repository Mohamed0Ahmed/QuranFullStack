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

import { DetailOverlayHistoryService } from '../../../../core/navigation/detail-overlay/detail-overlay-history.service';
import { DETAIL_OVERLAY_LINK_MODE } from '../../../../core/navigation/detail-overlay/detail-overlay-link.directive';
import { StemDetailFrame } from '../../../../core/navigation/detail-overlay/detail-overlay.models';
import { AyahMatchesListComponent } from '../../components/ayah-matches-list/ayah-matches-list.component';
import { MissingSurahsListComponent } from '../../components/missing-surahs-list/missing-surahs-list.component';
import { StemAyahTypeFiltersComponent } from '../../components/stem-ayah-type-filters/stem-ayah-type-filters.component';
import { StemDetailsPanelComponent } from '../../components/stem-details-panel/stem-details-panel.component';
import { StemLemmasListComponent } from '../../components/stem-lemmas-list/stem-lemmas-list.component';
import { StemWordsListComponent } from '../../components/stem-words-list/stem-words-list.component';
import { SurahOccurrencesListComponent } from '../../components/surah-occurrences-list/surah-occurrences-list.component';
import {
  STEMS_EMPTY_VIEW_LABEL,
  STEMS_LOADING_LABEL,
  STEMS_NOT_FOUND_LABEL,
  STEMS_SURAHS_TABLIST_LABEL,
  STEMS_SURAHS_VIEW_LABELS,
  STEMS_WORDS_TABLIST_LABEL,
  STEMS_WORD_VIEW_LABELS,
} from '../../models/stems.labels';
import {
  DEFAULT_STEM_DETAIL_PAGE,
  DEFAULT_STEM_SURAHS_VIEW,
  DEFAULT_STEM_WORD_VIEW,
  PagedResultDto,
  STEM_DETAIL_PAGE_SIZE,
  StemSurahView,
  StemView,
  StemWordItemDto,
  StemWordView,
} from '../../models/stems.models';
import { AyahMatchDto } from '../../models/unique-words.models';
import { StemsDetailController } from '../../state/stems-detail.controller';
import { mapStemAyahMatchToShared } from '../../utils/stem-ayah-match.mapper';
import { EntityDetailOverlayTitleStore } from '../entity-detail-overlay-title.store';

/**
 * Overlay adapter for stem frames (Feature 029, Change B4): replicates the
 * root reference implementation of the route-independent detail pattern. It
 * owns a component-scoped `StemsDetailController` (never the page facade),
 * maps every `frame` input change onto `applyUrlState`, and renders the
 * existing stem detail content in frameless mode inside the global dialog
 * shell. Unlike roots, the ayahs view carries a `typeCode` filter that is part
 * of the frame identity.
 *
 * All view/sub-view/page/type changes go to the URL through
 * `DetailOverlayHistoryService.replaceTopFrame(...)` — never to the Router and
 * never directly into controller state. The URL sync feeds the new frame back
 * into this component, which re-drives the controller.
 */
@Component({
  selector: 'qd-stem-detail-overlay-adapter',
  standalone: true,
  imports: [
    AyahMatchesListComponent,
    MissingSurahsListComponent,
    StemAyahTypeFiltersComponent,
    StemDetailsPanelComponent,
    StemLemmasListComponent,
    StemWordsListComponent,
    SurahOccurrencesListComponent,
  ],
  providers: [StemsDetailController, { provide: DETAIL_OVERLAY_LINK_MODE, useValue: 'append' }],
  templateUrl: './stem-detail-overlay-adapter.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StemDetailOverlayAdapterComponent {
  private readonly controller = inject(StemsDetailController);
  private readonly overlay = inject(DetailOverlayHistoryService);
  private readonly titleStore = inject(EntityDetailOverlayTitleStore, { optional: true });

  readonly frame = input.required<StemDetailFrame>();

  protected readonly panelState = this.controller.panelState;

  /** Loaded entity title for the shell heading ('' while the summary loads). */
  readonly entityTitle = computed(() => this.panelState().summary?.stemText ?? '');

  protected readonly wordViewOptions: readonly StemWordView[] = ['simple', 'tashkeel'];
  protected readonly surahViewOptions: readonly StemSurahView[] = ['mentioned', 'missing'];
  protected readonly emptyAyahsPage: PagedResultDto<AyahMatchDto> = { page: 1, pageSize: STEM_DETAIL_PAGE_SIZE, totalCount: 0, items: [] };
  protected readonly emptyWordsPage: PagedResultDto<StemWordItemDto> = { page: 1, pageSize: STEM_DETAIL_PAGE_SIZE, totalCount: 0, items: [] };

  protected readonly ayahsPageForView = computed(() => {
    const page = this.panelState().ayahs;
    return page ? { ...page, items: page.items.map(mapStemAyahMatchToShared) } : this.emptyAyahsPage;
  });

  protected get wordViewLabels() {
    return STEMS_WORD_VIEW_LABELS;
  }

  protected get surahViewLabels() {
    return STEMS_SURAHS_VIEW_LABELS;
  }

  protected get wordsTablistLabel() {
    return STEMS_WORDS_TABLIST_LABEL;
  }

  protected get surahsTablistLabel() {
    return STEMS_SURAHS_TABLIST_LABEL;
  }

  protected get emptyViewLabel() {
    return STEMS_EMPTY_VIEW_LABEL;
  }

  protected get notFoundLabel() {
    return STEMS_NOT_FOUND_LABEL;
  }

  protected get panelLoadingLabel() {
    return STEMS_LOADING_LABEL;
  }

  constructor() {
    // Track ONLY the frame input: applyUrlState reads/writes the controller's
    // panel signal internally, and tracking it would re-trigger this effect on
    // every load-state change (cancelling in-flight summary loads).
    effect(() => {
      const frame = this.frame();
      untracked(() =>
        this.controller.applyUrlState({
          stemId: frame.id,
          view: frame.view,
          wordView: frame.wordView,
          surahView: frame.surahView,
          detailPage: frame.detailPage,
          typeCode: frame.typeCode,
        }),
      );
    });

    effect(() => this.titleStore?.setTitle(this.entityTitle()));
    inject(DestroyRef).onDestroy(() => this.titleStore?.clear());
  }

  protected onViewChange(view: StemView): void {
    const frame = this.frame();
    if (view === frame.view) {
      return;
    }

    this.overlay.replaceTopFrame({
      ...frame,
      view,
      wordView: view === 'words' ? frame.wordView : DEFAULT_STEM_WORD_VIEW,
      surahView: view === 'surahs' ? frame.surahView : DEFAULT_STEM_SURAHS_VIEW,
      detailPage: DEFAULT_STEM_DETAIL_PAGE,
      typeCode: null,
    });
  }

  protected onWordViewChange(wordView: StemWordView): void {
    const frame = this.frame();
    if (frame.view !== 'words' || wordView === frame.wordView) {
      return;
    }

    this.overlay.replaceTopFrame({ ...frame, wordView, detailPage: DEFAULT_STEM_DETAIL_PAGE, typeCode: null });
  }

  protected onSurahViewChange(surahView: StemSurahView): void {
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
    if (normalizedTypeCode === frame.typeCode && frame.detailPage === DEFAULT_STEM_DETAIL_PAGE) {
      return;
    }

    this.overlay.replaceTopFrame({
      ...frame,
      typeCode: normalizedTypeCode,
      detailPage: DEFAULT_STEM_DETAIL_PAGE,
    });
  }

  private normalizeTypeCode(typeCode: string | null): string | null {
    return typeCode === null || typeCode.trim().length === 0 ? null : typeCode.trim();
  }
}
