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
import { EntityDetailOverlayTitleStore } from '../entity-detail-overlay-title.store';

/**
 * Overlay adapter for Word Type frames (Feature 029, Change B4), following the
 * root reference implementation. It owns a component-scoped
 * `WordTypesDetailController` (never the page facade), maps every `frame` input
 * change onto `applyUrlState` with the full composite word identity, and
 * renders the existing word-kind detail content in frameless mode inside the
 * global dialog shell.
 *
 * All view/page changes go to the URL through
 * `DetailOverlayHistoryService.replaceTopFrame(...)` — never to the Router and
 * never directly into controller state. The URL sync feeds the new frame back
 * into this component, which re-drives the controller.
 */
@Component({
  selector: 'qd-word-type-detail-overlay-adapter',
  standalone: true,
  imports: [
    AyahMatchesListComponent,
    MissingSurahsListComponent,
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

  protected readonly panelState = this.controller.panelState;

  /** Loaded entity title for the shell heading ('' while the summary loads). */
  readonly entityTitle = computed(() => this.panelState().summary?.displayText ?? '');

  /** Entity-level ayah count for the shell header meta (null while the summary loads). */
  readonly entityAyahCount = computed(() => this.panelState().summary?.ayahsCount ?? null);

  /**
   * A word-kind Word Type detail has no member-words view — 'words' exists only
   * for grouped root/stem/lemma selections (`WordTypesDetailViewLoader` no-ops
   * it for kind 'word'). A frame carrying view 'words' (a hand-edited or shared
   * URL) is therefore clamped to the word detail default, 'ayahs'.
   */
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
    // Track ONLY the frame input: applyUrlState reads/writes the controller's
    // panel signal internally, and tracking it would re-trigger this effect on
    // every load-state change (cancelling in-flight summary loads).
    effect(() => {
      const frame = this.frame();
      untracked(() =>
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
        }),
      );
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

    // Canonical frame: the surahs view is single-shot, so it always serializes
    // detail page 1 — every tab switch resets the page.
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
