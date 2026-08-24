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
import { WordTypeDetailFrame } from '../../../../core/navigation/detail-overlay/detail-overlay.models';
import { AyahMatchesListComponent } from '../../components/ayah-matches-list/ayah-matches-list.component';
import { MissingSurahsListComponent } from '../../components/missing-surahs-list/missing-surahs-list.component';
import { SurahOccurrencesListComponent } from '../../components/surah-occurrences-list/surah-occurrences-list.component';
import { WordTypeDetailsPanelComponent } from '../../components/word-type-details-panel/word-type-details-panel.component';
import { AyahMatchDto } from '../../models/unique-words.models';
import { WORD_TYPE_DETAIL_PRESENTATIONS, WORD_TYPES_NOT_FOUND_LABEL } from '../../models/word-types.labels';
import {
  DEFAULT_WORD_TYPES_DETAIL_PAGE,
  DEFAULT_WORD_TYPES_DETAIL_VIEW,
  PagedResultDto,
  WORD_TYPES_DETAIL_PAGE_SIZE,
  WordTypeDetailView,
} from '../../models/word-types.models';
import { WordTypesDetailController } from '../../state/word-types-detail.controller';
import { mapWordTypeAyahMatchToShared } from '../../utils/word-type-ayah-match.mapper';
import { WORDS_DETAIL_RETRY_LABEL } from '../../models/words-shared.labels';
import { QdErrorStateComponent } from '../../../../shared/ui/error-state/error-state.component';
import { EntityDetailOverlayHeaderStore } from '../entity-detail-overlay-header.store';
import { LinkingSourceDescriptor } from '../../../linking/models/linking-source.models';

@Component({
  selector: 'qd-word-type-detail-overlay-adapter',
  standalone: true,
  imports: [
    AyahMatchesListComponent,
    MissingSurahsListComponent,
    NgTemplateOutlet,
    QdErrorStateComponent,
    SurahOccurrencesListComponent,
    WordTypeDetailsPanelComponent,
  ],
  providers: [WordTypesDetailController, { provide: DETAIL_OVERLAY_LINK_MODE, useValue: 'append' }],
  templateUrl: './word-type-detail-overlay-adapter.component.html',
  styleUrl: './word-type-detail-overlay-adapter.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WordTypeDetailOverlayAdapterComponent {
  private readonly controller = inject(WordTypesDetailController);
  private readonly overlay = inject(DetailOverlayHistoryService);
  private readonly headerStore = inject(EntityDetailOverlayHeaderStore, { optional: true });

  readonly frame = input.required<WordTypeDetailFrame>();

  protected readonly retryLabel = WORDS_DETAIL_RETRY_LABEL;

  protected onRetry(): void {
    this.controller.retryCurrentIdentity();
  }

  protected readonly panelState = this.controller.panelState;

  readonly entityTitle = computed(() => this.panelState().summary?.displayText ?? '');

  readonly entityAyahCount = computed(() => this.panelState().summary?.ayahsCount ?? null);

  protected readonly linkingSource = computed(() => {
    const selection = this.panelState().selection;
    const summary = this.panelState().summary;
    if (selection === null || selection.kind !== 'word' || summary === null) {
      return null;
    }
    return {
      kind: 'word-type',
      selection: { kind: 'word', ...selection.identity, scope: { ...selection.scope } },
      label: summary.displayText,
    } satisfies LinkingSourceDescriptor;
  });

  protected readonly effectiveView = computed<WordTypeDetailView>(() =>
    this.frame().view === 'words' ? DEFAULT_WORD_TYPES_DETAIL_VIEW : this.frame().view,
  );

  protected readonly emptyAyahsPage: PagedResultDto<AyahMatchDto> = {
    page: 1,
    pageSize: WORD_TYPES_DETAIL_PAGE_SIZE,
    totalCount: 0,
    items: [],
  };

  protected readonly ayahsPageForView = computed(() => {
    const page = this.panelState().ayahs;
    return page ? { ...page, items: page.items.map(mapWordTypeAyahMatchToShared) } : this.emptyAyahsPage;
  });

  protected readonly mentionedSurahs = computed(() =>
    (this.panelState().surahs?.surahs ?? []).map((surah) => ({
      surahNumber: surah.surahNumber,
      nameArabic: surah.nameArabic,
      occurrencesInSurah: surah.occurrencesCount,
    })),
  );

  protected readonly missingSurahs = computed(() =>
    (this.panelState().surahs?.missingSurahs ?? []).map((surah) => ({
      surahNumber: surah.surahNumber,
      nameArabic: surah.nameArabic,
    })),
  );

  protected readonly emptyViewLabel = computed(
    () => WORD_TYPE_DETAIL_PRESENTATIONS.word.emptyViewLabels[this.effectiveView()],
  );

  protected get notFoundLabel() {
    return WORD_TYPES_NOT_FOUND_LABEL;
  }

  constructor() {
    effect(() => {
      const frame = this.frame();
      untracked(() => {
        this.controller.applyUrlState({
          identity: {
            tashkeelWordId: frame.tashkeelWordId,
            contextCode: frame.contextCode,
            case: frame.case,
            tense: frame.tense,
            voice: frame.voice,
          },
          view: this.effectiveView(),
          detailPage: frame.detailPage,
        });
        if (frame.view === 'words') {
          this.overlay.replaceTopFrame({ ...frame, view: DEFAULT_WORD_TYPES_DETAIL_VIEW });
        }
      });
    });

    effect(() => this.headerStore?.setTitle(this.entityTitle()));
    effect(() => this.headerStore?.setAyahCount(this.entityAyahCount()));
    effect(() => this.headerStore?.setLinkingSource(this.linkingSource()));
    inject(DestroyRef).onDestroy(() => this.headerStore?.clear());
  }

  protected onViewChange(view: WordTypeDetailView): void {
    const frame = this.frame();
    if (view === this.effectiveView()) {
      return;
    }

    this.overlay.replaceTopFrame({ ...frame, view, detailPage: DEFAULT_WORD_TYPES_DETAIL_PAGE });
  }

  protected onDetailPageChange(page: number): void {
    const frame = this.frame();
    if (page === frame.detailPage) {
      return;
    }

    this.overlay.replaceTopFrame({ ...frame, view: this.effectiveView(), detailPage: page });
  }
}
