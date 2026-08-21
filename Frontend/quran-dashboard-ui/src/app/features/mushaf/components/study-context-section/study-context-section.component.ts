import { Component, computed, input, output } from '@angular/core';

import {
  AYAH_STUDY_TABS_BY_GROUP,
  AyahStudyGroup,
  AyahNavigationTarget,
  AyahStudyTab,
  AyahStudyViewModel,
  PanelMode,
  ResourceLoadState,
  SimilarAyahsDto,
  AyahMutashabihatDto,
  SourceOption,
  WordAnalysisViewModel,
} from '../../models/mushaf.models';
import { QdTabDirective } from '../../../../shared/ui/tabs/tab.directive';
import { QdTabsComponent } from '../../../../shared/ui/tabs/tabs.component';
import { SelectedAyahSectionComponent } from '../selected-ayah-section/selected-ayah-section.component';
import { SelectedWordSectionComponent } from '../selected-word-section/selected-word-section.component';
import { MushafDoorsPanelComponent } from '../mushaf-doors-panel/mushaf-doors-panel.component';

export type StudyContextTab = 'analysis' | 'doors' | AyahStudyGroup;

interface StudyContextTabDefinition {
  readonly key: StudyContextTab;
  readonly label: string;
  readonly disabled: boolean;
  readonly testId: string;
}

const STUDY_CONTEXT_TABS: readonly StudyContextTabDefinition[] = [
  { key: 'analysis', label: 'التحليل', disabled: false, testId: 'study-context-tab-analysis' },
  { key: 'doors', label: 'الأبواب', disabled: false, testId: 'study-context-tab-doors' },
  {
    key: 'sources',
    label: 'التفاسير والترجمات',
    disabled: false,
    testId: 'study-context-tab-sources',
  },
  {
    key: 'similarity',
    label: 'المتشابهات',
    disabled: false,
    testId: 'study-context-tab-similarity',
  },
];

let nextStudyContextInstance = 0;

@Component({
  selector: 'qd-study-context-section',
  standalone: true,
  imports: [
    QdTabsComponent,
    QdTabDirective,
    MushafDoorsPanelComponent,
    SelectedWordSectionComponent,
    SelectedAyahSectionComponent,
  ],
  templateUrl: './study-context-section.component.html',
  styleUrls: ['./study-context-section.component.scss'],
})
export class StudyContextSectionComponent {
  readonly wordAnalysis = input<WordAnalysisViewModel | null>(null);
  readonly wordLoadState = input.required<ResourceLoadState>();
  readonly selectedWordLocation = input<string | null>(null);

  readonly ayahStudy = input<AyahStudyViewModel | null>(null);
  readonly ayahLoadState = input.required<ResourceLoadState>();
  readonly similarAyahs = input<SimilarAyahsDto | null>(null);
  readonly similarAyahsLoadState = input.required<ResourceLoadState>();
  readonly mutashabihat = input<AyahMutashabihatDto | null>(null);
  readonly mutashabihatLoadState = input.required<ResourceLoadState>();
  readonly activePanel = input<PanelMode>('none');
  readonly activeAyahTab = input<AyahStudyTab>('tafsir');
  readonly selectedVerseKey = input<string | null>(null);

  readonly tafsirOptions = input<SourceOption[]>([]);
  readonly translationOptions = input<SourceOption[]>([]);
  readonly fullI3rabOptions = input<SourceOption[]>([]);

  readonly contextTabChange = output<StudyContextTab>();
  readonly ayahTabChange = output<AyahStudyTab>();
  readonly tafsirSourceChange = output<string>();
  readonly translationSourceChange = output<string>();
  readonly fullI3rabSourceChange = output<string>();
  readonly ayahNavigate = output<AyahNavigationTarget>();

  protected readonly tabs = STUDY_CONTEXT_TABS;
  private readonly instanceId = `qd-study-context-${nextStudyContextInstance++}`;

  protected readonly activeContextTab = computed<StudyContextTab>(() => {
    if (this.activePanel() === 'doors') {
      return 'doors';
    }

    if (this.activePanel() !== 'ayah') {
      return 'analysis';
    }

    return AYAH_STUDY_TABS_BY_GROUP.similarity.some(
      (tab) => tab === this.activeAyahTab(),
    )
      ? 'similarity'
      : 'sources';
  });

  protected readonly activeAyahGroup = computed<AyahStudyGroup>(() =>
    this.activeContextTab() === 'similarity' ? 'similarity' : 'sources',
  );

  protected readonly relatedStudyCount = computed<number | null>(() => {
    if (this.ayahLoadState().isLoading || !this.ayahStudy()) {
      return null;
    }

    const summary = this.ayahStudy()!.similaritySummary;
    return summary.similarAyahCount + summary.mutashabihatGroupCount;
  });

  protected relatedStudyCountLabel(): string {
    const count = this.relatedStudyCount();
    return count === null ? '' : `${count}`;
  }

  protected tabElementId(tab: StudyContextTab): string {
    return `${this.instanceId}-tab-${tab}`;
  }

  protected panelElementId(tab: StudyContextTab): string {
    return `${this.instanceId}-panel-${tab}`;
  }

  protected selectContextTab(tab: StudyContextTabDefinition): void {
    if (tab.disabled || tab.key === this.activeContextTab()) {
      return;
    }

    this.contextTabChange.emit(tab.key);
  }
}
