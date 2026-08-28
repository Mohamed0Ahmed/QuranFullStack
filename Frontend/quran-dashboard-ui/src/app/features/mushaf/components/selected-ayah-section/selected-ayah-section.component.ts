import { Component, ElementRef, computed, input, output, viewChild } from '@angular/core';

import { qdLoadingSizeReservation } from '../../../../shared/layout/loading-size-reservation';
import { QdEmptyStateComponent } from '../../../../shared/ui/empty-state/empty-state.component';
import { QdErrorStateComponent } from '../../../../shared/ui/error-state/error-state.component';
import { QdTabDirective } from '../../../../shared/ui/tabs/tab.directive';
import { QdTabsComponent } from '../../../../shared/ui/tabs/tabs.component';
import {
  AYAH_STUDY_TABS_BY_GROUP,
  AyahNavigationTarget,
  AyahStudyGroup,
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

interface AyahStudyTabDefinition {
  readonly key: AyahStudyTab;
  readonly testId: string | null;
  readonly countTestId: string | null;
}

const AYAH_STUDY_TAB_TEST_IDS: Partial<Record<AyahStudyTab, string>> = {
  'similar-ayahs': 'ayah-tab-similar-ayahs',
  mutashabihat: 'ayah-tab-mutashabihat',
};

const AYAH_STUDY_TAB_COUNT_TEST_IDS: Partial<Record<AyahStudyTab, string>> = {
  'similar-ayahs': 'similar-ayah-count',
  mutashabihat: 'mutashabihat-group-count',
};

let nextAyahStudyInstance = 0;

@Component({
  selector: 'qd-selected-ayah-section',
  standalone: true,
  imports: [
    QdEmptyStateComponent,
    QdErrorStateComponent,
    QdTabsComponent,
    QdTabDirective,
    SourceSelectorComponent,
    SimilarAyahsCardComponent,
    MutashabihatGroupsCardComponent,
    TafsirCardComponent,
    TranslationCardComponent,
    FullI3rabCardComponent,
  ],
  templateUrl: './selected-ayah-section.component.html',
  styleUrls: [
    './selected-ayah-section.component.scss',
    './selected-ayah-section.states.scss',
  ],
  host: {
    '[class.qd-selected-ayah-section--embedded]': 'embedded()',
  },
})
export class SelectedAyahSectionComponent {
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
  readonly group = input<AyahStudyGroup>('sources');
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

  protected readonly reservedBlockSize = qdLoadingSizeReservation({
    host: this.sectionElement,
    isLoading: computed(() => this.loadState().isLoading),
    isSettled: this.isLoadedSuccessfully,
  }).reservedBlockSize;

  protected readonly displayAyahText = computed(() => {
    const text = this.study()?.ayah.textUthmani;
    return text ? toStudyAyahDisplayText(text) : '';
  });

  protected readonly tabLabels = AYAH_STUDY_TAB_LABELS;
  protected readonly tabs = computed<readonly AyahStudyTabDefinition[]>(() =>
    AYAH_STUDY_TABS_BY_GROUP[this.group()].map((key) => ({
      key,
      testId: AYAH_STUDY_TAB_TEST_IDS[key] ?? null,
      countTestId: AYAH_STUDY_TAB_COUNT_TEST_IDS[key] ?? null,
    })),
  );
  protected readonly tabsAriaLabel = computed(() =>
    this.group() === 'sources'
      ? 'تبويبات التفاسير والترجمات والإعراب'
      : 'تبويبات المتشابهات والآيات القريبة',
  );

  private readonly instanceId = `qd-ayah-study-${nextAyahStudyInstance++}`;

  protected tabElementId(tab: AyahStudyTab): string {
    return `${this.instanceId}-tab-${tab}`;
  }

  protected panelElementId(tab: AyahStudyTab): string {
    return `${this.instanceId}-panel-${tab}`;
  }

  protected readonly similarAyahCount = computed<number | null>(
    () => this.study()?.similaritySummary.similarAyahCount ?? null,
  );

  protected readonly mutashabihatGroupCount = computed<number | null>(
    () => this.study()?.similaritySummary.mutashabihatGroupCount ?? null,
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

  protected tabCountLabel(tab: AyahStudyTab): string {
    const count = this.tabCount(tab);
    return count === null ? '' : `${count}`;
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
