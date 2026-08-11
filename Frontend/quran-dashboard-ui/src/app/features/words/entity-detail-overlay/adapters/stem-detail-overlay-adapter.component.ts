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
import { WORDS_DETAIL_RETRY_LABEL } from '../../models/words-shared.labels';
import { QdErrorStateComponent } from '../../../../shared/ui/error-state/error-state.component';
import { QdTabDirective } from '../../../../shared/ui/tabs/tab.directive';
import { QdTabsComponent } from '../../../../shared/ui/tabs/tabs.component';
import { EntityDetailOverlayHeaderStore } from '../entity-detail-overlay-header.store';
import { LinkingSourceDescriptor } from '../../../linking/models/linking-source.models';

let nextSubViewInstance = 0;

@Component({
  selector: 'qd-stem-detail-overlay-adapter',
  standalone: true,
  imports: [
    AyahMatchesListComponent,
    MissingSurahsListComponent,
    NgTemplateOutlet,
    QdErrorStateComponent,
    QdTabDirective,
    QdTabsComponent,
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
  private readonly headerStore = inject(EntityDetailOverlayHeaderStore, { optional: true });

  readonly frame = input.required<StemDetailFrame>();

  private readonly subViewId = `overlay-stems-subview-${nextSubViewInstance++}`;
  protected readonly subViewPanelId = `${this.subViewId}-panel`;
  protected readonly activeSubViewTabId = computed(() => {
    const frame = this.frame();
    if (frame.view === 'words') {
      return this.subViewTabId(frame.wordView);
    }
    return frame.view === 'surahs' ? this.subViewTabId(frame.surahView) : null;
  });

  protected readonly retryLabel = WORDS_DETAIL_RETRY_LABEL;

  protected onRetry(): void {
    this.controller.retryCurrentIdentity();
  }

  protected readonly panelState = this.controller.panelState;

  readonly entityTitle = computed(() => this.panelState().summary?.stemText ?? '');

  readonly entityAyahCount = computed(() => this.panelState().summary?.ayahsCount ?? null);

  protected readonly linkingSource = computed<LinkingSourceDescriptor | null>(() => {
    const frame = this.frame();
    const summary = this.panelState().summary;
    if (summary === null || summary.id !== frame.id) {
      return null;
    }
    return {
      kind: 'stem',
      stemId: frame.id,
      typeCode: frame.typeCode,
      label: summary.stemText,
    };
  });

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

    effect(() => this.headerStore?.setTitle(this.entityTitle()));
    effect(() => this.headerStore?.setAyahCount(this.entityAyahCount()));
    effect(() => this.headerStore?.setLinkingSource(this.linkingSource()));
    inject(DestroyRef).onDestroy(() => this.headerStore?.clear());
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

  protected subViewTabId(option: string): string {
    return `${this.subViewId}-tab-${option}`;
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
    if (normalizedTypeCode === frame.typeCode) {
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
