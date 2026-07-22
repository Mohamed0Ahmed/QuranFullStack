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
import { QdStateComponent } from '../../../../shared/ui/state/state.component';
import { EntityDetailOverlayTitleStore } from '../entity-detail-overlay-title.store';

@Component({
  selector: 'qd-word-type-detail-overlay-adapter',
  standalone: true,
  imports: [
    AyahMatchesListComponent,
    MissingSurahsListComponent,
    QdStateComponent,
    SurahOccurrencesListComponent,
    WordTypeDetailsPanelComponent,
  ],
  providers: [WordTypesDetailController, { provide: DETAIL_OVERLAY_LINK_MODE, useValue: 'append' }],
  templateUrl: './word-type-detail-overlay-adapter.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WordTypeDetailOverlayAdapterComponent {
  private readonly controller = inject(WordTypesDetailController);
  private readonly overlay = inject(DetailOverlayHistoryService);
  private readonly titleStore = inject(EntityDetailOverlayTitleStore, { optional: true });

  readonly frame = input.required<WordTypeDetailFrame>();

  protected readonly retryLabel = WORDS_DETAIL_RETRY_LABEL;

  protected onRetry(): void {
    this.controller.retryCurrentIdentity();
  }

  protected readonly panelState = this.controller.panelState;

  readonly entityTitle = computed(() => this.panelState().summary?.displayText ?? '');

  readonly entityAyahCount = computed(() => this.panelState().summary?.ayahsCount ?? null);

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
    // Track only frame(); untracked() keeps applyUrlState's own signal writes from
    // re-triggering this effect and cancelling in-flight loads.
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

        // Canonicalize a hand-edited view=words in the URL to the default; the replaced
        // frame re-enters this effect and short-circuits in the controller, so no loop.
        if (frame.view === 'words') {
          this.overlay.replaceTopFrame({ ...frame, view: DEFAULT_WORD_TYPES_DETAIL_VIEW });
        }
      });
    });

    effect(() => this.titleStore?.setTitle(this.entityTitle()));
    effect(() => this.titleStore?.setAyahCount(this.entityAyahCount()));
    inject(DestroyRef).onDestroy(() => this.titleStore?.clear());
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

    // Serialize the effective view so a clamped 'words' frame is canonicalized.
    this.overlay.replaceTopFrame({ ...frame, view: this.effectiveView(), detailPage: page });
  }
}
