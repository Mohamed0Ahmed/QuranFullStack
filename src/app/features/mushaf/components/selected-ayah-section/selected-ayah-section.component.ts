import { Component, computed, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';

import {
  AyahStudyTab,
  AyahStudyViewModel,
  AYAH_STUDY_TAB_LABELS,
  ResourceLoadState,
  SimilarAyahsDto,
  SourceOption,
} from '../../models/mushaf.models';
import { SourceSelectorComponent } from '../source-selector/source-selector.component';
import { SimilarAyahsCardComponent } from '../similar-ayahs-card/similar-ayahs-card.component';
import { TafsirCardComponent } from '../tafsir-card/tafsir-card.component';
import { TranslationCardComponent } from '../translation-card/translation-card.component';
import { FullI3rabCardComponent } from '../full-i3rab-card/full-i3rab-card.component';
import { toStudyAyahDisplayText } from '../../utils/mushaf-verse-key-display';

@Component({
  selector: 'qd-selected-ayah-section',
  standalone: true,
  imports: [
    CommonModule,
    SourceSelectorComponent,
    SimilarAyahsCardComponent,
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
  readonly study = input<AyahStudyViewModel | null>(null);
  readonly loadState = input.required<ResourceLoadState>();
  readonly similarAyahs = input<SimilarAyahsDto | null>(null);
  readonly similarAyahsLoadState = input<ResourceLoadState>({
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

  protected readonly displayAyahText = computed(() => {
    const text = this.study()?.ayah.textUthmani;
    return text ? toStudyAyahDisplayText(text) : '';
  });

  protected readonly tabLabels = AYAH_STUDY_TAB_LABELS;

  protected readonly similarAyahCount = computed(
    () => this.study()?.similaritySummary.similarAyahCount ?? 0,
  );

  protected readonly mutashabihatGroupCount = computed(
    () => this.study()?.similaritySummary.mutashabihatGroupCount ?? 0,
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
}
