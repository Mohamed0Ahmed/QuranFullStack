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
import { RootDetailFrame } from '../../../../core/navigation/detail-overlay/detail-overlay.models';
import { AyahMatchesListComponent } from '../../components/ayah-matches-list/ayah-matches-list.component';
import { AyahTypeFiltersComponent } from '../../components/ayah-type-filters/ayah-type-filters.component';
import { MissingSurahsListComponent } from '../../components/missing-surahs-list/missing-surahs-list.component';
import { RootDetailsPanelComponent } from '../../components/root-details-panel/root-details-panel.component';
import { RootLemmasListComponent } from '../../components/root-lemmas-list/root-lemmas-list.component';
import { RootStemsListComponent } from '../../components/root-stems-list/root-stems-list.component';
import { RootWordsListComponent } from '../../components/root-words-list/root-words-list.component';
import { SurahOccurrencesListComponent } from '../../components/surah-occurrences-list/surah-occurrences-list.component';
import {
  ROOTS_EMPTY_VIEW_LABEL,
  ROOTS_LOADING_LABEL,
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
import { WORDS_DETAIL_RETRY_LABEL } from '../../models/words-shared.labels';
import { QdErrorStateComponent } from '../../../../shared/ui/error-state/error-state.component';
import { QdTabDirective } from '../../../../shared/ui/tabs/tab.directive';
import { QdTabsComponent } from '../../../../shared/ui/tabs/tabs.component';
import { EntityDetailOverlayHeaderStore } from '../entity-detail-overlay-header.store';
import { LinkingSourceDescriptor } from '../../../linking/models/linking-source.models';

let nextSubViewInstance = 0;

@Component({
  selector: 'qd-root-detail-overlay-adapter',
  standalone: true,
  imports: [
    AyahMatchesListComponent,
    AyahTypeFiltersComponent,
    MissingSurahsListComponent,
    NgTemplateOutlet,
    QdErrorStateComponent,
    QdTabDirective,
    QdTabsComponent,
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
  private readonly headerStore = inject(EntityDetailOverlayHeaderStore, { optional: true });

  readonly frame = input.required<RootDetailFrame>();

  private readonly subViewId = `overlay-roots-subview-${nextSubViewInstance++}`;
  protected readonly subViewPanelId = `${this.subViewId}-panel`;
  protected readonly activeSubViewTabId = computed(() => {
    const frame = this.frame();
    if (frame.view === 'words') {
      return this.subViewTabId(frame.wordView);
    }
    return frame.view === 'surahs' ? this.subViewTabId(frame.surahView) : null;
  });

  protected readonly panelState = this.controller.panelState;

  readonly entityTitle = computed(() => this.panelState().summary?.rootText ?? '');

  readonly entityAyahCount = computed(() => this.panelState().summary?.ayahsCount ?? null);

  protected readonly linkingSource = computed<LinkingSourceDescriptor | null>(() => {
    const frame = this.frame();
    const summary = this.panelState().summary;
    if (summary === null || summary.id !== frame.id) {
      return null;
    }
    return {
      kind: 'root',
      rootId: frame.id,
      typeCodes: frame.typeCode === null ? [] : [frame.typeCode],
      label: summary.rootText,
    };
  });

  protected readonly retryLabel = WORDS_DETAIL_RETRY_LABEL;

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

  protected get panelLoadingLabel() {
    return ROOTS_LOADING_LABEL;
  }

  constructor() {
    effect(() => {
      const frame = this.frame();
      untracked(() =>
        this.controller.applyUrlState({
          rootId: frame.id,
          view: frame.view,
          wordView: frame.wordView,
          surahView: frame.surahView,
          detailPage: frame.detailPage,
          typeCode: frame.typeCode,
        }),
      );
    });

    effect(() => this.headerStore?.setTitle(this.entityTitle()));
    effect(() => this.headerStore?.setAyahCount(this.entityAyahCount()));
    effect(() => this.headerStore?.setLinkingSource(this.linkingSource()));
    inject(DestroyRef).onDestroy(() => this.headerStore?.clear());
  }

  protected onRetry(): void {
    this.controller.retryCurrentIdentity();
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
      typeCode: null,
    });
  }

  protected subViewTabId(option: string): string {
    return `${this.subViewId}-tab-${option}`;
  }

  protected onWordViewChange(wordView: RootWordView): void {
    const frame = this.frame();
    if (frame.view !== 'words' || wordView === frame.wordView) {
      return;
    }

    this.overlay.replaceTopFrame({ ...frame, wordView, detailPage: DEFAULT_ROOT_DETAIL_PAGE, typeCode: null });
  }

  protected onSurahViewChange(surahView: RootSurahView): void {
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

    this.overlay.replaceTopFrame({ ...frame, detailPage: page, typeCode: frame.view === 'ayahs' ? frame.typeCode : null });
  }

  protected onAyahTypeCodeChange(typeCode: string | null): void {
    const frame = this.frame();
    if (frame.view !== 'ayahs') {
      return;
    }

    const normalizedTypeCode = this.normalizeTypeCode(typeCode);
    if (normalizedTypeCode === frame.typeCode && frame.detailPage === DEFAULT_ROOT_DETAIL_PAGE) {
      return;
    }

    this.overlay.replaceTopFrame({
      ...frame,
      typeCode: normalizedTypeCode,
      detailPage: DEFAULT_ROOT_DETAIL_PAGE,
    });
  }

  private normalizeTypeCode(typeCode: string | null): string | null {
    return typeCode === null || typeCode.trim().length === 0 ? null : typeCode.trim();
  }
}
