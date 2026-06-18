import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';

import {
  AyahStudyTab,
  AyahStudyViewModel,
  ResourceLoadState,
  SourceOption,
} from '../../models/mushaf.models';
import { SourceSelectorComponent } from '../source-selector/source-selector.component';
import { TafsirCardComponent } from '../tafsir-card/tafsir-card.component';
import { TranslationCardComponent } from '../translation-card/translation-card.component';
import { FullI3rabCardComponent } from '../full-i3rab-card/full-i3rab-card.component';

@Component({
  selector: 'qd-selected-ayah-section',
  standalone: true,
  imports: [
    CommonModule,
    SourceSelectorComponent,
    TafsirCardComponent,
    TranslationCardComponent,
    FullI3rabCardComponent,
  ],
  templateUrl: './selected-ayah-section.component.html',
  styleUrls: ['./selected-ayah-section.component.scss'],
})
export class SelectedAyahSectionComponent {
  readonly study = input<AyahStudyViewModel | null>(null);
  readonly loadState = input.required<ResourceLoadState>();
  readonly activeTab = input<AyahStudyTab>('tafsir');
  readonly selectedVerseKey = input<string | null>(null);

  readonly tafsirOptions = input<SourceOption[]>([]);
  readonly translationOptions = input<SourceOption[]>([]);
  readonly fullI3rabOptions = input<SourceOption[]>([]);

  readonly tabChange = output<AyahStudyTab>();
  readonly tafsirSourceChange = output<string>();
  readonly translationSourceChange = output<string>();
  readonly fullI3rabSourceChange = output<string>();
}
