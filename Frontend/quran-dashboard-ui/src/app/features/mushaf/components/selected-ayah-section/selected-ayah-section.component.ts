import {
  Component,
  DestroyRef,
  ElementRef,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { CommonModule } from '@angular/common';

import {
  AyahNavigationTarget,
  AyahStudyTab,
  AyahStudyViewModel,
  AYAH_STUDY_TAB_LABELS,
  ResourceLoadState,
  SimilarAyahsDto,
  AyahMutashabihatDto,
  SourceOption,
} from '../../models/mushaf.models';
import { SourceSelectorComponent } from '../source-selector/source-selector.component';
import { SimilarAyahsCardComponent } from '../similar-ayahs-card/similar-ayahs-card.component';
import { MutashabihatGroupsCardComponent } from '../mutashabihat-groups-card/mutashabihat-groups-card.component';
import { TafsirCardComponent } from '../tafsir-card/tafsir-card.component';
import { TranslationCardComponent } from '../translation-card/translation-card.component';
import { FullI3rabCardComponent } from '../full-i3rab-card/full-i3rab-card.component';
import { toStudyAyahDisplayText } from '../../utils/mushaf-verse-key-display';

const RESERVATION_INLINE_SIZE_TOLERANCE_PX = 1;

@Component({
  selector: 'qd-selected-ayah-section',
  standalone: true,
  imports: [
    CommonModule,
    SourceSelectorComponent,
    SimilarAyahsCardComponent,
    MutashabihatGroupsCardComponent,
    TafsirCardComponent,
    TranslationCardComponent,
    FullI3rabCardComponent,
  ],
  templateUrl: './selected-ayah-section.component.html',
  styleUrls: ['./selected-ayah-section.component.scss'],
  host: {
    '[class.qd-selected-ayah-section--embedded]': 'embedded()',
  },
})
export class SelectedAyahSectionComponent {
  private readonly destroyRef = inject(DestroyRef);

  readonly study = input<AyahStudyViewModel | null>(null);
  readonly loadState = input.required<ResourceLoadState>();
  readonly similarAyahs = input<SimilarAyahsDto | null>(null);
  readonly similarAyahsLoadState = input<ResourceLoadState>({
    isLoading: false,
    isEmpty: false,
    errorMessage: null,
  });
  readonly mutashabihat = input<AyahMutashabihatDto | null>(null);
  readonly mutashabihatLoadState = input<ResourceLoadState>({
    isLoading: false,
    isEmpty: false,
    errorMessage: null,
  });
  readonly activeTab = input<AyahStudyTab>('tafsir');
  readonly selectedVerseKey = input<string | null>(null);
  readonly embedded = input(false);

  readonly tafsirOptions = input<SourceOption[]>([]);
  readonly translationOptions = input<SourceOption[]>([]);
  readonly fullI3rabOptions = input<SourceOption[]>([]);

  readonly tabChange = output<AyahStudyTab>();
  readonly tafsirSourceChange = output<string>();
  readonly translationSourceChange = output<string>();
  readonly fullI3rabSourceChange = output<string>();
  readonly sectionFocus = output<void>();
  readonly ayahNavigate = output<AyahNavigationTarget>();

  private readonly sectionElement = viewChild<ElementRef<HTMLElement>>('ayahSection');

  // N3 row 10 (Feature 030): loading layout reservation, a local port of the U1
  // pattern in selected-word-section (decision N3-a — extract a shared utility
  // only on a third consumer). A loaded tafsir/translation/إعراب has an arbitrary
  // height, so the only honest predictor of the next one is the last one; without
  // this the section collapses to the skeleton and the page jumps. Only numeric
  // geometry is retained — never prior text or Quran DOM.
  private readonly lastNaturalSize = signal<{ blockSize: number; inlineSize: number } | null>(null);

  protected readonly reservedBlockSize = computed<string | null>(() => {
    const natural = this.lastNaturalSize();
    return this.loadState().isLoading && natural !== null ? `${natural.blockSize}px` : null;
  });

  private readonly isLoadedSuccessfully = computed(() => {
    const state = this.loadState();
    return (
      !state.isLoading &&
      !state.isEmpty &&
      state.errorMessage === null &&
      this.selectedVerseKey() !== null &&
      this.study() !== null
    );
  });

  constructor() {
    if (typeof ResizeObserver !== 'undefined') {
      const observer = new ResizeObserver((entries) => this.onSectionResize(entries));
      this.destroyRef.onDestroy(() => observer.disconnect());
      effect(() => {
        const element = this.sectionElement()?.nativeElement;
        if (element) {
          observer.disconnect();
          observer.observe(element);
        }
      });
    }
  }

  /**
   * Records the loaded natural geometry, and drops a stale reservation when the
   * available inline size changes mid-loading (a wide measurement must never be
   * imposed on a narrower layout, or vice versa).
   */
  private onSectionResize(entries: ResizeObserverEntry[]): void {
    const target = entries[entries.length - 1]?.target;
    if (!target) {
      return;
    }
    const rect = target.getBoundingClientRect();

    if (this.isLoadedSuccessfully()) {
      this.lastNaturalSize.set({ blockSize: rect.height, inlineSize: rect.width });
      return;
    }

    const natural = this.lastNaturalSize();
    if (
      this.loadState().isLoading &&
      natural !== null &&
      Math.abs(natural.inlineSize - rect.width) > RESERVATION_INLINE_SIZE_TOLERANCE_PX
    ) {
      this.lastNaturalSize.set(null);
    }
  }

  protected readonly displayAyahText = computed(() => {
    const text = this.study()?.ayah.textUthmani;
    return text ? toStudyAyahDisplayText(text) : '';
  });

  protected readonly tabLabels = AYAH_STUDY_TAB_LABELS;

  // The summary's counts are what the child cards reserve their loading geometry from, so an
  // absent study must stay `null` ("unknown", fall back) and never collapse into a `0` the cards
  // would read as "known empty" (Feature 030, N3 rows 11-12).
  protected readonly similarAyahCount = computed<number | null>(
    () => this.study()?.similaritySummary.similarAyahCount ?? null,
  );

  protected readonly mutashabihatGroupCount = computed<number | null>(
    () => this.study()?.similaritySummary.mutashabihatGroupCount ?? null,
  );

  protected readonly mutashabihatOccurrenceCount = computed<number | null>(
    () => this.study()?.similaritySummary.mutashabihatOccurrenceCount ?? null,
  );

  protected tabCount(tab: AyahStudyTab): number | null {
    if (this.loadState().isLoading || !this.study()) {
      return null;
    }

    switch (tab) {
      case 'similar-ayahs':
        return this.similarAyahCount();
      case 'mutashabihat':
        return this.mutashabihatGroupCount();
      default:
        return null;
    }
  }

  protected selectedAyahNavigateLabel(): string {
    const ayah = this.study()?.ayah;
    if (!ayah) {
      return 'فتح الآية في المصحف';
    }

    return `فتح ${ayah.surahNameArabic} — ${ayah.ayahNumber} في المصحف`;
  }

  protected onSelectedAyahNavigate(): void {
    const ayah = this.study()?.ayah;
    if (!ayah) {
      return;
    }

    this.ayahNavigate.emit({
      verseKey: ayah.verseKey,
      pageNumber: ayah.pageFrom,
    });
  }
}
