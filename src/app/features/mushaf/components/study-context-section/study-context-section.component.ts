import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';

import {
  AyahStudyTab,
  AyahStudyViewModel,
  ResourceLoadState,
  SimilarAyahsDto,
  AyahMutashabihatDto,
  SourceOption,
  WordAnalysisViewModel,
} from '../../models/mushaf.models';
import { SelectedAyahSectionComponent } from '../selected-ayah-section/selected-ayah-section.component';
import { SelectedWordSectionComponent } from '../selected-word-section/selected-word-section.component';

@Component({
  selector: 'qd-study-context-section',
  standalone: true,
  imports: [CommonModule, SelectedWordSectionComponent, SelectedAyahSectionComponent],
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
  readonly activeAyahTab = input<AyahStudyTab>('tafsir');
  readonly selectedVerseKey = input<string | null>(null);

  readonly tafsirOptions = input<SourceOption[]>([]);
  readonly translationOptions = input<SourceOption[]>([]);
  readonly fullI3rabOptions = input<SourceOption[]>([]);

  readonly ayahTabChange = output<AyahStudyTab>();
  readonly tafsirSourceChange = output<string>();
  readonly translationSourceChange = output<string>();
  readonly fullI3rabSourceChange = output<string>();
}
