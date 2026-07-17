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
import { RootDetailFrame } from '../../../../core/navigation/detail-overlay/detail-overlay.models';
import { AyahMatchesListComponent } from '../../components/ayah-matches-list/ayah-matches-list.component';
import { MissingSurahsListComponent } from '../../components/missing-surahs-list/missing-surahs-list.component';
import { RootDetailsPanelComponent } from '../../components/root-details-panel/root-details-panel.component';
import { RootLemmasListComponent } from '../../components/root-lemmas-list/root-lemmas-list.component';
import { RootStemsListComponent } from '../../components/root-stems-list/root-stems-list.component';
import { RootWordsListComponent } from '../../components/root-words-list/root-words-list.component';
import { SurahOccurrencesListComponent } from '../../components/surah-occurrences-list/surah-occurrences-list.component';
import {
  ROOTS_EMPTY_VIEW_LABEL,
  ROOTS_NOT_FOUND_LABEL,
  ROOTS_SURAHS_TABLIST_LABEL,
  ROOTS_SURAHS_VIEW_LABELS,
  ROOTS_WORDS_TABLIST_LABEL,
  ROOTS_WORD_VIEW_LABELS,
} from '../../models/roots.labels';
import {
  DEFAULT_ROOT_DETAIL_PAGE,
  DEFAULT_ROOT_SURAHS_VIEW,
  DEFAULT_ROOT_WORD_VIEW,
  PagedResultDto,
  ROOT_DETAIL_PAGE_SIZE,
  RootSurahView,
  RootView,
  RootWordItemDto,
  RootWordView,
} from '../../models/roots.models';
import { AyahMatchDto } from '../../models/unique-words.models';
import { RootsDetailController } from '../../state/roots-detail.controller';
import { mapRootAyahMatchToShared } from '../../utils/root-ayah-match.mapper';
import { EntityDetailOverlayTitleStore } from '../entity-detail-overlay-title.store';

/**
 * Overlay adapter for root frames (Feature 029, Change B4): the reference
 * implementation of the route-independent detail pattern. It owns a
 * component-scoped `RootsDetailController` (never the page facade), maps every
 * `frame` input change onto `applyUrlState`, and renders the existing root
 * detail content in frameless mode inside the global dialog shell.
 *
 * All view/sub-view/page changes go to the URL through
 * `DetailOverlayHistoryService.replaceTopFrame(...)` — never to the Router and
 * never directly into controller state. The URL sync feeds the new frame back
 * into this component, which re-drives the controller.
 */
@Component({
  selector: 'qd-root-detail-overlay-adapter',
  standalone: true,
  imports: [
    AyahMatchesListComponent,
    MissingSurahsListComponent,
    RootDetailsPanelComponent,
    RootLemmasListComponent,
    RootStemsListComponent,
    RootWordsListComponent,
    SurahOccurrencesListComponent,
  ],
  providers: [RootsDetailController, { provide: DETAIL_OVERLAY_LINK_MODE, useValue: 'append' }],
  templateUrl: './root-detail-overlay-adapter.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RootDetailOverlayAdapterComponent {
  private readonly controller = inject(RootsDetailController);
  private readonly overlay = inject(DetailOverlayHistoryService);
  private readonly titleStore = inject(EntityDetailOverlayTitleStore, { optional: true });

  readonly frame = input.required<RootDetailFrame>();

  protected readonly panelState = this.controller.panelState;

  /** Loaded entity title for the shell heading ('' while the summary loads). */
  readonly entityTitle = computed(() => this.panelState().summary?.rootText ?? '');

  /** Entity-level ayah count for the shell header meta (null while the summary loads). */
  readonly entityAyahCount = computed(() => this.panelState().summary?.ayahsCount ?? null);

  protected readonly wordViewOptions: readonly RootWordView[] = ['simple', 'tashkeel'];
  protected readonly surahViewOptions: readonly RootSurahView[] = ['mentioned', 'missing'];
  protected readonly emptyAyahsPage: PagedResultDto<AyahMatchDto> = { page: 1, pageSize: ROOT_DETAIL_PAGE_SIZE, totalCount: 0, items: [] };
  protected readonly emptyWordsPage: PagedResultDto<RootWordItemDto> = { page: 1, pageSize: ROOT_DETAIL_PAGE_SIZE, totalCount: 0, items: [] };

  protected readonly ayahsPageForView = computed(() => {
    const page = this.panelState().ayahs;
    return page ? { ...page, items: page.items.map(mapRootAyahMatchToShared) } : this.emptyAyahsPage;
  });

  protected get wordViewLabels() {
    return ROOTS_WORD_VIEW_LABELS;
  }

  protected get surahViewLabels() {
    return ROOTS_SURAHS_VIEW_LABELS;
  }

  protected get wordsTablistLabel() {
    return ROOTS_WORDS_TABLIST_LABEL;
  }

  protected get surahsTablistLabel() {
    return ROOTS_SURAHS_TABLIST_LABEL;
  }

  protected get emptyViewLabel() {
    return ROOTS_EMPTY_VIEW_LABEL;
  }

  protected get notFoundLabel() {
    return ROOTS_NOT_FOUND_LABEL;
  }

  constructor() {
    // Track ONLY the frame input: applyUrlState reads/writes the controller's
    // panel signal internally, and tracking it would re-trigger this effect on
    // every load-state change (cancelling in-flight summary loads).
    effect(() => {
      const frame = this.frame();
      untracked(() =>
        this.controller.applyUrlState({
          rootId: frame.id,
          view: frame.view,
          wordView: frame.wordView,
          surahView: frame.surahView,
          detailPage: frame.detailPage,
        }),
      );
    });

    effect(() => this.titleStore?.setTitle(this.entityTitle()));
    effect(() => this.titleStore?.setAyahCount(this.entityAyahCount()));
    inject(DestroyRef).onDestroy(() => this.titleStore?.clear());
  }

  protected onViewChange(view: RootView): void {
    const frame = this.frame();
    if (view === frame.view) {
      return;
    }

    this.overlay.replaceTopFrame({
      ...frame,
      view,
      wordView: view === 'words' ? frame.wordView : DEFAULT_ROOT_WORD_VIEW,
      surahView: view === 'surahs' ? frame.surahView : DEFAULT_ROOT_SURAHS_VIEW,
      detailPage: DEFAULT_ROOT_DETAIL_PAGE,
    });
  }

  protected onWordViewChange(wordView: RootWordView): void {
    const frame = this.frame();
    if (frame.view !== 'words' || wordView === frame.wordView) {
      return;
    }

    this.overlay.replaceTopFrame({ ...frame, wordView, detailPage: DEFAULT_ROOT_DETAIL_PAGE });
  }

  protected onSurahViewChange(surahView: RootSurahView): void {
    const frame = this.frame();
    if (frame.view !== 'surahs' || surahView === frame.surahView) {
      return;
    }

    this.overlay.replaceTopFrame({ ...frame, surahView });
  }

  protected onDetailPageChange(page: number): void {
    const frame = this.frame();
    if (page === frame.detailPage) {
      return;
    }

    this.overlay.replaceTopFrame({ ...frame, detailPage: page });
  }
}
